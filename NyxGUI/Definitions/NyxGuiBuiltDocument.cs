using NyxGui.Binding;
using NyxGui;

namespace NyxGui.Definitions;

/// <summary>Result of loading a NyxGUI definition: root widget tree, id lookup, and window anchoring.</summary>
public sealed class NyxGuiBuiltDocument
{
    private readonly Dictionary<string, NyxElement> _byId;
    private readonly Dictionary<string, string> _bindKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NyxGuiEventLink> _eventLinks = [];
    private INyxUiValueSource? _valueSource;
    private bool _suspended;
    private double _bindingRefreshAccum;
    private NyxElement? _layoutDirtyRoot;

    public NyxGuiBuiltDocument(
        NyxElement root,
        Dictionary<string, NyxElement> byId,
        NyxGuiSettings settings,
        string rootWindowAnchorId = NyxGuiRootWindow.DefaultAnchorId)
    {
        Root = root;
        _byId = byId;
        Settings = settings;
        if (root is NyxContainer contentRoot)
        {
            contentRoot.Settings = settings;
            contentRoot.OwningDocument = this;
        }
        RootWindowAnchorId = rootWindowAnchorId;
        WindowBounds = NyxRect.Empty;
        foreach (var element in _byId.Values)
            AttachTextObserver(element);
    }

    /// <summary>When true, skip paint, hit-test, layout dirty, and binding refresh.</summary>
    public bool IsSuspended => _suspended;

    /// <summary>Low-rate binding refresh interval in seconds (default 0.25 ≈ 4 Hz).</summary>
    public double BindingRefreshIntervalSeconds { get; set; } = 0.25;

    /// <summary>Top-level widget returned by <c>[document] root</c> (what you usually paint / forward input to).</summary>
    public NyxElement Root { get; }

    /// <summary>Settings bound to this document.</summary>
    public NyxGuiSettings Settings { get; }

    public IReadOnlyDictionary<string, NyxElement> ById => _byId;

    internal Dictionary<string, NyxElement> WidgetMap => _byId;

    /// <summary>Name of the logical window for anchors (e.g. <c>rootWindow.top</c> by default).</summary>
    public string RootWindowAnchorId { get; }

    /// <summary>Last window / viewport rectangle (typically <c>0,0,width,height</c>).</summary>
    public NyxRect WindowBounds { get; private set; }

    public NyxElement? TryGet(string id) => _byId.TryGetValue(id, out var e) ? e : null;

    public T? TryGet<T>(string id) where T : NyxElement => TryGet(id) as T;

    public NyxLabel? TryGetLabel(string id) => TryGet<NyxLabel>(id);

    public NyxButton? TryGetButton(string id) => TryGet<NyxButton>(id);

    public NyxScrollablePanel? TryGetScrollablePanel(string id) => TryGet<NyxScrollablePanel>(id);

    public NyxTextBox? TryGetTextBox(string id) => TryGet<NyxTextBox>(id);

    public NyxTextArea? TryGetTextArea(string id) => TryGet<NyxTextArea>(id);

    public NyxCheckBox? TryGetCheckBox(string id) => TryGet<NyxCheckBox>(id);

    public NyxRadioButton? TryGetRadioButton(string id) => TryGet<NyxRadioButton>(id);

    public NyxComboBox? TryGetComboBox(string id) => TryGet<NyxComboBox>(id);

    public NyxExtendedLabel? TryGetExtendedLabel(string id) => TryGet<NyxExtendedLabel>(id);

    public NyxSlider? TryGetSlider(string id) => TryGet<NyxSlider>(id);

    public NyxProgressBar? TryGetProgressBar(string id) => TryGet<NyxProgressBar>(id);

    public NyxTable? TryGetTable(string id) => TryGet<NyxTable>(id);

    public NyxGraph? TryGetGraph(string id) => TryGet<NyxGraph>(id);

    public NyxImage? TryGetImage(string id) => TryGet<NyxImage>(id);

    public NyxVScrollBar? TryGetVScrollBar(string id) => TryGet<NyxVScrollBar>(id);

    public NyxHScrollBar? TryGetHScrollBar(string id) => TryGet<NyxHScrollBar>(id);

    public NyxMiniWindow? TryGetMiniWindow(string id) => TryGet<NyxMiniWindow>(id);

    public NyxFileDialogButton? TryGetFileDialogButton(string id) => TryGet<NyxFileDialogButton>(id);

    public void RegisterWidget(NyxElement element)
    {
        if (string.IsNullOrWhiteSpace(element.Id))
            throw new InvalidOperationException("Cannot register a widget without id.");
        _byId[element.Id] = element;
    }

    public void MergeWidgetsFrom(NyxGuiBuiltDocument other)
    {
        foreach (var element in other.ById.Values)
            RegisterWidget(element);
    }

    /// <summary>Registers <paramref name="child"/> widgets and parents its <see cref="Root"/> under <paramref name="parent"/>.</summary>
    public void Adopt(NyxGuiBuiltDocument child, NyxContainer parent)
    {
        MergeWidgetsFrom(child);
        parent.AddChild(child.Root);
        if (WindowBounds.Width > 0 && WindowBounds.Height > 0)
            SetWindowSize(WindowBounds.Width, WindowBounds.Height);
    }

    /// <summary>
    /// Sets the game window size and re-runs layout so widgets anchored to
    /// <see cref="RootWindowAnchorId"/> move correctly.
    /// </summary>
    public void SetWindowSize(int width, int height)
    {
        if (_suspended)
            return;

        WindowBounds = new NyxRect(0, 0, width, height);
        var env = CreateLayoutEnvironment();

        if (Root is NyxContainer rc)
            NyxLayoutResolver.RelayoutTree(rc, _byId, env);

        _layoutDirtyRoot = null;
    }

    /// <summary>Marks a layout pass needed (coalesced by <see cref="FlushLayout"/>).</summary>
    public void MarkLayoutDirty(NyxElement? subtreeRoot = null)
    {
        if (_suspended)
            return;

        if (subtreeRoot is null)
        {
            _layoutDirtyRoot = Root;
            return;
        }

        if (_layoutDirtyRoot is null)
        {
            _layoutDirtyRoot = subtreeRoot;
            return;
        }

        if (IsAncestorOf(subtreeRoot, _layoutDirtyRoot))
            _layoutDirtyRoot = subtreeRoot;
        else if (!IsAncestorOf(_layoutDirtyRoot, subtreeRoot))
            _layoutDirtyRoot = Root;
    }

    /// <summary>Runs a pending subtree or full-tree relayout.</summary>
    public void FlushLayout()
    {
        if (_suspended || _layoutDirtyRoot is null)
            return;

        var env = CreateLayoutEnvironment();
        if (ReferenceEquals(_layoutDirtyRoot, Root))
        {
            if (Root is NyxContainer rc)
                NyxLayoutResolver.RelayoutTree(rc, _byId, env);
        }
        else if (_layoutDirtyRoot is NyxContainer cc)
            NyxLayoutResolver.RelayoutContainer(cc, _byId, env);

        _layoutDirtyRoot = null;
    }

    internal void RegisterBindings(IReadOnlyDictionary<string, string> bindKeys)
    {
        foreach (var (widgetId, key) in bindKeys)
            _bindKeys[widgetId] = key;
    }

    internal void RegisterEventLinks(IEnumerable<NyxGuiEventLink> links)
    {
        _eventLinks.AddRange(links);
    }

    /// <summary>
    /// Registers action handlers keyed by action name. When a widget event triggers, the matching handler is invoked.
    /// </summary>
    public void BindActions(IReadOnlyDictionary<string, Action> handlers)
    {
        foreach (var link in _eventLinks)
        {
            if (!handlers.TryGetValue(link.ActionName, out var handler))
                continue;
            if (!_byId.TryGetValue(link.WidgetId, out var element))
                continue;

            WireEvent(element, link.EventName, handler);
        }
    }

    /// <summary>Raised when any button with <c>on_click</c> is clicked, passing the action name.</summary>
    public event Action<string, NyxButton>? ActionTriggered;

    /// <summary>Wires all events to raise <see cref="ActionTriggered"/>.</summary>
    public void WireActions()
    {
        foreach (var link in _eventLinks)
        {
            if (!_byId.TryGetValue(link.WidgetId, out var element))
                continue;

            var name = link.ActionName;
            WireEvent(element, link.EventName, () =>
            {
                if (element is NyxButton button)
                    ActionTriggered?.Invoke(name, button);
                else
                    ActionTriggered?.Invoke(name, new NyxButton { Id = element.Id });
            });
        }
    }

    private void WireEvent(NyxElement element, string eventName, Action handler)
    {
        switch (eventName.ToLowerInvariant())
        {
            case "click":
                if (element is NyxButton btn)
                    btn.Click += (_, _) => handler();
                break;
            case "right_click":
                if (element is NyxButton rightBtn)
                    rightBtn.RightClick += (_, _) => handler();
                else
                    element.RightClicked += (_, _) => handler();
                break;
            case "value_change":
                if (element is NyxSlider slider)
                    slider.ValueChanged += (_, _) => handler();
                break;
            case "change":
                if (element is NyxCheckBox checkBox)
                    checkBox.Changed += (_, _) => handler();
                else if (element is NyxRadioButton radioButton)
                    radioButton.Changed += (_, _) => handler();
                else if (element is NyxTextBox textBox)
                    textBox.Changed += (_, _) => handler();
                else if (element is NyxTextArea textArea)
                    textArea.Changed += (_, _) => handler();
                break;
            case "selection_change":
                if (element is NyxComboBox comboBox)
                    comboBox.SelectionChanged += (_, _) => handler();
                break;
            case "double_click":
                element.AddHandler(NyxEventType.DoubleClick, (_, _) => handler());
                break;
            case "mouse_enter":
                element.AddHandler(NyxEventType.MouseEnter, (_, _) => handler());
                break;
            case "mouse_leave":
                element.AddHandler(NyxEventType.MouseLeave, (_, _) => handler());
                break;
            case "key_down":
                element.AddHandler(NyxEventType.KeyDown, (_, _) => handler());
                break;
            case "key_up":
                element.AddHandler(NyxEventType.KeyUp, (_, _) => handler());
                break;
            case "focus_gained":
                element.AddHandler(NyxEventType.FocusGained, (_, _) => handler());
                break;
            case "focus_lost":
                element.AddHandler(NyxEventType.FocusLost, (_, _) => handler());
                break;
            case "drag_start":
                element.AddHandler(NyxEventType.DragStart, (_, _) => handler());
                break;
            case "drag_enter":
                element.AddHandler(NyxEventType.DragEnter, (_, _) => handler());
                break;
            case "drag_leave":
                element.AddHandler(NyxEventType.DragLeave, (_, _) => handler());
                break;
            case "drag_over":
                element.AddHandler(NyxEventType.DragOver, (_, _) => handler());
                break;
            case "drop":
                element.AddHandler(NyxEventType.Drop, (_, _) => handler());
                break;
            case "file_selected":
                if (element is NyxFileDialogButton fileDialogBtn)
                    fileDialogBtn.FileSelected += (_, _) => handler();
                break;
            case "drag_end":
                element.AddHandler(NyxEventType.DragEnd, (_, _) => handler());
                break;
        }
    }

    public void SetValueSource(INyxUiValueSource? source) => _valueSource = source;

    /// <summary>Removes document from hot path until <see cref="Resume"/>.</summary>
    public void Suspend()
    {
        _suspended = true;
        Root.Visible = false;
    }

    /// <summary>Restores visibility and runs one binding refresh.</summary>
    public void Resume()
    {
        _suspended = false;
        Root.Visible = true;
        RefreshBoundWidgets();
    }

    /// <summary>Updates bound labels; uses text-only path when possible.</summary>
    public void RefreshBoundWidgets()
    {
        if (_suspended || _valueSource is null)
            return;

        foreach (var (widgetId, bindKey) in _bindKeys)
        {
            if (!_valueSource.TryGetString(bindKey, out var text))
                continue;
            if (!_byId.TryGetValue(widgetId, out var element))
                continue;
            NyxGuiTextUpdate.ApplyText(element, text);
        }
    }

    /// <summary>Call once per frame while visible; refreshes bindings at <see cref="BindingRefreshIntervalSeconds"/>.</summary>
    public void TickBindings(double deltaSeconds)
    {
        if (_suspended || _bindKeys.Count == 0 || _valueSource is null)
            return;

        _bindingRefreshAccum += deltaSeconds;
        if (_bindingRefreshAccum < BindingRefreshIntervalSeconds)
            return;

        _bindingRefreshAccum = 0;
        RefreshBoundWidgets();
    }

    private static bool IsAncestorOf(NyxElement ancestor, NyxElement node)
    {
        for (var p = node.Parent; p is not null; p = p.Parent)
        {
            if (ReferenceEquals(p, ancestor))
                return true;
        }
        return false;
    }

    private NyxGuiLayoutEnvironment CreateLayoutEnvironment() => new()
    {
        WindowBounds = WindowBounds,
        RootWindowAnchorId = RootWindowAnchorId,
    };

    private void AttachTextObserver(NyxElement element)
    {
        if (element is NyxLabel label)
            label.TextChanged += OnLabelTextChanged;
    }

    private void OnLabelTextChanged(NyxLabel label, string oldText, string newText)
    {
        if (_suspended)
            return;

        if (NyxGuiTextUpdate.CanSkipLayoutForTextChange(label, oldText, newText))
            return;

        if (label.LayoutBox is not null)
        {
            var container = label.Parent as NyxContainer ?? Root as NyxContainer;
            if (container is not null)
            {
                MarkLayoutDirty(container);
                FlushLayout();
            }
        }
    }
}

/// <summary>Describes a declarative markup event mapped to a C# action name.</summary>
public sealed class NyxGuiEventLink
{
    public string WidgetId { get; }
    public string EventName { get; }
    public string ActionName { get; }

    public NyxGuiEventLink(string widgetId, string eventName, string actionName)
    {
        WidgetId = widgetId;
        EventName = eventName;
        ActionName = actionName;
    }
}
