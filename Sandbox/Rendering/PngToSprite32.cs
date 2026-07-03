using NyxRender;
using StbImageSharp;

namespace Sandbox;

internal static class PngToSprite32
{
    /// <summary>Decode an image file (PNG, etc.) and nearest-neighbor resample into 32×32 RGBA for <see cref="SpriteRenderer"/>.</summary>
    public static bool TryLoadFileToSprite32(string path, Span<byte> rgba4096)
    {
        if (rgba4096.Length != Sprite.Rgba32Length)
            return false;
        if (!File.Exists(path))
            return false;

        ImageResult image;
        try
        {
            using var stream = File.OpenRead(path);
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }
        catch
        {
            return false;
        }

        var sw = image.Width;
        var sh = image.Height;
        if (sw <= 0 || sh <= 0)
            return false;

        ResampleNearest(image.Data.AsSpan(), sw, sh, rgba4096);
        return true;
    }

    private static void ResampleNearest(ReadOnlySpan<byte> src, int sw, int sh, Span<byte> dst)
    {
        for (var y = 0; y < 32; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var sx = Math.Min(sw - 1, x * sw / 32);
                var sy = Math.Min(sh - 1, y * sh / 32);
                var si = (sy * sw + sx) * 4;
                var di = (y * 32 + x) * 4;
                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
                dst[di + 3] = src[si + 3];
            }
        }
    }
}
