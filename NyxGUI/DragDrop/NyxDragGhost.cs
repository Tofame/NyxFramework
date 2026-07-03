namespace NyxGui;

/// <summary>
/// Visual ghost that follows the cursor during a drag operation.
/// Renders a semi-transparent overlay at the pointer position.
/// </summary>
public sealed class NyxDragGhost
{
    private NyxRect _bounds;
    private readonly int _width;
    private readonly int _height;
    private readonly string? _label;

    public NyxDragGhost(int width, int height, string? label = null)
    {
        _width = width;
        _height = height;
        _label = label;
    }

    public NyxRect Bounds => _bounds;

    public void UpdatePosition(int x, int y)
    {
        _bounds = new NyxRect(x - _width / 2, y - _height / 2, _width, _height);
    }

    public void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (_bounds.Width <= 0 || _bounds.Height <= 0) return;

        var bg = new NyxColor(40, 40, 48, 180);
        var border = new NyxColor(120, 120, 140, 200);

        painter.FillRect(_bounds, bg);
        painter.DrawRect(_bounds, border, 1);

        if (!string.IsNullOrEmpty(_label))
        {
            var textRect = new NyxRect(
                _bounds.X + 4, _bounds.Y + 2,
                _bounds.Width - 8, _bounds.Height - 4);
            painter.DrawText(textRect, _label, NyxTextAlign.Center, theme.TextPrimary);
        }
    }
}
