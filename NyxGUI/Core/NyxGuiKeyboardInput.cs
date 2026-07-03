namespace NyxGui;

/// <summary>Host-side helpers for <see cref="NyxGuiRootStack.ProcessKeyboard"/>.</summary>
public static class NyxGuiKeyboardInput
{
    /// <summary>True when an editable <see cref="INyxTextEntry"/> has keyboard focus.</summary>
    public static bool HasFocusedTextEntry(NyxGuiRootStack roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        return roots.HasFocusedTextEntry();
    }

    /// <summary>When true, host shortcuts (panel toggles, etc.) should not run.</summary>
    public static bool CapturesGlobalShortcuts(NyxGuiRootStack roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        return roots.CapturesGlobalShortcuts();
    }
}
