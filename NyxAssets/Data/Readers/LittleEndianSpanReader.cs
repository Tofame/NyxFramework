using System.Buffers.Binary;

namespace NyxAssets.Data.Readers;

/// <summary>Sequential little-endian reader over a <see cref="ReadOnlySpan{T}"/> (used for .dat/.spr parsing).</summary>
internal ref struct LittleEndianSpanReader
{
    private ReadOnlySpan<byte> _span;
    private int _pos;

    public LittleEndianSpanReader(ReadOnlySpan<byte> span, int start = 0)
    {
        _span = span;
        _pos = start;
    }

    public readonly int Position => _pos;
    public readonly int Remaining => _span.Length - _pos;

    public void Skip(int count)
    {
        if (Remaining < count)
            throw new EndOfStreamException();
        _pos += count;
    }

    public byte ReadU8()
    {
        if (Remaining < 1)
            throw new EndOfStreamException();
        return _span[_pos++];
    }

    public sbyte ReadS8()
    {
        if (Remaining < 1)
            throw new EndOfStreamException();
        return unchecked((sbyte)_span[_pos++]);
    }

    public ushort ReadU16()
    {
        if (Remaining < 2)
            throw new EndOfStreamException();
        var v = BinaryPrimitives.ReadUInt16LittleEndian(_span.Slice(_pos, 2));
        _pos += 2;
        return v;
    }

    public short ReadI16()
    {
        if (Remaining < 2)
            throw new EndOfStreamException();
        var v = BinaryPrimitives.ReadInt16LittleEndian(_span.Slice(_pos, 2));
        _pos += 2;
        return v;
    }

    public uint ReadU32()
    {
        if (Remaining < 4)
            throw new EndOfStreamException();
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_span.Slice(_pos, 4));
        _pos += 4;
        return v;
    }

    public int ReadI32()
    {
        if (Remaining < 4)
            throw new EndOfStreamException();
        var v = BinaryPrimitives.ReadInt32LittleEndian(_span.Slice(_pos, 4));
        _pos += 4;
        return v;
    }

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (Remaining < length)
            throw new EndOfStreamException();
        var slice = _span.Slice(_pos, length);
        _pos += length;
        return slice;
    }
}
