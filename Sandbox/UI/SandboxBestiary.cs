using System.Diagnostics.CodeAnalysis;
using NyxGui;
using NyxGui.Definitions;
using NyxGui.Lists;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>
/// Bestiary panel (Lua layout, C# data). Toggle <c>B</c>; list + detail pane under <see cref="SandboxShell"/>.
/// </summary>
internal sealed class SandboxBestiary
{
    private static class WidgetId
    {
        public const string Scroll = "BestiaryScroll";
        public const string DetailName = "detailName";
        public const string DetailClass = "detailClass";
        public const string DetailHp = "detailHp";
        public const string DetailExp = "detailExp";
        public const string DetailSpeed = "detailSpeed";
        public const string DetailArmor = "detailArmor";
        public const string DetailDesc = "detailDesc";
    }

    private const string ModuleId = "bestiary";
    private const string ListRowStyle = "bestiary_entry";

    private readonly Key _toggleKey;
    private readonly SandboxShell? _shell;
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly BestiaryDetailPane? _detail;
    private readonly NyxGuiRowList<BestiaryEntry>? _entries;

    private bool _toggleWasDown;
    private int _lastViewportW;
    private int _lastViewportH;

    public bool Visible
    {
        get => _root.Visible;
        set => _root.Visible = value;
    }

    public int WidgetCount => _document?.ById.Count ?? 0;

    public SandboxBestiary(SandboxShell shell, NyxGuiSettings? settings = null)
    {
        _shell = shell;
        _toggleKey = SandboxUIKeyBinding.TryGetToggleKey(ModuleId) ?? Key.B;

        if (!TryLoad(settings, out var loaded))
        {
            _root = new NyxContainer(NyxRect.Empty);
            _detail = null;
            _entries = null;
            return;
        }

        _document = loaded.Document;
        _root = _document!.Root;
        _shell!.AdoptIntoGamePanel(_document);

        _detail = BestiaryDetailPane.Bind(_document);
        _entries = CreateEntryList(loaded, settings);
        if (_entries is not null)
            _entries.RowClicked += OnEntryClicked;

        ShowEntry(BestiaryCatalog.Default, selectedButton: null);

        Console.WriteLine(
            $"NyxGUI: bestiary loaded from \"{loaded.SourcePath}\" ({BestiaryCatalog.Entries.Count} entries, {_document.ById.Count} widgets).");
    }

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        var resized = width != _lastViewportW || height != _lastViewportH;
        _lastViewportW = width;
        _lastViewportH = height;

        _shell?.UpdateViewport(width, height);

        if (resized)
            _entries?.RelayoutRows();
    }

    public void Update(IInputContext? input, NyxGuiRootStack? guiRoots = null) =>
        TryHandleToggle(input, guiRoots);

    private static bool TryLoad(NyxGuiSettings? settings, [NotNullWhen(true)] out SandboxUIDefinitions.LoadResult? loaded)
    {
        loaded = SandboxUIDefinitions.TryLoad(ModuleId, SandboxUIDefinitions.CreateLoadOptions(settings));
        if (loaded is not null)
        {
            return true;
        }

        Console.WriteLine($"NyxGUI: missing resources/ui/{ModuleId}.nyxui — bestiary disabled.");
        return false;
    }

    private NyxGuiRowList<BestiaryEntry>? CreateEntryList(
        SandboxUIDefinitions.LoadResult loaded,
        NyxGuiSettings? settings)
    {
        var scroll = loaded.Document.TryGetScrollablePanel(WidgetId.Scroll);
        if (scroll is null)
            return null;

        var rowStyle = new NyxWidgetStates();

        var list = new NyxGuiRowList<BestiaryEntry>(scroll, rowStyle)
        {
            RowHeight = 28,
            PadX = 6,
        };

        list.Sync(BestiaryCatalog.Entries, e => e.Id, e => e.Name);
        return list;
    }

    private void OnEntryClicked(NyxButton button, BestiaryEntry entry) =>
        ShowEntry(entry, button);

    private void ShowEntry(BestiaryEntry entry, NyxButton? selectedButton)
    {
        _entries?.SetSelected(selectedButton);
        _detail?.Show(entry);
    }

    private void TryHandleToggle(IInputContext? input, NyxGuiRootStack? guiRoots)
    {
        if (input is not { Keyboards.Count: > 0 })
            return;

        var keyboard = input.Keyboards[0];
        if (keyboard is null)
            return;

        if (guiRoots is not null && NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots))
            return;

        var pressed = keyboard.IsKeyPressed(_toggleKey);
        if (pressed && !_toggleWasDown)
            Visible = !Visible;

        _toggleWasDown = pressed;
    }

    /// <summary>Detail column labels from <c>bestiary.nyxui</c>.</summary>
    private sealed class BestiaryDetailPane
    {
        private readonly NyxLabel? _name;
        private readonly NyxLabel? _class;
        private readonly NyxLabel? _hp;
        private readonly NyxLabel? _exp;
        private readonly NyxLabel? _speed;
        private readonly NyxLabel? _armor;
        private readonly NyxLabel? _desc;

        private BestiaryDetailPane(
            NyxLabel? name,
            NyxLabel? classification,
            NyxLabel? hp,
            NyxLabel? exp,
            NyxLabel? speed,
            NyxLabel? armor,
            NyxLabel? desc)
        {
            _name = name;
            _class = classification;
            _hp = hp;
            _exp = exp;
            _speed = speed;
            _armor = armor;
            _desc = desc;
        }

        public static BestiaryDetailPane? Bind(NyxGuiBuiltDocument document) =>
            new(
                document.TryGetLabel(WidgetId.DetailName),
                document.TryGetLabel(WidgetId.DetailClass),
                document.TryGetLabel(WidgetId.DetailHp),
                document.TryGetLabel(WidgetId.DetailExp),
                document.TryGetLabel(WidgetId.DetailSpeed),
                document.TryGetLabel(WidgetId.DetailArmor),
                document.TryGetLabel(WidgetId.DetailDesc));

        public void Show(BestiaryEntry entry)
        {
            if (_name is null)
                return;

            _name.Text = entry.Name;
            _class!.Text = entry.Classification;
            _hp!.Text = $"Hit Points: {entry.HitPoints}";
            _exp!.Text = $"Experience: {entry.Experience}";
            _speed!.Text = $"Speed: {entry.Speed}";
            _armor!.Text = $"Armor: {entry.Armor}";
            _desc!.Text = entry.Description;
        }
    }
}
