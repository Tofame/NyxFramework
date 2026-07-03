namespace NyxGui;

/// <summary>
/// Configuration attached to a widget to make it a drop target.
/// </summary>
public sealed class NyxDropTarget
{
    /// <summary>Accepted data type names. Empty means accept all.</summary>
    public HashSet<string> AcceptTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Called when a drag enters this widget.</summary>
    public Action<NyxDragData>? OnDragEnter { get; set; }

    /// <summary>Called when a drag leaves this widget.</summary>
    public Action? OnDragLeave { get; set; }

    /// <summary>Called when a drop occurs. Return true to accept, false to reject.</summary>
    public Func<NyxDragData, bool>? OnDrop { get; set; }

    /// <summary>Checks if this target accepts the given drag data.</summary>
    public bool Accepts(NyxDragData data)
    {
        if (AcceptTypes.Count == 0) return true;
        return AcceptTypes.Contains(data.DataType);
    }
}
