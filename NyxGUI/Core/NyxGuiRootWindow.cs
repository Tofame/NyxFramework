namespace NyxGui;

/// <summary>
/// The logical game window / viewport used by edge anchors such as <c>rootWindow.top</c>.
/// This is not a widget — it is the coordinate rectangle the host sets via <see cref="NyxGuiBuiltDocument.SetWindowSize"/>.
/// </summary>
public static class NyxGuiRootWindow
{
    public const string DefaultAnchorId = "rootWindow";

    /// <summary>All names that resolve to <see cref="NyxLayoutContext.WindowBounds"/> (plus the configured id from TOML).</summary>
    public static bool IsRootWindowTarget(string targetId, string configuredAnchorId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
            return false;

        if (targetId.Equals(configuredAnchorId, StringComparison.OrdinalIgnoreCase))
            return true;

        return targetId.ToLowerInvariant() switch
        {
            "rootwindow" or "root" or "window" or "viewport" or "screen" => true,
            _ => false,
        };
    }
}
