using System;
using System.Collections.Generic;

namespace CannonSoundPoolFix
{
    internal static class SoundPoolRetentionPolicy
    {
        internal const float SuppressionBoxSize = 1.0f;
        internal const int MaxPlayingEffectsPerArea = 5;

        internal static void SelectSourcesToStop(
            IReadOnlyList<ActiveSfxSnapshot> activeEffects,
            HashSet<int> sourcesToStop)
        {
            sourcesToStop.Clear();
            if (activeEffects.Count <= MaxPlayingEffectsPerArea)
                return;

            float halfBoxSize = SuppressionBoxSize * 0.5f;
            var nearbyEffects = new List<ActiveSfxSnapshot>();
            var newestEffects = new List<ActiveSfxSnapshot>(
                MaxPlayingEffectsPerArea);
            var retainedIds = new HashSet<int>();

            for (int i = 0; i < activeEffects.Count; i++)
            {
                ActiveSfxSnapshot anchor = activeEffects[i];
                if (sourcesToStop.Contains(anchor.InstanceId))
                    continue;

                nearbyEffects.Clear();
                for (int j = 0; j < activeEffects.Count; j++)
                {
                    ActiveSfxSnapshot candidate = activeEffects[j];
                    if (sourcesToStop.Contains(candidate.InstanceId))
                        continue;

                    if (MathF.Abs(anchor.X - candidate.X) > halfBoxSize ||
                        MathF.Abs(anchor.Y - candidate.Y) > halfBoxSize ||
                        MathF.Abs(anchor.Z - candidate.Z) > halfBoxSize)
                    {
                        continue;
                    }

                    nearbyEffects.Add(candidate);
                }

                if (nearbyEffects.Count <= MaxPlayingEffectsPerArea)
                    continue;

                retainedIds.Clear();
                newestEffects.Clear();
                foreach (ActiveSfxSnapshot effect in nearbyEffects)
                    InsertNewest(effect, newestEffects);

                foreach (ActiveSfxSnapshot effect in newestEffects)
                    retainedIds.Add(effect.InstanceId);

                foreach (ActiveSfxSnapshot effect in nearbyEffects)
                {
                    if (!retainedIds.Contains(effect.InstanceId))
                        sourcesToStop.Add(effect.InstanceId);
                }
            }
        }

        private static void InsertNewest(
            ActiveSfxSnapshot effect,
            List<ActiveSfxSnapshot> newestEffects)
        {
            int index = 0;
            while (index < newestEffects.Count &&
                   newestEffects[index].Sequence > effect.Sequence)
            {
                index++;
            }

            newestEffects.Insert(index, effect);
            if (newestEffects.Count > MaxPlayingEffectsPerArea)
                newestEffects.RemoveAt(newestEffects.Count - 1);
        }
    }

    internal readonly struct ActiveSfxSnapshot
    {
        public ActiveSfxSnapshot(
            int instanceId,
            long sequence,
            float x,
            float y,
            float z)
        {
            InstanceId = instanceId;
            Sequence = sequence;
            X = x;
            Y = y;
            Z = z;
        }

        public int InstanceId { get; }
        public long Sequence { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
}
