namespace NyxAssets.Things.Frames;

/// <summary>NyxClient <c>Missile::draw</c> pattern X/Y from eight-way direction.</summary>
public static class MissileDirectionPatterns
{
    /// <summary>Maps <see cref="Direction8"/> to pattern coordinates in the missile frame group.</summary>
    public static (uint PatternX, uint PatternY) GetPattern(Direction8 direction) => ((int)direction) switch
    {
        (int)Direction8.NorthWest => (0, 0),
        (int)Direction8.North => (1, 0),
        (int)Direction8.NorthEast => (2, 0),
        (int)Direction8.East => (2, 1),
        (int)Direction8.SouthEast => (2, 2),
        (int)Direction8.South => (1, 2),
        (int)Direction8.SouthWest => (0, 2),
        (int)Direction8.West => (0, 1),
        _ => (1, 1),
    };

    /// <summary>Maps a raw 0–7 OTC direction index to pattern coordinates.</summary>
    public static (uint PatternX, uint PatternY) GetPattern(int direction8) =>
        GetPattern((Direction8)(direction8 % 8));

    /// <summary>
    /// NyxClient <c>Position::getDirectionFromPositions</c> — 45° sectors (±22.5° per direction).
    /// </summary>
    public static Direction8 DirectionFromTileDelta(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return Direction8.East;

        var angle = Math.Atan2(-dy, dx);
        if (angle < 0)
            angle += Math.PI * 2;

        var degrees = angle * (180.0 / Math.PI);

        if (degrees >= 360 - 22.5 || degrees < 22.5)
            return Direction8.East;
        if (degrees < 67.5)
            return Direction8.NorthEast;
        if (degrees < 112.5)
            return Direction8.North;
        if (degrees < 157.5)
            return Direction8.NorthWest;
        if (degrees < 202.5)
            return Direction8.West;
        if (degrees < 247.5)
            return Direction8.SouthWest;
        if (degrees < 292.5)
            return Direction8.South;
        if (degrees < 337.5)
            return Direction8.SouthEast;

        return Direction8.East;
    }

    /// <summary>Travel duration in ms (NyxClient <c>150 * sqrt(length)</c> on tile delta).</summary>
    public static float DurationMsFromTileDelta(int dx, int dy) =>
        150f * MathF.Sqrt(dx * dx + dy * dy);
}
