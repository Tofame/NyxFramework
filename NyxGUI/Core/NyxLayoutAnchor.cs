namespace NyxGui;

/// <summary>
/// One horizontal or vertical edge anchor: attaches to an edge of parent or another widget.
/// Examples: "parent.top", "otherWidget.right", "viewport.bottom".
/// </summary>
public readonly struct NyxLayoutAnchor : IEquatable<NyxLayoutAnchor>
{
    private NyxLayoutAnchor(string targetId, NyxAnchorEdge edge)
    {
        TargetId = targetId;
        Edge = edge;
    }

    /// <summary>Target widget id: "parent", a widget Id, or the root window alias.</summary>
    public string? TargetId { get; }

    /// <summary>Which edge of the target to attach to.</summary>
    public NyxAnchorEdge Edge { get; }

    public static NyxLayoutAnchor ParentEdge(NyxAnchorEdge edge) => new("parent", edge);

    public static NyxLayoutAnchor WidgetEdge(string widgetId, NyxAnchorEdge edge) => new(widgetId, edge);

    /// <summary>Parse "parent.top", "accountNameTextEdit.right", "viewport.bottom".</summary>
    public static bool TryParse(string text, out NyxLayoutAnchor anchor)
    {
        anchor = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var dot = text.IndexOf('.');
        if (dot <= 0 || dot >= text.Length - 1) return false;

        var target = text[..dot].Trim();
        var edgeName = text[(dot + 1)..].Trim();
        if (!TryParseEdge(edgeName, out var edge)) return false;

        anchor = new NyxLayoutAnchor(target, edge);
        return true;
    }

    public static bool TryParseEdge(string name, out NyxAnchorEdge edge)
    {
        switch (name.ToLowerInvariant())
        {
            case "left": edge = NyxAnchorEdge.Left; return true;
            case "right": edge = NyxAnchorEdge.Right; return true;
            case "top": edge = NyxAnchorEdge.Top; return true;
            case "bottom": edge = NyxAnchorEdge.Bottom; return true;
            case "centerx": case "center_x": case "hcenter": case "horizontalcenter": case "horizontal_center": case "center":
                edge = NyxAnchorEdge.CenterX; return true;
            case "centery": case "center_y": case "vcenter": case "verticalcenter": case "vertical_center":
                edge = NyxAnchorEdge.CenterY; return true;
            default: edge = default; return false;
        }
    }

    /// <summary>Yields the target widget ID if it's a real widget (excludes "parent" and root window).</summary>
    public IEnumerable<string> GetWidgetDependencies(string rootWindowAnchorId = NyxGuiRootWindow.DefaultAnchorId)
    {
        if (string.IsNullOrEmpty(TargetId)) yield break;
        if (TargetId.Equals("parent", StringComparison.OrdinalIgnoreCase)) yield break;
        if (NyxGuiRootWindow.IsRootWindowTarget(TargetId, rootWindowAnchorId)) yield break;
        yield return TargetId;
    }

    public bool Equals(NyxLayoutAnchor other) =>
        string.Equals(TargetId, other.TargetId, StringComparison.OrdinalIgnoreCase) && Edge == other.Edge;

    public override bool Equals(object? obj) => obj is NyxLayoutAnchor a && Equals(a);

    public override int GetHashCode() => HashCode.Combine(TargetId?.ToLowerInvariant(), Edge);
}
