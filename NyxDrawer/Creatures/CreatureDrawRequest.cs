using NyxDrawer.Appearance;
using NyxAssets.Things;

namespace NyxDrawer.Creatures;

/// <summary>Parameters for drawing a Nyx creature (outfit + optional mount).</summary>
public readonly struct CreatureDrawRequest
{
    public required ThingType Outfit { get; init; }
    public ThingType? Mount { get; init; }
    public bool Mounted { get; init; }
    public required CreatureOutfitAppearance Appearance { get; init; }

    /// <summary>Tile reference pixel (south-west corner of the 32×32 cell), not bitmap top-left.</summary>
    public required float AnchorX { get; init; }
    public required float AnchorY { get; init; }

    /// <summary>NyxClient direction: North=0, East=1, South=2, West=3.</summary>
    public int Direction { get; init; }

    /// <summary>0 = idle; &gt;0 = walk animation phase.</summary>
    public uint WalkPhase { get; init; }
}
