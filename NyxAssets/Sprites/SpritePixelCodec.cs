using System.Buffers.Binary;

namespace NyxAssets.Sprites;

/// <summary>RLE pixel codec for Nyx <c>.spr</c> payloads (Asset Editor <c>Sprite</c>).</summary>
public static class SpritePixelCodec
{
    public const int SpriteEdgeLength = 32;
    public const int RgbaBufferLength = SpriteEdgeLength * SpriteEdgeLength * 4;

    /// <summary>
    /// Expands RLE payload to 32×32 RGBA: per pixel <c>R, G, B, A</c> (OpenGL <c>GL_RGBA</c> order).
    /// </summary>
    public static void UncompressToRgba(ReadOnlySpan<byte> compressed, bool transparent, Span<byte> destinationRgba)
    {
        if (destinationRgba.Length < RgbaBufferLength)
            throw new ArgumentException("Destination must be at least 4096 bytes.", nameof(destinationRgba));

        var read = 0;
        var write = 0;
        var length = compressed.Length;
        var channels = transparent ? 4 : 3;
        const int totalPixels = SpriteEdgeLength * SpriteEdgeLength;
        var pixelCount = 0;

        while (read < length && pixelCount < totalPixels)
        {
            if (read + 4 > length)
                break;

            var transparentPixels = BinaryPrimitives.ReadUInt16LittleEndian(compressed.Slice(read));
            read += 2;
            var coloredPixels = BinaryPrimitives.ReadUInt16LittleEndian(compressed.Slice(read));
            read += 2;

            var remaining = totalPixels - pixelCount;
            if (transparentPixels > remaining)
                transparentPixels = (ushort)remaining;

            destinationRgba.Slice(write, transparentPixels * 4).Clear();
            write += transparentPixels * 4;
            pixelCount += transparentPixels;

            if (pixelCount >= totalPixels)
                break;

            remaining = totalPixels - pixelCount;
            if (coloredPixels > remaining)
                coloredPixels = (ushort)remaining;

            var coloredBytes = (int)coloredPixels * channels;
            if (read + coloredBytes > length)
                break;

            for (var i = 0; i < coloredPixels; i++)
            {
                var red = compressed[read++];
                var green = compressed[read++];
                var blue = compressed[read++];
                var alpha = transparent ? compressed[read++] : (byte)0xFF;
                destinationRgba[write++] = red;
                destinationRgba[write++] = green;
                destinationRgba[write++] = blue;
                destinationRgba[write++] = alpha;
            }

            pixelCount += coloredPixels;
        }

        if (write < RgbaBufferLength)
            destinationRgba.Slice(write).Clear();
    }

    /// <summary>True when all four channels of the pixel are zero (fully transparent).</summary>
    public static bool IsRgbaPixelZero(ReadOnlySpan<byte> rgba, int pixelIndex)
    {
        var o = pixelIndex * 4;
        return BinaryPrimitives.ReadUInt32LittleEndian(rgba.Slice(o, 4)) == 0;
    }

    /// <summary>RLE-compresses 32×32 RGBA into the Nyx / Asset Editor sprite payload format.</summary>
    public static byte[] CompressRgba(ReadOnlySpan<byte> rgba, bool transparent)
    {
        if (rgba.Length < RgbaBufferLength)
            throw new ArgumentException("Expected 4096 bytes of RGBA.", nameof(rgba));

        using var ms = new MemoryStream(256);
        var length = SpriteEdgeLength * SpriteEdgeLength;
        uint index = 0;

        while (index < length)
        {
            var transparentRun = 0u;
            while (index < length)
            {
                if (!IsRgbaPixelZero(rgba, (int)index))
                    break;
                transparentRun++;
                index++;
            }

            if (index >= length)
                break;

            WriteUInt16LE(ms, (ushort)transparentRun);
            var coloredCountPosition = ms.Position;
            WriteUInt16LE(ms, 0);
            var coloredRun = 0u;

            while (index < length)
            {
                if (IsRgbaPixelZero(rgba, (int)index))
                    break;

                var o = (int)index * 4;
                ms.WriteByte(rgba[o]);
                ms.WriteByte(rgba[o + 1]);
                ms.WriteByte(rgba[o + 2]);
                if (transparent)
                    ms.WriteByte(rgba[o + 3]);

                coloredRun++;
                index++;
            }

            var endPosition = ms.Position;
            ms.Position = coloredCountPosition;
            WriteUInt16LE(ms, (ushort)coloredRun);
            ms.Position = endPosition;
        }

        return ms.ToArray();
    }

    private static void WriteUInt16LE(Stream s, ushort v)
    {
        s.WriteByte((byte)v);
        s.WriteByte((byte)(v >> 8));
    }
}
