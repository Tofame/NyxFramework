using NyxGui;

namespace Sandbox.NyxGUI_Extend;

/// <summary>Stack count: white text, 1px black cardinal outline, 2px from slot right.</summary>
public static class UIItemStackOverlay
{
    private const int MarginRight = 2;
    private const int MarginBottom = 1;

    private static readonly NyxColor TextColor = NyxColor.FromRgb(255, 255, 255);
    public static void Paint(INyxGuiPainter painter, NyxRect iconDest, ushort count, NyxFontStyle? font = null)
    {
        if (count <= 1)
            return;

        var text = count.ToString();
        var style = (font ?? NyxFontStyle.Default).WithOutlined();
        painter.MeasureText(text, style, out var textW, out var textH);
        if (textW <= 0 || textH <= 0)
            return;

        var x = iconDest.Right - MarginRight - textW;
        var y = iconDest.Bottom - MarginBottom - textH;
        if (x < iconDest.X)
            x = iconDest.X;
        if (y < iconDest.Y)
            y = iconDest.Y;

        painter.DrawText(
            new NyxRect(x, y, textW, textH),
            text,
            NyxTextAlign.TopLeft,
            TextColor,
            style);
    }
}
