using NyxDrawer.Appearance;
using NyxRender;
using NyxAssets.Client;

namespace NyxDrawer.Drawing;

/// <summary>
/// CPU fallback when GPU outfit draw cannot run (cross-atlas, missing shader).
///
/// <b>Template-threshold coloring (<see cref="CompositePixels"/>):</b>
/// The mask texture encodes which body part each pixel belongs to using near-white thresholds:
/// <list type="bullet">
///   <item><b>Head:</b> mask R &gt; 230 AND mask G &gt; 230</item>
///   <item><b>Body:</b> mask R &gt; 230 (but mask G &lt;= 230)</item>
///   <item><b>Legs:</b> mask G &gt; 230 (but mask R &lt;= 230)</item>
///   <item><b>Feet:</b> mask B &gt; 230 (and R,G both &lt;= 230)</item>
///   <item><b>No match:</b> pixel is copied unchanged (non-template parts of the outfit).</item>
/// </list>
/// Matched pixels are tinted multiplicatively: <c>output = (base * tint) / 255</c>.
/// Alpha is preserved from the base sprite.
///
/// <b>Composite ID:</b> a deterministic negative int derived from base + mask sprite IDs
/// via prime multiplier to avoid collisions in the renderer's atlas slot table.
/// </summary>
internal static class OutfitColorCompositor
{
    private const byte TemplateThreshold = 230;

    /// <summary>
    /// Generates a deterministic negative sprite ID for the composited result.
    /// Uses unchecked arithmetic with prime 65537 to distribute ID pairs uniformly.
    /// </summary>
    public static int CompositeSpriteId(uint baseSpriteId, uint maskSpriteId) =>
        unchecked(-(int)(baseSpriteId * 65537u + maskSpriteId + 1u));

    public static bool TryComposite(
        ClientAssetBundle assets,
        uint baseSpriteId,
        uint maskSpriteId,
        OutfitColorLayout colors,
        Span<byte> outputRgba)
    {
        Span<byte> basePx = stackalloc byte[Sprite.Rgba32Length];
        Span<byte> maskPx = stackalloc byte[Sprite.Rgba32Length];
        if (!assets.TryDecodeSpriteById(baseSpriteId, basePx) ||
            !assets.TryDecodeSpriteById(maskSpriteId, maskPx))
            return false;

        CompositePixels(basePx, maskPx, colors, outputRgba);
        return true;
    }

    public static void CompositePixels(
        ReadOnlySpan<byte> baseRgba,
        ReadOnlySpan<byte> maskRgba,
        OutfitColorLayout colors,
        Span<byte> outputRgba)
    {
        for (var i = 0; i < 32 * 32; i++)
        {
            var o = i * 4;
            var baseR = baseRgba[o];
            var baseG = baseRgba[o + 1];
            var baseB = baseRgba[o + 2];
            var baseA = baseRgba[o + 3];
            if (baseA < 4)
            {
                outputRgba[o] = 0;
                outputRgba[o + 1] = 0;
                outputRgba[o + 2] = 0;
                outputRgba[o + 3] = 0;
                continue;
            }

            var maskR = maskRgba[o];
            var maskG = maskRgba[o + 1];
            var maskB = maskRgba[o + 2];

            Color tint;
            if (maskR > TemplateThreshold)
                tint = maskG > TemplateThreshold ? colors.Head : colors.Body;
            else if (maskG > TemplateThreshold)
                tint = colors.Legs;
            else if (maskB > TemplateThreshold)
                tint = colors.Feet;
            else
            {
                outputRgba[o] = baseR;
                outputRgba[o + 1] = baseG;
                outputRgba[o + 2] = baseB;
                outputRgba[o + 3] = baseA;
                continue;
            }

            outputRgba[o] = (byte)(baseR * tint.R / 255);
            outputRgba[o + 1] = (byte)(baseG * tint.G / 255);
            outputRgba[o + 2] = (byte)(baseB * tint.B / 255);
            outputRgba[o + 3] = baseA;
        }
    }
}
