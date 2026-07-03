using System;
using System.Collections.Generic;

namespace NyxGui;

/// <summary>
/// Arranges child elements in a vertical or horizontal stack.
/// </summary>
public class NyxStackLayout : NyxLayout
{
	public Orientation Orientation { get; set; } = Orientation.Vertical;
	public int Spacing { get; set; }
	public NyxThickness Padding { get; set; }
	public Alignment Alignment { get; set; } = Alignment.Start;

	/// <summary>
	/// Measures children in the stack direction (vertical or horizontal).
	/// Accumulates the stack-axis sizes with spacing between children, and tracks
	/// the maximum cross-axis size.  The cross axis is used for alignment in Arrange.
	/// </summary>
	public override void Measure(NyxContainer container, NyxSize availableSize)
	{
		var innerW = Math.Max(0, availableSize.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, availableSize.Height - Padding.Top - Padding.Bottom);
		var childAvailable = Orientation == Orientation.Vertical
			? new NyxSize(innerW, int.MaxValue)
			: new NyxSize(int.MaxValue, innerH);

		int stackSize = 0, crossSize = 0;
		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			child.Measure(childAvailable);
			if (i > 0) stackSize += Spacing;

			stackSize += Orientation == Orientation.Vertical
				? child.DesiredSize.Height
				: child.DesiredSize.Width;

			var childCross = Orientation == Orientation.Vertical
				? child.DesiredSize.Width
				: child.DesiredSize.Height;
			if (childCross > crossSize) crossSize = childCross;
		}

		var desiredW = Orientation == Orientation.Horizontal ? stackSize : crossSize;
		var desiredH = Orientation == Orientation.Horizontal ? crossSize : stackSize;

		container.DesiredSize = new NyxSize(
			Math.Min(desiredW + Padding.Left + Padding.Right, availableSize.Width),
			Math.Min(desiredH + Padding.Top + Padding.Bottom, availableSize.Height));
	}

	/// <summary>
	/// Positions children along the stack axis with spacing, and aligns each child
	/// along the cross axis according to <see cref="Alignment"/> (Start, Center, End, Stretch).
	/// Stretch uses the full container cross-size; others use the child's desired cross-size.
	/// </summary>
	public override void Arrange(NyxContainer container, NyxRect finalRect)
	{
		var innerX = finalRect.X + Padding.Left;
		var innerY = finalRect.Y + Padding.Top;
		var innerW = Math.Max(0, finalRect.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, finalRect.Height - Padding.Top - Padding.Bottom);
		var innerCross = Orientation == Orientation.Vertical ? innerW : innerH;

		int offset = 0;
		var visibleChildren = new List<NyxElement>();
		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (child.Visible) visibleChildren.Add(child);
		}

		for (var i = 0; i < visibleChildren.Count; i++)
		{
			var child = visibleChildren[i];
			if (i > 0) offset += Spacing;

			var childSize = child.DesiredSize;
			int childMain, childCross;

			if (Orientation == Orientation.Vertical)
			{
				childMain = childSize.Height;
				childCross = Alignment == Alignment.Stretch ? innerW : childSize.Width;
			}
			else
			{
				childMain = childSize.Width;
				childCross = Alignment == Alignment.Stretch ? innerH : childSize.Height;
			}

			int crossPos = Alignment switch
			{
				Alignment.Start => 0,
				Alignment.Center => Math.Max(0, (innerCross - childCross) / 2),
				Alignment.End => Math.Max(0, innerCross - childCross),
				Alignment.Stretch => 0,
				_ => 0,
			};

			var childRect = Orientation == Orientation.Vertical
				? new NyxRect(innerX + crossPos, innerY + offset, childCross, childMain)
				: new NyxRect(innerX + offset, innerY + crossPos, childMain, childCross);

			child.Arrange(childRect);
			offset += childMain;
		}
	}
}
