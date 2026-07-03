using System;
using System.IO;

namespace NyxNetwork.Messaging;

public static class PacketWriter
{
	public static byte[] WritePacket(IPacket packet)
	{
		using var ms = new MemoryStream();
		using var writer = new BinaryWriter(ms);
		writer.Write(packet.PacketId);
		packet.Serialize(writer);
		writer.Flush();
		
		byte[] rawPayload = ms.ToArray();
		byte[] processedPayload = PacketProcessor.Process(rawPayload);
		
		byte[] finalPacket = new byte[4 + processedPayload.Length];
		Buffer.BlockCopy(BitConverter.GetBytes(processedPayload.Length), 0, finalPacket, 0, 4);
		Buffer.BlockCopy(processedPayload, 0, finalPacket, 4, processedPayload.Length);
		
		return finalPacket;
	}
}
