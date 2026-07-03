namespace NyxGui;

/// <summary>
/// Per-widget font overrides from TOML <c>font</c>, <c>font-size</c>, <c>font-bold</c>, <c>text-outline</c>.
/// Unset fields inherit from ancestors or the renderer default.
/// </summary>
public sealed class NyxFontStyle
{
    public static readonly NyxFontStyle Default = new();

    /// <summary>Resolved font file path (from <see cref="Definitions.NyxGuiLoadOptions.ResolveFontFile"/>).</summary>
    public string? File { get; init; }

    /// <summary>Point size (<c>font-size</c>).</summary>
    public float? SizePt { get; init; }

    public bool Bold { get; init; }

    /// <summary>1px black outline around glyphs (CLIENT stack counts).</summary>
    public bool Outlined { get; init; }

    public bool IsDefault => File is null && SizePt is null && !Bold && !Outlined;

    public NyxFontStyle WithOutlined() =>
        Outlined ? this : new NyxFontStyle { File = File, SizePt = SizePt, Bold = Bold, Outlined = true };
}
