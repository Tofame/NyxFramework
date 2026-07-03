namespace NyxGui;

/// <summary>
/// Styling for textured backgrounds (similar to NyxClient-style <c>image-*</c> properties).
/// </summary>
public sealed class NyxImageStyle
{
    /// <summary>Host-resolved path or logical id (e.g. relative to game <c>resources/images/ui</c>).</summary>
    public string? ImageSource { get; set; }

    /// <summary>When true, the image keeps aspect ratio inside <see cref="NyxImagePaintCommand.Destination"/>.</summary>
    public bool ImageFixedRatio { get; set; }

	public NyxObjectFit ImageObjectFit { get; set; } = NyxObjectFit.Fill;

    /// <summary>Source rectangle in texture pixel space (x, y, width, height). Null = full texture.</summary>
    public NyxRect? ImageRect { get; set; }

    public bool ImageSmooth { get; set; } = true;

    /// <summary>Tint / modulation colour. Null = opaque white.</summary>
    public NyxColor? ImageColor { get; set; }

    /// <summary>Additional crop in texture pixel space (applied after <see cref="ImageRect"/> if both set).</summary>
    public NyxRect? ImageClip { get; set; }

    /// <summary>9-slice insets from TOML <c>image-border*</c> (see <see cref="NyxImageBorders"/>).</summary>
    public NyxImageBorders ImageBorders { get; set; }

    public NyxImagePaintCommand ToPaintCommand(NyxRect destination, NyxColor defaultTint, float opacity = 1f)
    {
        var tint = NyxElementPaint.WithOpacity(ImageColor ?? defaultTint, opacity);
        return new NyxImagePaintCommand(
            destination,
            ImageSource ?? string.Empty,
            ImageRect,
            ImageClip,
            ImageBorders,
            ImageFixedRatio,
            ImageSmooth,
            tint,
			ImageObjectFit);
    }
}

/// <summary>Host draw request for a styled image (see <see cref="NyxImageStyle"/>).</summary>
public readonly struct NyxImagePaintCommand
{
    public NyxImagePaintCommand(
        NyxRect destination,
        string imageSource,
        NyxRect? sourceRect,
        NyxRect? sourceClip,
        NyxImageBorders imageBorders,
        bool fixedRatio,
        bool smooth,
        NyxColor tint,
		NyxObjectFit objectFit = NyxObjectFit.Fill)
    {
        Destination = destination;
        ImageSource = imageSource;
        SourceRect = sourceRect;
        SourceClip = sourceClip;
        ImageBorders = imageBorders;
        FixedRatio = fixedRatio;
        Smooth = smooth;
        Tint = tint;
		ObjectFit = objectFit;
    }

    public NyxRect Destination { get; }
    public string ImageSource { get; }
    public NyxRect? SourceRect { get; }
    public NyxRect? SourceClip { get; }
    public NyxImageBorders ImageBorders { get; }
    public bool FixedRatio { get; }
    public bool Smooth { get; }
    public NyxColor Tint { get; }
	public NyxObjectFit ObjectFit { get; }
}
