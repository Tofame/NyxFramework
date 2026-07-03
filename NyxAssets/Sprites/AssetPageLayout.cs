using NyxAssets.Data.Readers;

namespace NyxAssets.Sprites;

/// <summary>Precomputed byte offsets for sprites stored sequentially inside a decompressed asset page.</summary>
internal static class AssetPageLayout
{
    public static int[] BuildSpriteOffsets(ReadOnlySpan<byte> pagePayload, uint spriteCount)
    {
        var offsets = new int[spriteCount];
        var offset = 0;
        for (var i = 0u; i < spriteCount; i++)
        {
            offsets[i] = offset;
            if (offset + 4 > pagePayload.Length)
                break;

            var width = BinaryLittleEndian.ReadUInt16(pagePayload, offset);
            var height = BinaryLittleEndian.ReadUInt16(pagePayload, offset + 2);
            offset += 4 + width * height * 4;
        }

        return offsets;
    }

    public static bool TryGetSpriteBounds(
        ReadOnlySpan<byte> pagePayload,
        int spriteOffset,
        out int pixelOffset,
        out ushort width,
        out ushort height)
    {
        pixelOffset = spriteOffset;
        width = 0;
        height = 0;

        if (pixelOffset + 4 > pagePayload.Length)
            return false;

        width = BinaryLittleEndian.ReadUInt16(pagePayload, pixelOffset);
        height = BinaryLittleEndian.ReadUInt16(pagePayload, pixelOffset + 2);
        pixelOffset += 4;
        return true;
    }
}
