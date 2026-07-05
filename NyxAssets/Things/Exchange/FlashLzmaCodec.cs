using System.Buffers.Binary;
using System.IO.Compression;
using Lzma;

namespace NyxAssets.Things.Exchange;

/// <summary>LZMA payloads produced by Adobe Flash / Object Builder <c>ByteArray.compress(LZMA)</c>.</summary>
internal static class FlashLzmaCodec
{
    private const int HeaderLength = 13;

    public static byte[] Decompress(ReadOnlySpan<byte> input)
    {
        if (input.Length < HeaderLength)
            throw new InvalidDataException("LZMA payload is too short.");

        using var inStream = new MemoryStream(input.ToArray());
        using var lzmaStream = new LzmaStream(inStream, CompressionMode.Decompress, leaveOpen: false);
        using var outStream = new MemoryStream();
        lzmaStream.CopyTo(outStream);
        return outStream.ToArray();
    }

    public static byte[] Compress(ReadOnlySpan<byte> input)
    {
        using var outStream = new MemoryStream();
        using (var inStream = new MemoryStream(input.ToArray()))
        using (var lzmaStream = new LzmaStream(outStream, CompressionMode.Compress, leaveOpen: true))
            inStream.CopyTo(lzmaStream);

        var compressed = outStream.ToArray();
        if (compressed.Length < HeaderLength)
            throw new InvalidDataException("LZMA encoder returned an invalid payload.");

        // Ecng.Lzma writes props + 8-byte LE size; Flash expects the same layout.
        BinaryPrimitives.WriteInt64LittleEndian(compressed.AsSpan(5, 8), input.Length);
        return compressed;
    }
}
