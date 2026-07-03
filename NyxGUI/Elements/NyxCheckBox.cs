namespace NyxGui;

public sealed class NyxCheckBoxChangedEventArgs : EventArgs
{
    public NyxCheckBoxChangedEventArgs(bool isChecked) => IsChecked = isChecked;
    public bool IsChecked { get; }
}

/// <summary>Two-state toggle checkbox.</summary>
public sealed class NyxCheckBox : NyxWidget, ICapturesPointer
{
    public NyxCheckBox(string? id = null) : base(0) { Id = id; }

    public string Label { get; set; } = string.Empty;

    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            Changed?.Invoke(this, new NyxCheckBoxChangedEventArgs(_isChecked));
            InvalidateRender();
        }
    }

    public event EventHandler<NyxCheckBoxChangedEventArgs>? Changed;

    private NyxRect BoxRect
    {
        get
        {
            var side = Math.Min(16, Math.Min(Bounds.Width, Bounds.Height));
            return new NyxRect(Bounds.X + 2, Bounds.Y + (Bounds.Height - side) / 2, side, side);
        }
    }

    private NyxRect LabelRect =>
        new(BoxRect.Right + 6, Bounds.Y, Math.Max(0, Bounds.Width - BoxRect.Width - 10), Bounds.Height);

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        base.OnMouseUp(x, y, button);
        if (button == NyxMouseButton.Left && PointerInside)
        {
            IsChecked = !IsChecked;
        }
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible) return;

        var box = BoxRect;
        var face = PointerPressed
            ? theme.ButtonFacePressed
            : PointerInside ? theme.ButtonFaceHover : theme.ButtonFace;

        painter.FillRect(box, face);
        painter.DrawRect(box, theme.ButtonBorder, 1);

        if (IsChecked)
        {
            var inset = new NyxRect(box.X + 3, box.Y + 3, Math.Max(0, box.Width - 6), Math.Max(0, box.Height - 6));
            painter.FillRect(inset, theme.CheckOn);
        }

        painter.DrawText(LabelRect, Label, NyxTextAlign.Center, theme.TextPrimary, GetPaintFont());
    }
}
