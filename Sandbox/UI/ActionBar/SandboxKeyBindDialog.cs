using NyxGui;
using Silk.NET.Input;

namespace Sandbox.UI.ActionBar;

/// <summary>Modal overlay: press a key to assign to an action-bar slot.</summary>
internal sealed class SandboxKeyBindDialog
{
    private const int PanelW = 300;
    private const int PanelH = 120;

    private readonly NyxContainer _root;
    private readonly NyxContainer _panel;
    private readonly NyxLabel _title;
    private readonly NyxLabel _hint;
    private readonly NyxButton _cancel;
    private Action<Key?>? _callback;
    private bool _listening;
    private bool _escapeWasDown;

    public SandboxKeyBindDialog(int viewportWidth, int viewportHeight)
    {
        _root = new NyxContainer(new NyxRect(0, 0, viewportWidth, viewportHeight))
        {
            Visible = false,
        };

        _panel = new NyxContainer(new NyxRect(0, 0, PanelW, PanelH));
        _panel.States.Normal.BackgroundColor = NyxColor.FromRgb(45, 45, 48);
        _panel.States.Normal.BorderWidth = 1;
        _panel.States.Normal.BorderColor = NyxColor.FromRgb(100, 100, 110);

        _title = new NyxLabel { Align = NyxTextAlign.TopCenter };
        _title.SetBounds(new NyxRect(12, 12, PanelW - 24, 20));
        _title.Text = "Change bound key";

        _hint = new NyxLabel { Align = NyxTextAlign.TopCenter };
        _hint.SetBounds(new NyxRect(12, 38, PanelW - 24, 36));
        _hint.Text = "Press a key…";

        _cancel = new NyxButton { Label = "Cancel" };
        _cancel.SetBounds(new NyxRect((PanelW - 80) / 2, PanelH - 40, 80, 26));
        _cancel.Click += (_, _) => Close(null);

        _panel.AddChild(_title);
        _panel.AddChild(_hint);
        _panel.AddChild(_cancel);
        _root.AddChild(_panel);
        _panel.SetBounds(CenterPanel(viewportWidth, viewportHeight));
    }

    public NyxContainer Root => _root;

    public bool IsOpen => _root.Visible;

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _root.SetBounds(new NyxRect(0, 0, width, height));
        _panel.SetBounds(CenterPanel(width, height));
    }

    public void Open(string slotLabel, Action<Key?> onComplete)
    {
        _callback = onComplete;
        _listening = true;
        _escapeWasDown = false;
        _title.Text = $"Change bound key — {slotLabel}";
        _hint.Text = "Press a key (Esc to cancel)…";
        _root.Visible = true;
    }

    public void HandleKeyboard(IKeyboard keyboard)
    {
        if (!IsOpen || !_listening)
            return;

        var escapeDown = keyboard.IsKeyPressed(Key.Escape);
        if (escapeDown && !_escapeWasDown)
        {
            Close(null);
            _escapeWasDown = escapeDown;
            return;
        }

        _escapeWasDown = escapeDown;

        foreach (var key in SandboxKeyBindCapture.CandidateKeys)
        {
            if (!keyboard.IsKeyPressed(key))
                continue;

            if (SandboxKeyBindCapture.IsModifierKey(key))
                continue;

            Close(key);
            return;
        }
    }

    private void Close(Key? chosen)
    {
        _listening = false;
        _root.Visible = false;
        var cb = _callback;
        _callback = null;
        cb?.Invoke(chosen);
    }

    private static NyxRect CenterPanel(int width, int height) =>
        new((width - PanelW) / 2, (height - PanelH) / 2, PanelW, PanelH);
}
