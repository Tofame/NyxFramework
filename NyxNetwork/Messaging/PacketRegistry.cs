using System;
using System.Collections.Generic;
using System.IO;
using NyxNetwork.Core;
using System.Threading;

namespace NyxNetwork.Messaging;

public class PacketRegistry
{
	private readonly Dictionary<ushort, Action<INyxConnection, BinaryReader>> _handlers = new();
	private readonly Dictionary<ushort, Func<IPacket>> _packetFactories = new();

	private static readonly ThreadLocal<(ReusableMemoryStream Stream, BinaryReader Reader)> _readCache = new(() =>
	{
		var ms = new ReusableMemoryStream();
		var reader = new BinaryReader(ms);
		return (ms, reader);
	});

	public void RegisterPacket<TPacket>(
		ushort packetId,
		Func<TPacket> factory,
		Action<INyxConnection, TPacket> handler)
		where TPacket : IPacket
	{
		_packetFactories[packetId] = () => factory();
		_handlers[packetId] = (connection, reader) =>
		{
			TPacket packet = factory();
			packet.Deserialize(reader);
			handler(connection, packet);
		};
	}

	public void ParseAndDispatch(INyxConnection connection, byte[] rawData)
	{
		byte[] revertedData = PacketProcessor.Revert(rawData);
		var (rms, reader) = _readCache.Value;
		rms.SetBuffer(revertedData, 0, revertedData.Length);
		if (revertedData.Length < 2)
			return;

		ushort packetId = reader.ReadUInt16();
		if (_handlers.TryGetValue(packetId, out var handler))
		{
			handler(connection, reader);
		}
	}
}
