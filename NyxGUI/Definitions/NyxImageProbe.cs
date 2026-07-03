namespace NyxGui.Definitions;

/// <summary>Reads image dimensions from disk without loading a full texture (PNG IHDR only).</summary>
internal static class NyxImageProbe
{
    public static bool TryGetPngDimensions(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[24];
            if (stream.Read(header) < 24)
                return false;

            if (header[0] != 0x89 || header[1] != (byte)'P' || header[2] != (byte)'N' || header[3] != (byte)'G')
                return false;

            width = ReadBigEndianInt32(header[16], header[17], header[18], header[19]);
            height = ReadBigEndianInt32(header[20], header[21], header[22], header[23]);
            return width > 0 && height > 0;
        }
        catch
        {
            return false;
        }
    }

    private static int ReadBigEndianInt32(byte b0, byte b1, byte b2, byte b3) =>
        (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
}
