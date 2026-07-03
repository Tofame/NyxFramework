using System;
using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;

namespace Sandbox.UI;

/// <summary>
/// Controller wrapper for resources/ui/menu.nyxui.
/// Handles events for clicking play and exit buttons, and displays loading status updates.
/// </summary>
internal sealed class SandboxMainMenu
{
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private int _lastVpW;
    private int _lastVpH;

    public event Action? PlayClicked;
    public event Action? JoinClicked;
    public event Action? ExitClicked;

    public SandboxMainMenu(NyxGuiRenderer renderer, NyxGuiSettings? settings = null, NyxGuiRootStack? guiRoots = null)
    {
        var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
        var loaded = SandboxUIDefinitions.TryLoad("menu", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/menu.nyxui — Main Menu disabled.");
            _root = new NyxContainer(NyxRect.Empty);
            return;
        }

        _document = loaded.Document;
        _root = _document.Root;
        guiRoots?.Add(_root, () => true);

        var playBtn = _document.TryGetButton("PlayButton");
        if (playBtn is not null)
        {
            playBtn.Click += (_, _) => PlayClicked?.Invoke();
        }

        var joinBtn = _document.TryGetButton("JoinButton");
        if (joinBtn is not null)
        {
            joinBtn.Click += (_, _) => JoinClicked?.Invoke();
        }

        var exitBtn = _document.TryGetButton("ExitButton");
        if (exitBtn is not null)
        {
            exitBtn.Click += (_, _) => ExitClicked?.Invoke();
        }
    }

    public bool Visible
    {
        get => _root.Visible;
        set
        {
            _root.Visible = value;
            if (_document is not null)
            {
                // Ensure layout flushes when visibility toggles
                _document.Root.Visible = value;
            }
        }
    }

    public void SetStatusText(string text)
    {
        var label = _document?.TryGetLabel("StatusLabel");
        if (label is not null)
        {
            label.Text = text;
            label.Visible = !string.IsNullOrEmpty(text);
        }
    }

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        if (width == _lastVpW && height == _lastVpH)
            return;

        _lastVpW = width;
        _lastVpH = height;
        _document?.SetWindowSize(width, height);
    }
}
