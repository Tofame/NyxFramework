namespace NyxGuiRender.Gl;

/// <summary>8-bit RGBA colour with conversion from <see cref="NyxGui.NyxColor"/>.</summary>
public readonly struct GuiColor
{
    public GuiColor(byte r, byte g, byte b, byte a = 255)
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

    /// <summary>Converts from the NyxGUI colour type (same layout, different namespace).</summary>
    public static GuiColor FromNyx(NyxGui.NyxColor c) => new(c.R, c.G, c.B, c.A);
}
