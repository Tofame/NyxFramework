namespace NyxGui;

/// <summary>Horizontal or vertical rule; mirrors <c>GUI_Separator</c>.</summary>
public sealed class NyxSeparator : NyxElement
{
    public NyxSeparator(NyxRect bounds, uint internalId = 0)
        : base(internalId)
    {
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual))
            return;

        try
        {
            PaintChrome(painter, visual);
            if (visual.Image is null && !visual.HasBackground)
                painter.FillRect(Bounds, theme.Separator);
        }
        finally
        {
            EndPaintVisual();
        }
    }
}
