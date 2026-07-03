namespace NyxGui;

/// <summary>
/// Framework-level drag-and-drop service. Manages drag lifecycle:
/// threshold check → DragStart → ghost tracking → target highlighting → Drop → DragEnd.
/// </summary>
public sealed class NyxDragDrop
{
    private NyxElement? _source;
    private NyxDragData? _data;
    private NyxDragGhost? _ghost;
    private NyxElement? _currentTargetElement;
    private NyxDropTarget? _currentTarget;
    private int _startX;
    private int _startY;
    private bool _isDragging;
    private bool _thresholdMet;

    /// <summary>True when a drag operation is in progress.</summary>
    public bool IsActive => _isDragging;

    /// <summary>Current drag payload (null when not dragging).</summary>
    public NyxDragData? Data => _data;

    /// <summary>Widget currently being hovered over as a drop target.</summary>
    public NyxElement? CurrentTarget => _currentTargetElement;

    // ── Drag Initiation ──────────────────────────────────────────────

    /// <summary>
    /// Call on MouseDown to prepare a potential drag. Does not start dragging
    /// until the pointer moves past the threshold.
    /// </summary>
    public void PrepareDrag(NyxElement source, int x, int y)
    {
        if (source.DragSource is null) return;

        _source = source;
        _startX = x;
        _startY = y;
        _isDragging = false;
        _thresholdMet = false;
    }

    /// <summary>
    /// Call on MouseMove to check threshold and begin dragging if met.
    /// </summary>
    public void UpdateDrag(int x, int y)
    {
        if (_source is null || _source.DragSource is null) return;

        if (!_thresholdMet)
        {
            var dx = x - _startX;
            var dy = y - _startY;
            if (dx * dx + dy * dy >= _source.DragSource.Threshold * _source.DragSource.Threshold)
            {
                _thresholdMet = true;
                BeginDrag(x, y);
            }
            return;
        }

        if (!_isDragging) return;

        // Update ghost position
        _ghost?.UpdatePosition(x, y);

        // Find drop target under cursor
        var (newTarget, newTargetElement) = FindDropTarget(_source, x, y);
        UpdateTargetHover(newTarget, newTargetElement, x, y);
    }

    /// <summary>
    /// Call on MouseUp to attempt a drop and end the drag operation.
    /// </summary>
    public bool TryDrop(int x, int y)
    {
        if (!_isDragging || _data is null)
        {
            EndDrag(false);
            return false;
        }

        var (target, _) = FindDropTarget(_source, x, y);
        if (target is not null && target.Accepts(_data))
        {
            var accepted = target.OnDrop?.Invoke(_data) ?? true;
            EndDrag(accepted);
            return accepted;
        }

        EndDrag(false);
        return false;
    }

    /// <summary>
    /// Cancels the current drag operation without dropping.
    /// </summary>
    public void CancelDrag()
    {
        EndDrag(false);
    }

    // ── Internal ──────────────────────────────────────────────────────

    private void BeginDrag(int x, int y)
    {
        if (_source is null || _source.DragSource is null) return;

        var dragData = _source.DragSource.GetData?.Invoke();
        if (dragData is null)
        {
            _source = null;
            return;
        }

        dragData.Source = _source;
        _data = dragData;
        _isDragging = true;

        // Create ghost
        var ghostLabel = _source is NyxWidget w ? w.Id ?? _source.GetType().Name : _source.GetType().Name;
        _ghost = new NyxDragGhost(
            _source.Bounds.Width > 0 ? _source.Bounds.Width : 64,
            _source.Bounds.Height > 0 ? _source.Bounds.Height : 32,
            _source.DragSource.GhostTemplate ?? ghostLabel);
        _ghost.UpdatePosition(x, y);

        // Fade source
        if (_source.DragSource.FadeSource)
            _source.Opacity = 0.4f;

        // Raise DragStart on source
        _source.RaiseEvent(new NyxDragEventArgs(NyxEventType.DragStart, _source, _data));
    }

    private void UpdateTargetHover(NyxDropTarget? newTarget, NyxElement? newTargetElement, int x, int y)
    {
        if (ReferenceEquals(newTargetElement, _currentTargetElement)) return;

        // Leave old target
        if (_currentTarget is not null)
        {
            _currentTarget.OnDragLeave?.Invoke();
            if (_currentTargetElement is not null)
                _currentTargetElement.RaiseEvent(new NyxDragEventArgs(NyxEventType.DragLeave, _currentTargetElement, _data));
        }

        // Enter new target
        _currentTarget = newTarget;
        _currentTargetElement = newTargetElement;
        if (_currentTarget is not null)
        {
            _currentTarget.OnDragEnter?.Invoke(_data!);
            if (_currentTargetElement is not null)
                _currentTargetElement.RaiseEvent(new NyxDragEventArgs(NyxEventType.DragEnter, _currentTargetElement, _data));
        }
    }

    private void EndDrag(bool dropped)
    {
        if (_currentTarget is not null)
        {
            _currentTarget.OnDragLeave?.Invoke();
        }

        if (_source is not null && _source.DragSource is not null)
        {
            if (_source.DragSource.FadeSource)
                _source.Opacity = 1f;

            _source.RaiseEvent(new NyxDragEndEventArgs(NyxEventType.DragEnd, _source, _data, dropped));
        }

        _source = null;
        _data = null;
        _ghost = null;
        _currentTarget = null;
        _currentTargetElement = null;
        _isDragging = false;
        _thresholdMet = false;
    }

    private static (NyxDropTarget? Target, NyxElement? Element) FindDropTarget(NyxElement? root, int x, int y)
    {
        if (root is null) return (null, null);
        return FindDropTargetInSubtree(root, x, y);
    }

    private static (NyxDropTarget? Target, NyxElement? Element) FindDropTargetInSubtree(NyxElement element, int x, int y)
    {
        if (!element.Visible || !element.Enabled) return (null, null);

        // Check children first (deepest first)
        if (element is NyxContainer container)
        {
            for (var i = container.Children.Count - 1; i >= 0; i--)
            {
                var hit = FindDropTargetInSubtree(container.Children[i], x, y);
                if (hit.Target is not null) return hit;
            }
        }

        // Check this element
        if (element.DropTarget is not null && element.HitTest(x, y))
            return (element.DropTarget, element);

        return (null, null);
    }

    // ── Paint ─────────────────────────────────────────────────────────

    /// <summary>Render the drag ghost. Call after the main frame is painted.</summary>
    public void PaintGhost(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        _ghost?.Paint(painter, theme);
    }
}
