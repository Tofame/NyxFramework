namespace NyxGui;

/// <summary>Displays a single textured image inside its bounds.</summary>
public sealed class NyxImage : NyxWidget
{
    public NyxImage(string? id = null) : base(0) { Id = id; }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            if (visual.Image is not null)
                PaintBackground(painter, visual);
            else if (Image is not null)
                PaintBackground(painter, GetPaintVisualOrResolve());
            else
                painter.FillRect(Bounds, Tint(theme.PanelBackground, visual));

            if (visual.HasBorder)
                PaintStateBorder(painter, visual);
        }
        finally
        {
            EndPaintVisual();
        }
    }
}
