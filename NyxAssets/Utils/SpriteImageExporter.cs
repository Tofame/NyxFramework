using SkiaSharp;
using NyxAssets.Sprites;

namespace NyxAssets.Utils;

/// <summary>
/// Writes decoded 32×32 sprite pixels (see <see cref="SpritePixelCodec.RgbaBufferLength"/>) to raster files via <b>SkiaSharp</b>.
/// Input layout matches <see cref="SpritePixelCodec.UncompressToRgba"/>: per pixel <c>R, G, B, A</c> bytes.
/// </summary>
public static class SpriteImageExporter
{
    /// <summary>JPEG quality 1–100 (higher = larger file, better fidelity).</summary>
    public const int DefaultJpegQuality = 90;

    private static void ValidateBuffer(ReadOnlySpan<byte> nyxAssetsRgba32x32)
    {
        if (nyxAssetsRgba32x32.Length != SpritePixelCodec.RgbaBufferLength)
        {
            throw new ArgumentException(
                $"Expected exactly {SpritePixelCodec.RgbaBufferLength} bytes (32×32 RGBA).",
                nameof(nyxAssetsRgba32x32));
        }
    }

    private static SKBitmap ToImage(ReadOnlySpan<byte> nyxAssetsRgba32x32)
    {
        ValidateBuffer(nyxAssetsRgba32x32);
        var edge = SpritePixelCodec.SpriteEdgeLength;
        var info = new SKImageInfo(edge, edge, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        for (var y = 0; y < edge; y++)
        {
            for (var x = 0; x < edge; x++)
            {
                var o = (y * edge + x) * 4;
                bitmap.SetPixel(x, y, new SKColor(
                    nyxAssetsRgba32x32[o],
                    nyxAssetsRgba32x32[o + 1],
                    nyxAssetsRgba32x32[o + 2],
                    nyxAssetsRgba32x32[o + 3]));
            }
        }

        return bitmap;
    }

    /// <summary>32×32 decoded buffer → new image (caller must dispose).</summary>
    public static SKBitmap CreateImageFromSpriteBuffer(ReadOnlySpan<byte> nyxAssetsRgba32x32) =>
        ToImage(nyxAssetsRgba32x32);

    /// <summary>Straight-alpha source-over destination (for compositing sheets).</summary>
    public static void BlitSpriteBufferOnto(SKBitmap dest, int destX, int destY, ReadOnlySpan<byte> nyxAssetsRgba32x32)
    {
        ValidateBuffer(nyxAssetsRgba32x32);
        var edge = SpritePixelCodec.SpriteEdgeLength;
        for (var y = 0; y < edge; y++)
        {
            for (var x = 0; x < edge; x++)
            {
                var dx = destX + x;
                var dy = destY + y;
                if ((uint)dx >= (uint)dest.Width || (uint)dy >= (uint)dest.Height)
                    continue;
                var o = (y * edge + x) * 4;
                var src = new SKColor(
                    nyxAssetsRgba32x32[o],
                    nyxAssetsRgba32x32[o + 1],
                    nyxAssetsRgba32x32[o + 2],
                    nyxAssetsRgba32x32[o + 3]);
                var d = dest.GetPixel(dx, dy);
                SrcOver(ref d, src);
                dest.SetPixel(dx, dy, d);
            }
        }
    }

    private static void SrcOver(ref SKColor dst, SKColor src)
    {
        if (src.Alpha == 0)
            return;
        if (dst.Alpha == 0)
        {
            dst = src;
            return;
        }

        var sa = src.Alpha / 255.0;
        var da = dst.Alpha / 255.0;
        var outA = sa + da * (1.0 - sa);
        if (outA <= 1e-6)
        {
            dst = SKColor.Empty;
            return;
        }

        var r = (byte)Math.Clamp((int)((src.Red * sa + dst.Red * da * (1.0 - sa)) / outA), 0, 255);
        var g = (byte)Math.Clamp((int)((src.Green * sa + dst.Green * da * (1.0 - sa)) / outA), 0, 255);
        var b = (byte)Math.Clamp((int)((src.Blue * sa + dst.Blue * da * (1.0 - sa)) / outA), 0, 255);
        var a = (byte)Math.Clamp((int)(outA * 255.0), 0, 255);
        dst = new SKColor(r, g, b, a);
    }

    public static void SavePng(SKBitmap image, Stream destination)
    {
        using var img = SKImage.FromBitmap(image);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        data?.SaveTo(destination);
    }

    public static void SaveJpeg(SKBitmap image, Stream destination, int quality = DefaultJpegQuality)
    {
        using var img = SKImage.FromBitmap(image);
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, quality);
        data?.SaveTo(destination);
    }

    public static void SaveBmp(SKBitmap image, Stream destination)
    {
        using var img = SKImage.FromBitmap(image);
        using var data = img.Encode(SKEncodedImageFormat.Bmp, 100);
        data?.SaveTo(destination);
    }

    public static void WritePng(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination)
    {
        using var image = ToImage(nyxAssetsRgba32x32);
        SavePng(image, destination);
    }

    /// <summary>JPEG has no alpha channel; transparent pixels are encoded like other RGBA→JPEG pipelines (expect premultiplication / blending behaviour from ImageSharp).</summary>
    public static void WriteJpeg(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination, int quality = DefaultJpegQuality)
    {
        using var image = ToImage(nyxAssetsRgba32x32);
        SaveJpeg(image, destination, quality);
    }

    public static void WriteBmp(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination)
    {
        using var image = ToImage(nyxAssetsRgba32x32);
        SaveBmp(image, destination);
    }

    public static void WritePng(ReadOnlySpan<byte> nyxAssetsRgba32x32, string filePath)
    {
        using var fs = File.Create(filePath);
        WritePng(nyxAssetsRgba32x32, fs);
    }

    public static void WriteJpeg(ReadOnlySpan<byte> nyxAssetsRgba32x32, string filePath, int quality = DefaultJpegQuality)
    {
        using var fs = File.Create(filePath);
        WriteJpeg(nyxAssetsRgba32x32, fs, quality);
    }

    public static void WriteBmp(ReadOnlySpan<byte> nyxAssetsRgba32x32, string filePath)
    {
        using var fs = File.Create(filePath);
        WriteBmp(nyxAssetsRgba32x32, fs);
    }

    /// <summary>Decodes one sprite and writes PNG; returns <see langword="false"/> if the id is missing or invalid.</summary>
    public static bool TryDecodeAndWritePng(ISpriteSource archive, uint spriteId, string filePath) =>
        TryDecodeAndWrite(archive, spriteId, buf => WritePng(buf, filePath));

    public static bool TryDecodeAndWriteJpeg(ISpriteSource archive, uint spriteId, string filePath, int quality = DefaultJpegQuality) =>
        TryDecodeAndWrite(archive, spriteId, buf => WriteJpeg(buf, filePath, quality));

    public static bool TryDecodeAndWriteBmp(ISpriteSource archive, uint spriteId, string filePath) =>
        TryDecodeAndWrite(archive, spriteId, buf => WriteBmp(buf, filePath));

    private delegate void WriteDecoded(ReadOnlySpan<byte> buf);

    private static bool TryDecodeAndWrite(ISpriteSource archive, uint spriteId, WriteDecoded write)
    {
        Span<byte> buf = stackalloc byte[SpritePixelCodec.RgbaBufferLength];
        if (!archive.TryDecodeSpriteById(spriteId, buf))
            return false;
        write(buf);
        return true;
    }
}
