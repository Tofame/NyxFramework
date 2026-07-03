namespace NyxGui;

/// <summary>Viewport + anchor naming passed through layout passes.</summary>
public sealed class NyxGuiLayoutEnvironment
{
    public NyxRect WindowBounds { get; init; }

    public string RootWindowAnchorId { get; init; } = NyxGuiRootWindow.DefaultAnchorId;

    public NyxLayoutContext CreateContext(NyxRect parentBounds, IReadOnlyDictionary<string, NyxElement> widgetsById) =>
        new()
        {
            ParentBounds = parentBounds,
            WidgetsById = widgetsById,
            WindowBounds = WindowBounds,
            RootWindowAnchorId = RootWindowAnchorId,
        };
}
