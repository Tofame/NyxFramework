namespace NyxGui;

/// <summary>
/// Visual-only overrides per interaction state (<c>$hover</c>, <c>$pressed</c>, <c>$on</c>, …).
///
/// <b>State resolution (<see cref="Resolve"/>):</b>
/// When enabled, priority is: <c>On+Pressed</c> > <c>On+Hover</c> > <c>On</c> > <c>Pressed</c> >
/// <c>Focused</c> > <c>Hover</c> > <c>Normal</c>.  Each resolved state merges on top of
/// the Normal baseline via <see cref="MergeImage"/> which selectively copies fields from
/// the override onto the base (background colour, border width/colour, text colour, tint,
/// icon colour, opacity, image source).
///
/// When disabled, only <see cref="Disabled"/> is used.
///
/// <b>State table (<see cref="GetStateTable"/>):</b> state names are parsed from dotted
/// keys like <c>"CheckBox.red.checked.hover"</c>, mapping the base type + variant to an
/// existing state table entry.
/// </summary>
public sealed class NyxWidgetStates
{
    public NyxWidgetStateOverrides Normal { get; } = new();

    public NyxWidgetStateOverrides Hover { get; } = new();

    public NyxWidgetStateOverrides Pressed { get; } = new();

    public NyxWidgetStateOverrides Focused { get; } = new();

    public NyxWidgetStateOverrides On { get; } = new();

    public NyxWidgetStateOverrides OnHover { get; } = new();

    public NyxWidgetStateOverrides OnPressed { get; } = new();

    public NyxWidgetStateOverrides Disabled { get; } = new();

    public bool HasAnyOverrides =>
        Normal.HasAnyOverride || Hover.HasAnyOverride || Pressed.HasAnyOverride ||
        Focused.HasAnyOverride || On.HasAnyOverride || OnHover.HasAnyOverride || OnPressed.HasAnyOverride ||
        Disabled.HasAnyOverride;

    public bool ConfiguresBorder =>
        Normal.HasBorderOverride || Hover.HasBorderOverride || Pressed.HasBorderOverride ||
        Focused.HasBorderOverride || On.HasBorderOverride || OnHover.HasBorderOverride ||
        OnPressed.HasBorderOverride;

    /// <summary>Merges widget base chrome with the highest-priority active state override.</summary>
    public NyxWidgetVisual Resolve(
        in NyxWidgetVisualContext context,
        bool hovered,
        bool pressed,
        bool focused,
        bool isOn,
        bool enablement = true)
    {
        if (!enablement)
            return ResolveDisabled(in context);

        if (!HasAnyOverrides)
            return FromContext(in context);

        var active = GetActiveOverride(hovered, pressed, focused, isOn);

        var opacity = active?.Opacity ?? context.Opacity;
        var image = MergeImage(context.Image, active?.Image);

        NyxColor? background = null;
        if (active?.BackgroundColor is not null)
            background = active.BackgroundColor;
        else if (Normal.BackgroundColor is not null)
            background = Normal.BackgroundColor;

        int? borderWidth = null;
        NyxColor? borderColor = null;
        if (active?.HasBorderOverride == true)
        {
            borderWidth = active.BorderWidth;
            borderColor = active.BorderColor;
        }
        else if (Normal.HasBorderOverride)
        {
            borderWidth = Normal.BorderWidth;
            borderColor = Normal.BorderColor;
        }

        return new NyxWidgetVisual
        {
            Opacity = opacity,
            Image = image,
            BackgroundColor = background,
            BorderWidth = borderWidth,
            BorderColor = borderColor,
        };
    }

    public static NyxWidgetVisual FromContext(in NyxWidgetVisualContext context) =>
        new() { Opacity = context.Opacity, Image = context.Image };

    private NyxWidgetVisual ResolveDisabled(in NyxWidgetVisualContext context)
    {
        if (Disabled.HasAnyOverride)
        {
            var opacity = Disabled.Opacity ?? context.Opacity * 0.55f;
            var image = MergeImage(context.Image, Disabled.Image);
            return new NyxWidgetVisual
            {
                Opacity = opacity,
                Image = image,
                BackgroundColor = Disabled.BackgroundColor ?? Normal.BackgroundColor,
                BorderWidth = Disabled.HasBorderOverride ? Disabled.BorderWidth : Normal.BorderWidth,
                BorderColor = Disabled.BorderColor ?? Normal.BorderColor,
            };
        }

        var baseVisual = Normal.HasAnyOverride
            ? Resolve(context, false, false, false, false, enablement: true)
            : FromContext(in context);

        return baseVisual with { Opacity = baseVisual.Opacity * 0.55f };
    }

    /// <summary>Merges per-state image deltas (e.g. hover <c>image-clip</c> only) onto the widget base image.</summary>
    private static NyxImageStyle? MergeImage(NyxImageStyle? baseImage, NyxImageStyle? overrideImage)
    {
        if (overrideImage is null)
            return baseImage;
        if (baseImage is null)
            return overrideImage;
        if (!string.IsNullOrWhiteSpace(overrideImage.ImageSource))
            return overrideImage;

        return new NyxImageStyle
        {
            ImageSource = baseImage.ImageSource,
            ImageRect = overrideImage.ImageRect ?? baseImage.ImageRect,
            ImageClip = overrideImage.ImageClip ?? baseImage.ImageClip,
            ImageColor = overrideImage.ImageColor ?? baseImage.ImageColor,
            ImageBorders = overrideImage.ImageBorders.HasAny ? overrideImage.ImageBorders : baseImage.ImageBorders,
            ImageFixedRatio = baseImage.ImageFixedRatio,
            ImageSmooth = baseImage.ImageSmooth,
			ImageObjectFit = overrideImage.ImageObjectFit != NyxObjectFit.Fill ? overrideImage.ImageObjectFit : baseImage.ImageObjectFit,
        };
    }

    public NyxWidgetStateOverrides? GetActiveOverride(bool hovered, bool pressed, bool focused, bool isOn)
    {
        if (isOn)
        {
            if (pressed && OnPressed.HasAnyOverride)
                return OnPressed;
            if (hovered && OnHover.HasAnyOverride)
                return OnHover;
            if (On.HasAnyOverride)
                return On;
        }

        if (pressed && Pressed.HasAnyOverride)
            return Pressed;
        if (focused && Focused.HasAnyOverride)
            return Focused;
        if (hovered && Hover.HasAnyOverride)
            return Hover;
        if (Normal.HasAnyOverride)
            return Normal;
        return null;
    }

    public NyxWidgetStateOverrides GetStateTable(string stateName)
    {
        if (stateName.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
            stateName.Equals("default", StringComparison.OrdinalIgnoreCase))
            return Normal;
        if (stateName.Equals("hover", StringComparison.OrdinalIgnoreCase) ||
            stateName.Equals("hovered", StringComparison.OrdinalIgnoreCase))
            return Hover;
        if (stateName.Equals("pressed", StringComparison.OrdinalIgnoreCase) ||
            stateName.Equals("clicked", StringComparison.OrdinalIgnoreCase))
            return Pressed;
        if (stateName.Equals("focused", StringComparison.OrdinalIgnoreCase))
            return Focused;
        if (stateName.Equals("on", StringComparison.OrdinalIgnoreCase))
            return On;

        if (stateName.Equals("on.hover", StringComparison.OrdinalIgnoreCase) ||
            stateName.Equals("on.hovered", StringComparison.OrdinalIgnoreCase))
            return OnHover;
        if (stateName.Equals("on.pressed", StringComparison.OrdinalIgnoreCase) ||
            stateName.Equals("on.clicked", StringComparison.OrdinalIgnoreCase))
            return OnPressed;
        if (stateName.Equals("disabled", StringComparison.OrdinalIgnoreCase))
            return Disabled;

        throw new InvalidDataException(
            $"Unknown widget state \"{stateName}\" (use normal, hover, pressed, focused, disabled, on, on.hover, on.pressed).");
    }

    public void CopyTo(NyxWidgetStates target) => target.CopyFrom(this);

    public void CopyFrom(NyxWidgetStates source)
    {
        Normal.CopyFrom(source.Normal);
        Hover.CopyFrom(source.Hover);
        Pressed.CopyFrom(source.Pressed);
        Focused.CopyFrom(source.Focused);
        On.CopyFrom(source.On);
        OnHover.CopyFrom(source.OnHover);
        OnPressed.CopyFrom(source.OnPressed);
        Disabled.CopyFrom(source.Disabled);
    }
}
