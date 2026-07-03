namespace NyxAssets.Things.Frames;

/// <summary>
/// High-level frame/sprite slice resolution for outfits, items, effects, and missiles.
/// Mirrors NyxClient / NyxDrawer conventions so asset tools can decode sprites without GPU code.
/// </summary>
public static class ThingFrameResolver
{
    /// <summary>Normalizes a direction index into 0 … <paramref name="patternCount"/>−1.</summary>
    public static uint NormalizeDirection(int direction, uint patternCount)
    {
        if (patternCount == 0)
            return 0;
        var mod = direction % (int)patternCount;
        if (mod < 0)
            mod += (int)patternCount;
        return (uint)mod;
    }

    /// <summary>Positive modulo for tile coordinates (effects).</summary>
    public static uint PositiveMod(int value, uint count)
    {
        if (count == 0)
            return 0;
        var mod = value % (int)count;
        if (mod < 0)
            mod += (int)count;
        return (uint)mod;
    }

    /// <summary>Pattern Y=0 is always visible; Y≥1 requires the matching addon bit in <paramref name="addonMask"/>.</summary>
    public static bool IsAddonPatternVisible(int patternY, byte addonMask) =>
        patternY <= 0 || (addonMask & (1 << (patternY - 1))) != 0;

    /// <summary>NyxClient <c>zPattern = mount ? min(1, numPatternZ - 1) : 0</c>.</summary>
    public static uint GetMountedPatternZ(ThingFrameGroup frameGroup, bool mounted) =>
        mounted && frameGroup.PatternZ > 1 ? Math.Min(1u, frameGroup.PatternZ - 1) : 0u;

    /// <summary>Resolves idle vs walking frame group and animation frame (NyxClient outfit groups).</summary>
    public static void ResolveWalkingFrame(ThingType outfit, uint walkPhase, out ThingFrameGroup frameGroup, out uint frame, out int frameGroupIndex)
    {
        if (outfit.FrameGroups.Count == 0)
            throw new InvalidOperationException("Outfit has no frame groups.");

        var walking = walkPhase > 0;
        if (outfit.FrameGroups.Count > 1 && walking)
        {
            frameGroupIndex = 1;
            frameGroup = outfit.FrameGroups[1];
            frame = frameGroup.Frames > 0 ? (walkPhase - 1) % frameGroup.Frames : 0u;
            return;
        }

        frameGroupIndex = 0;
        frameGroup = outfit.FrameGroups[0];
        if (!walking)
            frame = 0;
        else if (frameGroup.Frames <= 1)
            frame = 0;
        else
            frame = (uint)Math.Clamp((int)walkPhase, 1, (int)frameGroup.Frames - 1);
    }

    /// <summary>Resolves one outfit slice (direction, walk phase, mounted pattern Z).</summary>
    public static ThingFrameSelection GetOutfitFrame(ThingType outfit, OutfitFrameRequest request = default)
    {
        if (outfit.Kind != ThingKind.Outfit)
            throw new ArgumentException($"Expected outfit, got {outfit.Kind}.", nameof(outfit));
        if (outfit.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Outfit {outfit.Id} has no frame groups.");

        ThingFrameGroup frameGroup;
        uint frame;
        int frameGroupIndex;

        if (request.FrameGroupIndex >= 0)
        {
            frameGroupIndex = request.FrameGroupIndex;
            frameGroup = outfit.GetFrameGroup(frameGroupIndex)
                ?? throw new ArgumentOutOfRangeException(nameof(request), "Frame group missing.");
            frame = frameGroup.Frames > 0 ? request.WalkPhase % frameGroup.Frames : 0u;
        }
        else
        {
            ResolveWalkingFrame(outfit, request.WalkPhase, out frameGroup, out frame, out frameGroupIndex);
        }

        return new ThingFrameSelection
        {
            FrameGroup = frameGroup,
            FrameGroupIndex = frameGroupIndex,
            PatternX = NormalizeDirection(request.Direction, frameGroup.PatternX),
            PatternY = 0,
            PatternZ = GetMountedPatternZ(frameGroup, request.Mounted),
            Frame = frame,
        };
    }

    /// <summary>
    /// Yields one <see cref="ThingFrameSelection"/> per visible addon pattern row (base + enabled addons).
    /// Use this to preview or export each outfit layer separately (matches NyxDrawer creature draw order).
    /// </summary>
    public static IEnumerable<ThingFrameSelection> EnumerateOutfitAddonFrames(ThingType outfit, OutfitFrameRequest request = default)
    {
        var baseSelection = GetOutfitFrame(outfit, request);
        var fg = baseSelection.FrameGroup;

        for (var patternY = 0; patternY < (int)fg.PatternY; patternY++)
        {
            if (!IsAddonPatternVisible(patternY, request.AddonMask))
                continue;

            yield return baseSelection with { PatternY = (uint)patternY };
        }
    }

    /// <summary>Resolves mount frame group + direction for the same walk phase as the rider.</summary>
    public static ThingFrameSelection GetMountFrame(ThingType mount, OutfitFrameRequest request = default)
    {
        if (mount.Kind != ThingKind.Outfit)
            throw new ArgumentException($"Mounts use outfit frame layout; got {mount.Kind}.", nameof(mount));
        if (mount.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Mount {mount.Id} has no frame groups.");

        ResolveWalkingFrame(mount, request.WalkPhase, out var frameGroup, out var frame, out var frameGroupIndex);

        return new ThingFrameSelection
        {
            FrameGroup = frameGroup,
            FrameGroupIndex = frameGroupIndex,
            PatternX = NormalizeDirection(request.Direction, frameGroup.PatternX),
            PatternY = 0,
            PatternZ = 0,
            Frame = frame,
        };
    }

    /// <summary>
    /// Yields mount + each visible rider addon slice. Mount is emitted first when <paramref name="mount"/> is non-null.
    /// </summary>
    public static IEnumerable<(ThingType Thing, ThingFrameSelection Selection, bool IsMount)> EnumerateMountedOutfitFrames(
        ThingType outfit,
        ThingType? mount,
        OutfitFrameRequest request = default)
    {
        if (mount is { FrameGroups.Count: > 0 })
        {
            foreach (var patternY in EnumerateMountPatternRows(mount, request))
                yield return (mount, patternY, IsMount: true);
        }

        foreach (var riderSlice in EnumerateOutfitAddonFrames(outfit, request))
            yield return (outfit, riderSlice, IsMount: false);
    }

    private static IEnumerable<ThingFrameSelection> EnumerateMountPatternRows(ThingType mount, OutfitFrameRequest request)
    {
        var baseSelection = GetMountFrame(mount, request);
        var fg = baseSelection.FrameGroup;
        for (var patternY = 0; patternY < (int)fg.PatternY; patternY++)
            yield return baseSelection with { PatternY = (uint)patternY };
    }

    /// <summary>Resolves item patterns (stack pile grid or explicit overrides).</summary>
    public static ThingFrameSelection GetItemFrame(ThingType item, ItemFrameRequest request = default)
    {
        if (item.Kind != ThingKind.Item)
            throw new ArgumentException($"Expected item, got {item.Kind}.", nameof(item));
        if (item.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Item {item.Id} has no frame groups.");

        var frameGroup = item.FrameGroups[0];
        var frame = frameGroup.Frames > 0 ? request.Frame % frameGroup.Frames : 0u;

        uint patternX;
        uint patternY;
        if (request.PatternX.HasValue || request.PatternY.HasValue)
        {
            patternX = request.PatternX ?? 0;
            patternY = request.PatternY ?? 0;
        }
        else
        {
            ItemStackPatterns.Resolve(frameGroup, item.Stackable, request.StackCount, out patternX, out patternY);
        }

        return new ThingFrameSelection
        {
            FrameGroup = frameGroup,
            FrameGroupIndex = 0,
            PatternX = patternX,
            PatternY = patternY,
            PatternZ = request.PatternZ,
            Frame = frame,
        };
    }

    /// <summary>Resolves effect animation frame and tile-based pattern variation.</summary>
    public static ThingFrameSelection GetEffectFrame(ThingType effect, EffectFrameRequest request = default)
    {
        if (effect.Kind != ThingKind.Effect)
            throw new ArgumentException($"Expected effect, got {effect.Kind}.", nameof(effect));
        if (effect.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Effect {effect.Id} has no frame groups.");

        var frameGroup = effect.FrameGroups[0];
        var frame = frameGroup.Frames > 0 ? request.Frame % frameGroup.Frames : 0u;

        return new ThingFrameSelection
        {
            FrameGroup = frameGroup,
            FrameGroupIndex = 0,
            PatternX = PositiveMod(request.TileX, frameGroup.PatternX),
            PatternY = PositiveMod(request.TileY, frameGroup.PatternY),
            PatternZ = 0,
            Frame = frame,
        };
    }

    /// <summary>Computes effect animation frame from elapsed time (NyxClient default 75 ms per frame).</summary>
    public static uint GetEffectFrameIndex(ThingType effect, float elapsedMs, int ticksPerFrame = 75)
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
    public static uint GetCyclicFrameIndex(ThingType thing, float elapsedMs, int ticksPerFrame = 333)
    {
        if (thing.FrameGroups.Count == 0)
            return 0;
        var frameGroup = thing.FrameGroups[0];
        if (frameGroup.Frames <= 1)
            return 0;
        var ticks = Math.Max(1, ticksPerFrame);
        return (uint)((elapsedMs / ticks) % frameGroup.Frames);
    }

    /// <summary>Resolves missile / distance-effect aim from eight-way direction or tile delta.</summary>
    public static ThingFrameSelection GetMissileFrame(ThingType missile, MissileFrameRequest request = default)
    {
        if (missile.Kind != ThingKind.Missile)
            throw new ArgumentException($"Expected missile, got {missile.Kind}.", nameof(missile));
        if (missile.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Missile {missile.Id} has no frame groups.");

        var frameGroup = missile.FrameGroups[0];
        var direction = request.Direction
            ?? MissileDirectionPatterns.DirectionFromTileDelta(request.TileDeltaX, request.TileDeltaY);
        var (patternX, patternY) = MissileDirectionPatterns.GetPattern(direction);

        return new ThingFrameSelection
        {
            FrameGroup = frameGroup,
            FrameGroupIndex = 0,
            PatternX = patternX,
            PatternY = patternY,
            PatternZ = 0,
            Frame = 0,
        };
    }
}
