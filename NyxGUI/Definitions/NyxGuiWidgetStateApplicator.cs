
namespace NyxGui.Definitions;

/// <summary>Loads visual-only widget state tables into <see cref="NyxWidgetStates"/>.</summary>
internal static class NyxGuiWidgetStateApplicator
{
    public static void ApplySpecStateGroups(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, object?>> stateGroups,
        IReadOnlyDictionary<string, NyxElement> byId,
        NyxGuiLoadOptions options)
    {
        foreach (var (key, group) in stateGroups)
        {
            var table = NyxGuiPropertyBag.From(group);
            if (TryApplyDottedStateKey(key, table, byId, options))
                continue;

            if (byId.TryGetValue(key, out var widget))
                ApplyStateGroupToElement(widget, table, options);
        }
    }

    public static void ApplyWidgetStates(NyxElement element, NyxGuiPropertyBag table, NyxGuiLoadOptions options)
    {
        ApplyVisualState(element.States.Normal, table, options);

        if (table.TryGetValue("normal", out var normalObj) &&
            NyxGuiPropertyBag.TryWrap(normalObj) is { } normalTable)
            ApplyVisualState(element.States.Normal, normalTable, options);

        foreach (var state in NyxGuiWidgetStateNames.InteractionOnly)
        {
            if (table.TryGetValue(state, out var stateObj) &&
                NyxGuiPropertyBag.TryWrap(stateObj) is { } stateTable)
                ApplyStateToTable(element.States, state, stateTable, options);
        }
    }

    public static void ApplyStateToTable(
        NyxWidgetStates states,
        string stateName,
        NyxGuiPropertyBag table,
        NyxGuiLoadOptions options) =>
        ApplyVisualState(states.GetStateTable(stateName), table, options);

    public static void ApplyStateGroup(NyxWidgetStates states, NyxGuiPropertyBag groupTable, NyxGuiLoadOptions options)
    {
        foreach (var stateKey in NyxGuiWidgetStateNames.All)
        {
            if (!groupTable.TryGetValue(stateKey, out var stateObj) ||
                NyxGuiPropertyBag.TryWrap(stateObj) is not { } stateTable)
                continue;
            ApplyStateToTable(states, stateKey, stateTable, options);
        }
    }

    public static void ApplyStateGroupToElement(NyxElement element, NyxGuiPropertyBag groupTable, NyxGuiLoadOptions options) =>
        ApplyStateGroup(element.States, groupTable, options);

    internal static bool TryApplyDottedStateKey(
        string key,
        NyxGuiPropertyBag stateTable,
        IReadOnlyDictionary<string, NyxElement> byId,
        NyxGuiLoadOptions options)
    {
        foreach (var suffix in NyxGuiWidgetStateNames.DottedSuffixes)
        {
            if (!key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var widgetId = key[..^suffix.Length];
            if (!byId.TryGetValue(widgetId, out var element))
                return false;

            var stateName = suffix[1..];
            ApplyStateToTable(element.States, stateName, stateTable, options);
            return true;
        }

        return false;
    }

    /// <summary>Visual keys only — border, opacity, image_*, background_color (not text/label/align).</summary>
    public static void ApplyVisualState(
        NyxWidgetStateOverrides state,
        NyxGuiPropertyBag table,
        NyxGuiLoadOptions options)
    {
        if (NyxGuiDefinitionProperties.TryGetFloat(table, "opacity", out var opacity))
            state.Opacity = Math.Clamp(opacity, 0f, 1f);

        if (table.TryGetValue("border", out var borderObj) && borderObj is not null)
        {
            if (NyxGuiPropertyBag.TryWrap(borderObj) is { } borderTable)
            {
                if (NyxGuiDefinitionProperties.TryGetInt(borderTable, "width", out var w))
                    state.BorderWidth = w;
                if (NyxGuiDefinitionProperties.TryGetColor(borderTable, "color", out var c))
                    state.BorderColor = c;
            }
        }

        if (NyxGuiDefinitionProperties.TryGetInt(table, "border_width", out var borderWidth))
            state.BorderWidth = borderWidth;

        if (NyxGuiDefinitionProperties.TryGetColor(table, "border_color", out var borderColor))
            state.BorderColor = borderColor;

        if (NyxGuiDefinitionProperties.TryGetColor(table, "background_color", out var background))
            state.BackgroundColor = background;

        ApplyImageToOverrides(state, table, options);
    }

    private static void ApplyImageToOverrides(
        NyxWidgetStateOverrides state,
        NyxGuiPropertyBag table,
        NyxGuiLoadOptions options)
    {
        if (!HasAnyImagePropertyKey(table))
            return;

        var style = state.Image ?? new NyxImageStyle();

        if (NyxGuiDefinitionProperties.TryGetString(table, "image_source", out var src) && !string.IsNullOrWhiteSpace(src))
            style.ImageSource = options.ResolveImagePath(src.Trim());

        if (NyxGuiDefinitionProperties.TryGetBool(table, "image_fixed_ratio", out var fixedRatio))
            style.ImageFixedRatio = fixedRatio;
        if (NyxGuiDefinitionProperties.TryGetBool(table, "image_smooth", out var smooth))
            style.ImageSmooth = smooth;
        if (NyxGuiDefinitionProperties.TryGetRect(table, "image_rect", out var irect))
            style.ImageRect = irect;
        if (NyxGuiDefinitionProperties.TryGetRect(table, "image_clip", out var iclip))
            style.ImageClip = iclip;
        NyxGuiDefinitionProperties.ApplyImageBorders(style, table);
        if (NyxGuiDefinitionProperties.TryGetString(table, "image_color", out var colorStr) &&
            NyxColor.TryParseHex(colorStr, out var ic))
            style.ImageColor = ic;
        if (NyxGuiDefinitionProperties.TryGetString(table, "image_object_fit", out var ofStr) || NyxGuiDefinitionProperties.TryGetString(table, "object_fit", out ofStr))
        {
			var normalizedOfStr = ofStr.Replace("-", "").Replace("_", "");
            if (Enum.TryParse<NyxObjectFit>(normalizedOfStr, true, out var of))
                style.ImageObjectFit = of;
        }

        state.Image = style;
    }

    private static bool HasAnyImagePropertyKey(NyxGuiPropertyBag table)
    {
        foreach (var name in table.Keys)
        {
            if (NyxGuiKeyNames.IsImagePropertyKey(name))
                return true;
        }

        return false;
    }
}
