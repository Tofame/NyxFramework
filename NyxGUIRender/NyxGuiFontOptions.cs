namespace NyxGuiRender;

/// <summary>
/// Font configuration for <see cref="NyxGuiRenderer"/>.  Tells the renderer which
/// font file to load, at what size, and provides an optional path-resolver delegate
/// for mapping logical font names to disk paths.
/// </summary>
public sealed class NyxGuiFontOptions
{
    /// <summary>Logical font file name or path (e.g. "fonts/DejaVuSans.ttf").</summary>
    public string? FontFileName { get; init; }

    /// <summary>Point size for glyph rasterization.  Default 14.</summary>
    public float SizePt { get; init; } = 14f;

    /// <summary>
    /// Optional delegate to resolve a logical font name (e.g. from <c>.nyxui</c> markup)
    /// to an absolute file path.  If null, font names are used as-is.
    /// </summary>
    public Func<string, string?>? ResolveFontPath { get; init; }
}
