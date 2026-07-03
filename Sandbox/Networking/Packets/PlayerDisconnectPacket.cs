using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class PlayerDisconnectPacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.PlayerDisconnect;

	public string ClientId { get; set; } = string.Empty;

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(ClientId);
	}

	public void Deserialize(BinaryReader reader)
	{
		ClientId = reader.ReadString();
	}
}
