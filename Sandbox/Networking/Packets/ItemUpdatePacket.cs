using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class ItemUpdatePacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.ItemUpdate;

	public int X { get; set; }
	public int Y { get; set; }
	public int Z { get; set; }
	public ushort ItemTypeId { get; set; }
	public ushort Count { get; set; }
	public bool IsPlacement { get; set; }

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
		writer.Write(ItemTypeId);
		writer.Write(Count);
		writer.Write(IsPlacement);
	}

	public void Deserialize(BinaryReader reader)
	{
		X = reader.ReadInt32();
		Y = reader.ReadInt32();
		Z = reader.ReadInt32();
		ItemTypeId = reader.ReadUInt16();
		Count = reader.ReadUInt16();
		IsPlacement = reader.ReadBoolean();
	}
}
