namespace NyxGui.Definitions;

/// <summary>Builds a NyxGUI widget tree from a <see cref="NyxGuiBuildSpec"/> produced by <see cref="NyxuiParser"/>.</summary>
public static class NyxGuiDefinitionBuilder
{
    public static NyxGuiBuiltDocument Build(NyxGuiBuildSpec spec, NyxGuiLoadOptions? options = null)
    {
        options ??= new NyxGuiLoadOptions();
        var documentRootId = options.RootId ?? spec.DocumentRootId;
        var rootWindowAnchorId = spec.RootWindowAnchorId;

        var byId = new Dictionary<string, NyxElement>(StringComparer.OrdinalIgnoreCase);
        var containers = new Dictionary<string, NyxElement>(StringComparer.OrdinalIgnoreCase);
        var bindKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var eventLinks = new List<NyxGuiEventLink>();

        // Phase 1: Create all widgets
        foreach (var def in spec.Widgets)
        {
            var id = def.Id;
            if (string.IsNullOrEmpty(id))
                throw new InvalidDataException($"Widget at line {def.SourceLine} has no id.");
            if (byId.ContainsKey(id))
                throw new InvalidDataException($"Duplicate widget id \"{id}\".");

            var table = NyxGuiPropertyBag.From(def.Properties);
            var element = CreateWidget(def.Kind, table, options);
            NyxGuiDefinitionProperties.ApplyCommon(element, table, options);
            ApplyInlineStates(element, def.States, options);
            element.Id = id;
            byId[id] = element;

            if (TryGetBindKey(table, out var bindKey))
                bindKeys[id] = bindKey;

            if (TryGetEventAction(table, "on_file_selected", out var actFileSelected))
                eventLinks.Add(new NyxGuiEventLink(id, "file_selected", actFileSelected));

            if (TryGetEventAction(table, "on_click", out var actClick))
                eventLinks.Add(new NyxGuiEventLink(id, "click", actClick));

            if (TryGetEventAction(table, "on_right_click", out var actRightClick))
                eventLinks.Add(new NyxGuiEventLink(id, "right_click", actRightClick));

            if (TryGetEventAction(table, "on_value_change", out var actValChange))
                eventLinks.Add(new NyxGuiEventLink(id, "value_change", actValChange));

            if (TryGetEventAction(table, "on_change", out var actChange))
                eventLinks.Add(new NyxGuiEventLink(id, "change", actChange));

            if (TryGetEventAction(table, "on_selection_change", out var actSelChange))
                eventLinks.Add(new NyxGuiEventLink(id, "selection_change", actSelChange));

            if (TryGetEventAction(table, "on_double_click", out var actDbClick))
                eventLinks.Add(new NyxGuiEventLink(id, "double_click", actDbClick));

            if (TryGetEventAction(table, "on_mouse_enter", out var actMouseEnter))
                eventLinks.Add(new NyxGuiEventLink(id, "mouse_enter", actMouseEnter));

            if (TryGetEventAction(table, "on_mouse_leave", out var actMouseLeave))
                eventLinks.Add(new NyxGuiEventLink(id, "mouse_leave", actMouseLeave));

            if (TryGetEventAction(table, "on_key_down", out var actKeyDown))
                eventLinks.Add(new NyxGuiEventLink(id, "key_down", actKeyDown));

            if (TryGetEventAction(table, "on_key_up", out var actKeyUp))
                eventLinks.Add(new NyxGuiEventLink(id, "key_up", actKeyUp));

            if (TryGetEventAction(table, "on_focus_gained", out var actFocusGained))
                eventLinks.Add(new NyxGuiEventLink(id, "focus_gained", actFocusGained));

            if (TryGetEventAction(table, "on_focus_lost", out var actFocusLost))
                eventLinks.Add(new NyxGuiEventLink(id, "focus_lost", actFocusLost));

            if (TryGetEventAction(table, "on_drag_start", out var actDragStart))
                eventLinks.Add(new NyxGuiEventLink(id, "drag_start", actDragStart));

            if (TryGetEventAction(table, "on_drag_enter", out var actDragEnter))
                eventLinks.Add(new NyxGuiEventLink(id, "drag_enter", actDragEnter));

            if (TryGetEventAction(table, "on_drag_leave", out var actDragLeave))
                eventLinks.Add(new NyxGuiEventLink(id, "drag_leave", actDragLeave));

            if (TryGetEventAction(table, "on_drag_over", out var actDragOver))
                eventLinks.Add(new NyxGuiEventLink(id, "drag_over", actDragOver));

            if (TryGetEventAction(table, "on_drop", out var actDrop))
                eventLinks.Add(new NyxGuiEventLink(id, "drop", actDrop));

            if (TryGetEventAction(table, "on_drag_end", out var actDragEnd))
                eventLinks.Add(new NyxGuiEventLink(id, "drag_end", actDragEnd));


            // Track containers for child parenting
            if (element is NyxMiniWindow mini)
                containers[id] = mini.Body;
            else if (element is NyxContainer container)
                containers[id] = container;
            else if (element is NyxScrollablePanel scroll)
                containers[id] = scroll.Body;
        }

        ApplyRadioGroupSelections(byId);

        // Phase 2: Parent widgets using ParentId from parser
        foreach (var def in spec.Widgets)
        {
            var parentId = def.ParentId;
            if (string.IsNullOrWhiteSpace(parentId))
                continue;
            if (!byId.TryGetValue(def.Id, out var child))
                continue;
            if (!containers.TryGetValue(parentId, out var parent))
                throw new InvalidDataException($"Widget \"{def.Id}\" parent \"{parentId}\" is not a container.");

            if (parent is NyxContainer c)
                c.AddChild(child);
        }

		// Phase 2.5: Link custom scrollbars and settings to ScrollablePanels
		foreach (var def in spec.Widgets)
		{
			if (!byId.TryGetValue(def.Id, out var element) || element is not NyxScrollablePanel scroll)
				continue;

			var table = NyxGuiPropertyBag.From(def.Properties);

			if (NyxGuiDefinitionProperties.TryGetString(table, "vertical_scrollbar", out var vBarId))
			{
				if (byId.TryGetValue(vBarId, out var barElement) && barElement is NyxVScrollBar vBar)
					scroll.VerticalScrollBar = vBar;
				else
					throw new InvalidDataException($"ScrollablePanel \"{def.Id}\" vertical_scrollbar reference \"{vBarId}\" not found or not a VScrollBar.");
			}

			if (NyxGuiDefinitionProperties.TryGetString(table, "horizontal_scrollbar", out var hBarId))
			{
				if (byId.TryGetValue(hBarId, out var barElement) && barElement is NyxHScrollBar hBar)
					scroll.HorizontalScrollBar = hBar;
				else
					throw new InvalidDataException($"ScrollablePanel \"{def.Id}\" horizontal_scrollbar reference \"{hBarId}\" not found or not a HScrollBar.");
			}

			if (NyxGuiDefinitionProperties.TryGetBool(table, "inverted_scroll", out var inverted))
			{
				scroll.InvertedScroll = inverted;
			}
		}

        // Phase 3: Determine root
        var layoutEnv = new NyxGuiLayoutEnvironment
        {
            WindowBounds = options.InitialWindowWidth > 0 && options.InitialWindowHeight > 0
                ? new NyxRect(0, 0, options.InitialWindowWidth, options.InitialWindowHeight)
                : NyxRect.Empty,
            RootWindowAnchorId = rootWindowAnchorId,
        };

        NyxElement root;
        if (!string.IsNullOrWhiteSpace(documentRootId))
        {
            if (!byId.TryGetValue(documentRootId, out var rootEl) || rootEl is not NyxContainer rootContent)
                throw new InvalidDataException($"document.root \"{documentRootId}\" is missing or not a container.");
            root = rootContent;
        }
        else
        {
            var definedParents = new HashSet<string?>(spec.Widgets.Select(w => w.ParentId), StringComparer.OrdinalIgnoreCase);
            var orphans = byId.Values.Where(e => !definedParents.Contains(e.Id)).ToList();
            if (orphans.Count == 1 && orphans[0] is NyxContainer single)
                root = single;
            else
            {
                var implicitRoot = new NyxContainer(NyxRect.Empty);
                foreach (var o in orphans)
                    implicitRoot.AddChild(o);
                root = implicitRoot;
            }
        }

        var settings = options.Settings ?? spec.Settings;
        var document = new NyxGuiBuiltDocument(root, byId, settings, rootWindowAnchorId);
        document.RegisterBindings(bindKeys);
        document.RegisterEventLinks(eventLinks);

        // Ensure MiniWindow internal body is synced before layout resolves children
        if (root is NyxMiniWindow miniWin)
            miniWin.SetBounds(miniWin.Bounds);

        if (layoutEnv.WindowBounds.Width > 0 && layoutEnv.WindowBounds.Height > 0)
            document.SetWindowSize(layoutEnv.WindowBounds.Width, layoutEnv.WindowBounds.Height);
        else if (root is NyxContainer rc)
            NyxLayoutResolver.RelayoutTree(rc, byId, layoutEnv);

        return document;
    }

    private static bool TryGetBindKey(NyxGuiPropertyBag table, out string bindKey)
    {
        bindKey = string.Empty;
        if (!NyxGuiDefinitionProperties.TryGetString(table, "bind_key", out bindKey))
            return false;
        return !string.IsNullOrWhiteSpace(bindKey);
    }

    private static bool TryGetEventAction(NyxGuiPropertyBag table, string key, out string actionName)
    {
        actionName = string.Empty;
        if (!NyxGuiDefinitionProperties.TryGetString(table, key, out actionName))
            return false;
        return !string.IsNullOrWhiteSpace(actionName);
    }

    private static void ApplyRadioGroupSelections(IReadOnlyDictionary<string, NyxElement> byId)
    {
        foreach (var element in byId.Values)
        {
            if (element is NyxRadioButton { IsChecked: true } rb)
                NyxRadioGroup.Select(rb);
        }
    }

    private static NyxElement CreateWidget(string typeName, NyxGuiPropertyBag table, NyxGuiLoadOptions options)
    {
        var bounds = NyxGuiDefinitionProperties.ReadBounds(table);
        return typeName.ToLowerInvariant() switch
        {
            "container" => new NyxContainer(bounds),
            "dockpanel" => CreateDockPanel(table, bounds),
            "miniwindow" => CreateMiniWindow(table, bounds),
            "scrollablepanel" => CreateScrollable(table, bounds),
            "label" => CreateLabel(table, bounds),
            "extendedlabel" => CreateExtendedLabel(table, bounds),
            "button" => CreateButton(table, bounds),
            "filedialogbutton" => CreateFileDialogButton(table, bounds),
            "checkbox" => CreateCheckBox(table, bounds),
            "radiobutton" => CreateRadioButton(table, bounds),
            "textbox" or "textedit" => CreateTextBox(table, bounds),
            "textarea" => CreateTextArea(table, bounds),
            "combobox" => CreateComboBox(table, bounds),
            "image" => new NyxImage { Bounds = bounds },
            "imagecontainer" => CreateImageContainer(table, bounds),
            "slider" => CreateSlider(table, bounds),
            "progressbar" => CreateProgressBar(table, bounds),
            "tooltip" => CreateTooltip(table, bounds),
            "table" => CreateTable(table, bounds),
            "graph" => CreateGraph(table, bounds),
            "separator" => new NyxSeparator(bounds),
            "vscrollbar" => CreateVScrollBar(table, bounds),
            "hscrollbar" => CreateHScrollBar(table, bounds),
            "scrollbar" => CreateScrollBar(table, bounds),
            _ => throw new InvalidDataException($"Unknown widget type \"{typeName}\"."),
        };
    }

    private static NyxElement CreateVScrollBar(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var bar = new NyxVScrollBar(bounds);
        if (NyxGuiDefinitionProperties.TryGetInt(table, "step", out var step))
            bar.ScrollStep = step;
        return bar;
    }

    private static NyxElement CreateHScrollBar(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var bar = new NyxHScrollBar(bounds);
        if (NyxGuiDefinitionProperties.TryGetInt(table, "step", out var step))
            bar.ScrollStep = step;
        return bar;
    }

    private static NyxElement CreateScrollBar(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var vertical = true;
        if (NyxGuiDefinitionProperties.TryGetString(table, "orientation", out var o))
            vertical = !o.Equals("horizontal", StringComparison.OrdinalIgnoreCase);
        var bar = vertical ? (NyxElement)new NyxVScrollBar(bounds) : new NyxHScrollBar(bounds);
        if (NyxGuiDefinitionProperties.TryGetInt(table, "step", out var step))
        {
            if (bar is NyxVScrollBar vb) vb.ScrollStep = step;
            else if (bar is NyxHScrollBar hb) hb.ScrollStep = step;
        }
        return bar;
    }

	private static NyxElement CreateDockPanel(NyxGuiPropertyBag table, NyxRect bounds)
	{
		var panel = new NyxDockPanel(bounds);
		if (NyxGuiDefinitionProperties.TryGetInt(table, "margin", out var m))
			panel.Margin = m;
		if (NyxGuiDefinitionProperties.TryGetInt(table, "gap", out var g))
			panel.Gap = g;
		return panel;
	}

    private static NyxMiniWindow CreateMiniWindow(NyxGuiPropertyBag table, NyxRect bounds)
    {
        bounds = NyxGuiDefinitionProperties.EnsureFixedSizeBounds(table, bounds);
        var win = new NyxMiniWindow { Bounds = bounds };
        if (NyxGuiDefinitionProperties.TryGetInt(table, "title_bar_height", out var th))
            win.TitleBarHeight = Math.Max(14, th);
        return win;
    }

    private static NyxScrollablePanel CreateScrollable(NyxGuiPropertyBag table, NyxRect bounds)
    {
        return new NyxScrollablePanel(bounds);
    }

    private static NyxLabel CreateLabel(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var text = NyxGuiDefinitionProperties.TryGetString(table, "text", out var t) ? t : string.Empty;
        text = text.Replace("\\n", "\n", StringComparison.Ordinal);
        var label = new NyxLabel { Bounds = bounds, Text = text };
        if (NyxGuiDefinitionProperties.TryGetTextWrap(table, out var wrap))
            label.Wrap = wrap;
        if (NyxGuiDefinitionProperties.TryParseTextAlign(table, label.Wrap, out var align))
            label.Align = align;
        if (NyxGuiDefinitionProperties.TryGetInt(table, "line_height", out var lh))
            label.LineHeight = Math.Max(8, lh);
        return label;
    }

    private static NyxExtendedLabel CreateExtendedLabel(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var label = new NyxExtendedLabel { Bounds = bounds };
        var text = NyxGuiDefinitionProperties.TryGetString(table, "text", out var t) ? t : string.Empty;
        var defaultColor = NyxColor.FromRgb(220, 220, 225);
        if (NyxGuiDefinitionProperties.TryGetColor(table, "color", out var c))
            defaultColor = c;
        label.SetMarkup(text, defaultColor);
        if (NyxGuiDefinitionProperties.TryGetTextWrap(table, out var wrap))
            label.Wrap = wrap;
        if (NyxGuiDefinitionProperties.TryParseTextAlign(table, label.Wrap, out var align))
            label.Align = align;
        return label;
    }

    private static NyxButton CreateButton(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var label = NyxGuiDefinitionProperties.TryGetString(table, "label", out var l)
            ? l
            : NyxGuiDefinitionProperties.TryGetString(table, "text", out var t)
                ? t
                : "Button";
        NyxGuiDefinitionProperties.TryGetString(table, "description", out var desc);
        return new NyxButton { Bounds = bounds, Label = label, Description = desc ?? string.Empty };
    }

    private static NyxFileDialogButton CreateFileDialogButton(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var btn = new NyxFileDialogButton { Bounds = bounds };

        if (NyxGuiDefinitionProperties.TryGetString(table, "button_label", out var bl))
            btn.ButtonLabel = bl;

        if (NyxGuiDefinitionProperties.TryGetInt(table, "button_width", out var bw))
            btn.ButtonWidth = bw;

        if (NyxGuiDefinitionProperties.TryGetString(table, "placeholder", out var ph))
            btn.PlaceholderText = ph;

        if (NyxGuiDefinitionProperties.TryGetBool(table, "show_path", out var sp))
            btn.ShowSelectedPath = sp;

        if (NyxGuiDefinitionProperties.TryGetString(table, "mode", out var mode))
            btn.Mode = mode.Equals("save", StringComparison.OrdinalIgnoreCase)
                ? NyxFileDialogMode.Save
                : NyxFileDialogMode.Open;

        // Dialog options
        var opts = new NyxFileDialogOptions();

        if (NyxGuiDefinitionProperties.TryGetString(table, "dialog_title", out var dt))
            opts.Title = dt;

        if (NyxGuiDefinitionProperties.TryGetString(table, "filter_label", out var fl))
            opts.FilterLabel = fl;

        if (NyxGuiDefinitionProperties.TryGetString(table, "default_extension", out var de))
            opts.DefaultExtension = de;

        if (NyxGuiDefinitionProperties.TryGetString(table, "initial_directory", out var idir))
            opts.InitialDirectory = idir;

        // extensions = ["png", "jpg"]
        if (table.TryGetValue("extensions", out var extObj) && extObj is IReadOnlyList<object?> extList)
        {
            var exts = new List<string>();
            foreach (var e in extList)
                if (e is string s && !string.IsNullOrWhiteSpace(s))
                    exts.Add(s.TrimStart('.'));
            if (exts.Count > 0)
                opts.Extensions = exts.ToArray();
        }

        btn.DialogOptions = opts;
        return btn;
    }

    private static NyxCheckBox CreateCheckBox(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var label = NyxGuiDefinitionProperties.TryGetString(table, "label", out var l) ? l : string.Empty;
        var isChecked = NyxGuiDefinitionProperties.TryGetBool(table, "checked", out var c) && c;
        return new NyxCheckBox { Bounds = bounds, Label = label, IsChecked = isChecked };
    }

    private static NyxRadioButton CreateRadioButton(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var label = NyxGuiDefinitionProperties.TryGetString(table, "label", out var l) ? l : string.Empty;
        var group = NyxGuiDefinitionProperties.TryGetString(table, "group", out var g) ? g : "default";
        var isChecked = NyxGuiDefinitionProperties.TryGetBool(table, "checked", out var c) && c;
        var rb = new NyxRadioButton { Bounds = bounds, Label = label, Group = group };
        if (isChecked) rb.SetCheckedSilently(true);
        return rb;
    }

    private static NyxTextBox CreateTextBox(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var text = NyxGuiDefinitionProperties.TryGetString(table, "text", out var t) ? t : string.Empty;
        var box = new NyxTextBox { Bounds = bounds, Text = text };
        ApplyTextEntryProperties(box, table);
        return box;
    }

    private static void ApplyTextEntryProperties(NyxTextBox box, NyxGuiPropertyBag table)
    {
        if (NyxGuiDefinitionProperties.TryGetInt(table, "max_length", out var max))
            box.MaxLength = max;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "read_only", out var ro))
            box.ReadOnly = ro;
        if (NyxGuiDefinitionProperties.TryParseTextAlign(table, wrap: false, out var align))
            box.Align = align;
    }

    private static NyxTextArea CreateTextArea(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var text = NyxGuiDefinitionProperties.TryGetString(table, "text", out var t) ? t : string.Empty;
        text = text.Replace("\\n", "\n", StringComparison.Ordinal);
        var area = new NyxTextArea(bounds, text);
        if (NyxGuiDefinitionProperties.TryGetInt(table, "max_length", out var max))
            area.MaxLength = max;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "read_only", out var ro))
            area.ReadOnly = ro;
        if (NyxGuiDefinitionProperties.TryGetInt(table, "line_height", out var lh))
            area.LineHeight = Math.Max(8, lh);
        if (NyxGuiDefinitionProperties.TryGetInt(table, "scroll_bar_width", out var sw))
            area.ScrollBarWidth = Math.Max(0, sw);
        if (NyxGuiDefinitionProperties.TryGetTextWrap(table, out var wrap))
            area.Wrap = wrap;
        if (NyxGuiDefinitionProperties.TryParseTextAlign(table, area.Wrap, out var align))
            area.Align = align;
        return area;
    }

    private static NyxComboBox CreateComboBox(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var combo = new NyxComboBox { Bounds = bounds };
        if (table.TryGetValue("items", out var itemsObj) && itemsObj is IReadOnlyList<object?> items)
        {
            var list = new List<string>();
            foreach (var item in items)
            {
                if (item is string s)
                    list.Add(s);
            }

            combo.SetItems(list);
            if (NyxGuiDefinitionProperties.TryGetInt(table, "selected_index", out var idx))
                combo.SelectIndex(idx);
            else if (list.Count > 0)
                combo.SelectIndex(0);
        }

        if (NyxGuiDefinitionProperties.TryGetInt(table, "row_height", out var rh))
            combo.RowHeight = rh;
        return combo;
    }

    private static NyxImageContainer CreateImageContainer(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var container = new NyxImageContainer { Bounds = bounds };
        if (NyxGuiDefinitionProperties.TryGetInt(table, "content_width", out var cw))
            container.ContentWidth = cw;
        if (NyxGuiDefinitionProperties.TryGetInt(table, "content_height", out var ch))
            container.ContentHeight = ch;
        return container;
    }

    private static NyxSlider CreateSlider(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var value = NyxGuiDefinitionProperties.TryGetFloat(table, "value", out var v) ? v : 0f;
        var slider = new NyxSlider { Bounds = bounds, Value = value };
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "minimum", out var min))
            slider.Minimum = min;
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "maximum", out var max))
            slider.Maximum = max;
        return slider;
    }

    private static NyxProgressBar CreateProgressBar(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var value = NyxGuiDefinitionProperties.TryGetFloat(table, "value", out var v) ? v : 0f;
        var bar = new NyxProgressBar { Bounds = bounds, Value = value };
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "minimum", out var min))
            bar.Minimum = min;
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "maximum", out var max))
            bar.Maximum = max;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "show_label", out var show))
            bar.ShowLabel = show;
        return bar;
    }

    private static NyxTooltip CreateTooltip(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var text = NyxGuiDefinitionProperties.TryGetString(table, "text", out var t) ? t : string.Empty;
        var tip = new NyxTooltip { Bounds = bounds, Text = text };
        if (NyxGuiDefinitionProperties.TryGetInt(table, "delay_ms", out var delay))
            tip.TooltipDelayMs = Math.Max(0, delay);
        return tip;
    }

    private static NyxTable CreateTable(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var grid = new NyxTable { Bounds = bounds };
        if (NyxGuiDefinitionProperties.TryGetBool(table, "show_header", out var header))
            grid.ShowHeader = header;
        if (NyxGuiDefinitionProperties.TryGetInt(table, "row_height", out var rh))
            grid.RowHeight = rh;
        return grid;
    }

    private static NyxGraph CreateGraph(NyxGuiPropertyBag table, NyxRect bounds)
    {
        var graph = new NyxGraph { Bounds = bounds };
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "minimum_y", out var minY))
            graph.MinimumY = minY;
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "maximum_y", out var maxY))
            graph.MaximumY = maxY;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "auto_scale_y", out var auto))
            graph.AutoScaleY = auto;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "show_series_b", out var showB))
            graph.ShowSeriesB = showB;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "show_series_c", out var showC))
            graph.ShowSeriesC = showC;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "square_plot", out var square))
            graph.SquarePlot = square;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "show_scale_labels", out var scaleLabels))
            graph.ShowScaleLabels = scaleLabels;
        return graph;
    }

    /// <summary>Applies inline $hover: / $pressed: / $focused: state blocks from .nyxui.</summary>
    private static void ApplyInlineStates(
        NyxElement element,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> states,
        NyxGuiLoadOptions options)
    {
        foreach (var (stateName, props) in states)
        {
            var table = NyxGuiPropertyBag.From(props);
            NyxGuiWidgetStateApplicator.ApplyStateToTable(element.States, stateName, table, options);
        }
    }
}
