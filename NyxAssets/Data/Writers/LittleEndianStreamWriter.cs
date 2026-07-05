using System.Buffers.Binary;

namespace NyxAssets.Data.Writers;

internal sealed class LittleEndianStreamWriter(Stream stream)
{
    private readonly byte[] _scratch = new byte[8];

    public long Position => stream.Position;

    public void WriteU8(byte value)
    {
        stream.WriteByte(value);
    }

    public void WriteU16(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_scratch, value);
        stream.Write(_scratch, 0, 2);
    }

    public void WriteI16(short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(_scratch, value);
        stream.Write(_scratch, 0, 2);
    }

    public void WriteU32(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_scratch, value);
        stream.Write(_scratch, 0, 4);
    }

    public void WriteI32(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_scratch, value);
        stream.Write(_scratch, 0, 4);
    }

    public void WriteBytes(ReadOnlySpan<byte> data) =>
        stream.Write(data);

    public void WriteU32At(long position, uint value)
    {
        var saved = stream.Position;
        stream.Position = position;
        WriteU32(value);
        stream.Position = saved;
    }
}
