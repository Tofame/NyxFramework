using NyxGui;
using NyxGui.Definitions;

namespace Sandbox.UI;

/// <summary>Wires NyxClient mini window chrome button ids (close / minimize / lock) after UI load.</summary>
internal static class SandboxMiniWindowBehavior
{
    public static void Bind(NyxMiniWindow? window, NyxGuiBuiltDocument? document)
    {
        if (window is null || document is null)
            return;

        if (document.TryGetButton("closeButton") is { } close)
        {
            close.Click += (_, _) => window.Visible = false;
        }

        if (document.TryGetButton("minimizeButton") is { } minimize)
        {
            minimize.IsSelected = window.Minimized;
            minimize.Click += (_, _) =>
            {
                window.SetMinimized(!window.Minimized);
                minimize.IsSelected = window.Minimized;
            };
        }
    }

    public static void TryAppendChrome(
        NyxMiniWindow? window,
        NyxGuiBuiltDocument? document,
        NyxGuiLoadOptions options)
    {
        if (window is null || document is null)
            return;

        Bind(window, document);
    }
}
