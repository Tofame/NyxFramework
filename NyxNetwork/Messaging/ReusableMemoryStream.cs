using System;
using System.IO;

namespace NyxNetwork.Messaging;

public class ReusableMemoryStream : Stream
{
	private byte[] _buffer = Array.Empty<byte>();
	private int _position;
	private int _length;

	public void SetBuffer(byte[] buffer, int offset, int length)
	{
		_buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
		_position = offset;
		_length = length;
	}

	public override bool CanRead => true;
	public override bool CanSeek => true;
	public override bool CanWrite => false;
	public override long Length => _length;
	public override long Position
	{
		get => _position;
		set => _position = (int)value;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		int remaining = _length - _position;
		if (remaining <= 0) return 0;
		int toCopy = Math.Min(count, remaining);
		Buffer.BlockCopy(_buffer, _position, buffer, offset, toCopy);
		_position += toCopy;
		return toCopy;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		_position = origin switch
		{
			SeekOrigin.Begin => (int)offset,
			SeekOrigin.Current => _position + (int)offset,
			SeekOrigin.End => _length + (int)offset,
			_ => _position
		};
		return _position;
	}

	public override void Flush() { }
	public override void SetLength(long value) => throw new NotSupportedException();
	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
