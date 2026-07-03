using NyxGui.Definitions;

namespace NyxGui;

/// <summary>
/// Mini window with built-in chrome: title bar, close/minimize buttons, optional resize grip.
/// Replaces the old NyxMiniWindow which relied on TOML chrome and Sandbox behavior.
/// </summary>
public sealed class NyxMiniWindow : NyxContainer
{
    private readonly NyxContainer _body;

    private enum InteractionMode { None, Dragging, Resizing }

    private InteractionMode _mode;
    private NyxRect _expandedBounds;
    private int _dragOffsetX;
    private int _dragOffsetY;
    private int _resizeStartY;
    private int _resizeStartHeight;
    private int _pointerX;
    private int _pointerY;
    private bool _closePressed;
    private bool _minimizePressed;
    private bool _lockPressed;
    private bool _suppressBoundsEvent;

    public NyxMiniWindow(string? id = null) : base(0)
    {
        Id = id;
        _body = new NyxContainer();
        AddChild(_body);
        Draggable = true;
        Resizable = true;
        TitleBarHeight = 22;
        ShowCloseButton = true;
        ShowMinimizeButton = true;
        ShowLockButton = true;
    }

    // ── Body ──────────────────────────────────────────────────────────

    /// <summary>Container for window content. Clipped to the area below the title bar.</summary>
    public NyxContainer Body => _body;

    // ── Appearance ────────────────────────────────────────────────────

    /// <summary>Title bar caption.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Logical title bar height.</summary>
    public int TitleBarHeight { get; set; } = 22;

    /// <summary>When true, shows a close button in the title bar.</summary>
    public bool ShowCloseButton { get; set; }

    /// <summary>When true, shows a minimize button in the title bar.</summary>
    public bool ShowMinimizeButton { get; set; }

    /// <summary>When true, shows a lock button in the title bar.</summary>
    public bool ShowLockButton { get; set; }

    /// <summary>When true, a bottom drag handle can increase window height.</summary>
    public bool Resizable { get; set; }

    /// <summary>
    /// Controls auto-sizing behavior.
    /// <list type="bullet">
    ///   <item><see langword="null"/> — auto-size when no fixed_height and no bottom anchor (default).</item>
    ///   <item><see langword="true"/> — always auto-size to content height.</item>
    ///   <item><see langword="false"/> — never auto-size; use explicit bounds only.</item>
    /// </list>
    /// </summary>
    public bool? AutoSize { get; set; }

    /// <summary>Hit area height for the bottom resize strip.</summary>
    public int ResizeGripHitHeight { get; set; } = 6;

    /// <summary>Minimum expanded height when resizing.</summary>
    public int MinExpandedHeight { get; set; }

    /// <summary>Maximum expanded height when resizing. 0 = unconstrained.</summary>
    public int MaxExpandedHeight { get; set; }

    // ── State ─────────────────────────────────────────────────────────

    /// <summary>When true, the window is locked and cannot be dragged.</summary>
    public bool Locked { get; set; }

    /// <summary>True while this window is being dragged.</summary>
    public override bool IsDragging => _mode == InteractionMode.Dragging;

    /// <summary>True while the user is resizing the window height.</summary>
    public bool IsResizingHeight => _mode == InteractionMode.Resizing;

    /// <summary>True when the window is minimized to title-bar only.</summary>
    public bool Minimized { get; private set; }

    // ── Events ────────────────────────────────────────────────────────

    public new event EventHandler<NyxMiniWindowBoundsEventArgs>? DragEnded;
    public event EventHandler<NyxMiniWindowBoundsEventArgs>? DragProgress;
    public event EventHandler<NyxMiniWindowBoundsEventArgs>? BoundsChanged;
    public event EventHandler<NyxMiniWindowBoundsEventArgs>? ResizeEnded;
    public event EventHandler? Closed;
    public event EventHandler? MinimizedChanged;

// ── Bounds ────────────────────────────────────────────────────────

    public override void SetBounds(NyxRect bounds)
    {
        if (!Minimized)
        {
            var minH = Math.Max(TitleBarHeight + 1, MinExpandedHeight);
            if (bounds.Height < minH)
                bounds = new NyxRect(bounds.X, bounds.Y, bounds.Width, minH);
            _expandedBounds = bounds;
        }

        // Bypass NyxContainer.SetBounds which delta-shifts children.
        // Body position is computed from content bounds, and body children
        // are re-resolved by SyncBodyLayout instead of being shifted.
        Bounds = bounds;

        SyncBodyLayout();
        if (!_suppressBoundsEvent)
            BoundsChanged?.Invoke(this, new NyxMiniWindowBoundsEventArgs(bounds));
    }

    /// <summary>
    /// Synchronises the body container bounds and re-resolves child layouts.
    ///
    /// First computes the content bounds (client area below the title bar) and sets
    /// them on <see cref="_body"/>.  Then looks up the owning document; if found,
    /// uses the full <see cref="NyxLayoutResolver.RelayoutContainer"/> with the document's
    /// widget map; otherwise does a lighter parent-anchored-only relayout.
    /// </summary>
    private void SyncBodyLayout()
    {
        if (Minimized) return;
        var content = GetContentBounds();
        _body.SetBoundsSilently(content);
        _body.Visible = content.Height > 0;

        if (!_body.Visible) return;

        var doc = FindOwningDocument();
        if (doc is not null)
        {
            var env = new NyxGuiLayoutEnvironment
            {
                WindowBounds = doc.WindowBounds,
                RootWindowAnchorId = doc.RootWindowAnchorId,
            };
            NyxLayoutResolver.RelayoutContainer(content, _body, doc.WidgetMap, env);
        }
        else
        {
            var widgetsById = new System.Collections.Generic.Dictionary<string, NyxElement>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var child in _body.Children)
            {
                if (!System.String.IsNullOrEmpty(child.Id))
                    widgetsById[child.Id] = child;
            }
            var env = new NyxGuiLayoutEnvironment
            {
                WindowBounds = content,
                RootWindowAnchorId = NyxGuiRootWindow.DefaultAnchorId,
            };
            NyxLayoutResolver.RelayoutContainer(content, _body, widgetsById, env);
        }
    }

    private NyxGuiBuiltDocument? FindOwningDocument()
    {
        for (var n = (NyxElement?)this; n is not null; n = n.Parent)
        {
            if (n is NyxContainer c && c.OwningDocument is not null)
                return c.OwningDocument;
        }
        return null;
    }

    /// <summary>True when this window should auto-size to content height.</summary>
    public bool AutoSized
    {
        get
        {
            if (AutoSize.HasValue) return AutoSize.Value;
            return LayoutBox is null
                || LayoutBox.Bottom is null && LayoutBox.FixedHeight <= 0 && !LayoutBox.FixedSize;
        }
    }

    private bool NeedsAutoSize()
    {
        if (AutoSize.HasValue) return AutoSize.Value;
        if (LayoutBox is null)
            return true;
        if (LayoutBox.Bottom is not null)
            return false;
        if (LayoutBox.FixedHeight > 0 || LayoutBox.FixedSize)
            return false;
        return true;
    }

/// <summary>True when the layout resolver should auto-size this window.</summary>
    public bool NeedsAutoSizeFromLayout()
    {
        if (Minimized) return false;
        if (!NeedsAutoSize()) return false;
        var content = GetContentBounds();
        if (content.Height <= 0) return true;
        var needed = MeasureContentHeight();
        return needed > content.Height;
    }

/// <summary>
/// Expands the window height to fit all body children.
///
/// Uses a two-pass approach:
/// 1. Lay out children with a tall temporary area (10000px) so all anchors, including
///    bottom-anchored children, resolve correctly.
/// 2. Measure the lowest child that is NOT anchored to parent.bottom (stretching children
///    just fill whatever height we give them, so they are excluded from AutoSize measurement).
///    The measured height + title bar + resize grip becomes the new window height.
/// </summary>
    public void AutoSizeToContent()
    {
        if (_body.Children.Count == 0) return;

        var gripH = Resizable ? ResizeGripHitHeight : 0;

        // Two-pass approach:
        // 1. Lay out with a tall temp area so all anchors (including bottom anchors) resolve.
        //    Children anchored to parent.bottom will stretch to fill the tall area.
        // 2. Find the lowest non-stretching child — this is our desired content height.
        //    Stretching children (anchored to parent.bottom) are excluded since they just fill whatever we give.
        var content = GetContentBounds();
        if (content.Width <= 0)
            content = new NyxRect(Bounds.X, Bounds.Y + TitleBarHeight, Bounds.Width, 100);
        var tempContent = new NyxRect(content.X, content.Y, content.Width, 10000);
        _body.SetBoundsSilently(tempContent);
        _body.Visible = true;

        var doc = FindOwningDocument();
        if (doc is not null)
        {
            var env = new NyxGuiLayoutEnvironment
            {
                WindowBounds = doc.WindowBounds,
                RootWindowAnchorId = doc.RootWindowAnchorId,
            };
            NyxLayoutResolver.RelayoutContainer(tempContent, _body, doc.WidgetMap, env);
        }

        // Measure from children that are NOT bottom-stretching.
        var neededHeight = MeasureTopChainedHeight();

        if (neededHeight <= 0)
            neededHeight = 100; // fallback

        var newHeight = TitleBarHeight + gripH + neededHeight;
        if (MaxExpandedHeight > 0 && newHeight > MaxExpandedHeight)
            newHeight = MaxExpandedHeight;
        if (MinExpandedHeight > 0 && newHeight < MinExpandedHeight)
            newHeight = MinExpandedHeight;
        var newBounds = new NyxRect(Bounds.X, Bounds.Y, Bounds.Width, newHeight);
        _expandedBounds = newBounds;
        _suppressBoundsEvent = true;
        SetBounds(newBounds);
        _suppressBoundsEvent = false;
    }

    /// <summary>
    /// Measures content height from children that are NOT bottom-stretching.
    /// Children anchored to <c>parent.bottom</c> always stretch to fill available height,
    /// so they are excluded from the measurement.  If ALL children are bottom-stretching,
    /// the measurement falls back to the max of all children.
    /// </summary>
    private int MeasureTopChainedHeight()
    {
        if (_body.Children.Count == 0) return 0;

        var bodyTop = _body.Bounds.Y;
        var maxBottom = 0;

        foreach (var child in _body.Children)
        {
            if (!child.Visible) continue;
            // Skip children anchored to parent.bottom — they stretch to fill.
            if (child.LayoutBox?.Bottom is { } bottom
                && bottom.TargetId?.Equals("parent", StringComparison.OrdinalIgnoreCase) == true
                && bottom.Edge == NyxAnchorEdge.Bottom)
                continue;
            var childBottom = child.Bounds.Y + child.Bounds.Height - bodyTop;
            if (childBottom > maxBottom)
                maxBottom = childBottom;
        }

        // If ALL children stretch to bottom, measure from the first one's top + a fixed height.
        if (maxBottom <= 0)
        {
            foreach (var child in _body.Children)
            {
                if (!child.Visible) continue;
                var childBottom = child.Bounds.Y + child.Bounds.Height - bodyTop;
                if (childBottom > maxBottom)
                    maxBottom = childBottom;
            }
        }

        return maxBottom;
    }

    /// <summary>Measures the minimum content height needed to fit all body children.</summary>
    public int MeasureContentHeight()
    {
        var bodyY = _body.Bounds.Y;
        var bottom = 0;
        foreach (var c in _body.Children)
        {
            if (!c.Visible) continue;
            var cb = c.Bounds;
            var childBottom = cb.Y + cb.Height - bodyY;
            if (childBottom > bottom)
                bottom = childBottom;
        }
        return bottom;
    }

    private static void RelayoutParentAnchoredOnly(NyxContainer container)
    {
        var parent = container.Bounds;
        var ctx = new NyxLayoutContext { ParentBounds = parent };
        foreach (var child in container.Children)
        {
            if (child is NyxScrollablePanel scroll)
            {
                scroll.RefreshLayout();
                continue;
            }

            if (child.LayoutBox is { HasAnyAnchor: true } box && !box.GetWidgetDependencies().Any())
                child.SetBounds(NyxLayoutResolver.Resolve(parent, child, ctx));

            if (child is NyxContainer nested)
                RelayoutParentAnchoredOnly(nested);
        }
    }

    public void SetMinimized(bool minimized)
    {
        if (Minimized == minimized) return;
        Minimized = minimized;
        IsOn = minimized;

        if (minimized)
        {
            if (_expandedBounds.Height <= TitleBarHeight)
                _expandedBounds = Bounds;
            _body.Visible = false;
            Bounds = new NyxRect(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
        }
        else
        {
            _body.Visible = true;
            if (_expandedBounds.Height > TitleBarHeight)
                SetBounds(_expandedBounds);
        }

        MinimizedChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Input ─────────────────────────────────────────────────────────

    public override void OnMouseMove(int x, int y)
    {
        _pointerX = x;
        _pointerY = y;

        switch (_mode)
        {
            case InteractionMode.Resizing:
                ApplyHeightResize(y);
                return;
            case InteractionMode.Dragging:
                ApplyDrag(x, y);
                return;
        }

        if (!Visible) return;
        base.OnMouseMove(x, y);
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        if (!Visible) return;

        if (button != NyxMouseButton.Left)
        {
            base.OnMouseDown(x, y, button);
            return;
        }

        // Check resize grip first
        if (Resizable && !Minimized && HitTestResizeGrip(x, y))
        {
            BeginHeightResize(y);
            return;
        }

        // Check chrome buttons
        if (ShowCloseButton && HitTestCloseButton(x, y))
        {
            _closePressed = true;
            return;
        }
        if (ShowMinimizeButton && HitTestMinimizeButton(x, y))
        {
            _minimizePressed = true;
            return;
        }
        if (ShowLockButton && HitTestLockButton(x, y))
        {
            _lockPressed = true;
            return;
        }

        // Check title bar for drag (only if not locked)
        if (Draggable && !Locked && HitTestTitleBar(x, y))
        {
            BeginDrag(x, y);
            return;
        }

        base.OnMouseDown(x, y, button);
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        switch (_mode)
        {
            case InteractionMode.Resizing:
                EndHeightResize();
                return;
            case InteractionMode.Dragging:
                EndDrag();
                break;
        }

        if (_closePressed && HitTestCloseButton(x, y))
            Close();
        _closePressed = false;

        if (_minimizePressed && HitTestMinimizeButton(x, y))
            ToggleMinimize();
        _minimizePressed = false;

        if (_lockPressed && HitTestLockButton(x, y))
            Locked = !Locked;
        _lockPressed = false;

        base.OnMouseUp(x, y, button);
    }

    public override void OnMouseWheel(int x, int y, int delta)
    {
        if (!Minimized && _body.Bounds.Contains(x, y))
            _body.OnMouseWheel(x, y, delta);
    }

    // ── Drag ──────────────────────────────────────────────────────────

    private void BeginDrag(int x, int y)
    {
        _mode = InteractionMode.Dragging;
        _dragOffsetX = x - Bounds.X;
        _dragOffsetY = y - Bounds.Y;
    }

    private void ApplyDrag(int x, int y)
    {
        _suppressBoundsEvent = true;
        try
        {
            SetBounds(new NyxRect(x - _dragOffsetX, y - _dragOffsetY, Bounds.Width, Bounds.Height));
        }
        finally
        {
            _suppressBoundsEvent = false;
        }
        DragProgress?.Invoke(this, new NyxMiniWindowBoundsEventArgs(Bounds));
    }

    private void EndDrag()
    {
        _mode = InteractionMode.None;
        BoundsChanged?.Invoke(this, new NyxMiniWindowBoundsEventArgs(Bounds));
        DragEnded?.Invoke(this, new NyxMiniWindowBoundsEventArgs(Bounds));
    }

    // ── Resize ────────────────────────────────────────────────────────

    private void BeginHeightResize(int pointerY)
    {
        if (!Resizable || Minimized) return;
        _mode = InteractionMode.Resizing;
        _resizeStartY = pointerY;
        _resizeStartHeight = Bounds.Height;
    }
    private void ApplyHeightResize(int pointerY)
    {
        var delta = pointerY - _resizeStartY;
        var height = Math.Max(GetMinExpandedHeight(), _resizeStartHeight + delta);
        height = Math.Min(height, GetMaxExpandedHeight());
        SetBounds(new NyxRect(Bounds.X, Bounds.Y, Bounds.Width, height));
    }
    private void EndHeightResize()
    {
        _mode = InteractionMode.None;
        BoundsChanged?.Invoke(this, new NyxMiniWindowBoundsEventArgs(Bounds));
        ResizeEnded?.Invoke(this, new NyxMiniWindowBoundsEventArgs(Bounds));
    }

    private int GetMinExpandedHeight()
    {
        if (MinExpandedHeight > 0) return MinExpandedHeight;
        return TitleBarHeight + 24;
    }

    private int GetMaxExpandedHeight()
    {
        return MaxExpandedHeight > 0 ? MaxExpandedHeight : int.MaxValue;
    }

    // ── Hit Testing ───────────────────────────────────────────────────

    private bool HitTestTitleBar(int x, int y) =>
        Bounds.Contains(x, y) && y >= Bounds.Y && y < Bounds.Y + TitleBarHeight;

    private bool HitTestResizeGrip(int x, int y)
    {
        if (!Resizable || Minimized) return false;
        var gripY = Bounds.Bottom - ResizeGripHitHeight;
        return x >= Bounds.X && x < Bounds.Right && y >= gripY && y < Bounds.Bottom;
    }

    private const int ChromeButtonSize = 14;
    private const int ChromeButtonGap = 4;

    private bool HitTestCloseButton(int x, int y)
    {
        if (!ShowCloseButton) return false;
        var btnX = Bounds.Right - ChromeButtonSize - 4;
        var btnY = Bounds.Y + (TitleBarHeight - ChromeButtonSize) / 2;
        return x >= btnX && x < btnX + ChromeButtonSize && y >= btnY && y < btnY + ChromeButtonSize;
    }

    private bool HitTestMinimizeButton(int x, int y)
    {
        if (!ShowMinimizeButton) return false;
        var btnX = Bounds.Right - ChromeButtonSize - 4 - ChromeButtonSize - (ShowCloseButton ? ChromeButtonGap : 0);
        var btnY = Bounds.Y + (TitleBarHeight - ChromeButtonSize) / 2;
        return x >= btnX && x < btnX + ChromeButtonSize && y >= btnY && y < btnY + ChromeButtonSize;
    }

    private bool HitTestLockButton(int x, int y)
    {
        if (!ShowLockButton) return false;
        var btnX = Bounds.Right - ChromeButtonSize - 4
            - ChromeButtonSize - (ShowCloseButton ? ChromeButtonGap : 0)
            - ChromeButtonSize - (ShowMinimizeButton ? ChromeButtonGap : 0);
        var btnY = Bounds.Y + (TitleBarHeight - ChromeButtonSize) / 2;
        return x >= btnX && x < btnX + ChromeButtonSize && y >= btnY && y < btnY + ChromeButtonSize;
    }

    private bool HitTestChromeButtons(int x, int y) =>
        HitTestCloseButton(x, y) || HitTestMinimizeButton(x, y) || HitTestLockButton(x, y);

    /// <summary>
    /// Resolves the button sprite path from the window's image source directory.
    /// If <see cref="NyxElement.Image"/> has an <c>ImageSource</c>, the button sprite
    /// is expected as <c>miniwindow_buttons.png</c> in the same directory.
    /// Otherwise falls back to the bare filename.
    /// </summary>
    private string? GetButtonSpritePath()
    {
        if (Image?.ImageSource is { } imgSrc)
        {
            var dir = System.IO.Path.GetDirectoryName(imgSrc);
            if (!string.IsNullOrEmpty(dir))
                return System.IO.Path.Combine(dir, "miniwindow_buttons.png");
        }
        return "miniwindow_buttons.png";
    }

    private void PaintButtonSprite(INyxGuiPainter painter, NyxRect destRect, int col, bool isHovered, bool isPressed, in NyxWidgetVisual visual)
    {
        var row = isPressed ? 2 : isHovered ? 1 : 0;
        var srcRect = new NyxRect(col * ChromeButtonSize, row * ChromeButtonSize, ChromeButtonSize, ChromeButtonSize);
        var cmd = new NyxImagePaintCommand(destRect, GetButtonSpritePath() ?? string.Empty, srcRect, null, default, fixedRatio: false, smooth: false, Tint(NyxColor.FromRgb(255, 255, 255), visual));
        painter.DrawImage(in cmd);
    }

    // ── Paint ─────────────────────────────────────────────────────────

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            PaintChrome(painter, visual);

            // Icon
            PaintIcon(painter, visual);

            // Title (shifted right if icon present)
            if (!string.IsNullOrEmpty(Title))
            {
                var color = Tint(theme.TextPrimary, visual);
                var textX = Bounds.X + 4;
                if (Icon is { HasSource: true } ic)
                {
                    var iconDest = ic.ComputeDestination(Bounds);
                    textX = iconDest.Right + 4;
                }
                var titleWidth = Math.Max(0, Bounds.Width - (textX - Bounds.X) - 44);
                var titleRect = new NyxRect(textX, Bounds.Y + 3, titleWidth, TitleBarHeight - 6);
                painter.DrawText(titleRect, Title, NyxTextAlign.TopLeft, color, GetPaintFont());
            }

            // Chrome buttons
            PaintChromeButtons(painter, visual, theme);

            // Body (clipped)
            if (!Minimized)
            {
                var content = GetContentBounds();
                if (content.Width > 0 && content.Height > 0)
                {
                    _body.SetBoundsSilently(content);
                    painter.PushClip(content);
                    try
                    {
                        _body.Paint(painter, theme);
                    }
                    finally
                    {
                        painter.PopClip();
                    }
                }
            }

            // Resize grip
            if (Resizable && !Minimized)
                PaintResizeGrip(painter, visual, theme);

            NyxTooltipRouting.PaintActiveTooltip(this, painter, theme);
        }
        finally
        {
            EndPaintVisual();
        }
    }

    /// <summary>
    /// Paints the close/minimize/lock buttons on the title bar.
    /// Buttons are positioned right-to-left from the window's right edge:
    /// close (col 2) → minimize (col 0) → lock (col 7/8 for closed/open).
    /// Each button uses a 3-row sprite sheet: row 0=normal, 1=hover, 2=pressed,
    /// sampled from a shared <c>miniwindow_buttons.png</c> next to the window's image source.
    /// </summary>
    private void PaintChromeButtons(INyxGuiPainter painter, in NyxWidgetVisual visual, NyxGuiTheme theme)
    {
        var btnY = Bounds.Y + (TitleBarHeight - ChromeButtonSize) / 2;

        // Compute positions right-to-left: close, minimize, lock
        int rightEdge = Bounds.Right - 4;
        int closeX = 0, minimizeX = 0, lockX = 0;
        if (ShowCloseButton) { closeX = rightEdge - ChromeButtonSize; rightEdge = closeX - ChromeButtonGap; }
        if (ShowMinimizeButton) { minimizeX = rightEdge - ChromeButtonSize; rightEdge = minimizeX - ChromeButtonGap; }
        if (ShowLockButton) { lockX = rightEdge - ChromeButtonSize; rightEdge = lockX - ChromeButtonGap; }

        // Lock button (col 7=closed, col 8=open)
        if (ShowLockButton)
        {
            var btnRect = new NyxRect(lockX, btnY, ChromeButtonSize, ChromeButtonSize);
            var isHovered = PointerInside && btnRect.Contains(_pointerX, _pointerY);
            var isPressed = isHovered && PointerPressed;
            var col = Locked ? 7 : 8;
            PaintButtonSprite(painter, btnRect, col, isHovered, isPressed, visual);
        }

        // Minimize button (col 0)
        if (ShowMinimizeButton)
        {
            var btnRect = new NyxRect(minimizeX, btnY, ChromeButtonSize, ChromeButtonSize);
            var isHovered = PointerInside && btnRect.Contains(_pointerX, _pointerY);
            var isPressed = isHovered && PointerPressed;
            PaintButtonSprite(painter, btnRect, 0, isHovered, isPressed, visual);
        }

        // Close button (col 2)
        if (ShowCloseButton)
        {
            var btnRect = new NyxRect(closeX, btnY, ChromeButtonSize, ChromeButtonSize);
            var isHovered = PointerInside && btnRect.Contains(_pointerX, _pointerY);
            var isPressed = isHovered && PointerPressed;
            PaintButtonSprite(painter, btnRect, 2, isHovered, isPressed, visual);
        }
    }

    private void PaintResizeGrip(INyxGuiPainter painter, in NyxWidgetVisual visual, NyxGuiTheme theme)
    {
        var gripY = Bounds.Bottom - ResizeGripHitHeight;
        var gripRect = new NyxRect(Bounds.X, gripY, Bounds.Width, ResizeGripHitHeight);
        var hovered = (PointerInside && gripRect.Contains(_pointerX, _pointerY)) || _mode == InteractionMode.Resizing;
        if (!hovered)
            return;

        var lineY = gripY + ResizeGripHitHeight / 2;
        var lineX = Bounds.X + 4;
        var lineWidth = Math.Max(0, Bounds.Width - 8);
        if (lineWidth > 0)
            painter.FillRect(new NyxRect(lineX, lineY, lineWidth, 1), Tint(NyxColor.FromRgb(255, 255, 255), visual));
    }

    /// <summary>Client area below the title bar.</summary>
    public NyxRect GetContentBounds()
    {
        var top = Bounds.Y + TitleBarHeight;
        var height = Math.Max(0, Bounds.Height - TitleBarHeight);
        if (Resizable && !Minimized)
            height = Math.Max(0, height - ResizeGripHitHeight);
        return new NyxRect(Bounds.X, top, Bounds.Width, height);
    }

    // ── Chrome Button Actions ─────────────────────────────────────────

    /// <summary>Called when the close button is clicked.</summary>
    public void Close()
    {
        Visible = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called when the minimize button is clicked.</summary>
    public void ToggleMinimize() => SetMinimized(!Minimized);
}

public sealed class NyxMiniWindowBoundsEventArgs : EventArgs
{
    public NyxMiniWindowBoundsEventArgs(NyxRect bounds) => Bounds = bounds;
    public NyxRect Bounds { get; }
}
