using System;
using System.Collections.Generic;

namespace NyxGui;

/// <summary>
/// Arranges child elements docked to parent boundaries.
/// </summary>
public class NyxDockLayout : NyxLayout
{
	public NyxThickness Padding { get; set; }

	public static Dock GetDock(NyxElement child)
	{
		if (child.LayoutBox is not null && child.LayoutBox.Dock is not null)
			return child.LayoutBox.Dock.Value;
		return Dock.Fill;
	}

	public override void Measure(NyxContainer container, NyxSize availableSize)
	{
		var innerW = Math.Max(0, availableSize.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, availableSize.Height - Padding.Top - Padding.Bottom);

		NyxElement? fillChild = null;
		var dockedChildren = new List<(NyxElement Child, Dock Dock)>();

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;
			var dock = GetDock(child);
			if (dock == Dock.Fill) fillChild = child;
			else dockedChildren.Add((child, dock));
		}

		var remaining = new NyxRect(0, 0, innerW, innerH);

		foreach (var (child, dock) in dockedChildren)
		{
			var avail = dock switch
			{
				Dock.Top => new NyxSize(remaining.Width, int.MaxValue),
				Dock.Bottom => new NyxSize(remaining.Width, int.MaxValue),
				Dock.Left => new NyxSize(int.MaxValue, remaining.Height),
				Dock.Right => new NyxSize(int.MaxValue, remaining.Height),
				_ => new NyxSize(remaining.Width, remaining.Height),
			};
			child.Measure(avail);
		}

		fillChild?.Measure(new NyxSize(remaining.Width, remaining.Height));

		container.DesiredSize = new NyxSize(
			Math.Min(innerW + Padding.Left + Padding.Right, availableSize.Width),
			Math.Min(innerH + Padding.Top + Padding.Bottom, availableSize.Height));
	}

	/// <summary>
	/// Docked children are arranged in order: Top/Bottom/Left/Right consume space from the
	/// remaining rectangle, shrinking it for subsequent children.  The last child with
	/// <c>Dock.Fill</c> (if any) takes the remaining space.  Same algorithm as WinForms/WPF dock.
	/// </summary>
	public override void Arrange(NyxContainer container, NyxRect finalRect)
	{
		var x = finalRect.X + Padding.Left;
		var y = finalRect.Y + Padding.Top;
		var w = Math.Max(0, finalRect.Width - Padding.Left - Padding.Right);
		var h = Math.Max(0, finalRect.Height - Padding.Top - Padding.Bottom);

		NyxElement? fillChild = null;
		var dockedChildren = new List<(NyxElement Child, Dock Dock)>();

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;
			var dock = GetDock(child);
			if (dock == Dock.Fill) fillChild = child;
			else dockedChildren.Add((child, dock));
		}

		foreach (var (child, dock) in dockedChildren)
		{
			var ds = child.DesiredSize;
			var rect = dock switch
			{
				Dock.Top => new NyxRect(x, y, w, ds.Height),
				Dock.Bottom => new NyxRect(x, y + h - ds.Height, w, ds.Height),
				Dock.Left => new NyxRect(x, y, ds.Width, h),
				Dock.Right => new NyxRect(x + w - ds.Width, y, ds.Width, h),
				_ => NyxRect.Empty,
			};
			child.Arrange(rect);

			switch (dock)
			{
				case Dock.Top: y += ds.Height; h -= ds.Height; break;
				case Dock.Bottom: h -= ds.Height; break;
				case Dock.Left: x += ds.Width; w -= ds.Width; break;
				case Dock.Right: w -= ds.Width; break;
			}
		}

		if (fillChild is not null)
			fillChild.Arrange(new NyxRect(x, y, Math.Max(0, w), Math.Max(0, h)));
	}
}
