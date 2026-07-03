namespace NyxGui;

/// <summary>
/// Configuration attached to a widget to make it a drag source.
/// </summary>
public sealed class NyxDragSource
{
    /// <summary>Factory that produces the drag data when a drag begins.</summary>
    public Func<NyxDragData>? GetData { get; set; }

    /// <summary>Minimum pointer movement (pixels) before a drag starts. Default: 4.</summary>
    public int Threshold { get; set; } = 4;

    /// <summary>Optional template id for a custom drag ghost widget.</summary>
    public string? GhostTemplate { get; set; }

    /// <summary>When true, the source widget becomes semi-transparent during drag.</summary>
    public bool FadeSource { get; set; } = true;
}
