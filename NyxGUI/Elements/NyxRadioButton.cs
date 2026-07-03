namespace NyxGui;

public sealed class NyxRadioChangedEventArgs : EventArgs
{
    public NyxRadioChangedEventArgs(bool isChecked) => IsChecked = isChecked;
    public bool IsChecked { get; }
}

/// <summary>Single-choice option within a named group.</summary>
public sealed class NyxRadioButton : NyxWidget, ICapturesPointer
{
    public NyxRadioButton(string? id = null) : base(0) { Id = id; }

    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;

    private bool _isChecked;
    public bool IsChecked => _isChecked;

    public event EventHandler<NyxRadioChangedEventArgs>? Changed;

    internal void SetCheckedSilently(bool value) => _isChecked = value;

    private NyxRect DotRect
    {
        get
        {
            var side = Math.Min(16, Math.Min(Bounds.Width, Bounds.Height));
            return new NyxRect(Bounds.X + 2, Bounds.Y + (Bounds.Height - side) / 2, side, side);
        }
    }

    private NyxRect LabelRect =>
        new(DotRect.Right + 6, Bounds.Y, Math.Max(0, Bounds.Width - DotRect.Width - 10), Bounds.Height);

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        base.OnMouseUp(x, y, button);
        if (button == NyxMouseButton.Left && PointerInside && !_isChecked)
        {
            _isChecked = true;
            NyxRadioGroup.Select(this);
            Changed?.Invoke(this, new NyxRadioChangedEventArgs(true));
            InvalidateRender();
        }
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible) return;

        var dot = DotRect;
        var face = PointerPressed
            ? theme.ButtonFacePressed
            : PointerInside ? theme.ButtonFaceHover : theme.ButtonFace;

        painter.FillRect(dot, face);
        painter.DrawRect(dot, theme.ButtonBorder, 1);

        if (_isChecked)
        {
            var inset = new NyxRect(dot.X + 4, dot.Y + 4, Math.Max(0, dot.Width - 8), Math.Max(0, dot.Height - 8));
            painter.FillRect(inset, theme.CheckOn);
        }

        painter.DrawText(LabelRect, Label, NyxTextAlign.Center, theme.TextPrimary, GetPaintFont());
    }
}
