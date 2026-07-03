namespace NyxGui;

public sealed class NyxComboSelectionChangedEventArgs : EventArgs
{
    public NyxComboSelectionChangedEventArgs(int index, string item) { Index = index; Item = item; }
    public int Index { get; }
    public string Item { get; }
}

/// <summary>Drop-down list of strings.</summary>
public sealed class NyxComboBox : NyxElement
{
    private readonly List<string> _items = [];
    private int _selectedIndex = -1;
    private bool _open;

    public NyxComboBox(NyxRect bounds, uint internalId = 0)
        : base(internalId)
    {
    }

    public NyxComboBox(string? id = null) : base(0) { Id = id; }

    public IReadOnlyList<string> Items => _items;
    public int SelectedIndex => _selectedIndex;

    public string? SelectedItem =>
        _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;

    public int RowHeight { get; set; } = 22;
    public int MaxVisibleRows { get; set; } = 6;

    public event EventHandler<NyxComboSelectionChangedEventArgs>? SelectionChanged;

    public void SetItems(IEnumerable<string> items)
    {
        _items.Clear();
        _items.AddRange(items);
        if (_selectedIndex >= _items.Count)
            _selectedIndex = _items.Count > 0 ? 0 : -1;
    }

    public void SelectIndex(int index)
    {
        if (_items.Count == 0)
        {
            _selectedIndex = -1;
            return;
        }

        var i = Math.Clamp(index, 0, _items.Count - 1);
        if (i == _selectedIndex)
            return;
        _selectedIndex = i;
        SelectionChanged?.Invoke(this, new NyxComboSelectionChangedEventArgs(i, _items[i]));
    }

    private NyxRect DropdownRect
    {
        get
        {
            var rows = Math.Min(_items.Count, MaxVisibleRows);
            var h = rows * RowHeight;
            return new NyxRect(Bounds.X, Bounds.Bottom, Bounds.Width, h);
        }
    }

    public override bool HitTest(int x, int y)
    {
        if (!Enabled || !Visible || Phantom)
            return false;
        if (Bounds.Contains(x, y))
            return true;
        return _open && DropdownRect.Contains(x, y);
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        if (!HitTest(x, y))
            return;

        PointerPressed = true;

        if (_open && DropdownRect.Contains(x, y))
        {
            var relY = y - DropdownRect.Y;
            var idx = relY / RowHeight;
            if (idx >= 0 && idx < _items.Count)
            {
                SelectIndex(idx);
                _open = false;
            }

            return;
        }

        if (Bounds.Contains(x, y))
            _open = !_open;
        else
            _open = false;
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        PointerPressed = false;
        if (!_open && !Bounds.Contains(x, y))
            _open = false;
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible)
            return;

        var face = PointerInside ? theme.ButtonFaceHover : theme.ButtonFace;
        painter.FillRect(Bounds, face);
        painter.DrawRect(Bounds, theme.ButtonBorder, 1);

        var label = SelectedItem ?? string.Empty;
        var textRect = Bounds.Inset(6, 0, 20, 0);
        painter.DrawText(textRect, label, NyxTextAlign.Center, theme.TextPrimary, GetPaintFont());

        var arrowX = Bounds.Right - 14;
        var arrowY = Bounds.Y + Bounds.Height / 2 - 2;
        painter.FillRect(new NyxRect(arrowX, arrowY, 8, 2), theme.TextMuted);
        painter.FillRect(new NyxRect(arrowX + 2, arrowY + 3, 4, 2), theme.TextMuted);

        if (!_open || _items.Count == 0)
            return;

        var drop = DropdownRect;
        painter.FillRect(drop, theme.ComboDropdown);
        painter.DrawRect(drop, theme.ButtonBorder, 1);

        for (var i = 0; i < Math.Min(_items.Count, MaxVisibleRows); i++)
        {
            var row = new NyxRect(drop.X, drop.Y + i * RowHeight, drop.Width, RowHeight);
            if (i == _selectedIndex)
                painter.FillRect(row, theme.ButtonFacePressed);
            painter.DrawText(row.Inset(6, 0, 4, 0), _items[i], NyxTextAlign.Center, theme.TextPrimary, GetPaintFont());
        }
    }
}
