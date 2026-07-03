namespace NyxGui;

/// <summary>Filled progress indicator between <see cref="Minimum"/> and <see cref="Maximum"/>.</summary>
public sealed class NyxProgressBar : NyxWidget
{
    public NyxProgressBar(string? id = null) : base(0) { Id = id; }

    public float Minimum { get; set; }
    public float Maximum { get; set; } = 100f;

    private float _value;
    public float Value
    {
        get => _value;
        set { _value = value; InvalidateRender(); }
    }

    public bool ShowLabel { get; set; } = true;

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            painter.FillRect(Bounds, Tint(theme.ProgressTrack, visual));
            var t = Maximum > Minimum ? (_value - Minimum) / (Maximum - Minimum) : 0f;
            t = Math.Clamp(t, 0f, 1f);
            var fillW = (int)(Bounds.Width * t);
            if (fillW > 0)
                painter.FillRect(new NyxRect(Bounds.X, Bounds.Y, fillW, Bounds.Height), Tint(theme.ProgressFill, visual));

            painter.DrawRect(Bounds, Tint(theme.PanelBorder, visual), 1);

            if (ShowLabel)
            {
                var label = $"{Math.Round(_value, 1)} / {Math.Round(Maximum, 1)}";
                painter.DrawText(Bounds, label, NyxTextAlign.Center, Tint(theme.TextPrimary, visual), GetPaintFont());
            }
        }
        finally
        {
            EndPaintVisual();
        }
    }
}
