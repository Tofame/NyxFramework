namespace NyxGui;

/// <summary>Host-wide NyxGUI behaviour (drag visuals, etc.). Load from Lua/settings host or pass at init.</summary>
public sealed class NyxGuiSettings
{
    public static NyxGuiSettings Default { get; } = new();

    /// <summary>Opacity multiplier for a panel subtree while it is being dragged (NyxClient-style).</summary>
    public float PanelDragOpacity { get; set; } = 0.7f;
}
