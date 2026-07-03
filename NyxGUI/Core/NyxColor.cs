namespace NyxGui;

public readonly struct NyxColor : IEquatable<NyxColor>
{
    public NyxColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public static NyxColor FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);

    public static NyxColor Transparent => default;

    /// <summary>
    /// Parses hex colours: <c>#rrggbb</c>, <c>#rrggbbaa</c>, <c>rrggbb</c>, or <c>rrggbbaa</c>.
    /// Each pair of hex characters is parsed as a byte using
    /// <c>Convert.ToByte(hexSpan, 16)</c>.
    /// </summary>
    public static bool TryParseHex(ReadOnlySpan<char> text, out NyxColor color)
    {
        color = default;
        if (text.Length == 0)
            return false;
        if (text[0] == '#')
            text = text[1..];

        if (text.Length is not (6 or 8))
            return false;

        if (!TryParseHexByte(text.Slice(0, 2), out var r) ||
            !TryParseHexByte(text.Slice(2, 2), out var g) ||
            !TryParseHexByte(text.Slice(4, 2), out var b))
            return false;

        byte a = 255;
        if (text.Length == 8 && !TryParseHexByte(text.Slice(6, 2), out a))
            return false;

        color = new NyxColor(r, g, b, a);
        return true;
    }

    private static bool TryParseHexByte(ReadOnlySpan<char> twoChars, out byte value)
    {
        value = 0;
        if (twoChars.Length != 2)
            return false;
        var hi = HexValue(twoChars[0]);
        var lo = HexValue(twoChars[1]);
        if (hi < 0 || lo < 0)
            return false;
        value = (byte)((hi << 4) | lo);
        return true;
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    public bool Equals(NyxColor other) => R == other.R && G == other.G && B == other.B && A == other.A;
    public override bool Equals(object? obj) => obj is NyxColor c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(R, G, B, A);
}
