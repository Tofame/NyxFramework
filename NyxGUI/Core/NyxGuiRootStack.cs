namespace NyxGui;

/// <summary>
/// Multi-root input and paint router.
///
/// Roots are painted in insertion order (earlier = below, later = above).
/// Input is tested top-to-bottom: the last-added visible root containing the point
/// receives events.
///
/// <b>Pointer capture:</b> while a mouse button is held, all events route to the
/// root that received the initial press (<c>_capture</c>).  This prevents other roots
/// from stealing input during drag or button-hold.  Capture is released on button-up.
///
/// <b>Double-click detection:</b> <c>_lastLeftClickTime</c>, <c>_lastLeftClickX</c>,
/// and <c>_lastLeftClickY</c> track the most recent click.  If a subsequent click
/// arrives within <c>DoubleClickThresholdMs</c> (250ms) and within
/// <c>DoubleClickDistanceThreshold</c> (4 pixels), a <c>NyxEventType.DoubleClick</c>
/// is raised.  After a double-click, the time is reset to 0 to prevent triple-click
/// from being treated as a second double-click.
///
/// <b>Drag-drop:</b> before processing normal mouse events, the dispatch checks
/// <c>DragDrop.IsActive</c>.  If a drag is in progress, mouse-move updates the
/// ghost position and right-click cancels the drag.  Left-up attempts a drop
/// (<c>DragDrop.TryDrop</c>).
///
/// <b>_capture vs _leftPressedElement:</b>
/// <c>_capture</c> is the root that owns the current mouse-down (per-root capture
/// for multi-root stacks).  <c>_leftPressedElement</c> is the specific child element
/// that received the left-down, used to ensure the same element receives the
/// corresponding left-up (even if the pointer moved to a different element).
/// </summary>
public sealed class NyxGuiRootStack
{
    private readonly List<Layer> _layers = new();
    private NyxElement? _capture;
    private bool _prevLeftPressed;
    private bool _prevRightPressed;
	private NyxElement? _leftPressedElement;
	private NyxElement? _rightPressedElement;
	private long _lastLeftClickTime;
	private int _lastLeftClickX;
	private int _lastLeftClickY;
	private const long DoubleClickThresholdMs = 250;
	private const int DoubleClickDistanceThreshold = 4;

    /// <summary>App-wide keyboard focus for this stack (one widget at a time).</summary>
    public NyxGuiFocus Focus { get; } = new();

    /// <summary>Registers a root. Later adds are painted/input-tested above earlier adds unless reordered.</summary>
    public void Add(NyxElement root, Func<bool>? isVisible = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        root.IsRoot = true;
        _layers.Add(new Layer(root, isVisible));
    }

    /// <summary>Removes a root from the stack.</summary>
    public void Remove(NyxElement root)
    {
        var i = _layers.FindIndex(l => ReferenceEquals(l.Root, root));
        if (i >= 0)
        {
            if (_capture == root) _capture = null;
            _layers.RemoveAt(i);
        }
    }

    /// <summary>True when any visible root contains <paramref name="x"/>, <paramref name="y"/>.</summary>
    public bool HitTest(int x, int y)
    {
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (!layer.IsVisible || !layer.Root.Visible) continue;
            if (layer.Root.HitTestSubtree(x, y)) return true;
        }
        return false;
    }

    /// <summary>Moves <paramref name="root"/> to the top of the input/paint order.</summary>
    public void BringToFront(NyxElement root)
    {
        var i = _layers.FindIndex(l => ReferenceEquals(l.Root, root));
        if (i < 0) return;
        var layer = _layers[i];
        _layers.RemoveAt(i);
        _layers.Add(layer);
    }

    /// <summary>
    /// Processes mouse input across all roots.
    ///
    /// Flow:
    /// 1. Detect edge transitions (pressed → released, released → pressed).
    /// 2. If <c>_capture</c> is active, route everything to the captured root.
    /// 3. Otherwise find the topmost visible root containing the point and dispatch.
    /// 4. While pressed, set <c>_capture</c> to lock input to that root.
    /// </summary>
    public void ProcessMouse(int x, int y, bool leftPressed, bool rightPressed, int wheelDelta)
    {
        var leftDown = leftPressed && !_prevLeftPressed;
        var leftUp = !leftPressed && _prevLeftPressed;
        var rightDown = rightPressed && !_prevRightPressed;
        var rightUp = !rightPressed && _prevRightPressed;
        _prevLeftPressed = leftPressed;
        _prevRightPressed = rightPressed;

        if (_capture is not null)
        {
            if (leftDown) UpdateFocusFromPointer(x, y);
            Dispatch(_capture, x, y, leftDown, leftUp, rightDown, rightUp, wheelDelta);
            if (leftUp || rightUp) _capture = null;
            return;
        }

        if (leftDown) UpdateFocusFromPointer(x, y);

        // Find the topmost visible root that contains the point
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (!layer.IsVisible || !layer.Root.Visible) continue;
            if (!layer.Root.HitTestSubtree(x, y)) continue;

            Dispatch(layer.Root, x, y, leftDown, leftUp, rightDown, rightUp, wheelDelta);
            if (leftPressed || rightPressed) _capture = layer.Root;
            return;
        }
    }

    /// <summary>True when an editable text entry has keyboard focus.</summary>
    public bool HasFocusedTextEntry() => Focus.HasEditableTextEntry;

    /// <summary>When true, host shortcuts should not run (e.g. panel toggle keys while typing).</summary>
    public bool CapturesGlobalShortcuts() => Focus.CapturesGlobalShortcuts;

    /// <summary>
    /// Routes keyboard events.  First raises routed <c>KeyDown</c> and <c>TextInput</c>
    /// events on the focused element for host interception; if not handled, forwards
    /// to the focused text entry's <c>HandleKey</c>.
    /// </summary>
    public void ProcessKeyboard(NyxGuiKey key, char? character = null)
    {
		var target = Focus.FocusedElement;
		if (key != NyxGuiKey.None && target is not null)
		{
			var args = new NyxKeyEventArgs(NyxEventType.KeyDown, target, key, character);
			target.RaiseEvent(args);
			if (args.Handled)
				return;
		}
		if (character.HasValue && target is not null)
		{
			var args = new NyxTextInputEventArgs(target, character.Value);
			target.RaiseEvent(args);
			if (args.Handled)
				return;
		}
        if (Focus.FocusedTextEntry is { } textEntry)
            textEntry.HandleKey(key, character);
    }

    /// <summary>Paints all visible roots in order.</summary>
    public void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        for (var i = 0; i < _layers.Count; i++)
        {
            var layer = _layers[i];
            if (!layer.IsVisible || !layer.Root.Visible) continue;
            layer.Root.Paint(painter, theme);
        }
    }

    private void UpdateFocusFromPointer(int x, int y)
    {
        var hit = FindDeepestHit(x, y);
        if (hit is { Focusable: true })
            Focus.SetFocus(hit);
        else if (Focus.FocusedElement is not null)
            Focus.Clear();
    }

    private NyxElement? FindDeepestHit(int x, int y)
    {
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (!layer.IsVisible || !layer.Root.Visible) continue;
            var hit = FindDeepestHit(layer.Root, x, y);
            if (hit is not null) return hit;
        }
        return null;
    }

    /// <summary>
    /// Dispatches mouse events to a root with capture/bubble routing.
    ///
    /// MouseMove is always dispatched (non-bubbling, each widget handles itself).
    /// Left/right down route to the deepest hit; left/right up route to the same
    /// element that received the corresponding down (tracked via <c>_leftPressedElement</c>
    /// / <c>_rightPressedElement</c>).
    /// Wheel events route to the deepest hit.
    ///
    /// Drag-drop is checked first: if active, mouse-move updates the ghost, right-click
    /// cancels, and left-up attempts a drop — normal events are suppressed.
    /// </summary>
    private void Dispatch(
        NyxElement root,
        int x, int y,
        bool leftDown, bool leftUp,
        bool rightDown, bool rightUp,
        int wheelDelta)
    {
        // MouseMove is always dispatched (does not bubble, handled per-widget)
        root.OnMouseMove(x, y);

        if (root is NyxContainer container)
        {
            if (container.DragDrop.IsActive)
            {
                container.DragDrop.UpdateDrag(x, y);
                if (rightDown)
                {
                    container.DragDrop.CancelDrag();
                }
                else if (leftUp)
                {
                    container.DragDrop.TryDrop(x, y);
                }
                return;
            }
            else
            {
                container.DragDrop.UpdateDrag(x, y);
                if (container.DragDrop.IsActive)
                {
                    return;
                }
            }
        }

        if (leftDown)
        {
            var target = FindDeepestHit(root, x, y);
            if (target is not null)
            {
				_leftPressedElement = target;
                target.OnMouseDown(x, y, NyxMouseButton.Left);
                target.RaiseEvent(new NyxMouseEventArgs(NyxEventType.MouseDown, target, x, y, NyxMouseButton.Left));
            }
        }

        if (leftUp)
        {
            var target = _leftPressedElement ?? FindDeepestHit(root, x, y);
			_leftPressedElement = null;
            if (target is not null)
            {
                target.OnMouseUp(x, y, NyxMouseButton.Left);
                target.RaiseEvent(new NyxMouseEventArgs(NyxEventType.MouseUp, target, x, y, NyxMouseButton.Left));

                if (target.PointerInside)
                {
                    target.RaiseEvent(new NyxClickEventArgs(NyxEventType.Click, target, x, y));
					var now = Environment.TickCount64;
					if (now - _lastLeftClickTime <= DoubleClickThresholdMs &&
						Math.Abs(x - _lastLeftClickX) <= DoubleClickDistanceThreshold &&
						Math.Abs(y - _lastLeftClickY) <= DoubleClickDistanceThreshold)
					{
						target.RaiseEvent(new NyxClickEventArgs(NyxEventType.DoubleClick, target, x, y));
						_lastLeftClickTime = 0;
					}
					else
					{
						_lastLeftClickTime = now;
						_lastLeftClickX = x;
						_lastLeftClickY = y;
					}
                }
            }
        }

        if (rightDown)
        {
            var target = FindDeepestHit(root, x, y);
            if (target is not null)
            {
				_rightPressedElement = target;
                target.OnRightButtonDown(x, y);
            }
        }

        if (rightUp)
        {
            var target = _rightPressedElement ?? FindDeepestHit(root, x, y);
			_rightPressedElement = null;
            if (target is not null)
            {
                target.OnRightButtonUp(x, y);
            }
        }

        if (wheelDelta != 0)
        {
            var target = FindDeepestHit(root, x, y);
            if (target is not null)
                target.OnMouseWheel(x, y, wheelDelta);
        }
    }

    public NyxElement? FindActiveTooltipElement()
    {
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            var layer = _layers[i];
            if (!layer.IsVisible || !layer.Root.Visible) continue;
            if (NyxTooltipRouting.FindDeepestHovered(layer.Root) is { } target)
                return target;
        }
        return null;
    }

    private static NyxElement? FindDeepestHit(NyxElement scope, int x, int y)
    {
        if (!scope.Visible || !scope.Bounds.Contains(x, y)) return null;

        switch (scope)
        {
            case NyxMiniWindow mini:
                for (var i = mini.Children.Count - 1; i >= 0; i--)
                {
                    var hit = FindDeepestHit(mini.Children[i], x, y);
                    if (hit is not null) return hit;
                }

                if (!mini.Minimized)
                {
                    var body = FindDeepestHit(mini.Body, x, y);
                    if (body is not null) return body;
                }

                return scope;

            case NyxScrollablePanel scroll:
                return FindDeepestHit(scroll.Body, x, y) ?? scope;

            case NyxContainer container:
                for (var i = container.Children.Count - 1; i >= 0; i--)
                {
                    var hit = FindDeepestHit(container.Children[i], x, y);
                    if (hit is not null) return hit;
                }
                return scope;

            default:
                return scope;
        }
    }

    private sealed class Layer(NyxElement root, Func<bool>? isVisible)
    {
        public NyxElement Root { get; } = root;
        public bool IsVisible => isVisible?.Invoke() ?? true;
    }
}
