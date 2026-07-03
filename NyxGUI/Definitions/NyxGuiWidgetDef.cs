namespace NyxGui.Definitions;

/// <summary>Normalized widget definition for <see cref="NyxGuiDefinitionBuilder"/>.</summary>
public sealed class NyxGuiWidgetDef
{
    public required string Id { get; init; }

    /// <summary>Widget kind, e.g. Panel, Label, MiniWindow.</summary>
    public required string Kind { get; init; }

    /// <summary>Property bag: snake_case keys.</summary>
    public required IReadOnlyDictionary<string, object?> Properties { get; init; }

    /// <summary>Visual state overrides keyed by state name (hover, pressed, focused, etc.).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> States { get; init; }
        = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Order in which the widget appeared in the source.</summary>
    public int DefinitionOrder { get; init; }

    /// <summary>Parent widget id (null for root-level widgets).</summary>
    public string? ParentId { get; set; }

    /// <summary>Source line number (1-based), for diagnostics.</summary>
    public int SourceLine { get; init; }

    /// <summary>Source file path, for diagnostics and hot reload.</summary>
    public string? SourcePath { get; init; }
}
