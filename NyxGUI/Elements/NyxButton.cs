namespace NyxGui;

/// <summary>Push button with hover/pressed visuals and click events.</summary>
public class NyxButton : NyxWidget, ICapturesPointer
{
    public NyxButton(string? id = null) : base(0) { Id = id; }

    public string Label { get; set; } = string.Empty;

    /// <summary>Optional metadata (not shown in UI). Use <see cref="NyxElement.Tooltip"/> for hover hints.</summary>
    public string Description { get; set; } = string.Empty;


    /// <summary>Toggle/selection state. Uses NyxClient $on styling via <see cref="NyxElement.IsOn"/>.</summary>
    public bool IsSelected
    {
        get => IsOn;
        set => IsOn = value;
    }

    public event EventHandler<NyxClickEventArgs>? Click;

    /// <summary>Event raised when the button is right-clicked.</summary>
    public event EventHandler<NyxClickEventArgs>? RightClick;

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        base.OnMouseUp(x, y, button);
        if (button == NyxMouseButton.Left && PointerInside)
        {
            var args = new NyxClickEventArgs(NyxEventType.Click, this, x, y);
            Click?.Invoke(this, args);
            this.RaiseEvent(args);
        }
        else if (button == NyxMouseButton.Right && PointerInside)
        {
            var args = new NyxClickEventArgs(NyxEventType.RightClick, this, x, y);
            RightClick?.Invoke(this, args);
        }
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            if (visual.Image is not null && !string.IsNullOrEmpty(visual.Image.ImageSource))
            {
                PaintBackground(painter, visual);
            }
            else if (visual.HasBackground)
            {
                painter.FillRect(Bounds, Tint(visual.BackgroundColor!.Value, visual));
            }
            else
            {
                var face = PointerPressed
                    ? theme.ButtonFacePressed
                    : PointerInside ? theme.ButtonFaceHover : theme.ButtonFace;
                painter.FillRect(Bounds, Tint(face, visual));
            }

            if (visual.HasBorder)
                PaintStateBorder(painter, visual);
            else
                painter.DrawRect(Bounds, Tint(theme.ButtonBorder, visual), 1);

            painter.DrawText(TextLayoutBounds, Label, NyxTextAlign.Center, Tint(theme.TextPrimary, visual), GetPaintFont());
        }
        finally
        {
            EndPaintVisual();
        }
    }
}
