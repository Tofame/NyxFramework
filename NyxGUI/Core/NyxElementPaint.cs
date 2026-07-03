namespace NyxGui;

/// <summary>Opacity and tint helpers for widget painting.</summary>
public static class NyxElementPaint
{
    public static NyxColor WithOpacity(NyxColor color, float opacity)
    {
        if (opacity >= 1f)
            return color;
        if (opacity <= 0f)
            return new NyxColor(color.R, color.G, color.B, 0);
        var a = (byte)Math.Clamp((int)Math.Round(color.A * opacity), 0, 255);
        return new NyxColor(color.R, color.G, color.B, a);
    }
}
