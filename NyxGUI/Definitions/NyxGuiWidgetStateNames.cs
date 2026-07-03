namespace NyxGui.Definitions;

/// <summary>Canonical Lua state names for <see cref="NyxGui.NyxWidgetStates"/> tables.</summary>
internal static class NyxGuiWidgetStateNames
{
    public static readonly string[] All =
    [
        "normal", "default", "hover", "hovered", "pressed", "clicked", "focused", "disabled",
        "on", "on.hover", "on.hovered", "on.pressed", "on.clicked",
    ];

    public static readonly string[] InteractionOnly =
    [
        "hover", "hovered", "pressed", "clicked", "focused", "disabled",
        "on", "on.hover", "on.hovered", "on.pressed", "on.clicked",
    ];

    public static readonly string[] DottedSuffixes =
    [
        ".on.hover", ".on.hovered", ".on.pressed", ".on.clicked",
        ".on", ".normal", ".hover", ".hovered", ".pressed", ".clicked", ".focused", ".disabled",
    ];
}
