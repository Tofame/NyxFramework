namespace NyxGui.Definitions;

/// <summary>Parses canonical Lua flat definition tables into <see cref="NyxGuiBuildSpec"/>.</summary>
public static class NyxGuiLuaBuildSpec
{
    public const string DocumentKey = "_document";

    /// <summary>
    /// <paramref name="root"/> is a flat map: widget id → { type = "Label", parent = "…", … }.
    /// Optional <c>_document</c> = { root = "…", root_window = "…" }.
    /// </summary>
    public static NyxGuiBuildSpec FromFlatTable(IReadOnlyDictionary<string, object?> root)
    {
        string? documentRootId = null;
        var rootWindowAnchorId = NyxGuiRootWindow.DefaultAnchorId;
        if (root.TryGetValue(DocumentKey, out var docObj) &&
            docObj is IReadOnlyDictionary<string, object?> docTable)
        {
            var docBag = new NyxGuiPropertyBag(docTable);
            if (docBag.TryGetString("root", out var r))
                documentRootId = r;
            if (docBag.TryGetString("root_window", out var rw))
                rootWindowAnchorId = rw;
        }

        var widgets = new List<NyxGuiWidgetDef>();
        var order = 0;

        foreach (var (key, value) in root)
        {
            if (key.Equals(DocumentKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (value is not IReadOnlyDictionary<string, object?> props)
                continue;

            if (!TryGetWidgetKind(props, out var kind))
                continue;

            var id = key;
            var bag = new NyxGuiPropertyBag(props);
            if (bag.TryGetString("id", out var explicitId) && !string.IsNullOrWhiteSpace(explicitId))
                id = explicitId;

            widgets.Add(new NyxGuiWidgetDef
            {
                Id = id,
                Kind = kind,
                Properties = NormalizeLuaProperties(props),
                DefinitionOrder = order++,
            });
        }

        return new NyxGuiBuildSpec
        {
            Widgets = widgets,
            DocumentRootId = documentRootId,
            RootWindowAnchorId = rootWindowAnchorId,
        };
    }

    public static bool HasWidgetType(IReadOnlyDictionary<string, object?> props) =>
        TryGetWidgetKind(props, out _);

    private static bool TryGetWidgetKind(IReadOnlyDictionary<string, object?> props, out string kind)
    {
        kind = string.Empty;
        if (!props.TryGetValue("type", out var typeObj) && !props.TryGetValue("Type", out typeObj))
            return false;
        kind = typeObj?.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(kind);
    }

    private static Dictionary<string, object?> NormalizeLuaProperties(IReadOnlyDictionary<string, object?> props)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in props)
        {
            if (key.Equals("type", StringComparison.OrdinalIgnoreCase))
                continue;
            dict[NyxGuiKeyNames.ToSnakeCase(key)] = value;
        }

        return dict;
    }
}
