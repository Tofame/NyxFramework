using System;
using System.Collections.Generic;

namespace NyxGui;

public class NyxDockPanel : NyxContainer
{
	private readonly HashSet<NyxMiniWindow> _hooked = new();
	private bool _applyingLayout;

	public int Margin { get; set; } = 8;
	public int Gap { get; set; } = 6;

	public NyxDockPanel(NyxRect bounds, uint internalId = 0) : base(bounds, internalId)
	{
		Layout = new NyxDockPanelLayout(this);
	}

	public void MeasureDock(NyxSize availableSize)
	{
		DesiredSize = new NyxSize(
			Math.Min(Bounds.Width, availableSize.Width),
			Math.Min(Bounds.Height, availableSize.Height));
	}

	public void ArrangeDock(NyxRect finalRect)
	{
		SetBoundsSilently(finalRect);
		HookAllMiniWindows();
		StackColumn();
	}

	private void HookAllMiniWindows()
	{
		var root = FindRoot();
		if (root is null) return;

		var miniWindows = new List<NyxMiniWindow>();
		FindMiniWindowsRecursive(root, miniWindows);

		foreach (var win in miniWindows)
		{
			if (_hooked.Add(win))
			{
				win.DragProgress += OnGlobalWindowDragProgress;
				win.DragEnded += OnGlobalWindowDragEnded;
				win.BoundsChanged += OnGlobalWindowBoundsChanged;
				win.Closed += OnGlobalWindowClosed;
				win.MinimizedChanged += OnGlobalWindowMinimizedChanged;
			}
		}
	}

	private void FindMiniWindowsRecursive(NyxElement element, List<NyxMiniWindow> results)
	{
		if (element is NyxMiniWindow win)
		{
			results.Add(win);
		}
		if (element is NyxContainer container)
		{
			foreach (var child in container.Children)
			{
				FindMiniWindowsRecursive(child, results);
			}
		}
	}

	private void OnGlobalWindowDragProgress(object? sender, NyxMiniWindowBoundsEventArgs e)
	{
		if (sender is not NyxMiniWindow win) return;

		var bounds = win.Bounds;
		var dropY = bounds.Y + Math.Min(win.TitleBarHeight, bounds.Height) / 2;

		var isChild = win.Parent == this;
		var overlap = Math.Min(bounds.Right, Bounds.Right) - Math.Max(bounds.X, Bounds.X);
		var threshold = Math.Min(60, bounds.Width / 2);
		var isWithinPanel = overlap >= threshold;

		if (isChild)
		{
			if (!isWithinPanel)
			{
				var root = FindRoot();
				if (root is not null)
				{
					RemoveChild(win);
					root.AddChild(win);
					StackColumn();
				}
			}
			else
			{
				var newIndex = FindInsertIndex(dropY, win);
				var currentIndex = GetChildIndex(win);
				if (currentIndex >= 0 && currentIndex != newIndex)
				{
					MoveChild(win, newIndex);
					StackColumn();
				}
			}
		}
		else
		{
			if (isWithinPanel && win.Parent != this)
			{
				if (win.Parent is NyxContainer oldParent)
					oldParent.RemoveChild(win);

				var innerW = Math.Max(64, Bounds.Width - Margin * 2);
				win.SetBounds(new NyxRect(win.Bounds.X, win.Bounds.Y, innerW, win.Bounds.Height));

				AddChild(win);

				var newIndex = FindInsertIndex(dropY, win);
				MoveChild(win, newIndex);
				StackColumn();
			}
		}
	}

	private int GetChildIndex(NyxElement child)
	{
		for (var i = 0; i < Children.Count; i++)
		{
			if (ReferenceEquals(Children[i], child))
				return i;
		}
		return -1;
	}

	private void OnGlobalWindowDragEnded(object? sender, NyxMiniWindowBoundsEventArgs e)
	{
		if (sender is not NyxMiniWindow win) return;
		if (win.Parent == this)
		{
			StackColumn();
		}
	}

	private void OnGlobalWindowBoundsChanged(object? sender, NyxMiniWindowBoundsEventArgs e)
	{
		if (sender is not NyxMiniWindow win) return;
		if (win.Parent == this && !_applyingLayout && !win.IsDragging)
		{
			StackColumn();
		}
	}

	private void OnGlobalWindowClosed(object? sender, EventArgs e)
	{
		if (sender is not NyxMiniWindow win) return;
		if (_hooked.Remove(win))
		{
			win.DragProgress -= OnGlobalWindowDragProgress;
			win.DragEnded -= OnGlobalWindowDragEnded;
			win.BoundsChanged -= OnGlobalWindowBoundsChanged;
			win.Closed -= OnGlobalWindowClosed;
			win.MinimizedChanged -= OnGlobalWindowMinimizedChanged;
		}
		if (win.Parent == this)
		{
			RemoveChild(win);
			StackColumn();
		}
	}

	private void OnGlobalWindowMinimizedChanged(object? sender, EventArgs e)
	{
		if (sender is not NyxMiniWindow win) return;
		if (win.Parent == this && !_applyingLayout)
		{
			StackColumn();
		}
	}

	protected internal override void OnDetached()
	{
		base.OnDetached();
		foreach (var win in _hooked)
		{
			win.DragProgress -= OnGlobalWindowDragProgress;
			win.DragEnded -= OnGlobalWindowDragEnded;
			win.BoundsChanged -= OnGlobalWindowBoundsChanged;
			win.Closed -= OnGlobalWindowClosed;
			win.MinimizedChanged -= OnGlobalWindowMinimizedChanged;
		}
		_hooked.Clear();
	}

	private int FindInsertIndex(int dropY, NyxMiniWindow draggingWin)
	{
		var y = Bounds.Y + Margin;
		var lastVisibleIndex = -1;

		for (var i = 0; i < Children.Count; i++)
		{
			var child = Children[i];
			if (child is not NyxMiniWindow win || !win.Visible)
				continue;

			lastVisibleIndex = i;
			var h = win.Minimized ? win.TitleBarHeight : win.Bounds.Height;
			if (dropY < y + h / 2)
			{
				return i;
			}

			y += h + Gap;
		}

		return lastVisibleIndex >= 0 ? lastVisibleIndex + 1 : 0;
	}

	private void StackColumn()
	{
		var y = Bounds.Y + Margin;
		var innerW = Math.Max(64, Bounds.Width - Margin * 2);
		var x = Bounds.X + Margin;

		_applyingLayout = true;
		try
		{
			foreach (var child in Children)
			{
				if (child is not NyxMiniWindow win || !win.Visible)
					continue;

				if (win.IsResizingHeight)
				{
					y += win.Bounds.Height + Gap;
					continue;
				}

				if (win.IsDragging)
				{
					var hDrag = win.Minimized ? win.TitleBarHeight : win.Bounds.Height;
					y += hDrag + Gap;
					continue;
				}

				var h = win.Minimized ? win.TitleBarHeight : win.Bounds.Height;
				win.SetBounds(new NyxRect(x, y, innerW, h));
				y += win.Bounds.Height + Gap;
			}
		}
		finally
		{
			_applyingLayout = false;
		}
	}
}

internal sealed class NyxDockPanelLayout(NyxDockPanel panel) : NyxLayout
{
	public override void Measure(NyxContainer container, NyxSize availableSize)
	{
		panel.MeasureDock(availableSize);
	}

	public override void Arrange(NyxContainer container, NyxRect finalRect)
	{
		panel.ArrangeDock(finalRect);
	}
}
