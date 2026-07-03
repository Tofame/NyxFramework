namespace NyxGui;

/// <summary>
/// Visual-only overrides for one interaction state (border, image, opacity, background).
/// Content (<see cref="NyxLabel.Text"/>, <see cref="NyxButton.Label"/>, click handlers) stays on the widget.
/// </summary>
public sealed class NyxWidgetStateOverrides
{
    public int? BorderWidth { get; set; }

    public NyxColor? BorderColor { get; set; }

    public NyxColor? BackgroundColor { get; set; }

    public float? Opacity { get; set; }

    public NyxImageStyle? Image { get; set; }

    public bool HasAnyOverride =>
        BorderWidth is not null || BorderColor is not null || BackgroundColor is not null ||
        Opacity is not null || Image is not null;

    public bool HasBorderOverride => BorderWidth is not null || BorderColor is not null;

    public void CopyFrom(NyxWidgetStateOverrides source)
    {
        BorderWidth = source.BorderWidth;
        BorderColor = source.BorderColor;
        BackgroundColor = source.BackgroundColor;
        Opacity = source.Opacity;
        Image = source.Image;
    }
}
