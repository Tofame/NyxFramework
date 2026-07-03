namespace NyxGui.Definitions;

/// <summary>
/// Reads <c>snake_case</c> keys from <see cref="NyxGuiPropertyBag"/> and applies layout, images,
/// fonts, widget states, and common fields to <see cref="NyxElement"/> objects.
///
/// Properties are resolved in priority order:
/// 1. <see cref="ApplyLayout"/> sets edge anchors, margins, padding, dock, and compound sizes.
/// 2. <see cref="ApplyLayoutType"/> sets the layout engine (stack/grid/dock/wrap).
/// 3. Appearance properties: font, image, icon, text-offset.
/// 4. <see cref="NyxGuiWidgetStateApplicator.ApplyWidgetStates"/> applies visual state overrides.
///
/// <b>Compound sizes</b> (<c>width = { value = 300, min = 100, max = 600, fixed = 250 }</c>)
/// support both shorthand and full forms.  The shorthand <c>width = 300</c> is equivalent to
/// <c>fixed_width = 300</c>.  The compound form allows min/max constraints for resizable widgets.
///
/// <b>Edge anchors</b> support two syntaxes:
/// <list type="bullet">
///   <item>Inline: <c>anchors.top = "parent.top"</c>, <c>anchors.left = "widgetId.right"</c></item>
///   <item>Table: <c>[anchors]\ntop = "parent.top"\nleft = "widgetId.right"</c></item>
///   <item>Shortcut: <c>anchors = "fill"</c> → fill all four parent edges</item>
/// </list>
///
/// <b>Center anchors</b> (<c>horizontalCenter</c>, <c>verticalCenter</c>) pin both opposing
/// sides to the same center line, centering the element horizontally or vertically.
/// </summary>
internal static class NyxGuiDefinitionProperties
{
    public static string RequireString(NyxGuiPropertyBag table, string key, string context)
    {
        if (!TryGetString(table, key, out var s) || string.IsNullOrWhiteSpace(s))
            throw new InvalidDataException($"{context}: missing or empty \"{key}\".");
        return s;
    }

    public static bool TryGetString(NyxGuiPropertyBag table, string key, out string value)
    {
        value = string.Empty;
        if (!table.TryGetValue(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            string s => s,
            _ => obj.ToString() ?? string.Empty
        };
        return true;
    }

    public static bool TryGetBool(NyxGuiPropertyBag table, string key, out bool value)
    {
        value = false;
        if (!table.TryGetValue(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var b) && b,
            long l => l != 0,
            int i => i != 0,
            _ => false
        };
        return true;
    }

    public static bool TryGetFloat(NyxGuiPropertyBag table, string key, out float value)
    {
        value = 0;
        if (!table.TryGetValue(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            float f => f,
            double d => (float)d,
            long l => l,
            int i => i,
            string s => float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0,
            _ => 0
        };
        return true;
    }

    public static bool TryGetInt(NyxGuiPropertyBag table, string key, out int value)
    {
        value = 0;
        if (!table.TryGetValue(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            int i => i,
            long l => (int)l,
            float f => (int)f,
            double d => (int)d,
            string s => int.TryParse(s, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var i) ? i : 0,
            _ => 0
        };
        return true;
    }

    public static bool TryGetRect(NyxGuiPropertyBag table, string key, out NyxRect rect)
    {
        rect = default;
        if (!table.TryGetValue(key, out var obj))
            return false;

        if (NyxGuiPropertyBag.TryWrap(obj) is { } nested)
        {
            TryGetInt(nested, "x", out var x);
            TryGetInt(nested, "y", out var y);
            TryGetInt(nested, "width", out var w);
            TryGetInt(nested, "height", out var h);
            rect = new NyxRect(x, y, w, h);
            return true;
        }

        if (obj is not string s)
            return false;

        var parts = s.Split((char[]?)[' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            return false;

        if (!int.TryParse(parts[0], out var x0) ||
            !int.TryParse(parts[1], out var y0) ||
            !int.TryParse(parts[2], out var w0) ||
            !int.TryParse(parts[3], out var h0))
            return false;

        rect = new NyxRect(x0, y0, w0, h0);
        return true;
    }

    public static bool TryGetColor(NyxGuiPropertyBag table, string key, out NyxColor color)
    {
        color = default;
        if (!table.TryGetValue(key, out var obj) || obj is null)
            return false;
        if (obj is NyxColor c)
        {
            color = c;
            return true;
        }
        if (!TryGetString(table, key, out var s) || string.IsNullOrWhiteSpace(s))
            return false;
        return NyxColor.TryParseHex(s.Trim(), out color);
    }

    public static bool TryGetThickness(NyxGuiPropertyBag table, string key, out NyxThickness thickness)
    {
        thickness = default;
        if (!table.TryGetValue(key, out var obj))
            return false;

        if (NyxGuiPropertyBag.TryWrap(obj) is { } nested)
        {
            TryGetInt(nested, "left", out var left);
            TryGetInt(nested, "top", out var top);
            TryGetInt(nested, "right", out var right);
            TryGetInt(nested, "bottom", out var bottom);
            thickness = new NyxThickness(left, top, right, bottom);
            return true;
        }

        if (obj is not string s)
            return false;

        var parts = s.Split((char[]?)[' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && int.TryParse(parts[0], out var u))
        {
            thickness = NyxThickness.Uniform(u);
            return true;
        }

        if (parts.Length == 4 &&
            int.TryParse(parts[0], out var l) &&
            int.TryParse(parts[1], out var t) &&
            int.TryParse(parts[2], out var r) &&
            int.TryParse(parts[3], out var b))
        {
            thickness = new NyxThickness(l, t, r, b);
            return true;
        }

        return false;
    }

    public static void ApplyCommon(NyxElement element, NyxGuiPropertyBag table, NyxGuiLoadOptions options)
    {
        if (TryGetString(table, "id", out var id))
            element.Id = id;

        if (TryGetBool(table, "visible", out var visible))
            element.Visible = visible;
        if (TryGetBool(table, "enablement", out var enablement))
            element.Enabled = enablement;
        else if (TryGetBool(table, "events", out var events))
            element.Enabled = events;
        if (TryGetBool(table, "draggable", out var drag))
        {
            if (element is NyxContainer container)
                container.Draggable = drag;
        }
        if (TryGetBool(table, "phantom", out var phantom))
            element.Phantom = phantom;
        if (TryGetBool(table, "focusable", out var focusable))
            element.Focusable = focusable;
        if (TryGetString(table, "tooltip", out var tooltip))
            element.Tooltip = tooltip.Replace("\\n", "\n", StringComparison.Ordinal);
        if (TryGetInt(table, "tooltip_delay_ms", out var tipDelay))
            element.TooltipDelayMs = Math.Max(0, tipDelay);
        if (TryGetFloat(table, "opacity", out var opacity))
            element.Opacity = Math.Clamp(opacity, 0f, 1f);

		if (TryGetString(table, "box_sizing", out var boxSizingStr))
		{
			if (boxSizingStr.Equals("border_box", StringComparison.OrdinalIgnoreCase))
				element.BoxSizing = NyxBoxSizing.BorderBox;
			else if (boxSizingStr.Equals("content_box", StringComparison.OrdinalIgnoreCase))
				element.BoxSizing = NyxBoxSizing.ContentBox;
		}

        if (TryGetInt(table, "internal_id", out var iid))
            element.InternalId = (uint)iid;

		if (element is NyxWidget widget)
			ApplyWidgetFixedSize(widget, table);

        ApplyLayout(element, table);
        ApplyLayoutType(element, table);
        ApplyFont(element, table, options);
        ApplyImage(element, table, options);
        ApplyIcon(element, table, options);
        ApplyTextOffset(element, table);
        ApplyMiniWindowFields(element, table);
        NyxGuiWidgetStateApplicator.ApplyWidgetStates(element, table, options);
    }

	private static void ApplyWidgetFixedSize(NyxWidget widget, NyxGuiPropertyBag table)
	{
		if (TryGetInt(table, "fixed_width", out var fw))
			widget.FixedWidth = fw;
		else if (TryGetCompoundSize(table, "width", out var wv, out _, out _, out var wfixed))
			widget.FixedWidth = wfixed > 0 ? wfixed : (wv > 0 ? wv : 0);
		else if (TryGetInt(table, "width", out var sw) && sw > 0)
			widget.FixedWidth = sw;

		if (TryGetInt(table, "fixed_height", out var fh))
			widget.FixedHeight = fh;
		else if (TryGetCompoundSize(table, "height", out var hv, out _, out _, out var hfixed))
			widget.FixedHeight = hfixed > 0 ? hfixed : (hv > 0 ? hv : 0);
		else if (TryGetInt(table, "height", out var sh) && sh > 0)
			widget.FixedHeight = sh;
	}

    private static void ApplyMiniWindowFields(NyxElement element, NyxGuiPropertyBag table)
    {
        if (element is not NyxMiniWindow mini)
            return;

        if (TryGetString(table, "title", out var title))
            mini.Title = title;

        if (TryGetInt(table, "image_border_top", out var top))
            mini.TitleBarHeight = Math.Max(14, top);

        if (TryGetBool(table, "resizable", out var resizable))
            mini.Resizable = resizable;

        if (TryGetBool(table, "auto_size", out var autoSize))
            mini.AutoSize = autoSize;

        if (TryGetInt(table, "resize_grip_height", out var gripH))
            mini.ResizeGripHitHeight = Math.Max(1, gripH);

        if (TryGetInt(table, "min_height", out var minH))
            mini.MinExpandedHeight = Math.Max(mini.TitleBarHeight, minH);

        if (TryGetInt(table, "max_height", out var maxH))
            mini.MaxExpandedHeight = Math.Max(mini.TitleBarHeight, maxH);

        // height = { fixed = ... } or height = { fixed = true } makes the window non-resizable
        if (TryGetCompoundSize(table, "height", out _, out _, out _, out var hFixed) && hFixed > 0)
            mini.Resizable = false;
    }

    public static NyxRect ReadBounds(NyxGuiPropertyBag table)
    {
        if (TryGetRect(table, "rect", out var rect))
            return EnsureFixedSizeBounds(table, rect);

        TryGetInt(table, "x", out var x);
        TryGetInt(table, "y", out var y);
        TryGetInt(table, "width", out var w);
        TryGetInt(table, "height", out var h);
        return EnsureFixedSizeBounds(table, new NyxRect(x, y, w, h));
    }

    /// <summary>Applies <c>fixed-width</c> / <c>fixed-height</c> and compound <c>width</c>/<c>height</c> when explicit bounds are missing.</summary>
    public static NyxRect EnsureFixedSizeBounds(NyxGuiPropertyBag table, NyxRect bounds)
    {
        // Compound width = { value/min/max/fixed }
        if (bounds.Width <= 0 && TryGetCompoundSize(table, "width", out var wv, out var wmin, out var wmax, out var wfixed))
        {
            if (wfixed > 0)
                bounds = new NyxRect(bounds.X, bounds.Y, Math.Max(1, wfixed), bounds.Height);
            else if (wv > 0)
                bounds = new NyxRect(bounds.X, bounds.Y, Math.Max(1, wv), bounds.Height);
        }
        // Shorthand: width = 300 (same as fixed_width)
        if (bounds.Width <= 0 && TryGetInt(table, "width", out var sw) && sw > 0)
            bounds = new NyxRect(bounds.X, bounds.Y, Math.Max(1, sw), bounds.Height);
        // Legacy
        if (bounds.Width <= 0 && TryGetInt(table, "fixed_width", out var fw))
            bounds = new NyxRect(bounds.X, bounds.Y, Math.Max(1, fw), bounds.Height);

        if (bounds.Height <= 0 && TryGetCompoundSize(table, "height", out var hv, out var hmin, out var hmax, out var hfixed))
        {
            if (hfixed > 0)
                bounds = new NyxRect(bounds.X, bounds.Y, bounds.Width, Math.Max(1, hfixed));
            else if (hv > 0)
                bounds = new NyxRect(bounds.X, bounds.Y, bounds.Width, Math.Max(1, hv));
        }
        if (bounds.Height <= 0 && TryGetInt(table, "height", out var sh) && sh > 0)
            bounds = new NyxRect(bounds.X, bounds.Y, bounds.Width, Math.Max(1, sh));
        if (bounds.Height <= 0 && TryGetInt(table, "fixed_height", out var fh))
            bounds = new NyxRect(bounds.X, bounds.Y, bounds.Width, Math.Max(1, fh));

        return bounds;
    }

    private static void ApplyLayout(NyxElement element, NyxGuiPropertyBag table)
    {
        var hasEdgeAnchors = ApplyAnchorsTable(element, table);

        if (!hasEdgeAnchors && !HasMarginKeys(table) && !HasPaddingKeys(table) && !table.ContainsKey("dock"))
            return;

        var box = element.LayoutBox ?? new NyxLayoutBox();

        ApplyMargins(box, table);
        ApplyPadding(box, table);

        ApplySizeToLayoutBox(box, table);

        if (TryGetString(table, "dock", out var dockStr))
        {
            if (Enum.TryParse<Dock>(dockStr, true, out var d))
                box.Dock = d;
        }

        // Legacy
        if (TryGetInt(table, "fixed_width", out var fw))
            box.FixedWidth = fw;
        if (TryGetInt(table, "fixed_height", out var fh))
            box.FixedHeight = fh;

        element.LayoutBox = box;
    }

    /// <summary>Applies edge anchors and margins from a supplemental <c>widgetId.anchors</c> property table.</summary>
    public static void ApplyLayoutAnchorsOnly(NyxElement element, NyxGuiPropertyBag anchorsTable)
    {
        var box = element.LayoutBox ?? new NyxLayoutBox();
        var any = ApplyAnchorsFromTable(box, anchorsTable);

        if (HasMarginKeys(anchorsTable))
        {
            ApplyMargins(box, anchorsTable);
            any = true;
        }

        if (HasPaddingKeys(anchorsTable))
        {
            ApplyPadding(box, anchorsTable);
            any = true;
        }

        if (TryGetString(anchorsTable, "dock", out var dockStr))
        {
            if (Enum.TryParse<Dock>(dockStr, true, out var d))
            {
                box.Dock = d;
                any = true;
            }
        }

        if (TryGetInt(anchorsTable, "fixed_width", out var fw))
        {
            box.FixedWidth = fw;
            any = true;
        }

        if (TryGetInt(anchorsTable, "fixed_height", out var fh))
        {
            box.FixedHeight = fh;
            any = true;
        }

        if (TryGetCompoundSize(anchorsTable, "width", out var wv, out var wmin, out var wmax, out var wfixed))
        {
            if (wfixed > 0) { box.FixedWidth = wfixed; box.FixedSize = true; box.MinWidth = 0; box.MaxWidth = 0; }
            else { if (wv > 0) box.FixedWidth = wv; box.MinWidth = wmin; box.MaxWidth = wmax; }
            any = true;
        }

        if (TryGetCompoundSize(anchorsTable, "height", out var hv, out var hmin, out var hmax, out var hfixed))
        {
            if (hfixed > 0) { box.FixedHeight = hfixed; box.FixedSize = true; box.MinHeight = 0; box.MaxHeight = 0; }
            else { if (hv > 0) box.FixedHeight = hv; box.MinHeight = hmin; box.MaxHeight = hmax; }
            any = true;
        }

        if (any)
            element.LayoutBox = box;
    }

    /// <summary>
    /// Applies edge anchors from either a nested <c>anchors</c> property table or inline
    /// <c>anchors.{side}</c> keys.  An <c>anchors = "fill"</c> shorthand sets all four edges
    /// to parent edges (the element fills its parent).
    /// </summary>
    private static bool ApplyAnchorsTable(NyxElement element, NyxGuiPropertyBag table)
    {
        var box = element.LayoutBox ?? new NyxLayoutBox();
        var any = false;

        if (table.TryGetValue("anchors", out var anchorsObj))
        {
            if (NyxGuiPropertyBag.TryWrap(anchorsObj) is { } anchorsTable)
            {
                any |= ApplyAnchorsFromTable(box, anchorsTable);
            }
            else
            {
                // anchors = fill shorthand → anchors.fill = "parent"
                any |= TryApplyAnchorKey(box, "fill", "parent");
            }
        }

        foreach (var (key, value) in table)
        {
            if (!key.StartsWith("anchors.", StringComparison.OrdinalIgnoreCase))
                continue;

            var side = key["anchors.".Length..];
            if (TryApplyAnchorKey(box, side, value))
                any = true;
        }

        if (any)
            element.LayoutBox = box;

        return any;
    }

    private static bool ApplyAnchorsFromTable(NyxLayoutBox box, NyxGuiPropertyBag anchorsTable)
    {
        var any = false;
        foreach (var (key, value) in anchorsTable)
        {
            if (TryApplyAnchorKey(box, key, value))
                any = true;
        }

        return any;
    }

    private static bool TryApplyAnchorKey(NyxLayoutBox box, string side, object? value)
    {
        if (side.Equals("fill", StringComparison.OrdinalIgnoreCase))
        {
            var target = value switch
            {
                string s => s,
                _ => value?.ToString() ?? "parent"
            };
            return TryApplyFillAnchor(box, target);
        }

        if (TryApplyCenterAxisAnchor(box, side, value))
            return true;

        var text = value switch
        {
            string s => s,
            _ => value?.ToString() ?? string.Empty
        };

        if (!NyxLayoutAnchor.TryParse(text, out var anchor))
            return false;

        SetAnchorSide(box, side, anchor);
        return true;
    }

    /// <summary><c>anchors.fill = "parent"</c> → stretch to all four parent edges.</summary>
    private static bool TryApplyFillAnchor(NyxLayoutBox box, string target)
    {
        if (!target.Equals("parent", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"anchors.fill target \"{target}\" is not supported (use \"parent\").");

        box.Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left);
        box.Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right);
        box.Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top);
        box.Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom);
        return true;
    }

    private static void SetAnchorSide(NyxLayoutBox box, string side, NyxLayoutAnchor anchor)
    {
        switch (side.ToLowerInvariant())
        {
            case "left":
                box.Left = anchor;
                break;
            case "right":
                box.Right = anchor;
                break;
            case "top":
                box.Top = anchor;
                break;
            case "bottom":
                box.Bottom = anchor;
                break;
            default:
                throw new InvalidDataException(
                    $"Unknown anchor side \"{side}\" (use left, right, top, bottom, fill, horizontalCenter, or verticalCenter).");
        }
    }

    /// <summary>
    /// <c>anchors.horizontalCenter = "parent.horizontalCenter"</c> (pins left+right on that X line).
    /// <c>anchors.verticalCenter = "otherId.verticalCenter"</c> (pins top+bottom on that Y line).
    /// </summary>
    private static bool TryApplyCenterAxisAnchor(NyxLayoutBox box, string side, object? value)
    {
        var horizontal = side.Equals("horizontalcenter", StringComparison.OrdinalIgnoreCase) ||
                         side.Equals("horizontal_center", StringComparison.OrdinalIgnoreCase);
        var vertical = side.Equals("verticalcenter", StringComparison.OrdinalIgnoreCase) ||
                       side.Equals("vertical_center", StringComparison.OrdinalIgnoreCase);
        if (!horizontal && !vertical)
            return false;

        var expectedEdge = horizontal ? NyxAnchorEdge.CenterX : NyxAnchorEdge.CenterY;

        NyxLayoutAnchor anchor;
        switch (value)
        {
            case bool b when b:
                anchor = NyxLayoutAnchor.ParentEdge(expectedEdge);
                break;
            case string text when NyxLayoutAnchor.TryParse(text, out var parsed):
                if (parsed.Edge != expectedEdge)
                {
                    throw new InvalidDataException(
                        $"anchors.{side} expects a horizontal_center edge (e.g. \"parent.horizontalCenter\"), not \"{text}\".");
                }

                anchor = parsed;
                break;
            default:
                return false;
        }

        if (horizontal)
        {
            box.Left = anchor;
            box.Right = anchor;
        }
        else
        {
            box.Top = anchor;
            box.Bottom = anchor;
        }

        return true;
    }

    private static bool HasMarginKeys(NyxGuiPropertyBag table) =>
        table.ContainsKey("margin") ||
        table.ContainsKey("margin_left") ||
        table.ContainsKey("margin_right") ||
        table.ContainsKey("margin_top") ||
        table.ContainsKey("margin_bottom");

	private static bool HasPaddingKeys(NyxGuiPropertyBag table) =>
		table.ContainsKey("padding") ||
		table.ContainsKey("padding_left") ||
		table.ContainsKey("padding_right") ||
		table.ContainsKey("padding_top") ||
		table.ContainsKey("padding_bottom");

    /// <summary><c>margin</c> sets defaults; <c>margin-top</c> etc. override individual sides.</summary>
    /// <summary>
    /// Applies size properties to the layout box.
    ///
    /// Resolution priority (highest wins):
    /// 1. Compound <c>width = { ... }</c> — if <c>fixed</c> is set, overrides all others with FixedSize=true.
    /// 2. Compound <c>width = { value = ... }</c> — sets fixed width + min/max constraints.
    /// 3. Shorthand <c>width = 300</c> — equivalent to fixed_width.
    /// 4. Legacy <c>min_width</c>/<c>max_width</c> keys.
    /// </summary>
    private static void ApplySizeToLayoutBox(NyxLayoutBox box, NyxGuiPropertyBag table)
    {
        // Compound: width = { value = 300, min = 100, max = 600, fixed = 300 }
        if (TryGetCompoundSize(table, "width", out var wv, out var wmin, out var wmax, out var wfixed))
        {
            if (wfixed > 0) { box.FixedWidth = wfixed; box.FixedSize = true; box.MinWidth = 0; box.MaxWidth = 0; }
            else { box.FixedWidth = wv > 0 ? wv : box.FixedWidth; box.MinWidth = wmin; box.MaxWidth = wmax; }
        }
        // Shorthand: width = 300
        if (TryGetInt(table, "width", out var sw) && sw > 0)
            box.FixedWidth = sw;
        // Compound: height = { value = 300, min = 100, max = 600, fixed = 300 }
        if (TryGetCompoundSize(table, "height", out var hv, out var hmin, out var hmax, out var hfixed))
        {
            if (hfixed > 0) { box.FixedHeight = hfixed; box.FixedSize = true; box.MinHeight = 0; box.MaxHeight = 0; }
            else { box.FixedHeight = hv > 0 ? hv : box.FixedHeight; box.MinHeight = hmin; box.MaxHeight = hmax; }
        }
        // Shorthand: height = 300
        if (TryGetInt(table, "height", out var sh) && sh > 0)
            box.FixedHeight = sh;
        // Legacy: min_height / min_width
        if (TryGetInt(table, "min_width", out var mnw) && mnw > 0) box.MinWidth = mnw;
        if (TryGetInt(table, "min_height", out var mnh) && mnh > 0) box.MinHeight = mnh;
        if (TryGetInt(table, "max_width", out var mxw) && mxw > 0) box.MaxWidth = mxw;
        if (TryGetInt(table, "max_height", out var mxh) && mxh > 0) box.MaxHeight = mxh;
    }

    /// <summary>
    /// Parses a compound size property: <c>width = { value = 300, min = 100, max = 600, fixed = 250 }</c>.
    /// Returns true if the compound table was found.
    /// </summary>
    /// <summary>
    /// Parses a compound size property from a nested property bag.
    /// Accepts <c>value</c>, <c>min</c>, <c>max</c>, and <c>fixed</c> keys.
    /// Returns true only if at least one size key has a value > 0.
    /// </summary>
    private static bool TryGetCompoundSize(
        NyxGuiPropertyBag table, string key,
        out int sizeValue, out int sizeMin, out int sizeMax, out int sizeFixed)
    {
        sizeValue = 0; sizeMin = 0; sizeMax = 0; sizeFixed = 0;

        if (!table.TryGetNested(key, out var nested))
            return false;

        nested.TryGetInt("value", out sizeValue);
        nested.TryGetInt("min", out sizeMin);
        nested.TryGetInt("max", out sizeMax);
        nested.TryGetInt("fixed", out sizeFixed);

        return sizeValue > 0 || sizeMin > 0 || sizeMax > 0 || sizeFixed > 0;
    }

    private static void ApplyMargins(NyxLayoutBox box, NyxGuiPropertyBag table)
    {
        var left = 0;
        var top = 0;
        var right = 0;
        var bottom = 0;
        var any = false;

        if (TryGetThickness(table, "margin", out var margin))
        {
            left = margin.Left;
            top = margin.Top;
            right = margin.Right;
            bottom = margin.Bottom;
            any = true;
        }

        if (TryGetMarginSide(table, "left", out var ml))
        {
            left = ml;
            any = true;
        }

        if (TryGetMarginSide(table, "right", out var mr))
        {
            right = mr;
            any = true;
        }

        if (TryGetMarginSide(table, "top", out var mt))
        {
            top = mt;
            any = true;
        }

        if (TryGetMarginSide(table, "bottom", out var mb))
        {
            bottom = mb;
            any = true;
        }

        if (any)
            box.Margin = new NyxThickness(left, top, right, bottom);
    }

    private static bool TryGetMarginSide(NyxGuiPropertyBag table, string side, out int value) =>
        TryGetInt(table, $"margin_{side}", out value);

	private static void ApplyPadding(NyxLayoutBox box, NyxGuiPropertyBag table)
	{
		var left = 0;
		var top = 0;
		var right = 0;
		var bottom = 0;
		var any = false;

		if (TryGetThickness(table, "padding", out var padding))
		{
			left = padding.Left;
			top = padding.Top;
			right = padding.Right;
			bottom = padding.Bottom;
			any = true;
		}

		if (TryGetPaddingSide(table, "left", out var pl))
		{
			left = pl;
			any = true;
		}

		if (TryGetPaddingSide(table, "right", out var pr))
		{
			right = pr;
			any = true;
		}

		if (TryGetPaddingSide(table, "top", out var pt))
		{
			top = pt;
			any = true;
		}

		if (TryGetPaddingSide(table, "bottom", out var pb))
		{
			bottom = pb;
			any = true;
		}

		if (any)
			box.Padding = new NyxThickness(left, top, right, bottom);
	}

	private static bool TryGetPaddingSide(NyxGuiPropertyBag table, string side, out int value) =>
		TryGetInt(table, $"padding_{side}", out value);

	private static void ApplyLayoutType(NyxElement element, NyxGuiPropertyBag table)
	{
		NyxContainer? target = null;
		if (element is NyxScrollablePanel scroll)
			target = scroll.Body;
		else if (element is NyxMiniWindow mini)
			target = mini.Body;
		else
			target = element as NyxContainer;

		if (target is null) return;

		if (table.TryGetValue("layout", out var layoutObj) && layoutObj is not null)
		{
			if (layoutObj is string layoutStr)
			{
				var type = layoutStr.Trim().ToLowerInvariant();
				target.Layout = CreateLayoutFromType(type, null);
			}
			else if (NyxGuiPropertyBag.TryWrap(layoutObj) is { } layoutTable)
			{
				if (TryGetString(layoutTable, "type", out var typeStr))
				{
					var type = typeStr.Trim().ToLowerInvariant();
					target.Layout = CreateLayoutFromType(type, layoutTable);
				}
			}
		}
	}

	private static NyxLayout? CreateLayoutFromType(string type, NyxGuiPropertyBag? table)
	{
		switch (type)
		{
			case "stack":
			case "vertical":
			case "horizontal":
			{
				var layout = new NyxStackLayout();
				layout.Orientation = type == "horizontal" ? Orientation.Horizontal : Orientation.Vertical;
				if (table is not null)
				{
					if (TryGetString(table, "orientation", out var orientStr))
						layout.Orientation = orientStr.Equals("horizontal", StringComparison.OrdinalIgnoreCase) ? Orientation.Horizontal : Orientation.Vertical;
					if (TryGetInt(table, "spacing", out var spacing))
						layout.Spacing = spacing;
					if (TryGetThickness(table, "padding", out var padding))
						layout.Padding = padding;
					if (TryGetString(table, "alignment", out var alignStr))
					{
						if (Enum.TryParse<Alignment>(alignStr, true, out var align))
							layout.Alignment = align;
					}
				}
				return layout;
			}
			case "grid":
			{
				var layout = new NyxGridLayout();
				if (table is not null)
				{
					if (TryGetInt(table, "columns", out var cols))
						layout.Columns = cols;
					if (TryGetInt(table, "rows", out var rows))
						layout.Rows = rows;

					if (TryGetInt(table, "spacing", out var spacing))
						layout.Spacing = spacing;

					if (TryGetBool(table, "fit_children", out var fit))
						layout.FitChildren = fit;

					if (TryGetThickness(table, "padding", out var padding))
						layout.Padding = padding;

					if (TryGetIntPair(table, "cell_size", out var cw, out var ch))
					{
						layout.CellWidth = cw;
						layout.CellHeight = ch;
					}
					else
					{
						if (TryGetInt(table, "cell_width", out var w))
							layout.CellWidth = w;
						if (TryGetInt(table, "cell_height", out var h))
							layout.CellHeight = h;
					}
				}
				return layout;
			}
			case "dock":
			{
				var layout = new NyxDockLayout();
				if (table is not null)
				{
					if (TryGetThickness(table, "padding", out var padding))
						layout.Padding = padding;
				}
				return layout;
			}
			case "wrap":
			{
				var layout = new NyxWrapLayout();
				if (table is not null)
				{
					if (TryGetString(table, "orientation", out var orientStr))
						layout.Orientation = orientStr.Equals("vertical", StringComparison.OrdinalIgnoreCase) ? Orientation.Vertical : Orientation.Horizontal;
					if (TryGetInt(table, "spacing", out var spacing))
						layout.Spacing = spacing;
					if (TryGetThickness(table, "padding", out var padding))
						layout.Padding = padding;
				}
				return layout;
			}
			default:
				return null;
		}
	}

    private static void ApplyImage(NyxElement element, NyxGuiPropertyBag table, NyxGuiLoadOptions options)
    {
        if (!TryGetString(table, "image_source", out var src) || string.IsNullOrWhiteSpace(src))
            return;

        var style = new NyxImageStyle
        {
            ImageSource = options.ResolveImagePath(src.Trim()),
        };

        if (TryGetBool(table, "image_fixed_ratio", out var fixedRatio))
            style.ImageFixedRatio = fixedRatio;
        if (TryGetBool(table, "image_smooth", out var smooth))
            style.ImageSmooth = smooth;
        if (TryGetRect(table, "image_rect", out var irect))
            style.ImageRect = irect;
        if (TryGetRect(table, "image_clip", out var iclip))
            style.ImageClip = iclip;
        ApplyImageBorders(style, table);
        if (TryGetString(table, "image_color", out var colorStr) &&
            NyxColor.TryParseHex(colorStr, out var ic))
            style.ImageColor = ic;
        if (TryGetString(table, "image_object_fit", out var ofStr) || TryGetString(table, "object_fit", out ofStr))
        {
			var normalizedOfStr = ofStr.Replace("-", "").Replace("_", "");
            if (Enum.TryParse<NyxObjectFit>(normalizedOfStr, true, out var of))
                style.ImageObjectFit = of;
        }

        element.Image = style;
    }

    internal static void ApplyImageBorders(NyxImageStyle style, NyxGuiPropertyBag table)
    {
        var top = 0;
        var right = 0;
        var bottom = 0;
        var left = 0;
        var any = false;

        if (TryGetInt(table, "image_border", out var all))
        {
            top = right = bottom = left = all;
            any = true;
        }

        if (TryGetInt(table, "image_border_top", out var t))
        {
            top = t;
            any = true;
        }

        if (TryGetInt(table, "image_border_right", out var r))
        {
            right = r;
            any = true;
        }

        if (TryGetInt(table, "image_border_bottom", out var b))
        {
            bottom = b;
            any = true;
        }

        if (TryGetInt(table, "image_border_left", out var l))
        {
            left = l;
            any = true;
        }

        if (any)
            style.ImageBorders = new NyxImageBorders(top, right, bottom, left);
    }

    private static void ApplyIcon(NyxElement element, NyxGuiPropertyBag table, NyxGuiLoadOptions options)
    {
        if (!TryGetString(table, "icon", out var src) || string.IsNullOrWhiteSpace(src))
            return;

        var style = new NyxIconStyle
        {
            IconSource = options.ResolveImagePath(src.Trim()),
        };

        style.HasExplicitSize = TryGetIntPair(table, "icon_size", out var iw, out var ih);
        if (style.HasExplicitSize)
        {
            style.Width = Math.Max(1, iw);
            style.Height = Math.Max(1, ih);
        }

        if (TryGetIntPair(table, "icon_offset", out var ox, out var oy))
        {
            style.OffsetX = ox;
            style.OffsetY = oy;
        }

        if (TryGetRect(table, "icon_rect", out var iconRect))
            style.DestinationRect = iconRect;

        if (TryGetString(table, "icon_align", out var alignRaw))
        {
            style.Align = alignRaw.ToLowerInvariant() switch
            {
                "right" => NyxIconAlign.Right,
                "center" or "centre" => NyxIconAlign.Center,
                "left" => NyxIconAlign.Left,
                _ => throw new InvalidDataException(
                    $"icon-align \"{alignRaw}\" is not supported (use left, center, or right)."),
            };
        }

        if (TryGetRect(table, "icon_clip", out var clip))
            style.Clip = clip;

        if (TryGetBool(table, "icon_smooth", out var smooth))
            style.Smooth = smooth;

        if (TryGetString(table, "icon_color", out var colorStr) &&
            NyxColor.TryParseHex(colorStr, out var ic))
            style.Color = ic;

        style.ResolveSize(style.IconSource);
        element.Icon = style;
    }

    private static void ApplyFont(NyxElement element, NyxGuiPropertyBag table, NyxGuiLoadOptions options)
    {
        string? file = null;
        float? size = null;
        var bold = false;
        var outlined = false;
        var any = false;

        if (TryGetString(table, "font", out var fontRaw) && !string.IsNullOrWhiteSpace(fontRaw))
        {
            file = options.ResolveFontFile(fontRaw.Trim());
            any = true;
        }

        if (TryGetFloat(table, "font_size", out var fontSize) && fontSize > 0f)
        {
            size = fontSize;
            any = true;
        }

        if (TryGetBool(table, "font_bold", out var fontBold) && fontBold)
        {
            bold = true;
            any = true;
        }

        if (TryGetBool(table, "text_outline", out var textOutline) && textOutline)
        {
            outlined = true;
            any = true;
        }

        if (!any)
            return;

        element.Font = new NyxFontStyle
        {
            File = file,
            SizePt = size,
            Bold = bold,
            Outlined = outlined,
        };
    }

    private static void ApplyTextOffset(NyxElement element, NyxGuiPropertyBag table)
    {
        if (TryGetIntPair(table, "text_offset", out var x, out var y))
        {
            element.TextOffsetX = x;
            element.TextOffsetY = y;
        }
    }

    public static bool TryGetIntPair(NyxGuiPropertyBag table, string key, out int a, out int b)
    {
        a = 0;
        b = 0;
        if (!table.TryGetValue(key, out var obj))
            return false;

        if (NyxGuiPropertyBag.TryWrap(obj) is { } nested)
        {
            TryGetInt(nested, "width", out a);
            TryGetInt(nested, "height", out b);
            if (a == 0 && TryGetInt(nested, "x", out var x))
                a = x;
            if (b == 0 && TryGetInt(nested, "y", out var y))
                b = y;
            return a > 0 || b > 0;
        }

        if (obj is not string s)
            return false;

        var parts = s.Split((char[]?)[' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        return int.TryParse(parts[0], out a) && int.TryParse(parts[1], out b);
    }

    public static string? TryGetParentId(NyxGuiPropertyBag table) =>
        TryGetString(table, "parent", out var p) ? p : null;

    public static bool TryGetTextWrap(NyxGuiPropertyBag table, out bool wrap) =>
        TryGetBool(table, "text_wrap", out wrap);

    public static NyxTextAlign ParseTextAlign(string raw, bool wrap)
    {
        if (raw.Equals("right", StringComparison.OrdinalIgnoreCase))
            return NyxTextAlign.TopRight;
        if (raw.Equals("center", StringComparison.OrdinalIgnoreCase))
            return wrap ? NyxTextAlign.TopCenter : NyxTextAlign.Center;
        return NyxTextAlign.TopLeft;
    }

    public static bool TryParseTextAlign(NyxGuiPropertyBag table, bool wrap, out NyxTextAlign align)
    {
        align = NyxTextAlign.TopLeft;
        if (!TryGetString(table, "text_align", out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        align = ParseTextAlign(raw, wrap);
        return true;
    }
}
