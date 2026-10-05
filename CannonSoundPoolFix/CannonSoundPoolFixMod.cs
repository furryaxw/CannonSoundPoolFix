using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameEffect = Sprocket.Effect;
using MuzzleFlashEffect = Sprocket.Vehicles.Fires.MuzzleFlashEffect;

[assembly: AssemblyMetadata("Sprocket.Mod.Id", "furryaxw.cannon-sound-pool-fix")]
[assembly: AssemblyMetadata("Sprocket.Mod.DisplayName", "Cannon Sound Pool Fix")]
[assembly: AssemblyMetadata("Sprocket.Mod.Description", "Prevents rapid-fire cannons from exhausting Sprocket's audio pool while preserving muzzle effects.")]
[assembly: AssemblyMetadata("Sprocket.Mod.Authors", "furryAxw")]
[assembly: AssemblyMetadata("Sprocket.Mod.Repository", "furryaxw/CannonSoundPoolFix")]
[assembly: AssemblyMetadata("Sprocket.Mod.Category", "audio")]
[assembly: AssemblyMetadata("Sprocket.Mod.License", "GPL-3.0-only")]

namespace CannonSoundPoolFix
{
    [BepInPlugin(PluginGuid, "Cannon Sound Pool Fix", "2.0.0")]
    public sealed class CannonSoundPoolFixMain : BasePlugin
    {
        internal const string PluginGuid = "furryaxw.cannon-sound-pool-fix";

        private const string EffectObjectName =
            "CannonMuzzleFlashEffect(Clone)";
        private const int MaximumEffectAncestorDepth = 4;

        private readonly Dictionary<int, PlayingEffectRecord>
            playingEffects = new();
        private readonly List<ActiveSfxSnapshot> activeSnapshots = new();
        private readonly HashSet<int> sourcesToStop = new();
        private readonly List<int> staleEffectIds = new();
        private readonly Dictionary<int, PrototypeLimitRecord>
            prototypeLimits = new();
        private readonly HashSet<string> loggedFailures =
            new(StringComparer.Ordinal);

        private long nextEffectSequence;
        private bool loggedFirstAudioStop;

        internal static CannonSoundPoolFixMain? Instance
        {
            get;
            private set;
        }

        public override void Load()
        {
            Instance = this;
            AddComponent<ActiveSceneWatcher>().Configure(OnActiveSceneChanged);
            Harmony.CreateAndPatchAll(typeof(CannonSoundPoolFixMain).Assembly, PluginGuid);
            Log.LogInfo(
                "[CSPF] Enabled. Keeps the latest " +
                $"{SoundPoolRetentionPolicy.MaxPlayingEffectsPerArea} " +
                "cannon sounds per 1 x 1 x 1 area and caps each " +
                "muzzle-effect prototype at " +
                $"{EffectPoolPolicy.DefaultMuzzleEffectLimit}. " +
                "SFX objects and VFX remain enabled.");
        }

        public override bool Unload()
        {
            RestorePrototypeLimits();
            ClearSoundState();
            loggedFailures.Clear();
            Instance = null;
            return true;
        }

        private void OnActiveSceneChanged()
        {
            RestorePrototypeLimits();
            ClearSoundState();
        }

        internal void RegisterMuzzleEffect(MuzzleFlashEffect effect)
        {
            try
            {
                if (effect == null || effect.transform == null)
                    return;

                ConfigurePrototypeLimit(effect);

                Transform? effectRoot = FindEffectRoot(effect.transform);
                if (effectRoot == null || effectRoot.gameObject == null)
                {
                    LogFailure(
                        "effect-root",
                        $"'{EffectObjectName}' ancestor was not found");
                    return;
                }

                PruneInactiveEffects();

                int instanceId = effectRoot.gameObject.GetInstanceID();
                playingEffects[instanceId] = new PlayingEffectRecord(
                    effectRoot,
                    effect.audioSource,
                    effect.longRangeAudioSource,
                    ++nextEffectSequence);
                StopExcessAudioSources();
            }
            catch (Exception exception)
            {
                LogFailure("register", exception.ToString());
            }
        }

        private void StopExcessAudioSources()
        {
            activeSnapshots.Clear();
            foreach (PlayingEffectRecord effect in playingEffects.Values)
            {
                if (!IsActive(effect.Root))
                    continue;

                Vector3 position = effect.Root.position;
                activeSnapshots.Add(new ActiveSfxSnapshot(
                    effect.InstanceId,
                    effect.Sequence,
                    position.x,
                    position.y,
                    position.z));
            }

            SoundPoolRetentionPolicy.SelectSourcesToStop(
                activeSnapshots,
                sourcesToStop);

            foreach (int instanceId in sourcesToStop)
            {
                if (!playingEffects.TryGetValue(
                        instanceId,
                        out PlayingEffectRecord? effect))
                {
                    continue;
                }

                try
                {
                    bool stoppedPlayingSource =
                        StopAudioSource(effect.AudioSource);
                    stoppedPlayingSource |=
                        StopAudioSource(effect.LongRangeAudioSource);
                    if (stoppedPlayingSource && !loggedFirstAudioStop)
                    {
                        loggedFirstAudioStop = true;
                        Log.LogInfo(
                            "[CSPF] Cannon voice limit engaged: stopped the " +
                            "oldest playing cannon AudioSource pair.");
                    }
                }
                catch (Exception exception)
                {
                    LogFailure(
                        $"stop-audio-{instanceId}",
                        exception.ToString());
                }

                playingEffects.Remove(instanceId);
            }
        }

        private static bool StopAudioSource(AudioSource? source)
        {
            if (source == null || source.gameObject == null)
                return false;

            bool wasPlaying = source.isPlaying;
            source.Stop();
            return wasPlaying;
        }

        private void ConfigurePrototypeLimit(MuzzleFlashEffect effect)
        {
            GameEffect? prototype = effect.prototype;
            if (prototype == null)
            {
                LogFailure(
                    "prototype",
                    "MuzzleFlashEffect has no prototype");
                return;
            }

            int prototypeId = prototype.GetInstanceID();
            if (!prototypeLimits.TryGetValue(
                    prototypeId,
                    out PrototypeLimitRecord? record))
            {
                int originalLimit = prototype.MaxInstanceCount;
                int configuredLimit =
                    EffectPoolPolicy.ResolveLimit(originalLimit);
                record = new PrototypeLimitRecord(
                    prototype,
                    originalLimit,
                    configuredLimit);
                prototypeLimits.Add(prototypeId, record);

                if (prototype.MaxInstanceCount != configuredLimit)
                    prototype.MaxInstanceCount = configuredLimit;

                Log.LogInfo(
                    "[CSPF] Configured cannon muzzle-effect prototype " +
                    $"id={prototypeId},originalLimit={originalLimit}," +
                    $"effectiveLimit={configuredLimit}.");
                return;
            }

            int effectiveLimit = record.EffectiveLimit;
            if (prototype.MaxInstanceCount != effectiveLimit)
                prototype.MaxInstanceCount = effectiveLimit;
        }

        private void PruneInactiveEffects()
        {
            staleEffectIds.Clear();
            foreach (var pair in playingEffects)
            {
                if (!IsActive(pair.Value.Root))
                    staleEffectIds.Add(pair.Key);
            }

            foreach (int instanceId in staleEffectIds)
                playingEffects.Remove(instanceId);
        }

        private static Transform? FindEffectRoot(Transform start)
        {
            Transform? current = start;
            for (int depth = 0;
                 depth <= MaximumEffectAncestorDepth && current != null;
                 depth++)
            {
                GameObject? gameObject = current.gameObject;
                if (gameObject != null &&
                    string.Equals(
                        gameObject.name,
                        EffectObjectName,
                        StringComparison.Ordinal))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private void RestorePrototypeLimits()
        {
            foreach (PrototypeLimitRecord record in prototypeLimits.Values)
            {
                try
                {
                    GameEffect prototype = record.Prototype;
                    if (prototype != null)
                        prototype.MaxInstanceCount = record.OriginalLimit;
                }
                catch (Exception exception)
                {
                    LogFailure("restore", exception.ToString());
                }
            }

            prototypeLimits.Clear();
        }

        private void ClearSoundState()
        {
            playingEffects.Clear();
            activeSnapshots.Clear();
            sourcesToStop.Clear();
            staleEffectIds.Clear();
            nextEffectSequence = 0;
            loggedFirstAudioStop = false;
        }

        private void LogFailure(string category, string failure)
        {
            string key = $"{category}|{failure}";
            if (!loggedFailures.Add(key))
                return;

            Log.LogError(
                $"[CSPF] category={category},error={failure}");
        }

        private static bool IsActive(Transform? transform)
        {
            return transform != null &&
                   transform.gameObject != null &&
                   transform.gameObject.activeInHierarchy;
        }

        private sealed class PlayingEffectRecord
        {
            public PlayingEffectRecord(
                Transform root,
                AudioSource? audioSource,
                AudioSource? longRangeAudioSource,
                long sequence)
            {
                Root = root;
                AudioSource = audioSource;
                LongRangeAudioSource = longRangeAudioSource;
                InstanceId = root.gameObject.GetInstanceID();
                Sequence = sequence;
            }

            public Transform Root { get; }
            public AudioSource? AudioSource { get; }
            public AudioSource? LongRangeAudioSource { get; }
            public int InstanceId { get; }
            public long Sequence { get; }
        }

        private sealed class PrototypeLimitRecord
        {
            public PrototypeLimitRecord(
                GameEffect prototype,
                int originalLimit,
                int effectiveLimit)
            {
                Prototype = prototype;
                OriginalLimit = originalLimit;
                EffectiveLimit = effectiveLimit;
            }

            public GameEffect Prototype { get; }
            public int OriginalLimit { get; }
            public int EffectiveLimit { get; }
        }
    }

    // BepInEx 没有场景回调；这个注入组件逐帧比较活动场景句柄，变化时通知宿主。
    internal sealed class ActiveSceneWatcher : MonoBehaviour
    {
        private Action? onSceneChanged;
        private int lastHandle = int.MinValue;

        public ActiveSceneWatcher(IntPtr ptr) : base(ptr)
        {
        }

        // 带托管参数的成员注册不进 il2cpp 域，只从托管侧调用。
        [HideFromIl2Cpp]
        public void Configure(Action callback) => onSceneChanged = callback;

        private void Update()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.handle == lastHandle)
                return;

            lastHandle = active.handle;
            onSceneChanged?.Invoke();
        }
    }

    [HarmonyPatch(typeof(MuzzleFlashEffect), nameof(MuzzleFlashEffect.Setup))]
    internal static class MuzzleFlashSoundPoolPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MuzzleFlashEffect __instance)
        {
            CannonSoundPoolFixMain.Instance?.RegisterMuzzleEffect(__instance);
        }
    }
}
