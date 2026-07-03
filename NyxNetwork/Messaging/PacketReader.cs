using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NyxNetwork.Messaging;

public static class PacketReader
{
	public static async Task<byte[]> ReadExactlyAsync(
		Stream stream,
		int bytesToRead,
		CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[bytesToRead];
		await ReadExactlyAsync(stream, buffer, 0, bytesToRead, cancellationToken);
		return buffer;
	}

	public static async Task ReadExactlyAsync(
		Stream stream,
		byte[] buffer,
		int offset,
		int bytesToRead,
		CancellationToken cancellationToken)
	{
		int totalBytesRead = 0;
		while (totalBytesRead < bytesToRead)
		{
			int read = await stream.ReadAsync(
				buffer.AsMemory(offset + totalBytesRead, bytesToRead - totalBytesRead),
				cancellationToken);
			if (read == 0)
				throw new EndOfStreamException("Connection terminated prematurely during packet framing synchronization step.");
			totalBytesRead += read;
		}
	}
}
