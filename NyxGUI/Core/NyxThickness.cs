namespace NyxGui;

/// <summary>Margins / padding: left, top, right, bottom.</summary>
public readonly struct NyxThickness : IEquatable<NyxThickness>
{
    public NyxThickness(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public static NyxThickness Uniform(int all) => new(all, all, all, all);

	public static NyxThickness Zero => default;

    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }

    public bool Equals(NyxThickness other) =>
        Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

    public override bool Equals(object? obj) => obj is NyxThickness t && Equals(t);
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);
}
