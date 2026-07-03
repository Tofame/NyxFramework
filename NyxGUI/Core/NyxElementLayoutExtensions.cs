using System;

namespace NyxGui;

/// <summary>
/// Fluent extension methods for programmatically defining edge anchors, margins, and sizes on widgets in C#.
/// </summary>
public static class NyxElementLayoutExtensions
{
	private static NyxLayoutBox EnsureLayoutBox(NyxElement element)
	{
		var box = element.LayoutBox ?? new NyxLayoutBox();
		element.LayoutBox = box;
		return box;
	}

	/// <summary>Anchors the top edge of the widget to the specified target's edge.</summary>
	public static T AnchorTop<T>(this T element, string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Top) where T : NyxElement
	{
		EnsureLayoutBox(element).Top = NyxLayoutAnchor.WidgetEdge(target, edge);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Anchors the bottom edge of the widget to the specified target's edge.</summary>
	public static T AnchorBottom<T>(this T element, string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Bottom) where T : NyxElement
	{
		EnsureLayoutBox(element).Bottom = NyxLayoutAnchor.WidgetEdge(target, edge);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Anchors the left edge of the widget to the specified target's edge.</summary>
	public static T AnchorLeft<T>(this T element, string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Left) where T : NyxElement
	{
		EnsureLayoutBox(element).Left = NyxLayoutAnchor.WidgetEdge(target, edge);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Anchors the right edge of the widget to the specified target's edge.</summary>
	public static T AnchorRight<T>(this T element, string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Right) where T : NyxElement
	{
		EnsureLayoutBox(element).Right = NyxLayoutAnchor.WidgetEdge(target, edge);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Anchors the horizontal center (CenterX) of the widget to the target's horizontal center line.</summary>
	public static T AnchorHorizontalCenter<T>(this T element, string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.CenterX) where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		var anchor = NyxLayoutAnchor.WidgetEdge(target, edge);
		box.Left = anchor;
		box.Right = anchor;
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Anchors the vertical center (CenterY) of the widget to the target's vertical center line.</summary>
	public static T AnchorVerticalCenter<T>(this T element, string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.CenterY) where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		var anchor = NyxLayoutAnchor.WidgetEdge(target, edge);
		box.Top = anchor;
		box.Bottom = anchor;
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Anchors all four edges of the widget to the target's corresponding edges, stretching to fill it.</summary>
	public static T AnchorFill<T>(this T element, string target = "parent") where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		box.Left = NyxLayoutAnchor.WidgetEdge(target, NyxAnchorEdge.Left);
		box.Right = NyxLayoutAnchor.WidgetEdge(target, NyxAnchorEdge.Right);
		box.Top = NyxLayoutAnchor.WidgetEdge(target, NyxAnchorEdge.Top);
		box.Bottom = NyxLayoutAnchor.WidgetEdge(target, NyxAnchorEdge.Bottom);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Applies a uniform layout margin to all sides of the widget.</summary>
	public static T Margin<T>(this T element, int uniform) where T : NyxElement
	{
		EnsureLayoutBox(element).Margin = NyxThickness.Uniform(uniform);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Applies specific layout margins to each side of the widget.</summary>
	public static T Margin<T>(this T element, int left, int top, int right, int bottom) where T : NyxElement
	{
		EnsureLayoutBox(element).Margin = new NyxThickness(left, top, right, bottom);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Applies specific layout margins with optional parameters, preserving unchanged values.</summary>
	public static T Margin<T>(this T element, int? left = null, int? top = null, int? right = null, int? bottom = null) where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		var current = box.Margin;
		box.Margin = new NyxThickness(
			left ?? current.Left,
			top ?? current.Top,
			right ?? current.Right,
			bottom ?? current.Bottom
		);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Applies a uniform layout padding to all sides of the widget.</summary>
	public static T Padding<T>(this T element, int uniform) where T : NyxElement
	{
		EnsureLayoutBox(element).Padding = NyxThickness.Uniform(uniform);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Applies specific layout paddings to each side of the widget.</summary>
	public static T Padding<T>(this T element, int left, int top, int right, int bottom) where T : NyxElement
	{
		EnsureLayoutBox(element).Padding = new NyxThickness(left, top, right, bottom);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Applies specific layout paddings with optional parameters, preserving unchanged values.</summary>
	public static T Padding<T>(this T element, int? left = null, int? top = null, int? right = null, int? bottom = null) where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		var current = box.Padding;
		box.Padding = new NyxThickness(
			left ?? current.Left,
			top ?? current.Top,
			right ?? current.Right,
			bottom ?? current.Bottom
		);
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Specifies a static/fixed layout width for the widget.</summary>
	public static T FixedWidth<T>(this T element, int width) where T : NyxElement
	{
		EnsureLayoutBox(element).FixedWidth = width;
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Specifies a static/fixed layout height for the widget.</summary>
	public static T FixedHeight<T>(this T element, int height) where T : NyxElement
	{
		EnsureLayoutBox(element).FixedHeight = height;
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Specifies a static/fixed layout width and height for the widget.</summary>
	public static T FixedSize<T>(this T element, int width, int height) where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		box.FixedWidth = width;
		box.FixedHeight = height;
		box.FixedSize = true;
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Attaches a stack layout strategy to the container.</summary>
	public static T StackLayout<T>(this T element, Orientation orientation = Orientation.Vertical, int spacing = 0, NyxThickness? padding = null, Alignment alignment = Alignment.Start) where T : NyxElement
	{
		var target = element is NyxScrollablePanel scroll ? scroll.Body : element is NyxMiniWindow mini ? mini.Body : element as NyxContainer;
		if (target is not null)
		{
			target.Layout = new NyxStackLayout
			{
				Orientation = orientation,
				Spacing = spacing,
				Padding = padding ?? NyxThickness.Uniform(0),
				Alignment = alignment
			};
			element.InvalidateLayout();
		}
		return element;
	}

	/// <summary>Attaches a grid layout strategy to the container.</summary>
	public static T GridLayout<T>(this T element, int columns = 1, int rows = 0, int spacing = 0, NyxThickness? padding = null, int cellWidth = 0, int cellHeight = 0, bool? fitChildren = null) where T : NyxElement
	{
		var target = element is NyxScrollablePanel scroll ? scroll.Body : element is NyxMiniWindow mini ? mini.Body : element as NyxContainer;
		if (target is not null)
		{
			var layout = new NyxGridLayout
			{
				Columns = columns,
				Rows = rows,
				Spacing = spacing,
				Padding = padding ?? NyxThickness.Uniform(0),
				CellWidth = cellWidth,
				CellHeight = cellHeight
			};
			if (fitChildren.HasValue)
				layout.FitChildren = fitChildren.Value;
			target.Layout = layout;
			element.InvalidateLayout();
		}
		return element;
	}

	/// <summary>Attaches a dock layout strategy to the container.</summary>
	public static T DockLayout<T>(this T element, NyxThickness? padding = null) where T : NyxElement
	{
		var target = element is NyxScrollablePanel scroll ? scroll.Body : element is NyxMiniWindow mini ? mini.Body : element as NyxContainer;
		if (target is not null)
		{
			target.Layout = new NyxDockLayout
			{
				Padding = padding ?? NyxThickness.Uniform(0)
			};
			element.InvalidateLayout();
		}
		return element;
	}

	/// <summary>Attaches a wrap layout strategy to the container.</summary>
	public static T WrapLayout<T>(this T element, Orientation orientation = Orientation.Horizontal, int spacing = 0, NyxThickness? padding = null) where T : NyxElement
	{
		var target = element is NyxScrollablePanel scroll ? scroll.Body : element is NyxMiniWindow mini ? mini.Body : element as NyxContainer;
		if (target is not null)
		{
			target.Layout = new NyxWrapLayout
			{
				Orientation = orientation,
				Spacing = spacing,
				Padding = padding ?? NyxThickness.Uniform(0)
			};
			element.InvalidateLayout();
		}
		return element;
	}

	/// <summary>Configures the docking edge for a child element inside a Dock layout.</summary>
	public static T DockChild<T>(this T element, Dock dock) where T : NyxElement
	{
		var box = EnsureLayoutBox(element);
		box.Dock = dock;
		element.InvalidateLayout();
		return element;
	}

	/// <summary>Configures the box sizing behavior for the element.</summary>
	public static T BoxSizing<T>(this T element, NyxBoxSizing boxSizing) where T : NyxElement
	{
		element.BoxSizing = boxSizing;
		element.InvalidateLayout();
		return element;
	}
}
