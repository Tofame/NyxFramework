namespace NyxGui;

/// <summary>Data needed to resolve edge anchors for widgets in the same coordinate space.</summary>
public sealed class NyxLayoutContext
{
    public required NyxRect ParentBounds { get; init; }

    public IReadOnlyDictionary<string, NyxElement> WidgetsById { get; init; } =
        new Dictionary<string, NyxElement>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Full window / viewport (anchors like <c>rootWindow.top</c>).</summary>
    public NyxRect WindowBounds { get; init; }

    /// <summary>Configured name for the window target (default <see cref="NyxGuiRootWindow.DefaultAnchorId"/>).</summary>
    public string RootWindowAnchorId { get; init; } = NyxGuiRootWindow.DefaultAnchorId;
}
