using NyxAssets.Things;

namespace NyxDrawer.Items;

/// <summary>Draw parameters for a Nyx item (ground/container tile object).</summary>
public readonly struct ItemDrawRequest
{
    /// <summary>The thing type to draw from the .dat catalog.</summary>
    public required ThingType Item { get; init; }

    /// <summary>Tile reference pixel (south-west corner of the 32×32 cell).</summary>
    public required float AnchorX { get; init; }
    public required float AnchorY { get; init; }

    /// <summary>Animation frame index (use <see cref="Animation.ThingAnimator"/> for timed frames).</summary>
    public uint Frame { get; init; }

    /// <summary>Sprite pattern X (direction/stack count).</summary>
    public uint PatternX { get; init; }

    /// <summary>Sprite pattern Y (addon/stack height).</summary>
    public uint PatternY { get; init; }

    /// <summary>Sprite pattern Z (elevation/layer).</summary>
    public uint PatternZ { get; init; }

    /// <summary>Optional effect shader name (e.g. for glowing/animated items).</summary>
    public string? SpriteShader { get; init; }
}
