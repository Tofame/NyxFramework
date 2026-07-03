using System;
using System.Collections.Generic;

namespace NyxGui;

/// <summary>
/// A container that applies a zoom and pan transform to its children.
///
/// Each child has a <em>virtual bounds</em> (world-space) tracked in
/// <see cref="_virtualBounds"/>.  On every zoom/pan change or child add,
/// virtual bounds are projected to screen-space via
/// <c>screen = world * zoom + pan</c> and the child receives
/// <see cref="NyxElement.SetBounds"/> with the screen rectangle.
///
/// Zoom is centered on the mouse cursor (<see cref="ZoomToAnchor"/>) so the
/// point under the cursor stays fixed as the view scales around it.
/// Pan is a simple delta drag: pan += (mouse delta).
/// </summary>
public class ZoomCanvas : NyxContainer
{
	private float _zoom = 1.0f;
	private float _panX = 0f;
	private float _panY = 0f;
	private bool _isPanning = false;
	private float _panStartX = 0f;
	private float _panStartY = 0f;
	private int _mouseStartX = 0;
	private int _mouseStartY = 0;

	/// <summary>Per-child world-space bounds.  Screen-space is derived via WorldToCanvas.</summary>
	private readonly Dictionary<NyxElement, NyxRect> _virtualBounds = new();

	public ZoomCanvas(uint internalId = 0) : base(internalId)
	{
		ZoomSensitivity = 0.1f;
		MinZoom = 0.1f;
		MaxZoom = 10.0f;
		IsPanEnabled = true;

		this.AddHandler(NyxEventType.MouseWheel, HandleMouseWheelEvent);
		this.AddHandler(NyxEventType.MouseDown, HandleMouseDownEvent);
		this.AddHandler(NyxEventType.MouseUp, HandleMouseUpEvent);
	}

	/// <summary>Zoom multiplier per mouse-wheel step (e.g. 0.1 = 10% per tick).</summary>
	public float ZoomSensitivity { get; set; }

	/// <summary>Minimum allowed zoom level.</summary>
	public float MinZoom { get; set; }

	/// <summary>Maximum allowed zoom level.</summary>
	public float MaxZoom { get; set; }

	/// <summary>When true, left-drag pans the canvas (otherwise only programmatic pan works).</summary>
	public bool IsPanEnabled { get; set; }

	public float Zoom
	{
		get => _zoom;
		set
		{
			var val = Math.Clamp(value, MinZoom, MaxZoom);
			if (Math.Abs(_zoom - val) > 0.0001f)
			{
				_zoom = val;
				UpdateChildrenBounds();
				InvalidateRender();
			}
		}
	}

	public float PanX
	{
		get => _panX;
		set
		{
			if (Math.Abs(_panX - value) > 0.0001f)
			{
				_panX = value;
				UpdateChildrenBounds();
				InvalidateRender();
			}
		}
	}

	public float PanY
	{
		get => _panY;
		set
		{
			if (Math.Abs(_panY - value) > 0.0001f)
			{
				_panY = value;
				UpdateChildrenBounds();
				InvalidateRender();
			}
		}
	}

	/// <summary>Zooms in at the center of the canvas bounds.</summary>
	public void ZoomIn()
	{
		ZoomToAnchor(Bounds.X + Bounds.Width / 2f, Bounds.Y + Bounds.Height / 2f, 1);
	}

	/// <summary>Zooms out at the center of the canvas bounds.</summary>
	public void ZoomOut()
	{
		ZoomToAnchor(Bounds.X + Bounds.Width / 2f, Bounds.Y + Bounds.Height / 2f, -1);
	}

	/// <summary>Centers the view on world origin (0, 0).</summary>
	public void Center()
	{
		CenterOn(0, 0);
	}

	/// <summary>
	/// Centers the view on the given world point, accounting for current zoom.
	/// Pan is set so that (wx, wy) appears at the center of the canvas.
	/// </summary>
	public void CenterOn(float wx, float wy)
	{
		_panX = (Bounds.Width / 2f) - (wx * _zoom);
		_panY = (Bounds.Height / 2f) - (wy * _zoom);
		UpdateChildrenBounds();
		InvalidateRender();
	}

	/// <summary>Maps a world-space rectangle to screen-space.</summary>
	public NyxRect WorldToCanvas(NyxRect worldRect)
	{
		var x = Bounds.X + (int)MathF.Round((worldRect.X * _zoom) + _panX);
		var y = Bounds.Y + (int)MathF.Round((worldRect.Y * _zoom) + _panY);
		var w = (int)MathF.Round(worldRect.Width * _zoom);
		var h = (int)MathF.Round(worldRect.Height * _zoom);
		return new NyxRect(x, y, w, h);
	}

	/// <summary>Maps a screen-space rectangle back to world-space.</summary>
	public NyxRect CanvasToWorld(NyxRect canvasRect)
	{
		var x = (int)MathF.Round((canvasRect.X - Bounds.X - _panX) / _zoom);
		var y = (int)MathF.Round((canvasRect.Y - Bounds.Y - _panY) / _zoom);
		var w = (int)MathF.Round(canvasRect.Width / _zoom);
		var h = (int)MathF.Round(canvasRect.Height / _zoom);
		return new NyxRect(x, y, w, h);
	}

	/// <summary>Maps a world-space point to screen-space.</summary>
	public (float X, float Y) WorldToCanvas(float wx, float wy)
	{
		var cx = Bounds.X + (wx * _zoom) + _panX;
		var cy = Bounds.Y + (wy * _zoom) + _panY;
		return (cx, cy);
	}

	/// <summary>Maps a screen-space point back to world-space.</summary>
	public (float X, float Y) CanvasToWorld(float cx, float cy)
	{
		var wx = (cx - Bounds.X - _panX) / _zoom;
		var wy = (cy - Bounds.Y - _panY) / _zoom;
		return (wx, wy);
	}

	/// <summary>
	/// Stores the world-space bounds for a child and immediately projects them to screen-space.
	/// This is the primary way to position children on the canvas.
	/// </summary>
	public void SetChildVirtualBounds(NyxElement child, NyxRect virtualBounds)
	{
		_virtualBounds[child] = virtualBounds;
		UpdateChildrenBounds();
	}

	/// <summary>Returns the stored world-space bounds for a child.</summary>
	public NyxRect GetChildVirtualBounds(NyxElement child)
	{
		if (_virtualBounds.TryGetValue(child, out var vb))
			return vb;
		return child.Bounds;
	}

	public override void AddChild(NyxElement child)
	{
		base.AddChild(child);
		if (!_virtualBounds.ContainsKey(child))
		{
			_virtualBounds[child] = child.Bounds;
		}
		UpdateChildrenBounds();
	}

	public override bool RemoveChild(NyxElement child)
	{
		var removed = base.RemoveChild(child);
		if (removed)
		{
			_virtualBounds.Remove(child);
		}
		return removed;
	}

	public override void SetBounds(NyxRect newBounds)
	{
		base.SetBounds(newBounds);
		UpdateChildrenBounds();
	}

	public override void Arrange(NyxRect finalRect)
	{
		base.Arrange(finalRect);
		UpdateChildrenBounds();
	}

	/// <summary>Projects every child's virtual bounds to screen-space via WorldToCanvas.</summary>
	private void UpdateChildrenBounds()
	{
		foreach (var child in Children)
		{
			var vb = GetChildVirtualBounds(child);
			var screenRect = WorldToCanvas(vb);
			child.SetBounds(screenRect);
		}
	}

	public override bool HitTestSubtree(int x, int y)
	{
		if (!Visible || !Enabled) return false;
		if (!Bounds.Contains(x, y)) return false;
		return base.HitTestSubtree(x, y);
	}

	/// <summary>
	/// Initiates panning if the click hit nothing interactive.
	/// Stores the pan starting offset and mouse position for delta calculation in OnMouseMove.
	/// </summary>
	private void HandleMouseDownEvent(object? sender, NyxEventArgs args)
	{
		if (args is NyxMouseEventArgs mouseArgs && mouseArgs.Button == NyxMouseButton.Left && IsPanEnabled)
		{
			var capturing = NyxPointerInput.FindCapturingWidget(this, mouseArgs.X, mouseArgs.Y);
			if (capturing == null || ReferenceEquals(capturing, this))
			{
				_isPanning = true;
				_panStartX = _panX;
				_panStartY = _panY;
				_mouseStartX = mouseArgs.X;
				_mouseStartY = mouseArgs.Y;
				mouseArgs.Handled = true;
			}
		}
	}

	public override void OnMouseMove(int x, int y)
	{
		if (_isPanning)
		{
			_panX = _panStartX + (x - _mouseStartX);
			_panY = _panStartY + (y - _mouseStartY);
			UpdateChildrenBounds();
			InvalidateRender();
			return;
		}
		base.OnMouseMove(x, y);
	}

	private void HandleMouseUpEvent(object? sender, NyxEventArgs args)
	{
		if (args is NyxMouseEventArgs mouseArgs && mouseArgs.Button == NyxMouseButton.Left)
		{
			_isPanning = false;
		}
	}

	/// <summary>
	/// Zoom handler: unless <see cref="PreventZoom"/> returns true, zooms in/out
	/// centered on the cursor's screen position.
	/// </summary>
	private void HandleMouseWheelEvent(object? sender, NyxEventArgs args)
	{
		if (args is NyxMouseWheelEventArgs wheelArgs)
		{
			if (PreventZoom != null && PreventZoom())
			{
				return;
			}

			if (wheelArgs.Delta != 0)
			{
				ZoomToAnchor(wheelArgs.X, wheelArgs.Y, wheelArgs.Delta > 0 ? 1 : -1);
				wheelArgs.Handled = true;
			}
		}
	}

	/// <summary>
	/// Callback evaluated before every zoom wheel event.  Return true to skip zooming
	/// (e.g. when a child composable or context menu is open above the canvas).
	/// </summary>
	public Func<bool>? PreventZoom { get; set; }

	/// <summary>
	/// Zooms toward or away from a screen-space anchor point.
	///
	/// Strategy: convert the anchor from screen to world at the OLD zoom, calculate
	/// the new zoom, then recompute pan so the same world point stays under the anchor.
	/// This keeps the point under the cursor visually pinned during zoom.
	/// </summary>
	private void ZoomToAnchor(float anchorX, float anchorY, int direction)
	{
		var oldZoom = _zoom;
		float factor = MathF.Pow(1.0f + ZoomSensitivity, direction);
		var newZoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
		if (Math.Abs(newZoom - oldZoom) < 0.0001f) return;

		float wx = (anchorX - Bounds.X - _panX) / oldZoom;
		float wy = (anchorY - Bounds.Y - _panY) / oldZoom;

		_zoom = newZoom;
		_panX = anchorX - Bounds.X - (wx * newZoom);
		_panY = anchorY - Bounds.Y - (wy * newZoom);

		UpdateChildrenBounds();
		InvalidateRender();
	}

	public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
	{
		if (!TryBeginPaintVisual(out var visual)) return;

		try
		{
			PaintChrome(painter, visual);

			painter.PushClip(Bounds);

			foreach (var child in Children)
			{
				if (child.Visible)
				{
					child.Paint(painter, theme);
				}
			}

			painter.PopClip();

			NyxTooltipRouting.PaintActiveTooltip(this, painter, theme);
		}
		finally
		{
			EndPaintVisual();
		}
	}
}
