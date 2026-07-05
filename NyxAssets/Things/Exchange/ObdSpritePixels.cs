using NyxAssets.Sprites;

namespace NyxAssets.Things.Exchange;

/// <summary>Converts between Nyx RGBA buffers and Object Builder sprite pixel layouts.</summary>
internal static class ObdSpritePixels
{
    /// <summary>Object Builder / Flash <c>BitmapData</c> order: A, R, G, B per pixel.</summary>
    public static byte[] RgbaToObjectBuilderArgb(ReadOnlySpan<byte> rgba)
    {
        if (rgba.Length < SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException("Expected 4096 RGBA bytes.", nameof(rgba));

        var argb = new byte[SpritePixelCodec.RgbaBufferLength];
        for (var p = 0; p < SpritePixelCodec.SpriteEdgeLength * SpritePixelCodec.SpriteEdgeLength; p++)
        {
            var src = p * 4;
            var dst = p * 4;
            argb[dst] = rgba[src + 3];
            argb[dst + 1] = rgba[src];
            argb[dst + 2] = rgba[src + 1];
            argb[dst + 3] = rgba[src + 2];
        }

        return argb;
    }

    public static void ObjectBuilderArgbToRgba(ReadOnlySpan<byte> argb, Span<byte> rgba)
    {
        if (argb.Length < SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException("Expected 4096 ARGB bytes.", nameof(argb));
        if (rgba.Length < SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException("Destination must be at least 4096 bytes.", nameof(rgba));

        for (var p = 0; p < SpritePixelCodec.SpriteEdgeLength * SpritePixelCodec.SpriteEdgeLength; p++)
        {
            var src = p * 4;
            var dst = p * 4;
            rgba[dst] = argb[src + 1];
            rgba[dst + 1] = argb[src + 2];
            rgba[dst + 2] = argb[src + 3];
            rgba[dst + 3] = argb[src];
        }
    }

    public static byte[] CompressForObd(ReadOnlySpan<byte> rgba, bool transparent) =>
        SpritePixelCodec.CompressRgba(rgba, transparent);

    public static void DecompressFromObd(ReadOnlySpan<byte> compressed, bool transparent, Span<byte> rgba) =>
        SpritePixelCodec.UncompressToRgba(compressed, transparent, rgba);
}
