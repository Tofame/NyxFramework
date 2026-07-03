namespace NyxAssets.Things;

/// <summary>Minimum / maximum duration for one animation frame (client .dat).</summary>
public readonly struct AnimationFrameTiming
{
    public AnimationFrameTiming(uint minimumMilliseconds, uint maximumMilliseconds)
    {
        MinimumMilliseconds = minimumMilliseconds;
        MaximumMilliseconds = maximumMilliseconds;
    }

    public uint MinimumMilliseconds { get; }
    public uint MaximumMilliseconds { get; }
}
