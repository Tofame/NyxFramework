namespace NyxGui;

public sealed class NyxSliderValueChangedEventArgs : EventArgs
{
    public NyxSliderValueChangedEventArgs(float value) => Value = value;
    public float Value { get; }
}

/// <summary>Horizontal value slider (0–1 by default).</summary>
public sealed class NyxSlider : NyxWidget, ICapturesPointer
{
    public NyxSlider(string? id = null) : base(0) { Id = id; }

    public float Minimum { get; set; }
    public float Maximum { get; set; } = 1f;

    private float _value;
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, Minimum, Maximum);
            if (Math.Abs(clamped - _value) < 0.0001f) return;
            _value = clamped;
            ValueChanged?.Invoke(this, new NyxSliderValueChangedEventArgs(_value));
            InvalidateRender();
        }
    }

    public event EventHandler<NyxSliderValueChangedEventArgs>? ValueChanged;

    private bool _dragThumb;

    private NyxRect TrackRect => Bounds.Inset(4, Bounds.Height / 2 - 3, 4, Bounds.Height / 2 - 3);

    private NyxRect ThumbRect
    {
        get
        {
            var track = TrackRect;
            var t = Maximum > Minimum ? (_value - Minimum) / (Maximum - Minimum) : 0f;
            t = Math.Clamp(t, 0f, 1f);
            var thumbW = Math.Min(12, Math.Max(8, track.Width / 8));
            var x = track.X + (int)((track.Width - thumbW) * t);
            return new NyxRect(x, track.Y - 4, thumbW, track.Height + 8);
        }
    }

    public override void OnMouseMove(int x, int y)
    {
        base.OnMouseMove(x, y);
        if (!_dragThumb) return;

        var track = TrackRect;
        var rel = Math.Clamp(x - track.X, 0, Math.Max(1, track.Width));
        var t = rel / (float)Math.Max(1, track.Width);
        Value = Minimum + t * (Maximum - Minimum);
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        base.OnMouseDown(x, y, button);
        if (!HitTest(x, y)) return;

        var thumb = ThumbRect;
        if (thumb.Contains(x, y))
        {
            _dragThumb = true;
            return;
        }

        var track = TrackRect;
        var rel = Math.Clamp(x - track.X, 0, Math.Max(1, track.Width));
        Value = Minimum + rel / (float)Math.Max(1, track.Width) * (Maximum - Minimum);
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        base.OnMouseUp(x, y, button);
        _dragThumb = false;
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible) return;

        var track = TrackRect;
        painter.FillRect(track, theme.SliderTrack);
        var fillW = (int)(track.Width * (Maximum > Minimum ? (_value - Minimum) / (Maximum - Minimum) : 0f));
        if (fillW > 0)
            painter.FillRect(new NyxRect(track.X, track.Y, fillW, track.Height), theme.SliderFill);

        var thumb = ThumbRect;
        painter.FillRect(thumb, PointerInside || _dragThumb ? theme.ScrollThumbHover : theme.SliderThumb);
        painter.DrawRect(Bounds, theme.PanelBorder, 1);
    }
}
