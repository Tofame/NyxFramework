using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;

namespace Sandbox.UI;

/// <summary>Loads <c>resources/ui/shell.nyxui</c> — left/right side chrome and <c>gamePanel</c> anchor.</summary>
internal sealed class SandboxShell
{
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly NyxGuiTheme _theme = new();
    private int _lastVpW;
    private int _lastVpH;

    public SandboxShell(NyxGuiRenderer renderer, NyxGuiSettings? settings = null, NyxGuiRootStack? guiRoots = null)
    {
        var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
        var loaded = SandboxUIDefinitions.TryLoad("shell", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/shell.nyxui — shell layout disabled.");
            _root = new NyxContainer(NyxRect.Empty);
            return;
        }

        _document = loaded.Document;
        _root = _document.Root;
        guiRoots?.Add(_root, () => true);

        Console.WriteLine(
            $"NyxGUI: loaded shell \"{loaded.SourcePath}\" ({_document.ById.Count} widgets).");
    }

    public bool Visible
    {
        get => _root.Visible;
        set
        {
            _root.Visible = value;
            if (_document is not null)
            {
                _document.Root.Visible = value;
            }
        }
    }

    public void UpdateViewport(int width, int height)
    {
        if (width == _lastVpW && height == _lastVpH)
            return;
        Relayout(width, height);
    }

    /// <summary>Forces a layout pass (e.g. after adopting panels into <c>gamePanel</c>).</summary>
    public void Relayout(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;
        _lastVpW = width;
        _lastVpH = height;
        _document?.SetWindowSize(width, height);
    }

    public NyxContainer? GamePanel => _document?.TryGet<NyxContainer>("gamePanel");

    public NyxGuiBuiltDocument? Document => _document;

	public NyxDockPanel? LeftDock => _document?.TryGet<NyxDockPanel>("leftPanel");

	public NyxDockPanel? RightDock => _document?.TryGet<NyxDockPanel>("rightPanel");

    /// <summary>Parents a loaded panel under <see cref="GamePanel"/>; <c>parent.*</c> anchors apply after adopt.</summary>
    public void AdoptIntoGamePanel(NyxGuiBuiltDocument child) =>
        _document?.Adopt(child, GamePanel!);

	public void AdoptIntoLeftDock(NyxGuiBuiltDocument child)
	{
		if (LeftDock is not null && _document is not null)
			_document.Adopt(child, LeftDock);
	}

	public void AdoptIntoRightDock(NyxGuiBuiltDocument child)
	{
		if (RightDock is not null && _document is not null)
			_document.Adopt(child, RightDock);
	}

	public void AdoptIntoShellRoot(NyxGuiBuiltDocument child)
	{
		if (_document is not null && _root is NyxContainer rc)
			_document.Adopt(child, rc);
	}

    public void Draw(NyxGuiRenderer renderer) => _root.Paint(renderer, _theme);
}
