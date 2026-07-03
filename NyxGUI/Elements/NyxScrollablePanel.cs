namespace NyxGui;

/// <summary>
/// Viewport + optional vertical and/or horizontal scroll linked to external scrollbars.
/// </summary>
public sealed class NyxScrollablePanel : NyxElement
{
	private int _scrollX;
	private int _scrollY;
	private int _contentExtentWidth;
	private int _contentExtentHeight;
	private NyxVScrollBar? _vBar;
	private NyxHScrollBar? _hBar;

	/// <summary>Raised after scroll offsets change.</summary>
	public event EventHandler? ScrollChanged;

	public NyxScrollablePanel(NyxRect bounds, uint internalId = 0)
		: base(internalId)
	{
		Body.Parent = this;
		RefreshLayout();
	}

	public NyxContainer Body { get; } = new(NyxRect.Empty);

	public int ScrollOffsetY => _scrollY;
	public int ScrollOffsetX => _scrollX;

	public bool InvertedScroll { get; set; }

	/// <summary>Scrollable content height when <see cref="Body"/> has no laid-out children.</summary>
	public int ContentExtentHeight
	{
		get => _contentExtentHeight;
		set
		{
			_contentExtentHeight = Math.Max(0, value);
			RefreshLayout();
		}
	}

	/// <summary>Scrollable content width when <see cref="Body"/> has no laid-out children.</summary>
	public int ContentExtentWidth
	{
		get => _contentExtentWidth;
		set
		{
			_contentExtentWidth = Math.Max(0, value);
			RefreshLayout();
		}
	}

	public NyxRect ClientRect => Bounds;

	public NyxVScrollBar? VerticalScrollBar
	{
		get => _vBar;
		set
		{
			if (_vBar == value) return;
			if (_vBar is not null)
				_vBar.ValueChanged -= OnVScrollBarValueChanged;
			_vBar = value;
			if (_vBar is not null)
				_vBar.ValueChanged += OnVScrollBarValueChanged;
			InvalidateLayout();
			RefreshLayout();
		}
	}

	public NyxHScrollBar? HorizontalScrollBar
	{
		get => _hBar;
		set
		{
			if (_hBar == value) return;
			if (_hBar is not null)
				_hBar.ValueChanged -= OnHScrollBarValueChanged;
			_hBar = value;
			if (_hBar is not null)
				_hBar.ValueChanged += OnHScrollBarValueChanged;
			InvalidateLayout();
			RefreshLayout();
		}
	}

	private void OnVScrollBarValueChanged(object? sender, EventArgs e)
	{
		if (_vBar is not null)
		{
			_scrollY = _vBar.Value;
			SyncBodyPosition();
			ScrollChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void OnHScrollBarValueChanged(object? sender, EventArgs e)
	{
		if (_hBar is not null)
		{
			_scrollX = _hBar.Value;
			SyncBodyPosition();
			ScrollChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public override void SetBounds(NyxRect bounds)
	{
		Bounds = bounds;
		RefreshLayout();
	}

	public void RefreshLayout()
	{
		var client = ClientRect;

		var contentH = ComputeContentHeight(client);
		var extentY = Math.Max(0, contentH - client.Height);
		_scrollY = Math.Clamp(_scrollY, 0, extentY);
		if (_vBar is not null)
		{
			_vBar.Configure(extentY, _scrollY, client.Height);
			_vBar.Enabled = extentY > 0;
		}

		var contentW = ComputeContentWidth(client);
		var extentX = Math.Max(0, contentW - client.Width);
		_scrollX = Math.Clamp(_scrollX, 0, extentX);
		if (_hBar is not null)
		{
			_hBar.Configure(extentX, _scrollX, client.Width);
			_hBar.Enabled = extentX > 0;
		}

		Body.SetBounds(new NyxRect(client.X - _scrollX, client.Y - _scrollY, Math.Max(contentW, client.Width), Math.Max(contentH, client.Height)));
	}

	public void AddToBody(NyxElement child) => AddToBody(child, 0, 0);

	/// <summary>Adds a child positioned relative to the scroll body top-left (in pixels).</summary>
	public void AddToBody(NyxElement child, int localX, int localY)
	{
		var body = Body.Bounds;
		var w = Math.Max(1, child.Bounds.Width);
		var h = Math.Max(1, child.Bounds.Height);
		child.SetBounds(new NyxRect(body.X + localX, body.Y + localY, w, h));
		Body.AddChild(child);
		RefreshLayout();
	}

	/// <summary>
	/// Moves a body child. <paramref name="localX"/> / <paramref name="localY"/> are relative to the body
	/// content top-left; stored bounds are screen-space (body moves on scroll via <see cref="NyxContainer.SetBounds"/>).
	/// </summary>
	public void SetBodyChildBounds(NyxElement child, int localX, int localY, int width, int height)
	{
		var body = Body.Bounds;
		child.SetBounds(new NyxRect(
			body.X + localX,
			body.Y + localY,
			Math.Max(1, width),
			Math.Max(1, height)));
	}

	/// <summary>Sets vertical scroll without resetting row layout.</summary>
	public void ScrollTo(int scrollY)
	{
		var client = ClientRect;
		var contentH = ComputeContentHeight(client);
		var extent = Math.Max(0, contentH - client.Height);
		_scrollY = Math.Clamp(scrollY, 0, extent);
		if (_vBar is not null)
		{
			_vBar.Configure(extent, _scrollY, client.Height);
			_vBar.Enabled = extent > 0;
		}
		SyncBodyPosition();
		ScrollChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Sets horizontal scroll without resetting layout.</summary>
	public void ScrollToHorizontal(int scrollX)
	{
		var client = ClientRect;
		var contentW = ComputeContentWidth(client);
		var extent = Math.Max(0, contentW - client.Width);
		_scrollX = Math.Clamp(scrollX, 0, extent);
		if (_hBar is not null)
		{
			_hBar.Configure(extent, _scrollX, client.Width);
			_hBar.Enabled = extent > 0;
		}
		SyncBodyPosition();
		ScrollChanged?.Invoke(this, EventArgs.Empty);
	}

	private void SyncBodyPosition()
	{
		var client = ClientRect;
		var contentW = ComputeContentWidth(client);
		var contentH = ComputeContentHeight(client);
		Body.SetBounds(new NyxRect(client.X - _scrollX, client.Y - _scrollY, Math.Max(contentW, client.Width), Math.Max(contentH, client.Height)));
	}

	private int ComputeContentHeight(NyxRect client)
	{
		if (Body.Children.Count == 0)
			return Math.Max(_contentExtentHeight, client.Height);

		var bodyY = Body.Bounds.Y;
		var bottom = 0;
		foreach (var c in Body.Children)
			bottom = Math.Max(bottom, c.Bounds.Bottom - bodyY);

		return Math.Max(bottom, client.Height);
	}

	private int ComputeContentWidth(NyxRect client)
	{
		if (Body.Children.Count == 0)
			return Math.Max(_contentExtentWidth, client.Width);

		var bodyX = Body.Bounds.X;
		var right = 0;
		foreach (var c in Body.Children)
			right = Math.Max(right, c.Bounds.Right - bodyX);

		return Math.Max(right, client.Width);
	}

	public override void OnMouseMove(int x, int y)
	{
		if (!Visible)
			return;
		Body.OnMouseMove(x, y);
	}

	public override void OnMouseDown(int x, int y, NyxMouseButton button)
	{
		if (!Visible)
			return;
		if (ClientRect.Contains(x, y))
			Body.OnMouseDown(x, y, button);
	}

	public override void OnMouseUp(int x, int y, NyxMouseButton button)
	{
		if (!Visible)
			return;
		Body.OnMouseUp(x, y, button);
	}

	public override void OnRightButtonDown(int x, int y)
	{
		if (!Visible)
			return;
		if (ClientRect.Contains(x, y))
			Body.OnRightButtonDown(x, y);
	}

	public override void OnRightButtonUp(int x, int y)
	{
		if (!Visible)
			return;
		Body.OnRightButtonUp(x, y);
	}

	public override void OnMouseWheel(int x, int y, int delta)
	{
		if (!Visible || !Bounds.Contains(x, y))
			return;

		if (!ClientRect.Contains(x, y))
			return;

		var client = ClientRect;
		if (_vBar is not null || _hBar is null)
		{
			var step = _vBar?.ScrollStep > 0 ? _vBar.ScrollStep : Math.Max(16, client.Height / 8);
			if (InvertedScroll) step = -step;
			var next = _scrollY + (delta < 0 ? step : -step);
			var contentH = ComputeContentHeight(client);
			var extent = Math.Max(0, contentH - client.Height);
			_scrollY = Math.Clamp(next, 0, extent);
			if (_vBar is not null)
			{
				_vBar.Configure(extent, _scrollY, client.Height);
				_vBar.Enabled = extent > 0;
			}
			SyncBodyPosition();
			ScrollChanged?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			var step = _hBar.ScrollStep > 0 ? _hBar.ScrollStep : Math.Max(16, client.Width / 8);
			var next = _scrollX + (delta < 0 ? step : -step);
			var contentW = ComputeContentWidth(client);
			var extent = Math.Max(0, contentW - client.Width);
			_scrollX = Math.Clamp(next, 0, extent);
			_hBar.Configure(extent, _scrollX, client.Width);
			_hBar.Enabled = extent > 0;
			SyncBodyPosition();
			ScrollChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
	{
		if (!TryBeginPaintVisual(out var visual))
			return;

		try
		{
			if (visual.Image is not null)
				PaintBackground(painter, visual);
			else if (visual.HasBackground)
				painter.FillRect(Bounds, Tint(visual.BackgroundColor!.Value, visual));

			if (visual.HasBorder)
				PaintStateBorder(painter, visual);

			painter.PushClip(ClientRect);
			Body.Paint(painter, theme);
			painter.PopClip();

			NyxTooltipRouting.PaintActiveTooltip(this, painter, theme);
		}
		finally
		{
			EndPaintVisual();
		}
	}
}
