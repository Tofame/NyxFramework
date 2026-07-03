namespace NyxGui;

/// <summary>Horizontal scrollbar; mirrors <c>GUI_HScrollBar</c> (simplified interaction).</summary>
public sealed class NyxHScrollBar : NyxElement
{
    private bool _hoverTrack;
    private bool _hoverThumb;
    private bool _dragThumb;
    private int _dragStartX;
    private int _valueAtDragStart;

    public NyxHScrollBar(NyxRect bounds, uint internalId = 0)
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

    private int ThumbWidth
    {
        get
        {
            var track = TrackRect;
            if (track.Width <= 0)
                return 0;
            if (Extent <= 0)
                return track.Width;
            var content = Viewport + Extent;
            var w = (int)(track.Width * (double)Viewport / content);
            return Math.Clamp(w, 12, track.Width);
        }
    }

    private NyxRect ThumbRect
    {
        get
        {
            var track = TrackRect;
            var tw = ThumbWidth;
            if (Extent <= 0 || tw >= track.Width)
                return track;
            var x = track.X + (int)((track.Width - tw) * (double)Value / Extent);
            return new NyxRect(x, track.Y, tw, track.Height);
        }
    }

    public override void OnMouseMove(int x, int y)
    {
        var inside = HitTest(x, y);
        _hoverTrack = inside;
        var tr = ThumbRect;
        _hoverThumb = inside && tr.Contains(x, y);
        if (!_hoverTrack)
            _hoverThumb = false;

        if (_dragThumb && Extent > 0)
        {
            var track = TrackRect;
            var tw = ThumbWidth;
            var range = Math.Max(1, track.Width - tw);
            var dx = x - _dragStartX;
            var dv = (int)Math.Round(dx * (double)Extent / range);
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
            _dragStartX = x;
            _valueAtDragStart = Value;
            return;
        }

        var track = TrackRect;
        if (x < tr.X)
            SetValue(Value - Viewport);
        else if (x > tr.Right)
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

        painter.FillRect(TrackRect, theme.ScrollTrack);
        var thumb = ThumbRect;
        painter.FillRect(thumb, _hoverThumb || _dragThumb ? theme.ScrollThumbHover : theme.ScrollThumb);
        painter.DrawRect(Bounds, theme.PanelBorder, 1);
    }
}
