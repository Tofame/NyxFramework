namespace NyxGui;

/// <summary>Base paint inputs from the widget (not from interaction states).</summary>
public readonly struct NyxWidgetVisualContext
{
    public float Opacity { get; init; }

    public NyxImageStyle? Image { get; init; }
}

/// <summary>Resolved visual chrome for one paint (borders, background, image, opacity).</summary>
public readonly struct NyxWidgetVisual
{
    public float Opacity { get; init; }

    public NyxImageStyle? Image { get; init; }

    public NyxColor? BackgroundColor { get; init; }

    public int? BorderWidth { get; init; }

    public NyxColor? BorderColor { get; init; }

    public bool HasBorder => BorderWidth is > 0 && BorderColor is not null;

    public bool HasBackground => BackgroundColor is not null;
}
