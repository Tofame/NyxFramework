using NyxGui;
using NyxGui.Definitions;
using NyxGui.Lists;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>
/// Quest log under shell <c>gamePanel</c> (toggle <c>Q</c>).
/// Demonstrates: static quest buttons with click handlers, dynamic row list, detail panel.
/// </summary>
internal sealed class SandboxQuestLog
{
    private readonly NyxWidgetStates _listButtonStates = new();
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly SandboxShell? _shell;
    private NyxGuiRowList<QuestLogEntry>? _rowList;
    private NyxLabel? _detailTitle;
    private NyxExtendedLabel? _detailDesc;
	private NyxScrollablePanel? _descScroll;
    private int _lastVpW;
    private int _lastVpH;
    private bool _layoutApplied;
    private bool _qWasDown;
    private readonly Key _toggleKey;

    public SandboxQuestLog(SandboxShell shell, NyxGuiSettings? settings = null)
    {
        _shell = shell;
        _toggleKey = SandboxUIKeyBinding.TryGetToggleKey("quest_log") ?? Key.Q;

        var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
        var loaded = SandboxUIDefinitions.TryLoad("quest_log", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/quest_log.nyxui — quest log disabled.");
            _root = new NyxContainer(NyxRect.Empty);
            return;
        }

        _document = loaded.Document;
        _root = _document.Root;
        _shell.AdoptIntoGamePanel(_document);

        _detailTitle = _document.TryGetLabel("detailMissionTitle");
        _detailDesc = _document.TryGetExtendedLabel("detailMissionDesc");
		_descScroll = _document.TryGetScrollablePanel("detailDescScroll");

        // Wire static quest buttons (defined in .nyxui) with click handlers
        WireStaticQuestButtons();

        // Dynamic row list for scrollable quest entries
        var scroll = _document.TryGetScrollablePanel("QuestScroll");
        if (scroll is not null)
        {
            _rowList = new NyxGuiRowList<QuestLogEntry>(scroll, _listButtonStates)
            {
                RowHeight = 30,
                PadX = 4,
            };
            _rowList.RowClicked += (btn, entry) => SelectEntry(entry, btn);
            _rowList.Sync(
                QuestLogCatalog.Entries,
                e => e.Id,
                e => e.Title);
        }

        SelectEntry(QuestLogCatalog.Default, null);

        Console.WriteLine(
            $"NyxGUI: loaded quest log \"{loaded.SourcePath}\" ({QuestLogCatalog.Entries.Count} missions).");
    }

    /// <summary>Wires click handlers for static quest buttons defined in .nyxui.</summary>
    private void WireStaticQuestButtons()
    {
        BindQuestButton("btnQuest1", QuestLogCatalog.Entries.Count > 0 ? QuestLogCatalog.Entries[0] : QuestLogCatalog.Default);
        BindQuestButton("btnQuest2", QuestLogCatalog.Entries.Count > 1 ? QuestLogCatalog.Entries[1] : QuestLogCatalog.Default);
        BindQuestButton("btnQuest3", QuestLogCatalog.Entries.Count > 2 ? QuestLogCatalog.Entries[2] : QuestLogCatalog.Default);
        BindQuestButton("btnQuest4", QuestLogCatalog.Entries.Count > 3 ? QuestLogCatalog.Entries[3] : QuestLogCatalog.Default);
    }

    private void BindQuestButton(string buttonId, QuestLogEntry entry)
    {
        var btn = _document?.TryGetButton(buttonId);
        if (btn is null)
            return;

        // Click handler — select this quest in the detail panel.
        // Visual states ($hover, $pressed) are defined in .nyxui.
        btn.Click += (_, _) => SelectEntry(entry, btn);
    }

    public bool Visible
    {
        get => _root.Visible;
        set => _root.Visible = value;
    }

    public int WidgetCount => _document?.ById.Count ?? 0;

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        var sizeChanged = width != _lastVpW || height != _lastVpH;
        _lastVpW = width;
        _lastVpH = height;

        _shell?.UpdateViewport(width, height);
        _rowList?.RelayoutRows();

        if (!sizeChanged && _layoutApplied)
            return;

        _layoutApplied = true;
    }

    public void Update(IInputContext? input, NyxGuiRootStack? guiRoots = null)
    {
        if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } kb
            && (guiRoots is null || !NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots)))
        {
            var q = kb.IsKeyPressed(_toggleKey);
            if (q && !_qWasDown)
                Visible = !Visible;
            _qWasDown = q;
        }
    }

    private void SelectEntry(QuestLogEntry entry, NyxButton? button)
    {
        _rowList?.SetSelected(button);

        if (_detailTitle is null || _detailDesc is null)
            return;

        _detailTitle.Text = entry.Title;
        _detailDesc.Text = entry.Description;
		_descScroll?.ScrollTo(0);
    }
}
