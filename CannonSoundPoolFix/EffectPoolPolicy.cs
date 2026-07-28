namespace CannonSoundPoolFix
{
    internal static class EffectPoolPolicy
    {
        internal const int DefaultMuzzleEffectLimit = 192;

        internal static int ResolveLimit(int currentLimit)
        {
            if (currentLimit < 1 ||
                currentLimit > DefaultMuzzleEffectLimit)
            {
                return DefaultMuzzleEffectLimit;
            }

            return currentLimit;
        }
    }
}
