using NyxGui.Definitions;

namespace NyxGui;

/// <summary>
/// Base class for widgets that contain children. Implements Measure/Arrange layout.
/// Replaces the old NyxContent which mixed anchor-based layout with child management.
///
/// <b>Drag handling:</b> when <see cref="Draggable"/> is true, a left-click on the
/// container background (not on any interactive child that <c>CapturesPointer</c>) starts
/// a drag.  The drag offsets are computed as the delta from click point to container
/// origin.  On each mouse-move, the bounds are translated by the cumulative delta.
/// LayoutBox is cleared during drag (the container is free-floating after release).
/// </summary>
public class NyxContainer : NyxElement
{
    private readonly List<NyxElement> _children = new();
    private NyxElement? _activeChild;

    internal NyxGuiBuiltDocument? OwningDocument { get; set; }

    public NyxContainer(NyxRect bounds, uint internalId = 0) : base(internalId)
    {
        Bounds = bounds;
		this.AddHandler(NyxEventType.MouseDown, HandleMouseDownEvent);
		this.AddHandler(NyxEventType.MouseUp, HandleMouseUpEvent);
    }

    internal NyxContainer(uint internalId = 0) : this(NyxRect.Empty, internalId) { }

	/// <summary>The layout strategy/engine used to arrange child elements. If null, standard anchor resolution is used.</summary>
	public NyxLayout? Layout { get; set; }

    // ── Children ──────────────────────────────────────────────────────

    public IReadOnlyList<NyxElement> Children => _children;

    public int ChildCount => _children.Count;

    /// <summary>Adds a child. The child's Parent is set to this container.</summary>
    public virtual void AddChild(NyxElement child)
    {
        if (child.Parent == this) return;
        if (child.Parent is NyxContainer oldParent) oldParent.RemoveChild(child);

        child.Parent = this;
        _children.Add(child);
        child.OnAttached();
        InvalidateLayout();
    }

    /// <summary>Removes a child. Returns true if the child was found and removed.</summary>
    public virtual bool RemoveChild(NyxElement child)
    {
        if (!_children.Remove(child)) return false;

        if (_activeChild == child) _activeChild = null;
        child.Parent = null;
        child.OnDetached();
        InvalidateLayout();
        return true;
    }

    /// <summary>Removes all children.</summary>
    public void ClearChildren()
    {
        _activeChild = null;
        foreach (var child in _children)
        {
            child.Parent = null;
            child.OnDetached();
        }
        _children.Clear();
        InvalidateLayout();
    }

    /// <summary>Moves a child to the end of the paint/input order (top of z-order).</summary>
    public void BringChildToFront(NyxElement child)
    {
        if (!_children.Remove(child)) return;
        _children.Add(child);
        InvalidateRender();
    }

	/// <summary>Moves a child to a specific index in the children list.</summary>
	public void MoveChild(NyxElement child, int index)
	{
		if (!_children.Remove(child)) return;
		index = Math.Clamp(index, 0, _children.Count);
		_children.Insert(index, child);
		InvalidateLayout();
	}

    // ── Active Child (focus tracking) ─────────────────────────────────

    /// <summary>The currently active/focused child within this container subtree.</summary>
    public NyxElement? ActiveChild => _activeChild;

    public void SetActiveChild(NyxElement? element)
    {
        if (element is { Focusable: false }) element = null;
        if (_activeChild == element) return;

        _activeChild = element;
    }

    // ── Root ──────────────────────────────────────────────────────────

    /// <summary>True if this container is a document root (manages settings, drag-drop, bindings).</summary>
    public override bool IsRoot { get; internal set; }

    /// <summary>GUI settings for this tree (set on the document root when loading).</summary>
    public NyxGuiSettings Settings { get; internal set; } = NyxGuiSettings.Default;

    private int _dragStartOffsetX;
    private int _dragStartOffsetY;
    private bool _isDragging;
    private NyxDragDrop? _dragDrop;

    /// <summary>When true, the container can be dragged by clicking and dragging its background/chrome.</summary>
    public virtual bool Draggable { get; set; }

    /// <summary>True while this container is being dragged.</summary>
    public virtual bool IsDragging => _isDragging;

    /// <summary>Fired when a drag operation completes.</summary>
    public event EventHandler<NyxContainerBoundsEventArgs>? DragEnded;

    /// <summary>
    /// Gets or creates the drag-drop state for this container's root.
    /// If <see cref="IsRoot"/> is true, owns its own instance; otherwise delegates
    /// to the root ancestor.  Falls back to a local instance if no root is found.
    /// </summary>
    public NyxDragDrop DragDrop
    {
        get
        {
            if (IsRoot)
            {
                _dragDrop ??= new NyxDragDrop();
                return _dragDrop;
            }
            var root = FindRoot();
            if (root is not null)
                return root.DragDrop;
            _dragDrop ??= new NyxDragDrop();
            return _dragDrop;
        }
    }

    /// <summary>
    /// Mouse-down handler installed via routed events.  If the container is draggable
    /// and the click did not land on an interactive child (<c>CapturesPointer</c>),
    /// starts a drag operation.
    /// </summary>
	private void HandleMouseDownEvent(object? sender, NyxEventArgs args)
	{
		if (args is NyxMouseEventArgs mouseArgs && mouseArgs.Button == NyxMouseButton.Left)
		{
			if (Draggable && !_isDragging)
			{
				var isInteractive = false;
				for (var curr = args.Source; curr is not null && !ReferenceEquals(curr, this); curr = curr.Parent)
				{
					if (NyxPointerInput.CapturesPointer(curr))
					{
						isInteractive = true;
						break;
					}
				}

				if (!isInteractive)
				{
					BeginDrag(mouseArgs.X, mouseArgs.Y);
					args.Handled = true;
				}
			}
		}
	}

    /// <summary>
    /// Mouse-up handler installed via routed events.  Ends an in-progress drag.
    /// </summary>
	private void HandleMouseUpEvent(object? sender, NyxEventArgs args)
	{
		if (args is NyxMouseEventArgs mouseArgs && mouseArgs.Button == NyxMouseButton.Left)
		{
			if (_isDragging)
			{
				EndDrag();
				args.Handled = true;
			}
		}
	}

    /// <summary>
    /// Records the click offset relative to the container's origin and begins drag state.
    /// </summary>
    private void BeginDrag(int x, int y)
    {
        _isDragging = true;
        _dragStartOffsetX = x - Bounds.X;
        _dragStartOffsetY = y - Bounds.Y;
    }

    /// <summary>
    /// Translates the container by the drag delta.  Clears <see cref="NyxElement.LayoutBox"/>
    /// so the container becomes free-floating (no longer anchor-resolved).
    /// </summary>
    private void ApplyDrag(int x, int y)
    {
        if (!_isDragging) return;
        LayoutBox = null;
        SetBounds(new NyxRect(x - _dragStartOffsetX, y - _dragStartOffsetY, Bounds.Width, Bounds.Height));
    }

    private void EndDrag()
    {
        _isDragging = false;
        DragEnded?.Invoke(this, new NyxContainerBoundsEventArgs(Bounds));
    }

    // ── Layout: Measure/Arrange ───────────────────────────────────────

    /// <summary>
    /// Measure pass: compute desired size based on content.
    /// Containers should measure their children and aggregate.
    /// Leaf widgets override this to measure their content (text, images).
    /// </summary>
    /// <param name="availableSize">Maximum size the parent can offer. Use int.MaxValue for unconstrained.</param>
    public override void Measure(NyxSize availableSize)
    {
		if (Layout is not null)
		{
			Layout.Measure(this, availableSize);
		}
		else
		{
			DesiredSize = NyxSize.Zero;
		}
    }

    /// <summary>
    /// Arrange pass: assign final bounds to this widget and position children.
    /// Containers should arrange their children here.
    /// </summary>
    /// <param name="finalRect">The rectangle assigned by the parent.</param>
    public override void Arrange(NyxRect finalRect)
    {
        SetBounds(finalRect);
		if (Layout is not null)
		{
			Layout.Arrange(this, finalRect);
		}
    }

    /// <summary>Runs Measure + Arrange on the entire subtree from this container.</summary>
    public void LayoutTree(NyxSize availableSize)
    {
        Measure(availableSize);
        Arrange(new NyxRect(0, 0, availableSize.Width, availableSize.Height));
    }

    /// <summary>
    /// Sets the container's bounds and delta-shifts all children by the same amount.
    /// This preserves child relative positions when the container moves.
    /// </summary>
    public override void SetBounds(NyxRect newBounds)
    {
        var old = Bounds;
        var dx = newBounds.X - old.X;
        var dy = newBounds.Y - old.Y;
        if (dx != 0 || dy != 0)
        {
            foreach (var c in _children)
            {
                var b = c.Bounds;
                c.SetBounds(new NyxRect(b.X + dx, b.Y + dy, b.Width, b.Height));
            }
        }
        base.SetBounds(newBounds);
    }

    /// <summary>Updates this container's bounds only (used by layout resolver).</summary>
    internal void SetBoundsSilently(NyxRect bounds) => base.SetBounds(bounds);

    // ── Hit Testing ───────────────────────────────────────────────────

    /// <summary>True if the point hits this widget or any visible descendant.</summary>
    public override bool HitTestSubtree(int x, int y)
    {
        if (!Visible) return false;

        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            if (child is NyxContainer container && container.HitTestSubtree(x, y))
                return true;
            if (child.HitTest(x, y))
                return true;
        }

        return HitTest(x, y);
    }

    // ── Input ─────────────────────────────────────────────────────────

    public override void OnMouseMove(int x, int y)
    {
        if (_isDragging)
        {
            ApplyDrag(x, y);
            return;
        }

        if (!Visible) return;
        base.OnMouseMove(x, y);

        var children = _children.ToArray();
        foreach (var child in children)
            child.OnMouseMove(x, y);
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        if (!Visible) return;

        var target = FindDeepestHit(x, y);
        if (target is not null && !ReferenceEquals(target, this))
        {
            if (target.Focusable) SetActiveChild(target);
            target.OnMouseDown(x, y, button);
        }
        else if (Draggable && button == NyxMouseButton.Left && HitTest(x, y))
        {
            BeginDrag(x, y);
        }
        else
        {
            base.OnMouseDown(x, y, button);
        }
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        if (_isDragging && button == NyxMouseButton.Left)
        {
            EndDrag();
        }

        if (!Visible) return;
        base.OnMouseUp(x, y, button);

        var children = _children.ToArray();
        foreach (var child in children)
        {
            if (child.Enabled && child.Visible)
                child.OnMouseUp(x, y, button);
        }
    }

    /// <summary>
    /// Routes wheel events to the topmost visible child that contains the point.
    /// Phantom/disabled children are passed through (they forward to their own children
    /// without consuming the event).
    /// </summary>
    public override void OnMouseWheel(int x, int y, int delta)
    {
        if (!Visible) return;

        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            if (!child.Visible || !child.Bounds.Contains(x, y)) continue;

            if (child.Phantom || !child.Enabled)
            {
                if (child is NyxContainer passThrough)
                    passThrough.OnMouseWheel(x, y, delta);
                continue;
            }

            child.OnMouseWheel(x, y, delta);
            return;
        }
    }

    public override void OnRightButtonDown(int x, int y)
    {
        if (!Visible) return;

        var target = FindDeepestHit(x, y);
        if (target is not null && !ReferenceEquals(target, this))
        {
            if (target.Focusable) SetActiveChild(target);
            target.OnRightButtonDown(x, y);
        }
    }

    public override void OnRightButtonUp(int x, int y)
    {
        if (!Visible) return;

        if (ActiveChild is { Enabled: true, Visible: true } active)
        {
            active.OnRightButtonUp(x, y);
            return;
        }

        var children = _children.ToArray();
        foreach (var child in children)
        {
            if (child.Enabled && child.Visible)
                child.OnRightButtonUp(x, y);
        }
    }

    private NyxElement? FindDeepestHit(int x, int y)
    {
        for (var i = _children.Count - 1; i >= 0; i--)
        {
            var child = _children[i];
            if (!child.Visible || !child.Enabled) continue;

            if (child is NyxContainer container)
            {
                var deepest = container.FindDeepestHit(x, y);
                if (deepest is not null) return deepest;
            }

            if (child.HitTest(x, y)) return child;
        }

        return HitTest(x, y) ? this : null;
    }

	protected internal override void OnAttached()
	{
		base.OnAttached();
		var children = _children.ToArray();
		foreach (var child in children)
			child.OnAttached();
	}

	protected internal override void OnDetached()
	{
		base.OnDetached();
		var children = _children.ToArray();
		foreach (var child in children)
			child.OnDetached();
	}

    // ── Paint ─────────────────────────────────────────────────────────

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual)) return;

        try
        {
            PaintChrome(painter, visual);
            foreach (var child in _children)
                child.Paint(painter, theme);

            NyxTooltipRouting.PaintActiveTooltip(this, painter, theme);

            if (IsRoot)
            {
                DragDrop.PaintGhost(painter, theme);
            }
        }
        finally
        {
            EndPaintVisual();
        }
    }
}

public sealed class NyxContainerBoundsEventArgs : EventArgs
{
    public NyxContainerBoundsEventArgs(NyxRect bounds) => Bounds = bounds;
    public NyxRect Bounds { get; }
}
