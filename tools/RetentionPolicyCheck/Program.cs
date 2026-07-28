using System;
using System.Collections.Generic;
using CannonSoundPoolFix;

AssertSameAreaRetention(5, Array.Empty<int>());
AssertSameAreaRetention(6, new[] { 1 });
AssertSameAreaRetention(8, new[] { 1, 2, 3 });
AssertIndependentAreas();
AssertLimit(-1, EffectPoolPolicy.DefaultMuzzleEffectLimit);
AssertLimit(0, EffectPoolPolicy.DefaultMuzzleEffectLimit);
AssertLimit(1, 1);
AssertLimit(64, 64);
AssertLimit(EffectPoolPolicy.DefaultMuzzleEffectLimit,
    EffectPoolPolicy.DefaultMuzzleEffectLimit);
AssertLimit(EffectPoolPolicy.DefaultMuzzleEffectLimit + 1,
    EffectPoolPolicy.DefaultMuzzleEffectLimit);

Console.WriteLine(
    "CannonSoundPoolFix sound/effect-pool policies passed: 10 cases");
return 0;

static void AssertSameAreaRetention(int count, int[] expectedStoppedIds)
{
    var effects = new List<ActiveSfxSnapshot>();
    for (int id = 1; id <= count; id++)
        effects.Add(new ActiveSfxSnapshot(id, id, 0.0f, 0.0f, 0.0f));

    var actual = new HashSet<int>();
    SoundPoolRetentionPolicy.SelectSourcesToStop(effects, actual);
    AssertSet(actual, expectedStoppedIds, $"same-area count={count}");
}

static void AssertIndependentAreas()
{
    var effects = new List<ActiveSfxSnapshot>();
    for (int id = 1; id <= 6; id++)
        effects.Add(new ActiveSfxSnapshot(id, id, 0.0f, 0.0f, 0.0f));

    for (int id = 7; id <= 12; id++)
        effects.Add(new ActiveSfxSnapshot(id, id, 2.0f, 0.0f, 0.0f));

    var actual = new HashSet<int>();
    SoundPoolRetentionPolicy.SelectSourcesToStop(effects, actual);
    AssertSet(actual, new[] { 1, 7 }, "independent areas");
}

static void AssertSet(
    HashSet<int> actual,
    IReadOnlyCollection<int> expected,
    string scenario)
{
    if (actual.Count != expected.Count)
    {
        throw new InvalidOperationException(
            $"{scenario}: expected {expected.Count} stopped sources, " +
            $"actual {actual.Count} [{string.Join(",", actual)}]");
    }

    foreach (int expectedId in expected)
    {
        if (!actual.Contains(expectedId))
        {
            throw new InvalidOperationException(
                $"{scenario}: missing stopped source {expectedId}; " +
                $"actual [{string.Join(",", actual)}]");
        }
    }
}

static void AssertLimit(int currentLimit, int expectedLimit)
{
    int actual = EffectPoolPolicy.ResolveLimit(currentLimit);
    if (actual != expectedLimit)
    {
        throw new InvalidOperationException(
            $"current={currentLimit}: expected {expectedLimit}, actual {actual}");
    }
}
