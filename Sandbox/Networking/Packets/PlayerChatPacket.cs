using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class PlayerChatPacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.PlayerChat;

	public string SenderId { get; set; } = string.Empty;
	public string SenderName { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public byte ChatType { get; set; } // 0 = Speak, 1 = Yell, 2 = System

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(SenderId);
		writer.Write(SenderName);
		writer.Write(Message);
		writer.Write(ChatType);
	}

	public void Deserialize(BinaryReader reader)
	{
		SenderId = reader.ReadString();
		SenderName = reader.ReadString();
		Message = reader.ReadString();
		ChatType = reader.ReadByte();
	}
}
