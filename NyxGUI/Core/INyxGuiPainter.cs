namespace NyxGui;

/// <summary>
/// Host-provided drawing backend. Widgets call these from <see cref="NyxElement.Paint"/>.
/// </summary>
public interface INyxGuiPainter
{
    void PushClip(NyxRect rect);

    void PopClip();

    void FillRect(NyxRect rect, NyxColor color);

    void DrawRect(NyxRect rect, NyxColor color, int thickness = 1);

    void DrawText(
        NyxRect bounds,
        ReadOnlySpan<char> text,
        NyxTextAlign align,
        NyxColor color,
        NyxFontStyle? font = null);

    /// <summary>Pixel size of <paramref name="text"/> for <paramref name="font"/> (0×0 if unavailable).</summary>
    void MeasureText(ReadOnlySpan<char> text, NyxFontStyle? font, out int width, out int height);

    /// <summary>Draws a textured quad or 9-slice using <see cref="NyxImagePaintCommand"/>.</summary>
    void DrawImage(in NyxImagePaintCommand command);

    /// <summary>Draws a 32×32 RGBA sprite (<see cref="NyxRender.Sprite.Rgba32Length"/> bytes) into <paramref name="dest"/>.</summary>
    void DrawSprite32(NyxRect dest, ReadOnlySpan<byte> rgba4096, uint cacheKey = 0, bool smooth = false);
}
