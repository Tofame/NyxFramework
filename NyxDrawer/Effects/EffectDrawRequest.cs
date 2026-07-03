using NyxAssets.Things;

namespace NyxDrawer.Effects;

/// <summary>Magic effect on a map tile (NyxClient <c>Effect::draw</c>).</summary>
public readonly struct EffectDrawRequest
{
    public required ThingType Effect { get; init; }
    public required float AnchorX { get; init; }
    public required float AnchorY { get; init; }

    /// <summary>Tile coordinates used for pattern X/Y (NyxClient: position % numPattern).</summary>
    public int TileX { get; init; }
    public int TileY { get; init; }

    /// <summary>Animation phase / frame index.</summary>
    public uint Frame { get; init; }
}
