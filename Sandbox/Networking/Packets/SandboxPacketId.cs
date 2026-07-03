namespace Sandbox.Networking.Packets;

internal enum SandboxPacketId : ushort
{
	JoinRequest = 10,
	JoinResponse = 11,
	PlayerMove = 20,
	PlayerDisconnect = 21,
	ItemUpdate = 30,
	SpellCast = 40,
	PlayerChat = 50
}
