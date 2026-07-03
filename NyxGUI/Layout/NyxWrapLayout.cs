using System;

namespace NyxGui;

/// <summary>
/// Arranges child elements in a wrapping layout.
/// </summary>
public class NyxWrapLayout : NyxLayout
{
	public Orientation Orientation { get; set; } = Orientation.Horizontal;
	public int Spacing { get; set; }
	public NyxThickness Padding { get; set; }

	public override void Measure(NyxContainer container, NyxSize availableSize)
	{
		var innerW = Math.Max(0, availableSize.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, availableSize.Height - Padding.Top - Padding.Bottom);

		if (Orientation == Orientation.Horizontal)
			MeasureHorizontal(container, innerW, innerH, availableSize);
		else
			MeasureVertical(container, innerW, innerH, availableSize);
	}

	/// <summary>
	/// Horizontal wrapping: children flow left-to-right.  When a child would overflow
	/// the inner width, a new row starts.  Each row's height is the max child height
	/// in that row.
	/// </summary>
	private void MeasureHorizontal(NyxContainer container, int innerW, int innerH, NyxSize availableSize)
	{
		int lineWidth = 0, lineHeight = 0, totalHeight = 0;
		var lineCount = 0;

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			child.Measure(new NyxSize(innerW, innerH));
			var cw = child.DesiredSize.Width;
			var ch = child.DesiredSize.Height;

			if (lineWidth > 0 && lineWidth + Spacing + cw > innerW)
			{
				totalHeight += lineHeight;
				if (lineCount > 0) totalHeight += Spacing;
				lineWidth = 0;
				lineHeight = 0;
				lineCount++;
			}

			lineWidth += cw + (lineWidth > 0 ? Spacing : 0);
			if (ch > lineHeight) lineHeight = ch;
		}

		if (lineHeight > 0)
		{
			totalHeight += lineHeight;
			if (lineCount > 0) totalHeight += Spacing;
		}

		container.DesiredSize = new NyxSize(
			Math.Min(innerW + Padding.Left + Padding.Right, availableSize.Width),
			Math.Min(totalHeight + Padding.Top + Padding.Bottom, availableSize.Height));
	}

	/// <summary>
	/// Vertical wrapping: children flow top-to-bottom.  When a child would overflow
	/// the inner height, a new column starts.  Each column's width is the max child width
	/// in that column.
	/// </summary>
	private void MeasureVertical(NyxContainer container, int innerW, int innerH, NyxSize availableSize)
	{
		int lineHeight = 0, lineWidth = 0, totalWidth = 0;
		var lineCount = 0;

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			child.Measure(new NyxSize(innerW, innerH));
			var cw = child.DesiredSize.Width;
			var ch = child.DesiredSize.Height;

			if (lineHeight > 0 && lineHeight + Spacing + ch > innerH)
			{
				totalWidth += lineWidth;
				if (lineCount > 0) totalWidth += Spacing;
				lineHeight = 0;
				lineWidth = 0;
				lineCount++;
			}

			lineHeight += ch + (lineHeight > 0 ? Spacing : 0);
			if (cw > lineWidth) lineWidth = cw;
		}

		if (lineWidth > 0)
		{
			totalWidth += lineWidth;
			if (lineCount > 0) totalWidth += Spacing;
		}

		container.DesiredSize = new NyxSize(
			Math.Min(totalWidth + Padding.Left + Padding.Right, availableSize.Width),
			Math.Min(innerH + Padding.Top + Padding.Bottom, availableSize.Height));
	}

	public override void Arrange(NyxContainer container, NyxRect finalRect)
	{
		var innerW = Math.Max(0, finalRect.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, finalRect.Height - Padding.Top - Padding.Bottom);
		var startX = finalRect.X + Padding.Left;
		var startY = finalRect.Y + Padding.Top;

		if (Orientation == Orientation.Horizontal)
			ArrangeHorizontal(container, startX, startY, innerW, innerH);
		else
			ArrangeVertical(container, startX, startY, innerW, innerH);
	}

	/// <summary>
	/// Positions children left-to-right in rows, wrapping to the next row when the
	/// current row would exceed the inner width.  Rows are separated by <see cref="Spacing"/>.
	/// </summary>
	private void ArrangeHorizontal(NyxContainer container, int startX, int startY, int innerW, int innerH)
	{
		int x = startX, y = startY, lineHeight = 0;

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			var cw = child.DesiredSize.Width;
			var ch = child.DesiredSize.Height;

			if (x > startX && x + cw > startX + innerW)
			{
				x = startX;
				y += lineHeight + Spacing;
				lineHeight = 0;
			}

			child.Arrange(new NyxRect(x, y, cw, ch));
			x += cw + Spacing;
			if (ch > lineHeight) lineHeight = ch;
		}
	}

	/// <summary>
	/// Positions children top-to-bottom in columns, wrapping to the next column when the
	/// current column would exceed the inner height.  Columns are separated by <see cref="Spacing"/>.
	/// </summary>
	private void ArrangeVertical(NyxContainer container, int startX, int startY, int innerW, int innerH)
	{
		int x = startX, y = startY, lineWidth = 0;

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			var cw = child.DesiredSize.Width;
			var ch = child.DesiredSize.Height;

			if (y > startY && y + ch > startY + innerH)
			{
				x += lineWidth + Spacing;
				y = startY;
				lineWidth = 0;
			}

			child.Arrange(new NyxRect(x, y, cw, ch));
			y += ch + Spacing;
			if (cw > lineWidth) lineWidth = cw;
		}
	}
}
