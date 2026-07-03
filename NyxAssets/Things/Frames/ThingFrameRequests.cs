namespace NyxAssets.Things.Frames;

/// <summary>NyxClient creature facing for outfits and mounts (North=0 … West=3).</summary>
public enum Direction4
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
}

/// <summary>Eight-way aim for distance effects / missiles (OTC <c>NyxDirection</c>).</summary>
public enum Direction8
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
    NorthEast = 4,
    SouthEast = 5,
    SouthWest = 6,
    NorthWest = 7,
}

/// <summary>Outfit or mount frame query (direction, walk cycle, addons, mounted pattern Z).</summary>
public readonly struct OutfitFrameRequest
{
    public OutfitFrameRequest()
    {
        Direction = (int)Direction4.South;
        AddonMask = 0xFF;
        FrameGroupIndex = -1;
    }

    /// <summary>NyxClient direction (0–3). Defaults to <see cref="Direction4.South"/>.</summary>
    public int Direction { get; init; }

    /// <summary>0 = idle; &gt;0 = walking animation phase.</summary>
    public uint WalkPhase { get; init; }

    /// <summary>Addon bitmask: bit 0 = pattern Y 1, bit 1 = pattern Y 2, … Base outfit (Y=0) is always included.</summary>
    public byte AddonMask { get; init; }

    /// <summary>When true, uses mounted pattern Z (<c>min(1, PatternZ−1)</c>) on the outfit frame group.</summary>
    public bool Mounted { get; init; }

    /// <summary>Force a frame group index. −1 picks idle/walking automatically from <see cref="WalkPhase"/>.</summary>
    public int FrameGroupIndex { get; init; }
}

/// <summary>Item tile object frame query (stack pile, optional explicit patterns).</summary>
public readonly struct ItemFrameRequest
{
    public ItemFrameRequest()
    {
        StackCount = 1;
    }

    public uint Frame { get; init; }

    /// <summary>Stack count for stackable 4×2 pile grids. Ignored when <see cref="PatternX"/> is set.</summary>
    public int StackCount { get; init; }

    /// <summary>Override pattern X (e.g. rotation). When null, derived from stack rules or 0.</summary>
    public uint? PatternX { get; init; }

    /// <summary>Override pattern Y. When null, derived from stack rules or 0.</summary>
    public uint? PatternY { get; init; }

    public uint PatternZ { get; init; }
}

/// <summary>Magic effect frame query (tile-varied patterns + animation time).</summary>
public readonly struct EffectFrameRequest
{
    public uint Frame { get; init; }

    /// <summary>World tile X — used for pattern X when the effect has multiple pattern columns.</summary>
    public int TileX { get; init; }

    /// <summary>World tile Y — used for pattern Y when the effect has multiple pattern rows.</summary>
    public int TileY { get; init; }
}

/// <summary>Distance effect / missile frame query (8-way aim).</summary>
public readonly struct MissileFrameRequest
{
    /// <summary>When set, used directly. Otherwise derived from <see cref="TileDeltaX"/> / <see cref="TileDeltaY"/>.</summary>
    public Direction8? Direction { get; init; }

    public int TileDeltaX { get; init; }
    public int TileDeltaY { get; init; }
}
