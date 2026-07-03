using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class JoinResponsePacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.JoinResponse;
	
	public string ClientId { get; set; } = string.Empty;
	public int SpawnX { get; set; }
	public int SpawnY { get; set; }
	public int SpawnZ { get; set; }

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(ClientId);
		writer.Write(SpawnX);
		writer.Write(SpawnY);
		writer.Write(SpawnZ);
	}

	public void Deserialize(BinaryReader reader)
	{
		ClientId = reader.ReadString();
		SpawnX = reader.ReadInt32();
		SpawnY = reader.ReadInt32();
		SpawnZ = reader.ReadInt32();
	}
}
