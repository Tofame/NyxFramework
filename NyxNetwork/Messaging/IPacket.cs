using System.IO;

namespace NyxNetwork.Messaging;

public interface IPacket
{
	ushort PacketId { get; }
	void Serialize(BinaryWriter writer);
	void Deserialize(BinaryReader reader);
}
