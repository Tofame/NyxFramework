namespace NyxGui;

/// <summary>Read-only grid of strings with optional header row.</summary>
public sealed class NyxTable : NyxWidget
{
    public NyxTable(string? id = null) : base(0) { Id = id; }

    private readonly List<string[]> _rows = [];
    private int[] _columnWidths = [];

    public bool ShowHeader { get; set; } = true;
    public int RowHeight { get; set; } = 22;
    public int HeaderHeight { get; set; } = 24;
    public IReadOnlyList<string[]> Rows => _rows;

    public void SetColumnWidths(params int[] widths) => _columnWidths = widths;

    public void SetRows(IEnumerable<string[]> rows)
    {
        _rows.Clear();
        _rows.AddRange(rows);
        InvalidateRender();
    }

    public void SetHeader(params string[] cells)
    {
        if (_rows.Count == 0) _rows.Add(cells);
        else _rows[0] = cells;
        InvalidateRender();
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible) return;

        painter.FillRect(Bounds, theme.PanelBackground);
        painter.DrawRect(Bounds, theme.PanelBorder, 1);

        if (_rows.Count == 0) return;

        var colCount = _rows.Max(r => r.Length);
        if (_columnWidths.Length != colCount)
        {
            var w = colCount > 0 ? Bounds.Width / colCount : Bounds.Width;
            _columnWidths = Enumerable.Repeat(w, colCount).ToArray();
        }

        var y = Bounds.Y;
        var rowIndex = 0;
        foreach (var row in _rows)
        {
            var isHeader = ShowHeader && rowIndex == 0;
            var h = isHeader ? HeaderHeight : RowHeight;
            var rowRect = new NyxRect(Bounds.X, y, Bounds.Width, h);
            var bg = isHeader ? theme.TableHeader : rowIndex % 2 == 0 ? theme.PanelBackground : theme.TableRowAlt;
            painter.FillRect(rowRect, bg);

            var x = Bounds.X;
            for (var c = 0; c < colCount; c++)
            {
                var cw = c < _columnWidths.Length ? _columnWidths[c] : 0;
                var cell = new NyxRect(x, y, cw, h);
                var text = c < row.Length ? row[c] : string.Empty;
                painter.DrawText(cell.Inset(4, 0, 4, 0), text, NyxTextAlign.Center, theme.TextPrimary, GetPaintFont());
                painter.FillRect(new NyxRect(cell.Right - 1, y, 1, h), theme.TableGrid);
                x += cw;
            }

            painter.FillRect(new NyxRect(Bounds.X, y + h - 1, Bounds.Width, 1), theme.TableGrid);
            y += h;
            rowIndex++;
            if (y >= Bounds.Bottom) break;
        }
    }
}
