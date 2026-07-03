using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class PlayerMovePacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.PlayerMove;

	public string ClientId { get; set; } = string.Empty;
	public string PlayerName { get; set; } = string.Empty;
	public int X { get; set; }
	public int Y { get; set; }
	public int Z { get; set; }
	public int Direction { get; set; }
	public ushort LookType { get; set; }
	public byte LookHead { get; set; }
	public byte LookBody { get; set; }
	public byte LookLegs { get; set; }
	public byte LookFeet { get; set; }
	public byte LookAddons { get; set; }
	public ushort LookMount { get; set; }
	public bool IsMounted { get; set; }

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(ClientId);
		writer.Write(PlayerName);
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
		writer.Write(Direction);
		writer.Write(LookType);
		writer.Write(LookHead);
		writer.Write(LookBody);
		writer.Write(LookLegs);
		writer.Write(LookFeet);
		writer.Write(LookAddons);
		writer.Write(LookMount);
		writer.Write(IsMounted);
	}

	public void Deserialize(BinaryReader reader)
	{
		ClientId = reader.ReadString();
		PlayerName = reader.ReadString();
		X = reader.ReadInt32();
		Y = reader.ReadInt32();
		Z = reader.ReadInt32();
		Direction = reader.ReadInt32();
		LookType = reader.ReadUInt16();
		LookHead = reader.ReadByte();
		LookBody = reader.ReadByte();
		LookLegs = reader.ReadByte();
		LookFeet = reader.ReadByte();
		LookAddons = reader.ReadByte();
		LookMount = reader.ReadUInt16();
		IsMounted = reader.ReadBoolean();
	}
}
