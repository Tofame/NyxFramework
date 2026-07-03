namespace NyxGui;

/// <summary>
/// Integer axis-aligned rectangle. Same convention as The Forgotten Client <c>iRect</c>:
/// <see cref="X"/>, <see cref="Y"/> is the top-left origin; <see cref="Width"/> and <see cref="Height"/> are extents.
/// </summary>
public readonly struct NyxRect : IEquatable<NyxRect>
{
    public static NyxRect Empty => new(0, 0, 0, 0);

    public NyxRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(int px, int py) =>
        (uint)(px - X) < (uint)Width && (uint)(py - Y) < (uint)Height;

    public NyxRect Translated(int dx, int dy) => new(X + dx, Y + dy, Width, Height);

    public NyxRect WithSize(int width, int height) => new(X, Y, width, height);

    public bool Intersects(NyxRect other) =>
        X < other.Right && other.X < Right && Y < other.Bottom && other.Y < Bottom;

    public NyxRect Intersection(NyxRect other)
    {
        var x1 = Math.Max(X, other.X);
        var y1 = Math.Max(Y, other.Y);
        var x2 = Math.Min(Right, other.Right);
        var y2 = Math.Min(Bottom, other.Bottom);
        if (x2 <= x1 || y2 <= y1)
            return new NyxRect(x1, y1, 0, 0);
        return new NyxRect(x1, y1, x2 - x1, y2 - y1);
    }

    /// <summary>Shrinks the rectangle by padding on each side (left, top, right, bottom).</summary>
    public NyxRect Inset(int left, int top, int right, int bottom) =>
        new(X + left, Y + top, Math.Max(0, Width - left - right), Math.Max(0, Height - top - bottom));

    public static bool operator ==(NyxRect a, NyxRect b) => a.Equals(b);
    public static bool operator !=(NyxRect a, NyxRect b) => !a.Equals(b);

    public bool Equals(NyxRect other) =>
        X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    public override bool Equals(object? obj) => obj is NyxRect r && Equals(r);

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
}
