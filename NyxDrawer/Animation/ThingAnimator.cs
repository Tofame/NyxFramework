using NyxAssets.Things;
using NyxAssets.Things.Frames;

namespace NyxDrawer.Animation;

/// <summary>Frame index helpers for Nyx thing animation (effects, items, walking).</summary>
public sealed class ThingAnimator
{
    /// <summary>Default ms per effect frame when not using enhanced animator data (NyxClient <c>EFFECT_TICKS_PER_FRAME</c>).</summary>
    public const int DefaultEffectTicksPerFrame = 75;

    /// <summary>NyxClient effect phase from elapsed time (simplified; no per-id hacks).</summary>
    public uint GetEffectFrame(ThingType effect, float elapsedMs, int ticksPerFrame = DefaultEffectTicksPerFrame)
    {
        if (effect.FrameGroups.Count == 0)
            return 0;
        var frameGroup = effect.FrameGroups[0];
        if (frameGroup.Frames <= 1)
            return 0;
        var ticks = Math.Max(1, ticksPerFrame);
        var phase = (int)(elapsedMs / ticks);
        return (uint)Math.Clamp(phase, 0, (int)frameGroup.Frames - 1);
    }

    /// <summary>Cyclic animation for <c>AnimateAlways</c> items/effects.</summary>
    public uint GetCyclicFrame(ThingType thing, float elapsedMs, int ticksPerFrame = 333)
    {
        if (thing.FrameGroups.Count == 0)
            return 0;
        var frameGroup = thing.FrameGroups[0];
        if (frameGroup.Frames <= 1)
            return 0;
        var ticks = Math.Max(1, ticksPerFrame);
        return (uint)((elapsedMs / ticks) % frameGroup.Frames);
    }

    /// <summary>Resolves walking frame group + frame index (NyxClient outfit idle/walk groups).</summary>
    public static void ResolveWalkingFrame(ThingType outfit, uint walkPhase, out ThingFrameGroup frameGroup, out uint frame)
    {
        ThingFrameResolver.ResolveWalkingFrame(outfit, walkPhase, out frameGroup, out frame, out _);
    }
}
