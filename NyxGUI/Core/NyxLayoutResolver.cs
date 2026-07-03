namespace NyxGui;

/// <summary>
/// Resolves edge-anchor layout for free-floating panels.
/// Containers (StackPanel, DockPanel, etc.) use Measure/Arrange — not this resolver.
///
/// <see cref="BuildRect"/> is the core rectangle-construction algorithm.
/// It takes four axis values (left/right/top/bottom) that may or may not be anchored,
/// and resolves a final (x, y, width, height) with the following priority:
///
/// If <b>both</b> opposing anchors are set:
///   - If the difference is &gt; epsilon → the element stretches to fill.
///   - If the anchors coincide (insufficient space) but a fixed size is set → center the fixed size.
///   - Otherwise → snap to the first anchor (left/top) with zero size.
///
/// If only <b>one</b> anchor is set:
///   - Position from the anchor edge, use the fixed size for the dimension.
///
/// If <b>neither</b> anchor is set:
///   - Position from the parent origin, use fixed size or zero.
/// </summary>
public static class NyxLayoutResolver
{
    private const float Epsilon = 0.0001f;

    /// <summary>Resolves bounds for an element with <see cref="NyxElement.LayoutBox"/> set.</summary>
    public static NyxRect Resolve(NyxRect parent, NyxElement element, NyxLayoutContext? context = null)
    {
        var box = element.LayoutBox;
        if (box is null || !box.HasAnyAnchor) return element.Bounds;

        context ??= new NyxLayoutContext { ParentBounds = parent };
		return ResolveEdgeAnchors(parent, element, box, context);
    }

    /// <summary>Re-layouts a subtree that uses edge anchors.</summary>
    public static void RelayoutSubtree(
        NyxContainer root,
        NyxElement changed,
        IReadOnlyDictionary<string, NyxElement> widgetsById,
        NyxGuiLayoutEnvironment environment)
    {
        var container = changed.Parent as NyxContainer ?? root;
        RelayoutContainer(container, widgetsById, environment);
    }

    /// <summary>Re-layouts a tree rooted at a container that uses edge anchors.</summary>
    public static void RelayoutTree(
        NyxContainer root,
        IReadOnlyDictionary<string, NyxElement> widgetsById,
        NyxGuiLayoutEnvironment environment)
    {
        // Auto-size MiniWindow root that has no explicit height.
        if (root is NyxMiniWindow rootMini && rootMini.NeedsAutoSizeFromLayout())
            rootMini.AutoSizeToContent();

        if (root.LayoutBox is { HasAnyAnchor: true } && environment.WindowBounds.Width > 0 && environment.WindowBounds.Height > 0)
        {
            var layoutParent = root.Parent is NyxContainer parent ? parent.Bounds : environment.WindowBounds;
			if (root.Parent is NyxContainer parentContainer)
			{
				var b = parentContainer.GetBorderWidth();
				if (parentContainer.LayoutBox is not null || b > 0)
				{
					var p = parentContainer.LayoutBox?.Padding ?? NyxThickness.Zero;
					layoutParent = new NyxRect(
						layoutParent.X + p.Left + b,
						layoutParent.Y + p.Top + b,
						layoutParent.Width - p.Left - p.Right - 2 * b,
						layoutParent.Height - p.Top - p.Bottom - 2 * b);
				}
			}
            var ctx = environment.CreateContext(layoutParent, widgetsById);
            root.SetBounds(Resolve(layoutParent, root, ctx));
        }

        RelayoutContainer(root, widgetsById, environment);
    }

    /// <summary>Re-layouts direct children of a container that use edge anchors, recursing into nested containers.</summary>
    public static void RelayoutContainer(
        NyxContainer content,
        IReadOnlyDictionary<string, NyxElement> widgetsById,
        NyxGuiLayoutEnvironment environment) =>
        RelayoutContainer(content.Bounds, content, widgetsById, environment);

    /// <summary>Re-layouts direct children against a specific parent rectangle.</summary>
    public static void RelayoutContainer(
        NyxRect layoutParent,
        NyxContainer content,
        IReadOnlyDictionary<string, NyxElement> widgetsById,
        NyxGuiLayoutEnvironment environment)
    {
		// Shrink the layout parent by the container's own padding and border if set.
		var borderVal = content.GetBorderWidth();
		if (content.LayoutBox is not null || borderVal > 0)
		{
			var p = content.LayoutBox?.Padding ?? NyxThickness.Zero;
			layoutParent = new NyxRect(
				layoutParent.X + p.Left + borderVal,
				layoutParent.Y + p.Top + borderVal,
				layoutParent.Width - p.Left - p.Right - 2 * borderVal,
				layoutParent.Height - p.Top - p.Bottom - 2 * borderVal);
		}

		if (content.Layout is not null)
		{
			content.Measure(new NyxSize(layoutParent.Width, layoutParent.Height));
			content.Arrange(layoutParent);
		}
		else
		{
			var ctx = environment.CreateContext(layoutParent, widgetsById);

			var withLayout = new List<NyxElement>();
			foreach (var c in content.Children)
			{
				if (c.LayoutBox is { HasAnyAnchor: true })
					withLayout.Add(c);
			}

			// Topological sort by anchor dependencies so referenced widgets are resolved first.
			// Detects circular dependencies (e.g. widgetA -> widgetB -> widgetA).
			foreach (var child in SortByDependencies(withLayout, environment.RootWindowAnchorId))
				child.SetBounds(Resolve(layoutParent, child, ctx));
		}

        // Recurse into nested containers and internal container-like elements
        foreach (var child in content.Children)
        {
            if (child is NyxMiniWindow mini)
            {
                // Auto-size: expand window height to fit body children if no explicit height.
                if (mini.NeedsAutoSizeFromLayout())
                    mini.AutoSizeToContent();

                var contentBounds = mini.GetContentBounds();
                if (contentBounds.Width > 0 && contentBounds.Height > 0)
                    RelayoutContainer(contentBounds, mini.Body, widgetsById, environment);
            }
            else if (child is NyxContainer nested)
            {
                RelayoutContainer(nested, widgetsById, environment);
            }
            else if (child is NyxScrollablePanel scroll)
            {
                RelayoutContainer(scroll.Body, widgetsById, environment);
				scroll.RefreshLayout();
            }
        }
    }

    /// <summary>
    /// Topological sort of children by their anchor-dependency graph.
    /// In each pass, children whose widget dependencies are already resolved are moved
    /// to the sorted list.  If no progress is made in a pass, the remaining children
    /// have circular or missing dependencies and an exception is thrown.
    /// </summary>
    private static List<NyxElement> SortByDependencies(List<NyxElement> children, string rootWindowAnchorId)
    {
        var remaining = new List<NyxElement>(children);
        var sorted = new List<NyxElement>();
        var resolvedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (remaining.Count > 0)
        {
            var ready = new List<NyxElement>();
            for (var k = 0; k < remaining.Count; k++)
            {
                var c = remaining[k];
                if (c.LayoutBox is null || c.LayoutBox.GetWidgetDependencies(rootWindowAnchorId).All(resolvedIds.Contains))
                    ready.Add(c);
            }

            if (ready.Count == 0)
                throw new InvalidOperationException("Circular or missing anchor dependency between widgets. Check anchors.* references.");

            foreach (var c in ready)
            {
                sorted.Add(c);
                remaining.Remove(c);
                if (!string.IsNullOrEmpty(c.Id))
                    resolvedIds.Add(c.Id);
            }
        }

        return sorted;
    }

    private static NyxRect ResolveEdgeAnchors(NyxRect parent, NyxElement element, NyxLayoutBox box, NyxLayoutContext ctx)
    {
        var m = box.Margin;
        var ax0 = ResolveAxis(box.Left, parent, ctx, horizontal: true) + m.Left;
        var ax1 = ResolveAxis(box.Right, parent, ctx, horizontal: true) - m.Right;
        var ay0 = ResolveAxis(box.Top, parent, ctx, horizontal: false) + m.Top;
        var ay1 = ResolveAxis(box.Bottom, parent, ctx, horizontal: false) - m.Bottom;

		var border = element.GetBorderWidth();
		var padding = box.Padding;
		var horizontalPaddingAndBorder = padding.Left + padding.Right + 2 * border;
		var verticalPaddingAndBorder = padding.Top + padding.Bottom + 2 * border;

		var fixedWidth = box.FixedWidth;
		var fixedHeight = box.FixedHeight;

		if (element.BoxSizing == NyxBoxSizing.ContentBox)
		{
			if (fixedWidth > 0)
				fixedWidth += horizontalPaddingAndBorder;
			if (fixedHeight > 0)
				fixedHeight += verticalPaddingAndBorder;
		}

		return BuildRect(ax0, ax1, ay0, ay1, box, fixedWidth, fixedHeight,
			box.Left is not null, box.Right is not null,
			box.Top is not null, box.Bottom is not null);
    }

    private static float ResolveAxis(NyxLayoutAnchor? anchor, NyxRect parent, NyxLayoutContext ctx, bool horizontal)
    {
        if (anchor is not { } a)
            return horizontal ? parent.X : parent.Y;

        var target = GetTargetBounds(a.TargetId!, parent, ctx);
        return horizontal ? ResolveX(target, a.Edge) : ResolveY(target, a.Edge);
    }

    private static NyxRect GetTargetBounds(string targetId, NyxRect parent, NyxLayoutContext ctx)
    {
        if (targetId.Equals("parent", StringComparison.OrdinalIgnoreCase)) return ctx.ParentBounds;
        if (NyxGuiRootWindow.IsRootWindowTarget(targetId, ctx.RootWindowAnchorId)) return ctx.WindowBounds;
        if (ctx.WidgetsById.TryGetValue(targetId, out var widget)) return widget.Bounds;

        throw new InvalidOperationException($"Unknown anchor target \"{targetId}\". Use parent, {ctx.RootWindowAnchorId}, or a widget id.");
    }

    private static float ResolveX(NyxRect r, NyxAnchorEdge edge) => edge switch
    {
        NyxAnchorEdge.Left => r.X,
        NyxAnchorEdge.Right => r.Right,
        NyxAnchorEdge.CenterX => r.X + r.Width * 0.5f,
        _ => r.X,
    };

    private static float ResolveY(NyxRect r, NyxAnchorEdge edge) => edge switch
    {
        NyxAnchorEdge.Top => r.Y,
        NyxAnchorEdge.Bottom => r.Bottom,
        NyxAnchorEdge.CenterY => r.Y + r.Height * 0.5f,
        _ => r.Y,
    };

    /// <summary>
    /// Resolves final (x, y, width, height) from four axis values and anchor presence.
    ///
    /// The logic handles all combinations of left/right and top/bottom anchoring:
    /// <list type="bullet">
    ///   <item><b>Both sides anchored:</b> stretch to fill the gap, center with fixed size, or snap to origin.</item>
    ///   <item><b>One side anchored:</b> position from that edge, use fixed size.</item>
    ///   <item><b>Neither anchored:</b> use fixed size at the origin-derived position.</item>
    /// </list>
    /// </summary>
    private static NyxRect BuildRect(float ax0, float ax1, float ay0, float ay1, NyxLayoutBox box, int fixedWidth, int fixedHeight,
        bool hasLeft, bool hasRight, bool hasTop, bool hasBottom)
    {
        int x = 0, y = 0, w = 0, h = 0;

        // ── Horizontal ──
        if (hasLeft && hasRight)
        {
            if (ax1 - ax0 > Epsilon)
            {
                x = (int)MathF.Round(ax0);
                w = Math.Max(0, (int)MathF.Round(ax1 - ax0));
            }
            else if (fixedWidth > 0)
            {
                w = fixedWidth;
                x = (int)MathF.Round((ax0 + ax1) * 0.5f - w * 0.5f);
            }
            else { x = (int)MathF.Round(ax0); }
        }
        else if (hasRight)
        {
            w = Math.Max(0, fixedWidth);
            x = (int)MathF.Round(ax1 - w);
        }
        else if (hasLeft)
        {
            w = Math.Max(0, fixedWidth);
            x = (int)MathF.Round(ax0);
        }
        else if (ax1 - ax0 > Epsilon)
        {
            x = (int)MathF.Round(ax0);
            w = Math.Max(0, (int)MathF.Round(ax1 - ax0));
        }
        else { w = Math.Max(0, fixedWidth); x = (int)MathF.Round(ax0); }

        // ── Vertical ──
        if (hasTop && hasBottom)
        {
            if (ay1 - ay0 > Epsilon)
            {
                y = (int)MathF.Round(ay0);
                h = Math.Max(0, (int)MathF.Round(ay1 - ay0));
            }
            else if (fixedHeight > 0)
            {
                h = fixedHeight;
                y = (int)MathF.Round((ay0 + ay1) * 0.5f - h * 0.5f);
            }
            else { y = (int)MathF.Round(ay0); }
        }
        else if (hasBottom)
        {
            h = Math.Max(0, fixedHeight);
            y = (int)MathF.Round(ay1 - h);
        }
        else if (hasTop)
        {
            h = Math.Max(0, fixedHeight);
            y = (int)MathF.Round(ay0);
        }
        else if (ay1 - ay0 > Epsilon)
        {
            y = (int)MathF.Round(ay0);
            h = Math.Max(0, (int)MathF.Round(ay1 - ay0));
        }
        else { h = Math.Max(0, fixedHeight); y = (int)MathF.Round(ay0); }

        return new NyxRect(x, y, w, h);
    }
}
