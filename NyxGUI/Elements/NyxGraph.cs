namespace NyxGui;

/// <summary>Simple line chart for one or more data series.</summary>
public sealed class NyxGraph : NyxWidget
{
    public NyxGraph(string? id = null) : base(0) { Id = id; }

    public float MinimumY { get; set; }
    public float MaximumY { get; set; } = 100f;
    public bool AutoScaleY { get; set; } = true;
    public bool ShowSeriesB { get; set; }
    public bool ShowSeriesC { get; set; }
    public bool SquarePlot { get; set; }
    public bool ShowScaleLabels { get; set; } = true;

    private readonly List<float> _seriesA = [];
    private readonly List<float> _seriesB = [];
    private readonly List<float> _seriesC = [];

    public IReadOnlyList<float> SeriesA => _seriesA;
    public IReadOnlyList<float> SeriesB => _seriesB;
    public IReadOnlyList<float> SeriesC => _seriesC;

    public void SetSeriesA(IEnumerable<float> values) { _seriesA.Clear(); _seriesA.AddRange(values); InvalidateRender(); }
    public void SetSeriesB(IEnumerable<float> values) { _seriesB.Clear(); _seriesB.AddRange(values); InvalidateRender(); }
    public void SetSeriesC(IEnumerable<float> values) { _seriesC.Clear(); _seriesC.AddRange(values); InvalidateRender(); }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            var plot = SquarePlot ? SquarePlotRect(Bounds) : Bounds.Inset(8, 8, 8, 16);
            painter.FillRect(Bounds, Tint(theme.PanelBackground, visual));
            painter.DrawRect(Bounds, Tint(theme.PanelBorder, visual), 1);
            painter.FillRect(plot, Tint(theme.InputBackground, visual));

            var minY = MinimumY;
            var maxY = MaximumY;
            if (AutoScaleY)
            {
                var all = _seriesA.ToList();
                if (ShowSeriesB) all.AddRange(_seriesB);
                if (ShowSeriesC) all.AddRange(_seriesC);
                if (all.Count > 0)
                {
                    minY = all.Min();
                    maxY = all.Max();
                    if (Math.Abs(maxY - minY) < 0.001f) maxY = minY + 1f;
                }
            }

            DrawAxis(painter, plot, theme, visual);
            if (ShowScaleLabels) DrawScaleLabels(painter, plot, minY, maxY, theme, GetPaintFont(), visual);
            DrawSeries(painter, plot, _seriesA, Tint(theme.GraphLine, visual), minY, maxY);
            if (ShowSeriesB && _seriesB.Count > 0) DrawSeries(painter, plot, _seriesB, Tint(theme.GraphLineSecondary, visual), minY, maxY);
            if (ShowSeriesC && _seriesC.Count > 0) DrawSeries(painter, plot, _seriesC, Tint(theme.GraphLineTertiary, visual), minY, maxY);
        }
        finally
        {
            EndPaintVisual();
        }
    }

    private static NyxRect SquarePlotRect(NyxRect bounds)
    {
        var inner = bounds.Inset(8, 8, 8, 8);
        var side = Math.Min(inner.Width, inner.Height);
        if (side <= 0) return inner;
        return new NyxRect(inner.X + (inner.Width - side) / 2, inner.Y + (inner.Height - side) / 2, side, side);
    }

    private void DrawScaleLabels(INyxGuiPainter painter, NyxRect plot, float minY, float maxY, NyxGuiTheme theme, NyxFontStyle? font, in NyxWidgetVisual visual)
    {
        painter.DrawText(new NyxRect(plot.X + 3, plot.Y + 2, plot.Width - 6, 14), FormatScaleValue(maxY), NyxTextAlign.TopLeft, Tint(theme.TextMuted, visual), font);
        painter.DrawText(new NyxRect(plot.X + 3, plot.Bottom - 16, plot.Width - 6, 14), FormatScaleValue(minY), NyxTextAlign.TopLeft, Tint(theme.TextMuted, visual), font);
    }

    private static string FormatScaleValue(float v) => v >= 10_000 ? $"{v / 1000f:0}k" : v >= 1000 ? $"{v / 1000f:0.#}k" : $"{v:0}";

    private void DrawAxis(INyxGuiPainter painter, NyxRect plot, NyxGuiTheme theme, in NyxWidgetVisual visual)
    {
        painter.FillRect(new NyxRect(plot.X, plot.Bottom - 1, plot.Width, 1), Tint(theme.GraphAxis, visual));
        painter.FillRect(new NyxRect(plot.X, plot.Y, 1, plot.Height), Tint(theme.GraphAxis, visual));
    }

    private static void DrawSeries(INyxGuiPainter painter, NyxRect plot, IReadOnlyList<float> values, NyxColor color, float minY, float maxY)
    {
        if (values.Count == 0) return;
        var range = maxY - minY;
        if (range <= 0f) range = 1f;

        int MapY(float v)
        {
            var t = Math.Clamp((v - minY) / range, 0f, 1f);
            return plot.Bottom - 1 - (int)(t * (plot.Height - 2));
        }

        if (values.Count == 1)
        {
            var y = MapY(values[0]);
            painter.FillRect(new NyxRect(plot.X + plot.Width / 2 - 2, y - 2, 4, 4), color);
            return;
        }

        var stepX = plot.Width / (float)Math.Max(1, values.Count - 1);
        var prevX = plot.X;
        var prevY = MapY(values[0]);
        for (var i = 1; i < values.Count; i++)
        {
            var x = plot.X + (int)(stepX * i);
            var y = MapY(values[i]);
            DrawLine(painter, prevX, prevY, x, y, color);
            prevX = x;
            prevY = y;
        }
    }

    private static void DrawLine(INyxGuiPainter painter, int x0, int y0, int x1, int y1, NyxColor color)
    {
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var steps = Math.Max(dx, dy);
        if (steps == 0) { painter.FillRect(new NyxRect(x0, y0, 1, 1), color); return; }

        for (var i = 0; i <= steps; i++)
        {
            var t = i / (float)steps;
            var x = (int)(x0 + (x1 - x0) * t);
            var y = (int)(y0 + (y1 - y0) * t);
            painter.FillRect(new NyxRect(x, y, 2, 2), color);
        }
    }
}
