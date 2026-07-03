using System;

namespace NyxGui;

/// <summary>
/// Arranges child elements in a grid layout.
/// </summary>
public class NyxGridLayout : NyxLayout
{
	public int Columns { get; set; } = 1;
	public int Rows { get; set; }
	public int Spacing { get; set; }
	public NyxThickness Padding { get; set; }
	public int CellWidth { get; set; }
	public int CellHeight { get; set; }

	/// <summary>
	/// When true, any dimension not constrained by an explicit CellWidth / CellHeight
	/// is automatically stretched to fill the available container space.
	/// Defaults to true.
	///
	/// Rules:
	///   no CellWidth  + FitChildren → cell width fills container width
	///   no CellHeight + FitChildren → cell height fills container height (divided by row count)
	///   both CellWidth and CellHeight set → FitChildren has no effect
	///   neither set   + FitChildren → both dimensions fill the available space
	/// </summary>
	public bool FitChildren { get; set; } = true;

	/// <summary>
	/// If <see cref="FitChildren"/> is true and no explicit cell size is set, the cell
	/// dimension is computed by dividing the remaining inner space (minus spacing) evenly
	/// among rows/columns.
	/// </summary>
	private (int cellW, int cellH) ResolveFixed(int colCount, int rowCount, int innerW, int innerH)
	{
		int cellW = 0;
		int cellH = 0;

		bool fitW = FitChildren && CellWidth <= 0;
		bool fitH = FitChildren && CellHeight <= 0;

		if (fitW)
		{
			var totalSpacingW = Math.Max(0, colCount - 1) * Spacing;
			cellW = Math.Max(0, (innerW - totalSpacingW) / colCount);
		}
		else if (CellWidth > 0)
		{
			cellW = CellWidth;
		}

		if (fitH && rowCount > 0)
		{
			var totalSpacingH = Math.Max(0, rowCount - 1) * Spacing;
			cellH = Math.Max(0, (innerH - totalSpacingH) / rowCount);
		}
		else if (CellHeight > 0)
		{
			cellH = CellHeight;
		}

		return (cellW, cellH);
	}

	/// <summary>
	/// Determines grid dimensions.  If only <see cref="Rows"/> is set (>0 and Columns=0),
	/// the grid fills vertically: rows are fixed, columns expand to fit children.
	/// Otherwise <see cref="Columns"/> determines the column count and rows expand.
	/// </summary>
	private (int colCount, int rowCount) ResolveGrid(int childCount)
	{
		var isVertical = Rows > 0 && Columns <= 0;
		int colCount, rowCount;

		if (isVertical)
		{
			rowCount = Rows;
			colCount = (childCount + rowCount - 1) / rowCount;
		}
		else
		{
			colCount = Math.Max(1, Columns);
			rowCount = (childCount + colCount - 1) / colCount;
		}

		return (colCount, rowCount);
	}

	public override void Measure(NyxContainer container, NyxSize availableSize)
	{
		var innerW = Math.Max(0, availableSize.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, availableSize.Height - Padding.Top - Padding.Bottom);

		var (colCount, rowCount) = ResolveGrid(container.ChildCount);
		var (fixedCellW, fixedCellH) = ResolveFixed(colCount, rowCount, innerW, innerH);

		var maxRowHeights = new int[rowCount];
		var maxColWidths = new int[colCount];
		var isVertical = Rows > 0 && Columns <= 0;

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			int col = isVertical ? (i / rowCount) : (i % colCount);
			int row = isVertical ? (i % rowCount) : (i / colCount);

			var cw = fixedCellW > 0 ? fixedCellW : availableSize.Width;
			var ch = fixedCellH > 0 ? fixedCellH : availableSize.Height;
			child.Measure(new NyxSize(cw, ch));

			var childW = fixedCellW > 0 ? fixedCellW : child.DesiredSize.Width;
			var childH = fixedCellH > 0 ? fixedCellH : child.DesiredSize.Height;

			if (childW > maxColWidths[col])
				maxColWidths[col] = childW;
			if (childH > maxRowHeights[row])
				maxRowHeights[row] = childH;
		}

		int totalW = Padding.Left + Padding.Right;
		if (fixedCellW > 0)
		{
			totalW += colCount * fixedCellW + Math.Max(0, colCount - 1) * Spacing;
		}
		else
		{
			for (var c = 0; c < colCount; c++)
			{
				if (c > 0) totalW += Spacing;
				totalW += maxColWidths[c];
			}
		}

		int totalH = Padding.Top + Padding.Bottom;
		if (fixedCellH > 0)
		{
			totalH += rowCount * fixedCellH + Math.Max(0, rowCount - 1) * Spacing;
		}
		else
		{
			for (var r = 0; r < rowCount; r++)
			{
				if (r > 0) totalH += Spacing;
				totalH += maxRowHeights[r];
			}
		}

		container.DesiredSize = new NyxSize(
			Math.Min(totalW, availableSize.Width),
			Math.Min(totalH, availableSize.Height));
	}

	public override void Arrange(NyxContainer container, NyxRect finalRect)
	{
		var innerW = Math.Max(0, finalRect.Width - Padding.Left - Padding.Right);
		var innerH = Math.Max(0, finalRect.Height - Padding.Top - Padding.Bottom);
		var x = finalRect.X + Padding.Left;
		var y = finalRect.Y + Padding.Top;

		var (colCount, rowCount) = ResolveGrid(container.ChildCount);
		var (fixedCellW, fixedCellH) = ResolveFixed(colCount, rowCount, innerW, innerH);

		var rowHeights = new int[rowCount];
		var colWidths = new int[colCount];
		var isVertical = Rows > 0 && Columns <= 0;

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			int col = isVertical ? (i / rowCount) : (i % colCount);
			int row = isVertical ? (i % rowCount) : (i / colCount);

			var childW = fixedCellW > 0 ? fixedCellW : child.DesiredSize.Width;
			var childH = fixedCellH > 0 ? fixedCellH : child.DesiredSize.Height;

			if (childW > colWidths[col]) colWidths[col] = childW;
			if (childH > rowHeights[row]) rowHeights[row] = childH;
		}

		for (var i = 0; i < container.ChildCount; i++)
		{
			var child = container.Children[i];
			if (!child.Visible) continue;

			int col = isVertical ? (i / rowCount) : (i % colCount);
			int row = isVertical ? (i % rowCount) : (i / colCount);

			var childX = x;
			for (var c = 0; c < col; c++)
			{
				childX += colWidths[c];
				childX += Spacing;
			}

			var childY = y;
			for (var r = 0; r < row; r++)
			{
				childY += rowHeights[r];
				childY += Spacing;
			}

			var cw = colWidths[col];
			var rh = rowHeights[row];

			child.Arrange(new NyxRect(childX, childY, cw, rh));
		}
	}
}
