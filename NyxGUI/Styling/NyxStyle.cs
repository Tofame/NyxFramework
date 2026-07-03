namespace NyxGui;

/// <summary>
/// Resolved style for a widget, computed from theme type selectors, class selectors,
/// inline overrides, and pseudo-state deltas.
/// </summary>
public sealed class NyxStyle
{
    // Colors
    public NyxColor? Background { get; set; }
    public NyxColor? Foreground { get; set; }
    public NyxColor? BorderColor { get; set; }

    // Spacing
    public NyxThickness? Padding { get; set; }
    public NyxThickness? Margin { get; set; }

    // Typography
    public string? FontFile { get; set; }
    public float? FontSize { get; set; }
    public bool? FontBold { get; set; }
    public bool? FontOutlined { get; set; }

    // Borders
    public int? BorderWidth { get; set; }
    public int? BorderRadius { get; set; }

    // Effects
    public float? Opacity { get; set; }

    /// <summary>Merges another style on top of this one. Only non-null values override.</summary>
    public void Merge(NyxStyle other)
    {
        Background ??= other.Background;
        Foreground ??= other.Foreground;
        BorderColor ??= other.BorderColor;
        Padding ??= other.Padding;
        Margin ??= other.Margin;
        FontFile ??= other.FontFile;
        FontSize ??= other.FontSize;
        FontBold ??= other.FontBold;
        FontOutlined ??= other.FontOutlined;
        BorderWidth ??= other.BorderWidth;
        BorderRadius ??= other.BorderRadius;
        Opacity ??= other.Opacity;
    }

    /// <summary>Creates a shallow clone.</summary>
    public NyxStyle Clone() => new()
    {
        Background = Background,
        Foreground = Foreground,
        BorderColor = BorderColor,
        Padding = Padding,
        Margin = Margin,
        FontFile = FontFile,
        FontSize = FontSize,
        FontBold = FontBold,
        FontOutlined = FontOutlined,
        BorderWidth = BorderWidth,
        BorderRadius = BorderRadius,
        Opacity = Opacity,
    };

    /// <summary>True if no properties are set.</summary>
    public bool IsEmpty =>
        Background is null && Foreground is null && BorderColor is null &&
        Padding is null && Margin is null && FontFile is null &&
        FontSize is null && FontBold is null && FontOutlined is null &&
        BorderWidth is null && BorderRadius is null && Opacity is null;
}
