namespace NyxAssets.Sprites;

/// <summary>Builds a Nyx <c>.spr</c> file (Asset Editor <c>SpriteStorage.compile</c> layout).</summary>
public static class SpriteSheetCompiler
{
    /// <summary>
    /// <paramref name="rgbaPerSpriteIdOneBased"/> index <c>0</c> is ignored; indices <c>1</c> … <c>Count - 1</c> are sprite ids.
    /// <c>null</c> or all-transparent RGBA produces an empty slot (address <c>0</c>).
    /// </summary>
    public static void WriteToStream(
        Stream output,
        uint sprSignature,
        bool extendedSpriteIds,
        bool transparentPixels,
        IReadOnlyList<byte[]?> rgbaPerSpriteIdOneBased)
    {
        if (rgbaPerSpriteIdOneBased.Count < 2)
            throw new ArgumentException("Expected at least two entries (index 0 unused + one sprite).", nameof(rgbaPerSpriteIdOneBased));

        var spriteCount = (uint)(rgbaPerSpriteIdOneBased.Count - 1);
        var countToWrite = spriteCount;
        if (!extendedSpriteIds && spriteCount >= 0xFFFF)
            countToWrite = 0xFFFE;

        var headSize = extendedSpriteIds ? 8 : 6;
        using var ms = new MemoryStream();
        using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            bw.Write(sprSignature);
            if (extendedSpriteIds)
                bw.Write(countToWrite);
            else
                bw.Write((ushort)countToWrite);

            var addressTableStart = headSize;
            var addressBytes = countToWrite * 4u;
            var dataOffset = headSize + addressBytes;

            bw.Flush();
            ms.Seek(addressTableStart, SeekOrigin.Begin);
            bw.Write(new byte[countToWrite * 4]);

            var compressedSprites = new byte[countToWrite + 1][];
            System.Threading.Tasks.Parallel.For(1, (int)countToWrite + 1, id =>
            {
                var rgba = rgbaPerSpriteIdOneBased[id];
                if (rgba is null || IsAllTransparent(rgba))
                    return;

                if (rgba.Length < SpritePixelCodec.RgbaBufferLength)
                    throw new ArgumentException($"Sprite {id}: expected {SpritePixelCodec.RgbaBufferLength} RGBA bytes.");

                compressedSprites[id] = SpritePixelCodec.CompressRgba(rgba, transparentPixels);
            });

            var currentOffset = (uint)dataOffset;

            for (var id = 1u; id <= countToWrite; id++)
            {
                var compressed = compressedSprites[(int)id];
                var addrSlot = addressTableStart + (int)((id - 1) * 4);
                bw.Flush();
                ms.Seek(addrSlot, SeekOrigin.Begin);

                if (compressed is null)
                {
                    bw.Write(0u);
                    continue;
                }

                bw.Write(currentOffset);
                bw.Flush();
                ms.Seek((int)currentOffset, SeekOrigin.Begin);
                bw.Write((byte)0xFF);
                bw.Write((byte)0x00);
                bw.Write((byte)0xFF);
                bw.Write((ushort)compressed.Length);
                if (compressed.Length > 0)
                    bw.Write(compressed);

                bw.Flush();
                currentOffset = (uint)ms.Length;
            }
        }

        ms.Position = 0;
        ms.CopyTo(output);
    }

    private static bool IsAllTransparent(byte[] rgba)
    {
        if (rgba.Length < SpritePixelCodec.RgbaBufferLength)
            return true;
        for (var p = 0; p < SpritePixelCodec.SpriteEdgeLength * SpritePixelCodec.SpriteEdgeLength; p++)
        {
            if (!SpritePixelCodec.IsRgbaPixelZero(rgba, p))
                return false;
        }

        return true;
    }
}
