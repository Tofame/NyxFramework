namespace NyxGui;

/// <summary>
/// Per-edge 9-slice insets in <b>source texture pixels</b> (NyxClient <c>image-border*</c>).
/// Destination edge sizes match these values 1:1; only the center stretches.
/// </summary>
public readonly struct NyxImageBorders : IEquatable<NyxImageBorders>
{
    public NyxImageBorders(int top, int right, int bottom, int left)
    {
        Top = Math.Max(0, top);
        Right = Math.Max(0, right);
        Bottom = Math.Max(0, bottom);
        Left = Math.Max(0, left);
    }

    public static NyxImageBorders Uniform(int border) => new(border, border, border, border);

    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
    public int Left { get; }

    public bool HasAny => Top > 0 || Right > 0 || Bottom > 0 || Left > 0;

    public bool Equals(NyxImageBorders other) =>
        Top == other.Top && Right == other.Right && Bottom == other.Bottom && Left == other.Left;

    public override bool Equals(object? obj) => obj is NyxImageBorders other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Top, Right, Bottom, Left);
}
