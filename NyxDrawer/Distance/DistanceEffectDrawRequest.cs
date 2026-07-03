using NyxAssets.Things;

namespace NyxDrawer.Distance;

/// <summary>Missile / distance effect traveling between two tile positions (NyxClient <c>Missile</c>).</summary>
public readonly struct DistanceEffectDrawRequest
{
    public required ThingType Missile { get; init; }

    /// <summary>Screen anchor at source tile (32×32 cell top-left in camera space).</summary>
    public required float FromAnchorX { get; init; }
    public required float FromAnchorY { get; init; }

    /// <summary>Tile delta (destination − source). Used for direction pattern and duration.</summary>
    public int TileDeltaX { get; init; }
    public int TileDeltaY { get; init; }

    /// <summary>0…1 progress along the path (NyxClient <c>fraction = elapsed / duration</c>).</summary>
    public float Progress { get; init; }

    /// <summary>Override travel direction (NyxClient 0–7). When null, derived from tile delta.</summary>
    public int? Direction { get; init; }
}
