namespace NyxGui;

/// <summary>Static text label.</summary>
public sealed class NyxLabel : NyxWidget
{
    public NyxLabel(string? id = null) : base(0) { Id = id; }

    private string _text = string.Empty;

    public event Action<NyxLabel, string, string>? TextChanged;

    public string Text
    {
        get => _text;
        set
        {
            var next = value ?? string.Empty;
            if (string.Equals(_text, next, StringComparison.Ordinal)) return;
            var old = _text;
            _text = next;
            TextChanged?.Invoke(this, old, next);
            InvalidateRender();
        }
    }

    public NyxTextAlign Align { get; set; } = NyxTextAlign.TopLeft;

    /// <summary>When true, wraps at word boundaries to fit width.</summary>
    public bool Wrap { get; set; }

    public int LineHeight { get; set; } = 14;

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            PaintChrome(painter, visual);
            var color = Tint(theme.TextPrimary, visual);
            if (!Wrap)
            {
                painter.DrawText(TextLayoutBounds, Text, Align, color, GetPaintFont());
            }
            else
            {
                var lines = NyxTextLayout.BuildLines(Text, true, Bounds.Width);
                NyxTextLayout.PaintMultiline(painter, TextLayoutBounds, lines, Align, LineHeight, color, GetPaintFont());
            }
        }
        finally
        {
            EndPaintVisual();
        }
    }
}
