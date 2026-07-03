namespace NyxGui.Definitions;

/// <summary>Input to <see cref="NyxGuiDefinitionBuilder"/>, typically produced by <see cref="NyxuiParser"/>.</summary>
public sealed class NyxGuiBuildSpec
{
    public IReadOnlyList<NyxGuiWidgetDef> Widgets { get; init; } = [];

    public string? DocumentRootId { get; init; }

    public string RootWindowAnchorId { get; init; } = NyxGuiRootWindow.DefaultAnchorId;

    /// <summary>Source file path, for diagnostics and hot reload.</summary>
    public string? SourcePath { get; init; }

    /// <summary>GUI settings from the [document] block.</summary>
    public NyxGuiSettings Settings { get; init; } = NyxGuiSettings.Default;
}
