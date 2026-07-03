namespace NyxGui;

/// <summary>Clipped viewport for a large image with optional scroll offsets.</summary>
public sealed class NyxImageContainer : NyxElement
{
    public NyxImageContainer(NyxRect bounds, uint internalId = 0)
        : base(internalId)
    {
    }

    public NyxImageContainer(string? id = null) : base(0) { Id = id; }

    public int ScrollOffsetX { get; set; }
    public int ScrollOffsetY { get; set; }
    public int ContentWidth { get; set; }
    public int ContentHeight { get; set; }

    public override void OnMouseWheel(int x, int y, int delta)
    {
        if (!HitTest(x, y))
            return;

        var step = 24;
        if (delta > 0)
            ScrollOffsetY = Math.Max(0, ScrollOffsetY - step);
        else if (delta < 0)
        {
            var maxY = Math.Max(0, ContentHeight - Bounds.Height);
            ScrollOffsetY = Math.Min(maxY, ScrollOffsetY + step);
        }
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual))
            return;

        try
        {
            painter.FillRect(Bounds, Tint(theme.PanelBackground, visual));
            painter.PushClip(Bounds);

            if (Image is { } style && !string.IsNullOrEmpty(style.ImageSource))
            {
                var dest = new NyxRect(
                    Bounds.X - ScrollOffsetX,
                    Bounds.Y - ScrollOffsetY,
                    ContentWidth > 0 ? ContentWidth : Bounds.Width,
                    ContentHeight > 0 ? ContentHeight : Bounds.Height);
                var cmd = style.ToPaintCommand(dest, NyxColor.FromRgb(255, 255, 255), EffectiveOpacity(visual));
                painter.DrawImage(in cmd);
            }
            else
                PaintBackground(painter, visual);

            painter.PopClip();
            painter.DrawRect(Bounds, Tint(theme.PanelBorder, visual), 1);
        }
        finally
        {
            EndPaintVisual();
        }
    }
}
