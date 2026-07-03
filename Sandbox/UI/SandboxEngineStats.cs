using System.Text;
using NyxGui;
using NyxGui.Binding;
using NyxGui.Definitions;
using NyxGuiRender;
using NyxRender;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>
/// Engine overlay from <c>resources/ui/engine_stats.nyxui</c>. Toggle with <c>U</c>.
/// </summary>
internal sealed class SandboxEngineStats
{
    private readonly NyxGuiRenderer _renderer;
    private readonly NyxGuiSettings _settings;
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly SandboxShell? _shell;
    private readonly NyxGuiTheme _theme = new();
    private readonly FpsTracker _fps = new();
    private readonly NyxUiState _uiState = new();
    private readonly StringBuilder _vramLine = new(96);
    private int _lastVpW;
    private int _lastVpH;
    private readonly Key _toggleKey = SandboxUIKeyBinding.GetToggleKey("engine_stats", Key.U);
    private bool _toggleWasDown;
    private int _playerStatsWidgets;
    private int _bestiaryWidgets;
    private int _expAnalyzerWidgets;
    private string _nyxScreensLine = "Screens: —";

    public bool Visible
    {
        get => _root.Visible;
        set => _root.Visible = value;
    }

    public SandboxEngineStats(NyxGuiRenderer renderer, SandboxShell shell, NyxGuiSettings? settings = null)
    {
        _renderer = renderer;
        _settings = settings ?? NyxGuiSettings.Default;
        _shell = shell;

        var loadOptions = CreateLoadOptions();
        var loaded = SandboxUIDefinitions.TryLoad("engine_stats", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/engine_stats.nyxui — engine stats panel disabled.");
            _root = new NyxContainer(NyxRect.Empty);
            return;
        }

        _document = loaded.Document;
        _root = _document.Root;
        _shell.AdoptIntoGamePanel(_document);

        _document.SetValueSource(_uiState);
        RegisterUiStateProviders();
        _document.RefreshBoundWidgets();

        Console.WriteLine($"NyxGUI: loaded engine stats \"{loaded.SourcePath}\" ({_document.ById.Count} widgets).");
    }

    private NyxGuiLoadOptions CreateLoadOptions() =>
        SandboxUIDefinitions.CreateLoadOptions(_settings);

    private void RegisterUiStateProviders()
    {
        _uiState.Register("sandbox.fps", () => $"FPS: {_fps.Fps:0.0}  ({_fps.FrameMs:0.0} ms)");
        _uiState.Register("sandbox.sprites", () =>
        {
            if (_lastSpriteStats is not { } stats)
                return "Sprites: —";
            return $"Sprites: {stats.LoadedSprites}";
        });
        _uiState.Register("sandbox.atlases", () =>
        {
            if (_lastSpriteStats is not { } stats)
                return "Atlases: —";
            return $"Atlases: {stats.AtlasCount}  slots {stats.AtlasSlotsUsed}/{stats.AtlasSlotCapacity}";
        });
        _uiState.Register("sandbox.vram", () => _vramLine.ToString());
        _uiState.Register("sandbox.nyx_font", () => _renderer.HasFont ? "UI font: loaded" : "UI font: missing");
        _uiState.Register("sandbox.nyx_drag", () => $"Panel drag opacity: {_settings.PanelDragOpacity:F2}");
        _uiState.Register("sandbox.nyx_screens", () => _nyxScreensLine);
        _uiState.Register("sandbox.gui_draws", () =>
        {
            var gui = _renderer.GetStats();
            return $"GL Draws: {gui.LastFrameGlDraws} (Quads: {gui.LastFrameQuads})";
        });
        _uiState.Register("sandbox.gui_textures", () =>
        {
            var gui = _renderer.GetStats();
            return $"Cached textures: {gui.CachedTextures}";
        });
        _uiState.Register("sandbox.gui_glyphs", () =>
        {
            var gui = _renderer.GetStats();
            return $"Text cache: {gui.CachedTextGlyphs} glyphs";
        });
    }

    private SpriteRendererStats? _lastSpriteStats;

    public void RegisterOtherScreens(int playerStatsWidgetCount, int bestiaryWidgetCount, int expAnalyzerWidgetCount = 0)
    {
        _playerStatsWidgets = playerStatsWidgetCount;
        _bestiaryWidgets = bestiaryWidgetCount;
        _expAnalyzerWidgets = expAnalyzerWidgetCount;
        var engineWidgets = _document?.ById.Count ?? 0;
        _nyxScreensLine =
            $"Screens: U engine ({engineWidgets}), G player ({_playerStatsWidgets}), E EXP ({_expAnalyzerWidgets}), B bestiary ({_bestiaryWidgets})";
        _document?.RefreshBoundWidgets();
    }

    public void UpdateViewport(int width, int height)
    {
        if (width == _lastVpW && height == _lastVpH)
            return;
        _lastVpW = width;
        _lastVpH = height;
        _shell?.UpdateViewport(width, height);
    }

    public void Update(double deltaSeconds, IInputContext? input, SpriteRenderer? spriteRenderer, NyxGuiRootStack? guiRoots = null)
    {
        _fps.Tick(deltaSeconds);

        if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } kb
            && (guiRoots is null || !NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots)))
        {
            var down = kb.IsKeyPressed(_toggleKey);
            if (down && !_toggleWasDown)
                Visible = !Visible;
            _toggleWasDown = down;
        }

        if (_document is null)
            return;

        if (spriteRenderer is not null)
        {
            _lastSpriteStats = spriteRenderer.GetStats();
            var stats = _lastSpriteStats!.Value;
            _vramLine.Clear();
            _vramLine.Append($"VRAM ~{stats.MemoryUsageMB:0.0} MB");
            if (stats.EvictedTotal > 0)
                _vramLine.Append($"  evicted {stats.EvictedTotal}");
            _vramLine.Append($"  frame {stats.FrameIndex}");
        }
        else
        {
            _lastSpriteStats = null;
            _vramLine.Clear();
            _vramLine.Append("VRAM: —");
        }

        _document.TickBindings(deltaSeconds);
    }

    private sealed class FpsTracker
    {
        private const int MaxSamples = 120;
        private readonly double[] _samples = new double[MaxSamples];
        private int _count;
        private int _head;

        public double Fps { get; private set; }
        public double FrameMs { get; private set; }

        public void Tick(double deltaSeconds)
        {
            if (deltaSeconds <= 0)
                deltaSeconds = 1.0 / 10_000.0;

            _samples[_head] = deltaSeconds;
            _head = (_head + 1) % MaxSamples;
            if (_count < MaxSamples)
                _count++;

            var sum = 0.0;
            for (var i = 0; i < _count; i++)
                sum += _samples[i];

            var avg = sum / _count;
            FrameMs = avg * 1000.0;
            Fps = 1.0 / avg;
        }
    }
}
