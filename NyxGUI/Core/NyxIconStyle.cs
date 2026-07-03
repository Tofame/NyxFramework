namespace NyxGui;

/// <summary>Small overlay icon (title bar, labels, buttons). Parsed from TOML <c>icon*</c> keys.</summary>
public sealed class NyxIconStyle
{
    public string? IconSource { get; set; }

    /// <summary>Destination width in pixels. Zero until <see cref="ResolveSize"/> (when <c>icon-size</c> was omitted).</summary>
    public int Width { get; set; }

    /// <summary>Destination height in pixels. Zero until <see cref="ResolveSize"/>.</summary>
    public int Height { get; set; }

    internal bool HasExplicitSize { get; set; }

    public int OffsetX { get; set; }

    public int OffsetY { get; set; }

    public NyxIconAlign Align { get; set; } = NyxIconAlign.Left;

    /// <summary>Destination rect relative to the paint area top-left (NyxClient <c>icon-rect</c>).</summary>
    public NyxRect? DestinationRect { get; set; }

    /// <summary>Source crop in texture pixels (x y width height).</summary>
    public NyxRect? Clip { get; set; }

    public bool Smooth { get; set; } = true;

    public NyxColor? Color { get; set; }

    public bool HasSource => !string.IsNullOrWhiteSpace(IconSource);

    /// <summary>
    /// Fills <see cref="Width"/> / <see cref="Height"/> when <c>icon-size</c> was not set:
    /// <see cref="Clip"/> size if present, else the PNG IHDR dimensions, else 16×16.
    /// </summary>
    public void ResolveSize(string? resolvedFilePath)
    {
        if (HasExplicitSize)
            return;

        if (Clip is { Width: > 0, Height: > 0 } clip)
        {
            Width = clip.Width;
            Height = clip.Height;
            return;
        }

        if (resolvedFilePath is not null &&
            Definitions.NyxImageProbe.TryGetPngDimensions(resolvedFilePath, out var w, out var h))
        {
            Width = w;
            Height = h;
            return;
        }

        Width = 16;
        Height = 16;
    }

    public NyxRect ComputeDestination(NyxRect area)
    {
        if (DestinationRect is { } rect)
        {
            return new NyxRect(
                area.X + rect.X + OffsetX,
                area.Y + rect.Y + OffsetY,
                Math.Max(1, rect.Width),
                Math.Max(1, rect.Height));
        }

        var w = Math.Max(1, Width);
        var h = Math.Max(1, Height);
        var x = Align switch
        {
            NyxIconAlign.Right => area.Right - w,
            NyxIconAlign.Center => area.X + (area.Width - w) / 2,
            _ => area.X,
        };
        var y = area.Y + OffsetY;
        x += OffsetX;
        return new NyxRect(x, y, w, h);
    }

    public NyxImagePaintCommand ToPaintCommand(NyxRect destination, NyxColor defaultTint, float opacity = 1f)
    {
        var tint = NyxElementPaint.WithOpacity(Color ?? defaultTint, opacity);
        return new NyxImagePaintCommand(
            destination,
            IconSource ?? string.Empty,
            null,
            Clip,
            default,
            fixedRatio: false,
            smooth: Smooth,
            tint);
    }
}
