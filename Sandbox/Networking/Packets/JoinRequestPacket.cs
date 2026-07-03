using System.IO;
using NyxNetwork.Messaging;

namespace Sandbox.Networking.Packets;

internal sealed class JoinRequestPacket : IPacket
{
	public ushort PacketId => (ushort)SandboxPacketId.JoinRequest;
	
	public string PlayerName { get; set; } = string.Empty;
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
		writer.Write(PlayerName);
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
		PlayerName = reader.ReadString();
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
