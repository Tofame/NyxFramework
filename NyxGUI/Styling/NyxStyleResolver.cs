namespace NyxGui;

/// <summary>
/// Resolves the final style for a widget by merging:
/// 1. Default values (hardcoded fallbacks)
/// 2. Theme type styles (e.g. NyxButton)
/// 3. Theme class styles (e.g. .primary)
/// 4. Inline overrides (from .nyxui or C#)
/// 5. Pseudo-state deltas (hover, pressed, focused, disabled)
/// </summary>
public static class NyxStyleResolver
{
    /// <summary>
    /// Resolves the base style for a widget (without pseudo-state overrides).
    /// Result is cached on the widget until StyleDirty is cleared.
    /// </summary>
    public static NyxStyle ResolveBase(NyxElement widget, NyxTheme theme)
    {
        var style = new NyxStyle();

        // 1. Type style from theme
        var typeName = widget.GetType().Name;
        if (theme.TypeStyles.TryGetValue(typeName, out var typeStyle))
            style.Merge(typeStyle);

        // 2. Class styles from theme
        if (!string.IsNullOrEmpty(widget.ThemeClass))
        {
            foreach (var cls in widget.ThemeClass.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (theme.ClassStyles.TryGetValue(cls, out var classStyle))
                    style.Merge(classStyle);
            }
        }

        return style;
    }

    /// <summary>
    /// Resolves the final visual state for painting, merging base style with pseudo-state overrides.
    /// </summary>
    public static NyxWidgetVisual ResolveVisual(
        NyxElement widget,
        NyxTheme theme,
        NyxStyle baseStyle)
    {
        var stateStyle = ResolveStateStyle(widget, theme, baseStyle);

        // Build visual from resolved style
        var visual = new NyxWidgetVisual
        {
            Opacity = stateStyle.Opacity ?? widget.Opacity,
            BackgroundColor = stateStyle.Background,
            BorderWidth = stateStyle.BorderWidth,
            BorderColor = stateStyle.BorderColor,
        };

        // Apply image style if set
        if (widget.Image is not null)
            visual = visual with { Image = widget.Image };

        return visual;
    }

    /// <summary>
    /// Resolves the active pseudo-state style, merging base style with state deltas.
    /// Priority: disabled > on.pressed > on.hover > on > pressed > focused > hover > normal
    /// </summary>
    private static NyxStyle ResolveStateStyle(NyxElement widget, NyxTheme theme, NyxStyle baseStyle)
    {
        var result = baseStyle.Clone();
        var typeName = widget.GetType().Name;

        // Check widget's own States first (from .nyxui/C# inline state overrides)
        if (widget.States.HasAnyOverrides)
        {
            var activeState = widget.States.GetActiveOverride(
                widget.PointerInside,
                widget.PointerPressed,
                widget.IsFocused,
                widget.IsOn);

            if (activeState is not null)
            {
                if (activeState.BackgroundColor is not null)
                    result.Background = activeState.BackgroundColor;
                if (activeState.BorderColor is not null)
                    result.BorderColor = activeState.BorderColor;
                if (activeState.BorderWidth is not null)
                    result.BorderWidth = activeState.BorderWidth;
                if (activeState.Opacity is not null)
                    result.Opacity = activeState.Opacity;
            }
        }

        // Check theme state styles
        if (!widget.Enabled)
        {
            var stateKey = $"{typeName}:disabled";
            if (theme.StateStyles.TryGetValue(stateKey, out var disabledStyle))
                result.Merge(disabledStyle);
        }
        else if (widget.IsOn && widget.PointerPressed)
        {
            var stateKey = $"{typeName}:on.pressed";
            if (theme.StateStyles.TryGetValue(stateKey, out var style))
                result.Merge(style);
        }
        else if (widget.IsOn && widget.PointerInside)
        {
            var stateKey = $"{typeName}:on.hover";
            if (theme.StateStyles.TryGetValue(stateKey, out var style))
                result.Merge(style);
        }
        else if (widget.IsOn)
        {
            var stateKey = $"{typeName}:on";
            if (theme.StateStyles.TryGetValue(stateKey, out var style))
                result.Merge(style);
        }
        else if (widget.PointerPressed)
        {
            var stateKey = $"{typeName}:pressed";
            if (theme.StateStyles.TryGetValue(stateKey, out var style))
                result.Merge(style);
        }
        else if (widget.IsFocused)
        {
            var stateKey = $"{typeName}:focused";
            if (theme.StateStyles.TryGetValue(stateKey, out var style))
                result.Merge(style);
        }
        else if (widget.PointerInside)
        {
            var stateKey = $"{typeName}:hover";
            if (theme.StateStyles.TryGetValue(stateKey, out var style))
                result.Merge(style);
        }

        // Disabled opacity fallback
        if (!widget.Enabled && result.Opacity is null)
            result.Opacity = widget.Opacity * 0.55f;

        return result;
    }
}
