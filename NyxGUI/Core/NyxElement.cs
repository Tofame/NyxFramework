using NyxGui.Definitions;

namespace NyxGui;

/// <summary>
/// Invalidation flags for the widget tree. Coalesced per-frame by <see cref="NyxGuiBuiltDocument.Flush"/>.
/// </summary>
[Flags]
public enum NyxInvalidationFlags
{
    None = 0,
    Layout = 1 << 0,
    Style = 1 << 1,
    Render = 1 << 2,
}

/// <summary>
/// Abstract base for all widgets. Holds identity, bounds, state, style, input hooks, and paint.
/// Widgets emit paint through <see cref="INyxGuiPainter"/> — never direct OpenGL calls.
/// </summary>
public abstract class NyxElement
{
    private NyxWidgetVisual _paintVisual;
    private bool _paintVisualActive;
    private long _tooltipHoverSinceMs = -1;
    private NyxInvalidationFlags _invalidation;

    /// <summary>Per-state visual overrides (hover, pressed, focused, disabled, etc.).</summary>
    public NyxWidgetStates States { get; } = new();

    protected NyxElement(uint internalId = 0)
    {
        InternalId = internalId;
    }

    // ── Identity ──────────────────────────────────────────────────────

    public uint InternalId { get; set; }

    /// <summary>Logical id for lookups, binding targets, and event handler registration.</summary>
    public string? Id { get; set; }

    // ── Tree ──────────────────────────────────────────────────────────

    public NyxElement? Parent { get; internal set; }

    /// <summary>True if this widget is attached to a tree (has a root ancestor).</summary>
    public bool IsAttached { get; internal set; }

    // ── Bounds ────────────────────────────────────────────────────────

    /// <summary>Final rectangle in screen space, assigned by parent during Arrange.</summary>
    public NyxRect Bounds { get; internal set; }

    /// <summary>Desired size computed during Measure. Set by the widget, read by parent containers.</summary>
    public NyxSize DesiredSize { get; protected internal set; }

    public virtual void SetBounds(NyxRect bounds) => Bounds = bounds;

    // ── Visibility / Interaction ──────────────────────────────────────

    private bool _visible = true;

    /// <summary>When false, the widget is not painted and does not participate in hit-testing.</summary>
    public bool Visible
    {
        get => _visible;
        set
        {
            if (_visible == value) return;
            _visible = value;
            InvalidateLayout();
        }
    }

    /// <summary>When false, no pointer input; paint uses the disabled state.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When true, the widget is drawn but does not participate in hit-testing (clicks pass through).</summary>
    public bool Phantom { get; set; }

    /// <summary>When true, the widget can receive keyboard focus via click.</summary>
    public bool Focusable { get; set; }

    // ── Layout ────────────────────────────────────────────────────────

    /// <summary>Optional anchor-based positioning. Only for free-floating panels; containers use Measure/Arrange instead.</summary>
    public NyxLayoutBox? LayoutBox { get; set; }

	/// <summary>Determines how width and height are calculated (content-box or border-box).</summary>
	public NyxBoxSizing BoxSizing { get; set; } = NyxBoxSizing.ContentBox;

	/// <summary>Resolves the border width from the effective visual state.</summary>
	public int GetBorderWidth()
	{
		var visual = GetPaintVisualOrResolve();
		return visual.HasBorder ? (visual.BorderWidth ?? 0) : 0;
	}

    /// <summary>True if this widget is a root of a GUI tree.</summary>
    public virtual bool IsRoot { get; internal set; }

    /// <summary>True if this widget or any descendant contains the given point.</summary>
    public virtual bool HitTestSubtree(int x, int y) => HitTest(x, y);

    // ── Drag & Drop ───────────────────────────────────────────────────

    /// <summary>When set, this widget can initiate drag operations.</summary>
    public NyxDragSource? DragSource { get; set; }

    /// <summary>When set, this widget can receive drop operations.</summary>
    public NyxDropTarget? DropTarget { get; set; }

    /// <summary>
    /// Measure pass: compute desired size based on content.
    /// Override in leaf widgets to measure text/images. Override in containers to measure children.
    /// </summary>
    /// <param name="availableSize">Maximum size the parent can offer.</param>
    public virtual void Measure(NyxSize availableSize) => DesiredSize = NyxSize.Zero;

    /// <summary>
    /// Arrange pass: assign final bounds to this widget and position children.
    /// Override in containers to arrange children. Leaf widgets typically just set Bounds.
    /// </summary>
    /// <param name="finalRect">The rectangle assigned by the parent.</param>
    public virtual void Arrange(NyxRect finalRect) => SetBounds(finalRect);

    // ── Style ─────────────────────────────────────────────────────────

    /// <summary>CSS-like class name for theme selectors (e.g. "primary", "inventory-slot").</summary>
    public string? ThemeClass { get; set; }

    /// <summary>Opacity in 0–1 range. Multiplied with drag-opacity when an ancestor panel is being dragged.</summary>
    public float Opacity { get; set; } = 1f;

    // ── Chrome ────────────────────────────────────────────────────────

    /// <summary>Optional textured background (9-slice, tint, clip).</summary>
    public NyxImageStyle? Image { get; set; }

    /// <summary>Optional icon overlay (small texture drawn inside widget bounds).</summary>
    public NyxIconStyle? Icon { get; set; }

    // ── Text ──────────────────────────────────────────────────────────

    /// <summary>Horizontal text draw offset in pixels.</summary>
    public int TextOffsetX { get; set; }

    /// <summary>Vertical text draw offset in pixels.</summary>
    public int TextOffsetY { get; set; }

    /// <summary>Optional font override. Children inherit unset fields from ancestors.</summary>
    public NyxFontStyle? Font { get; set; }

    /// <summary>Hover tooltip text. Shown after <see cref="TooltipDelayMs"/>.</summary>
    public string? Tooltip { get; set; }

    /// <summary>Delay before <see cref="Tooltip"/> appears (default 400ms).</summary>
    public int TooltipDelayMs { get; set; } = 400;

    // ── State ─────────────────────────────────────────────────────────

    /// <summary>Pointer is over this widget (updated by input routing).</summary>
    public bool PointerInside { get; private set; }

    /// <summary>True if a pointer button is currently pressed on this widget.</summary>
    public bool PointerPressed { get; set; }

    /// <summary>Keyboard focus — uses the focused state when styled.</summary>
    public bool IsFocused { get; set; }

    /// <summary>Toggle state (NyxClient $on) — uses the on state when styled.</summary>
    public bool IsOn { get; set; }

    // ── Invalidation ──────────────────────────────────────────────────

    /// <summary>Mark this widget as needing layout recalculation. Bubbles to parent containers.</summary>
    public void InvalidateLayout()
    {
        _invalidation |= NyxInvalidationFlags.Layout;
        Parent?.InvalidateLayout();

        NyxContainer? layoutContainer = null;
        for (var n = Parent; n is not null; n = n.Parent)
        {
            if (n is NyxContainer c && c.Layout is not null)
                layoutContainer = c;
        }

        NyxGuiBuiltDocument? doc = null;
        for (var n = Parent; n is not null; n = n.Parent)
        {
            if (n is NyxContainer c && c.OwningDocument is not null)
            {
                doc = c.OwningDocument;
                break;
            }
        }

        doc?.MarkLayoutDirty(layoutContainer);
    }

    /// <summary>Mark this widget and its descendants as needing style resolution.</summary>
    public void InvalidateStyle()
    {
        _invalidation |= NyxInvalidationFlags.Style;
        InvalidateStyleRecursive();
    }

    private void InvalidateStyleRecursive()
    {
        _invalidation |= NyxInvalidationFlags.Style;
        if (this is NyxContainer container)
        {
            foreach (var child in container.Children)
                child.InvalidateStyleRecursive();
        }
    }

    /// <summary>Mark this widget as needing repaint (without layout or style changes).</summary>
    public void InvalidateRender() => _invalidation |= NyxInvalidationFlags.Render;

    internal NyxInvalidationFlags TakeInvalidation()
    {
        var flags = _invalidation;
        _invalidation = NyxInvalidationFlags.None;
        return flags;
    }

    internal bool HasInvalidation(NyxInvalidationFlags flags) => (_invalidation & flags) != 0;

    // ── Font Resolution ───────────────────────────────────────────────

    /// <summary>Walks the parent chain to resolve the effective font. Returns defaults when none set.</summary>
    public NyxFontStyle ResolveEffectiveFont()
    {
        string? file = null;
        float? size = null;
        var bold = false;
        var outlined = false;

        for (var e = this; e is not null; e = e.Parent)
        {
            if (e.Font is not { } f) continue;
            file ??= f.File;
            size ??= f.SizePt;
            bold |= f.Bold;
            outlined |= f.Outlined;
        }

        if (file is null && size is null && !bold && !outlined)
            return NyxFontStyle.Default;

        return new NyxFontStyle { File = file, SizePt = size, Bold = bold, Outlined = outlined };
    }

    protected NyxFontStyle? GetPaintFont()
    {
        var f = ResolveEffectiveFont();
        return f.IsDefault ? null : f;
    }

    /// <summary>Bounds shifted by <see cref="TextOffsetX"/> / <see cref="TextOffsetY"/> for DrawText.</summary>
    protected NyxRect TextLayoutBounds => new(Bounds.X + TextOffsetX, Bounds.Y + TextOffsetY, Bounds.Width, Bounds.Height);

    // ── Hit Testing ───────────────────────────────────────────────────

    public virtual bool HitTest(int x, int y) =>
        Enabled && Visible && !Phantom && Bounds.Contains(x, y);

    // ── Paint ─────────────────────────────────────────────────────────

    /// <summary>Resolves visual chrome once for this Paint call. Returns false when invisible or fully transparent.</summary>
    protected bool TryBeginPaintVisual(out NyxWidgetVisual visual)
    {
        if (!Visible)
        {
            visual = default;
            return false;
        }

        visual = States.Resolve(
            new NyxWidgetVisualContext { Opacity = Opacity, Image = Image },
            PointerInside,
            PointerPressed,
            IsFocused,
            IsOn,
            Enabled);
        _paintVisual = visual;
        _paintVisualActive = true;

        if (EffectiveOpacity(visual) <= 0f)
        {
            EndPaintVisual();
            return false;
        }

        return true;
    }

    protected void EndPaintVisual() => _paintVisualActive = false;

    protected NyxWidgetVisual GetPaintVisualOrResolve()
    {
        if (_paintVisualActive) return _paintVisual;

        return States.Resolve(
            new NyxWidgetVisualContext { Opacity = Opacity, Image = Image },
            PointerInside, PointerPressed, IsFocused, IsOn, Enabled);
    }

    /// <summary>Combined opacity for this widget. During panel-dragging, the root applies a drag-opacity multiplier.</summary>
    protected float PaintOpacity => EffectiveOpacity(GetPaintVisualOrResolve());

    protected float EffectiveOpacity(in NyxWidgetVisual visual)
    {
        var o = Math.Clamp(visual.Opacity, 0f, 1f);
        for (var n = this; n is not null; n = n.Parent)
        {
            if (n is NyxContainer c && c.IsDragging)
            {
                var root = FindRoot();
                if (root is not null)
                {
                    o *= root.Settings.PanelDragOpacity;
                }
                break;
            }
        }
        return o;
    }

    internal bool IsUnderDragPanel(NyxElement panel)
    {
        for (var n = this; n is not null; n = n.Parent)
        {
            if (ReferenceEquals(n, panel)) return true;
        }
        return false;
    }

    protected NyxColor Tint(NyxColor color) => NyxElementPaint.WithOpacity(color, PaintOpacity);

    protected NyxColor Tint(NyxColor color, in NyxWidgetVisual visual) =>
        NyxElementPaint.WithOpacity(color, EffectiveOpacity(visual));

    protected void PaintChrome(INyxGuiPainter painter, in NyxWidgetVisual visual)
    {
        if (visual.Image is not null && !string.IsNullOrEmpty(visual.Image.ImageSource))
            PaintBackground(painter, visual);
        else if (visual.HasBackground)
            painter.FillRect(Bounds, Tint(visual.BackgroundColor!.Value, visual));

        if (visual.HasBorder)
            PaintStateBorder(painter, visual);
    }

    protected void PaintBackground(INyxGuiPainter painter, in NyxWidgetVisual visual)
    {
        if (visual.Image is null || string.IsNullOrEmpty(visual.Image.ImageSource)) return;

        var cmd = visual.Image.ToPaintCommand(Bounds, NyxColor.FromRgb(255, 255, 255), EffectiveOpacity(visual));
        painter.DrawImage(in cmd);
    }

    /// <summary>Draws <see cref="Icon"/> inside <paramref name="area"/> (defaults to <see cref="Bounds"/>).</summary>
    protected void PaintIcon(INyxGuiPainter painter, in NyxWidgetVisual visual, NyxRect? area = null)
    {
        if (Icon is not { HasSource: true } icon) return;

        var dest = icon.ComputeDestination(area ?? Bounds);
        var cmd = icon.ToPaintCommand(dest, NyxColor.FromRgb(255, 255, 255), EffectiveOpacity(visual));
        painter.DrawImage(in cmd);
    }

    protected void PaintStateBorder(INyxGuiPainter painter, in NyxWidgetVisual visual)
    {
        if (!visual.HasBorder) return;
        painter.DrawRect(Bounds, Tint(visual.BorderColor!.Value, visual), visual.BorderWidth!.Value);
    }

    // ── Input ─────────────────────────────────────────────────────────

    public virtual void OnMouseMove(int x, int y)
    {
        var wasInside = PointerInside;
        PointerInside = HitTest(x, y);
        if (!PointerInside) PointerPressed = false;

        if (IsTooltipHovered)
        {
            if (_tooltipHoverSinceMs < 0)
                _tooltipHoverSinceMs = Environment.TickCount64;
        }
        else
        {
            _tooltipHoverSinceMs = -1;
        }

        if (PointerInside != wasInside)
        {
            this.RaiseEvent(PointerInside
                ? new NyxMouseEventArgs(NyxEventType.MouseEnter, this, x, y)
                : new NyxMouseEventArgs(NyxEventType.MouseLeave, this, x, y));
        }
    }

    public virtual void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        if (HitTest(x, y))
            PointerPressed = true;

        if (button == NyxMouseButton.Left && DragSource is not null)
        {
            FindRoot()?.DragDrop.PrepareDrag(this, x, y);
        }

        this.RaiseEvent(new NyxMouseEventArgs(NyxEventType.MouseDown, this, x, y, button));
    }

    public virtual void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        PointerPressed = false;
        this.RaiseEvent(new NyxMouseEventArgs(NyxEventType.MouseUp, this, x, y, button));
    }

    public virtual void OnMouseWheel(int x, int y, int delta)
    {
        this.RaiseEvent(new NyxMouseWheelEventArgs(this, x, y, delta));
    }

    /// <summary>Invoked on right-button release while the pointer is inside <see cref="Bounds"/>.</summary>
    public Action<int, int>? RightClicked { get; set; }

    public virtual void OnRightButtonDown(int x, int y) { }

    public virtual void OnRightButtonUp(int x, int y)
    {
        if (HitTest(x, y))
        {
            RightClicked?.Invoke(x, y);
            this.RaiseEvent(new NyxClickEventArgs(NyxEventType.RightClick, this, x, y));
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────

    /// <summary>Called when the widget is added to a tree. Parent is set.</summary>
    protected internal virtual void OnAttached()
    {
        IsAttached = true;
    }

    /// <summary>Called when the widget is removed from a tree. Parent is cleared.</summary>
    protected internal virtual void OnDetached()
    {
        IsAttached = false;
        this.ClearHandlers();
    }

    /// <summary>Main paint entry. Emit draw commands via <paramref name="painter"/>.</summary>
    public abstract void Paint(INyxGuiPainter painter, NyxGuiTheme theme);

    // ── Internal ──────────────────────────────────────────────────────

    public virtual bool IsTooltipHovered => PointerInside && !string.IsNullOrEmpty(Tooltip);

    public long TooltipHoverSinceMs => _tooltipHoverSinceMs;

    public virtual void PaintTooltipPopup(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (string.IsNullOrEmpty(Tooltip)) return;

        NyxTooltip.PaintPopup(
            painter, theme, Bounds, Tooltip, GetPaintFont(),
            TooltipDelayMs, _tooltipHoverSinceMs, PointerInside);
    }

    public NyxContainer? FindRoot()
    {
        for (var n = this; n is not null; n = n.Parent)
        {
            if (n is NyxContainer c && c.IsRoot) return c;
        }
        return null;
    }
}

/// <summary>Integer size (width, height).</summary>
public readonly record struct NyxSize(int Width, int Height)
{
    public static NyxSize Zero => new(0, 0);
}

/// <summary>Mouse button identifier for input events.</summary>
public enum NyxMouseButton { Left, Right, Middle }
