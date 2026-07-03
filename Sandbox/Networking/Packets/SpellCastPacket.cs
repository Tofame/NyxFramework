using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class SpellCastPacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.SpellCast;

	public int SpellId { get; set; }
	public string CasterId { get; set; } = string.Empty;
	public int TargetX { get; set; }
	public int TargetY { get; set; }
	public int TargetZ { get; set; }

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(SpellId);
		writer.Write(CasterId);
		writer.Write(TargetX);
		writer.Write(TargetY);
		writer.Write(TargetZ);
	}

	public void Deserialize(BinaryReader reader)
	{
		SpellId = reader.ReadInt32();
		CasterId = reader.ReadString();
		TargetX = reader.ReadInt32();
		TargetY = reader.ReadInt32();
		TargetZ = reader.ReadInt32();
	}
}
