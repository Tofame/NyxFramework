using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using NyxAssets.Sprites;

namespace NyxAssets.Utils;

/// <summary>
/// Writes decoded 32×32 sprite pixels (see <see cref="SpritePixelCodec.RgbaBufferLength"/>) to raster files via <b>SixLabors.ImageSharp</b>.
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

    private static Image<Rgba32> ToImage(ReadOnlySpan<byte> nyxAssetsRgba32x32)
    {
        ValidateBuffer(nyxAssetsRgba32x32);
        var edge = SpritePixelCodec.SpriteEdgeLength;
        var image = new Image<Rgba32>(edge, edge);
        for (var y = 0; y < edge; y++)
        {
            for (var x = 0; x < edge; x++)
            {
                var o = (y * edge + x) * 4;
                image[x, y] = new Rgba32(
                    nyxAssetsRgba32x32[o],
                    nyxAssetsRgba32x32[o + 1],
                    nyxAssetsRgba32x32[o + 2],
                    nyxAssetsRgba32x32[o + 3]);
            }
        }

        return image;
    }

    /// <summary>32×32 decoded buffer → new image (caller must dispose).</summary>
    public static Image<Rgba32> CreateImageFromSpriteBuffer(ReadOnlySpan<byte> nyxAssetsRgba32x32) =>
        ToImage(nyxAssetsRgba32x32);

    /// <summary>Straight-alpha source-over destination (for compositing sheets).</summary>
    public static void BlitSpriteBufferOnto(Image<Rgba32> dest, int destX, int destY, ReadOnlySpan<byte> nyxAssetsRgba32x32)
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
                var src = new Rgba32(
                    nyxAssetsRgba32x32[o],
                    nyxAssetsRgba32x32[o + 1],
                    nyxAssetsRgba32x32[o + 2],
                    nyxAssetsRgba32x32[o + 3]);
                var d = dest[dx, dy];
                SrcOver(ref d, src);
                dest[dx, dy] = d;
            }
        }
    }

    private static void SrcOver(ref Rgba32 dst, Rgba32 src)
    {
        if (src.A == 0)
            return;
        if (dst.A == 0)
        {
            dst = src;
            return;
        }

        var sa = src.A / 255.0;
        var da = dst.A / 255.0;
        var outA = sa + da * (1.0 - sa);
        if (outA <= 1e-6)
        {
            dst = default;
            return;
        }

        dst.R = (byte)Math.Clamp((int)((src.R * sa + dst.R * da * (1.0 - sa)) / outA), 0, 255);
        dst.G = (byte)Math.Clamp((int)((src.G * sa + dst.G * da * (1.0 - sa)) / outA), 0, 255);
        dst.B = (byte)Math.Clamp((int)((src.B * sa + dst.B * da * (1.0 - sa)) / outA), 0, 255);
        dst.A = (byte)Math.Clamp((int)(outA * 255.0), 0, 255);
    }

    public static void SavePng(Image<Rgba32> image, Stream destination) =>
        image.Save(destination, new PngEncoder());

    public static void SaveJpeg(Image<Rgba32> image, Stream destination, int quality = DefaultJpegQuality) =>
        image.Save(destination, new JpegEncoder { Quality = quality });

    public static void SaveBmp(Image<Rgba32> image, Stream destination) =>
        image.Save(destination, new BmpEncoder());

    public static void WritePng(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination)
    {
        using var image = ToImage(nyxAssetsRgba32x32);
        image.Save(destination, new PngEncoder());
    }

    /// <summary>JPEG has no alpha channel; transparent pixels are encoded like other RGBA→JPEG pipelines (expect premultiplication / blending behaviour from ImageSharp).</summary>
    public static void WriteJpeg(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination, int quality = DefaultJpegQuality)
    {
        using var image = ToImage(nyxAssetsRgba32x32);
        image.Save(destination, new JpegEncoder { Quality = quality });
    }

    public static void WriteBmp(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination)
    {
        using var image = ToImage(nyxAssetsRgba32x32);
        image.Save(destination, new BmpEncoder());
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
