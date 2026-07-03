using System.Diagnostics.CodeAnalysis;
using NyxGui;
using NyxGui.Binding;
using NyxGui.Definitions;
using NyxGuiRender;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>
/// Player stats miniwindow. Toggle <c>G</c>; labels use <c>bind_key</c> for reactive updates,
/// buttons use <c>.Click</c> events wired by id, progress bars update from Player data.
/// </summary>
internal sealed class SandboxPlayerStats
{
    private const string ModuleId = "player_stats";

    private readonly NyxGuiRenderer _renderer;
    private readonly NyxGuiTheme _theme = new();
    private readonly NyxGuiSettings _settings;
    private readonly Key _toggleKey;
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly PlayerStatBars? _bars;
    private readonly NyxUiState _uiState = new();

    private Player? _player;
    private bool _toggleWasDown;
    private int _lastViewportW;
    private int _lastViewportH;
    private string _lastPositionText = "";

    public NyxMiniWindow? MiniWindow => _document?.Root as NyxMiniWindow;

    public NyxGuiBuiltDocument? Document => _document;

    public bool Visible
    {
        get => _root.Visible;
        set => _root.Visible = value;
    }

    public int WidgetCount => _document?.ById.Count ?? 0;

    public SandboxPlayerStats(NyxGuiRenderer renderer, NyxGuiSettings? settings = null, NyxGuiRootStack? guiRoots = null)
    {
        _renderer = renderer;
        _settings = settings ?? NyxGuiSettings.Default;
        _toggleKey = SandboxUIKeyBinding.GetToggleKey(ModuleId, Key.G);

        if (!TryLoad(_settings, out var loaded))
        {
            _root = new NyxContainer(NyxRect.Empty);
            _bars = null;
            return;
        }

        _document = loaded.Document;
        _root = _document!.Root;
        guiRoots?.Add(_root, () => Visible);

        var loadOptions = SandboxUIDefinitions.CreateLoadOptions(_settings);
        SandboxMiniWindowBehavior.TryAppendChrome(MiniWindow, _document, loadOptions);

        _bars = PlayerStatBars.Bind(_document);
        BindActions();

        _document.SetValueSource(_uiState);
        RegisterUiStateProviders();

        Console.WriteLine(
            $"NyxGUI: player stats loaded from \"{loaded.SourcePath}\" ({_document.ById.Count} widgets).");
    }

    public void UpdateViewport(int width, int height)
    {
        if (width == _lastViewportW && height == _lastViewportH)
            return;

        _lastViewportW = width;
        _lastViewportH = height;
        _document?.SetWindowSize(width, height);
    }

    public void Update(IInputContext? input, Player? player, NyxGuiRootStack? guiRoots = null)
    {
        TryHandleToggle(input, guiRoots);
        _player = player;
        _bars?.Update(player);

        // Only refresh bound widgets when position text actually changes.
        var posText = _uiState.TryGetString("player.position", out var txt) ? txt : "";
        if (posText != _lastPositionText)
        {
            _lastPositionText = posText;
            _document?.RefreshBoundWidgets();
        }
    }

    public void Draw()
    {
    }

    private static bool TryLoad(NyxGuiSettings settings, [NotNullWhen(true)] out SandboxUIDefinitions.LoadResult? loaded)
    {
        loaded = SandboxUIDefinitions.TryLoad(ModuleId, SandboxUIDefinitions.CreateLoadOptions(settings));
        if (loaded is not null)
            return true;

        Console.WriteLine($"NyxGUI: missing resources/ui/{ModuleId}.nyxui — player stats disabled.");
        return false;
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

    private void RegisterUiStateProviders()
    {
        _uiState.Register("player.position", () =>
        {
            if (_player is null)
                return "Position: —";
            var p = _player.Position;
            return $"Position: ({p.X}, {p.Y}, {p.Z})";
        });
    }

    private void MutatePlayer(Action<Player> mutate)
    {
        if (_player is null)
            return;

        mutate(_player);
        _player.Health = Math.Max(0, _player.Health);
        _player.Mana = Math.Max(0, _player.Mana);
        _player.Level = Math.Max(1, _player.Level);
        _player.Exp = Math.Max(0, _player.Exp);
        _bars?.Update(_player);
    }

    private void BindActions()
    {
        if (_document is null)
            return;

        _document.BindActions(new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
        {
            // ["increase_health"] = () => { _player.Health++; _bars?.Update(_player); },
            ["increase_health"] = () => MutatePlayer(p => p.Health++),
            ["decrease_health"] = () => MutatePlayer(p => p.Health--),
            ["increase_mana"] = () => MutatePlayer(p => p.Mana++),
            ["decrease_mana"] = () => MutatePlayer(p => p.Mana--),
            ["increase_level"] = () => MutatePlayer(p => p.Level++),
            ["decrease_level"] = () => MutatePlayer(p => p.Level--),
            ["increase_exp"] = () => MutatePlayer(p => p.Exp++),
            ["decrease_exp"] = () => MutatePlayer(p => p.Exp--),
        });
    }

    /// <summary>Progress bars updated from Player data.</summary>
    private sealed class PlayerStatBars
    {
        private const string HealthBar = "barHealth";
        private const string ManaBar = "barMana";
        private const string LevelBar = "barLevel";
        private const string ExpBar = "barExp";

        private readonly NyxProgressBar? _healthBar;
        private readonly NyxProgressBar? _manaBar;
        private readonly NyxProgressBar? _levelBar;
        private readonly NyxProgressBar? _expBar;

        private PlayerStatBars(NyxProgressBar? healthBar, NyxProgressBar? manaBar, NyxProgressBar? levelBar, NyxProgressBar? expBar)
        {
            _healthBar = healthBar;
            _manaBar = manaBar;
            _levelBar = levelBar;
            _expBar = expBar;
        }

        public static PlayerStatBars? Bind(NyxGuiBuiltDocument document) =>
            new(
                document.TryGet<NyxProgressBar>(HealthBar),
                document.TryGet<NyxProgressBar>(ManaBar),
                document.TryGet<NyxProgressBar>(LevelBar),
                document.TryGet<NyxProgressBar>(ExpBar));

        public void Update(Player? player)
        {
            if (_healthBar is null)
                return;

            _healthBar.Value = player?.Health ?? 0;
            _manaBar?.Value = player?.Mana ?? 0;
            _levelBar?.Value = player?.Level ?? 0;
            _expBar?.Value = player?.Exp ?? 0;
        }
    }
}
