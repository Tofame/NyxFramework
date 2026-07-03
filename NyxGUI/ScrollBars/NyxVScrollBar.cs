namespace NyxGui;

/// <summary>Vertical scrollbar; mirrors <c>GUI_VScrollBar</c> (simplified interaction).</summary>
public sealed class NyxVScrollBar : NyxElement
{
    private bool _hoverTrack;
    private bool _hoverThumb;
    private bool _dragThumb;
    private int _dragStartY;
    private int _valueAtDragStart;

    public NyxVScrollBar(NyxRect bounds, uint internalId = 0)
        : base(internalId)
    {
    }

    public int Extent { get; private set; }
    public int Value { get; private set; }
    public int Viewport { get; private set; }

    public event EventHandler? ValueChanged;

	public int ScrollStep { get; set; } = -1;

    public void Configure(int extent, int value, int viewport)
    {
        Extent = Math.Max(0, extent);
        Viewport = Math.Max(0, viewport);
        var v = Math.Clamp(value, 0, Extent);
        if (v != Value)
        {
            Value = v;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
        else
            Value = v;
    }

    public void SetValue(int value)
    {
        var v = Math.Clamp(value, 0, Extent);
        if (v == Value)
            return;
        Value = v;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    private NyxRect TrackRect => Bounds;

    private int ThumbHeight
    {
        get
        {
            var track = TrackRect;
            if (track.Height <= 0)
                return 0;
            if (Extent <= 0)
                return track.Height;
            var content = Viewport + Extent;
            var h = (int)(track.Height * (double)Viewport / content);
            return Math.Clamp(h, 12, track.Height);
        }
    }

    private NyxRect ThumbRect
    {
        get
        {
            var track = TrackRect;
            var th = ThumbHeight;
            if (Extent <= 0 || th >= track.Height)
                return track;
            var y = track.Y + (int)((track.Height - th) * (double)Value / Extent);
            return new NyxRect(track.X, y, track.Width, th);
        }
    }

    public override void OnMouseMove(int x, int y)
    {
        var inside = HitTest(x, y);
        _hoverTrack = inside;
        _hoverThumb = inside && ThumbRect.Contains(x, y);
        if (!_hoverTrack)
            _hoverThumb = false;

        if (_dragThumb && Extent > 0)
        {
            var track = TrackRect;
            var th = ThumbHeight;
            var range = Math.Max(1, track.Height - th);
            var dy = y - _dragStartY;
            var dv = (int)Math.Round(dy * (double)Extent / range);
            SetValue(_valueAtDragStart + dv);
        }
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        if (!HitTest(x, y))
            return;

        var tr = ThumbRect;
        if (tr.Contains(x, y))
        {
            _dragThumb = true;
            _dragStartY = y;
            _valueAtDragStart = Value;
            return;
        }

        var track = TrackRect;
        if (y < tr.Y)
            SetValue(Value - Viewport);
        else if (y > tr.Bottom)
            SetValue(Value + Viewport);
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button) => _dragThumb = false;

    public override void OnMouseWheel(int x, int y, int delta)
    {
        if (!HitTest(x, y))
            return;
		var step = ScrollStep > 0 ? ScrollStep : Math.Max(16, Viewport / 8);
        if (delta > 0)
            SetValue(Value - step);
        else if (delta < 0)
            SetValue(Value + step);
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible)
            return;

        var enabled = Enabled && Extent > 0;
        var track = enabled ? theme.ScrollTrack : theme.ScrollTrackDisabled;
        painter.FillRect(TrackRect, Tint(track));

        var thumb = ThumbRect;
        var thumbColor = enabled
            ? (_hoverThumb || _dragThumb ? theme.ScrollThumbHover : theme.ScrollThumb)
            : theme.ScrollThumbDisabled;
        painter.FillRect(thumb, Tint(thumbColor));
        painter.DrawRect(Bounds, Tint(theme.PanelBorder), 1);
    }
}
