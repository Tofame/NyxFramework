using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>
/// EXP analyzer: current rolling EXP/hr and session-average EXP/hr. Toggle with <c>E</c>.
/// </summary>
internal sealed class SandboxExpAnalyzer
{
    private readonly NyxGuiRenderer _renderer;
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly NyxGuiTheme _theme = new();
    private readonly ExpAnalyzerTracker _tracker;
    private readonly NyxLabel? _lblCurrentRate;
    private readonly NyxLabel? _lblSessionAvg;
    private readonly NyxGraph? _graphCurrentHr;
    private readonly NyxGraph? _graphSessionAvgHr;
    private int _lastVpW;
    private int _lastVpH;
    private readonly Key _toggleKey = SandboxUIKeyBinding.GetToggleKey("exp_analyzer", Key.E);
    private bool _toggleWasDown;

    public SandboxExpAnalyzer(
        NyxGuiRenderer renderer,
        ExpAnalyzerTracker tracker,
        NyxGuiSettings? settings = null,
        NyxGuiRootStack? guiRoots = null)
    {
        _renderer = renderer;
        _tracker = tracker;

        var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
        var loaded = SandboxUIDefinitions.TryLoad("exp_analyzer", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/exp_analyzer.nyxui — EXP analyzer disabled.");
            _root = new NyxContainer(NyxRect.Empty);
            return;
        }

        _document = loaded.Document;
        _root = _document.Root;
        SandboxMiniWindowBehavior.TryAppendChrome(MiniWindow, _document, loadOptions);
        guiRoots?.Add(_root, () => Visible);

        _lblCurrentRate = _document.TryGetLabel("lblCurrentRate");
        _lblSessionAvg = _document.TryGetLabel("lblSessionAvg");
        _graphCurrentHr = _document.TryGetGraph("graphCurrentHr");
        _graphSessionAvgHr = _document.TryGetGraph("graphSessionAvgHr");

        Console.WriteLine(
            $"NyxGUI: loaded EXP analyzer \"{loaded.SourcePath}\" ({_document.ById.Count} widgets).");
    }

    public NyxMiniWindow? MiniWindow => _document?.Root as NyxMiniWindow;

    public NyxGuiBuiltDocument? Document => _document;

    public bool Visible
    {
        get => _root.Visible;
        set => _root.Visible = value;
    }

    public int WidgetCount => _document?.ById.Count ?? 0;

    public void UpdateViewport(int width, int height)
    {
        if (width == _lastVpW && height == _lastVpH)
            return;
        _lastVpW = width;
        _lastVpH = height;
        _document?.SetWindowSize(width, height);
    }

    public void Update(IInputContext? input, Player? player, NyxGuiRootStack? guiRoots = null)
    {
        if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } kb
            && (guiRoots is null || !NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots)))
        {
            var down = kb.IsKeyPressed(_toggleKey);
            if (down && !_toggleWasDown)
                Visible = !Visible;
            _toggleWasDown = down;
        }

        var now = Environment.TickCount64;
        _tracker.Tick(now);

        if (_lblCurrentRate is null)
            return;

        var currentHr = _tracker.CurrentExpPerHour(now);
        var sessionHr = _tracker.SessionAverageExpPerHour(now);

        _lblCurrentRate.Text = $"Current: {currentHr:0} / hr";
        _lblSessionAvg!.Text = $"Session avg: {sessionHr:0} / hr";

        _graphCurrentHr?.SetSeriesA(ToGraphSeries(_tracker.GetCurrentExpPerHourSeries()));
        _graphSessionAvgHr?.SetSeriesA(ToGraphSeries(_tracker.GetSessionAverageExpPerHourSeries()));

        if (MiniWindow is not null)
            MiniWindow.Title = $"EXP Analyzer ({_tracker.SessionTotal} session)";
    }

    public void Draw()
    {
    }

    private static float[] ToGraphSeries(IReadOnlyList<float> values)
    {
        if (values.Count == 0)
            return [0f, 0f];

        if (values.Count == 1)
            return [values[0], values[0]];

        return values is float[] arr ? arr : values.ToArray();
    }
}
