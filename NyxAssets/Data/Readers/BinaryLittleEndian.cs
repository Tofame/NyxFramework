namespace NyxAssets.Data.Readers;

/// <summary>Shared little-endian primitive reads for span-backed binary formats.</summary>
internal static class BinaryLittleEndian
{
    public static uint ReadUInt32(ReadOnlySpan<byte> buffer, int offset) =>
        (uint)(buffer[offset]
            | (buffer[offset + 1] << 8)
            | (buffer[offset + 2] << 16)
            | (buffer[offset + 3] << 24));

    public static ushort ReadUInt16(ReadOnlySpan<byte> buffer, int offset) =>
        (ushort)(buffer[offset] | (buffer[offset + 1] << 8));

    public static ulong ReadUInt64(ReadOnlySpan<byte> buffer, int offset) =>
        (ulong)buffer[offset]
        | ((ulong)buffer[offset + 1] << 8)
        | ((ulong)buffer[offset + 2] << 16)
        | ((ulong)buffer[offset + 3] << 24)
        | ((ulong)buffer[offset + 4] << 32)
        | ((ulong)buffer[offset + 5] << 40)
        | ((ulong)buffer[offset + 6] << 48)
        | ((ulong)buffer[offset + 7] << 56);
}
