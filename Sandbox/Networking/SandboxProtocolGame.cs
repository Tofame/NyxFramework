using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NyxNetwork.Core;
using NyxNetwork.Messaging;
using Sandbox.Networking.Packets;
using NyxDrawer.Appearance;
using NyxAssets.Things;
using Sandbox.Spells;

namespace Sandbox.Networking;

internal sealed class SandboxProtocolGame : IDisposable
{
	private readonly SandboxGameWorld _world;
	private NetworkManager? _networkManager;
	private string _clientId = string.Empty;
	private bool _isClient = false;
	private bool _isHost = false;
	private int _hostPort;

	public bool IsClient => _isClient;
	public bool IsHost => _isHost;
	public bool IsActive => _isClient || _isHost;
	public int HostPort => _hostPort;
	public string ClientId => _clientId;

	public event Action<PlayerChatPacket>? OnMessageReceived;

	private readonly ConcurrentQueue<Action> _networkQueue = new();
	private readonly List<INyxConnection> _connections = new();

	public SandboxProtocolGame(SandboxGameWorld world)
	{
		_world = world;
	}

	public void Update(double deltaTime)
	{
		while (_networkQueue.TryDequeue(out var action))
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Network Action Error] {ex.Message}");
			}
		}
	}

	public void InitializeClient(NetworkManager netManager, string clientId)
	{
		_networkManager = netManager;
		_clientId = clientId;
		_isClient = true;
		_isHost = false;

		RegisterCommonPackets();
	}

	public bool StartHosting(int port, string serverName)
	{
		try
		{
			_networkManager = new NetworkManager();
			_networkManager.OnClientConnected += OnServerClientConnected;
			_networkManager.OnClientDisconnected += OnServerClientDisconnected;

			_networkManager.StartServer(TransportType.Tcp, port, serverName);

			_hostPort = port;
			_isHost = true;
			_isClient = false;
			_clientId = "HOST";

			RegisterCommonPackets();
			Console.WriteLine($"[LAN Host] Server started on port {port}.");
			return true;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[LAN Host] Failed to start server: {ex.Message}");
			StopHosting();
			return false;
		}
	}

	public void StopHosting()
	{
		if (!_isHost || _networkManager == null) return;

		_networkManager.StopServerAsync().Wait();
		_networkManager.Dispose();
		_networkManager = null;

		_isHost = false;
		_hostPort = 0;

		lock (_connections)
		{
			_connections.Clear();
		}

		_world.RemotePlayers.Clear();
		Console.WriteLine("[LAN Host] Server stopped.");
	}

	public void DisconnectClient()
	{
		if (!_isClient || _networkManager == null) return;

		_networkManager.DisconnectClientAsync().Wait();
		_networkManager.Dispose();
		_networkManager = null;

		_isClient = false;
		_clientId = string.Empty;

		_world.RemotePlayers.Clear();
	}

	private void RegisterCommonPackets()
	{
		if (_networkManager == null) return;

		var registry = _networkManager.PacketRegistry;

		registry.RegisterPacket((ushort)SandboxPacketId.JoinRequest, () => new JoinRequestPacket(), (conn, packet) =>
		{
			_networkQueue.Enqueue(() => HandleServerJoinRequest(conn, packet));
		});

		registry.RegisterPacket((ushort)SandboxPacketId.PlayerMove, () => new PlayerMovePacket(), (conn, packet) =>
		{
			_networkQueue.Enqueue(() => HandlePlayerMove(conn, packet));
		});

		registry.RegisterPacket((ushort)SandboxPacketId.PlayerDisconnect, () => new PlayerDisconnectPacket(), (conn, packet) =>
		{
			_networkQueue.Enqueue(() => HandlePlayerDisconnect(conn, packet));
		});

		registry.RegisterPacket((ushort)SandboxPacketId.ItemUpdate, () => new ItemUpdatePacket(), (conn, packet) =>
		{
			_networkQueue.Enqueue(() => HandleItemUpdate(conn, packet));
		});

		registry.RegisterPacket((ushort)SandboxPacketId.SpellCast, () => new SpellCastPacket(), (conn, packet) =>
		{
			_networkQueue.Enqueue(() => HandleSpellCast(conn, packet));
		});

		registry.RegisterPacket((ushort)SandboxPacketId.PlayerChat, () => new PlayerChatPacket(), (conn, packet) =>
		{
			_networkQueue.Enqueue(() => HandlePlayerChat(conn, packet));
		});

		_networkManager.OnDataReceived += (conn, data) =>
		{
			registry.ParseAndDispatch(conn, data);
		};
	}

	private void OnServerClientConnected(INyxConnection conn)
	{
		lock (_connections)
		{
			_connections.Add(conn);
		}
		Console.WriteLine($"[LAN Host] Client connected: {conn.Id}");
	}

	private void OnServerClientDisconnected(INyxConnection conn)
	{
		_networkQueue.Enqueue(() =>
		{
			lock (_connections)
			{
				_connections.Remove(conn);
			}

			string cid = conn.Id.ToString();
			var world = _world;
			{
				var rp = world.RemotePlayers.Find(p => p.ClientId == cid);
				if (rp is not null)
				{
					if (world.Map is not null && world.Map.IsInside(rp.Position))
					{
						world.Map.GetTile(rp.Position).RemoveCreature(rp);
					}
					world.RemotePlayers.Remove(rp);
				}
			}

			var disc = new PlayerDisconnectPacket { ClientId = cid };
			Broadcast(PacketWriter.WritePacket(disc));
			Console.WriteLine($"[LAN Host] Client disconnected: {conn.Id}");
		});
	}

	private void HandleServerJoinRequest(INyxConnection conn, JoinRequestPacket packet)
	{
		if (!_isHost) return;

		var world = _world;

		string newClientId = conn.Id.ToString();
		int spawnX = world.Player?.Position.X ?? 450;
		int spawnY = world.Player?.Position.Y ?? 450;
		int spawnZ = world.Player?.Position.Z ?? 7;

		var response = new JoinResponsePacket
		{
			ClientId = newClientId,
			SpawnX = spawnX,
			SpawnY = spawnY,
			SpawnZ = spawnZ
		};
		conn.SendAsync(PacketWriter.WritePacket(response));

		var appearance = new CreatureOutfitAppearance(
			packet.LookType,
			packet.LookHead,
			packet.LookBody,
			packet.LookLegs,
			packet.LookFeet,
			packet.LookAddons,
			packet.LookMount
		);

		var outfitThing = world.ClientAssets?.Things.TryGetOutfit(packet.LookType);
		ThingType? mountThing = null;
		if (packet.IsMounted)
			mountThing = world.ClientAssets?.Things.TryGetOutfit(packet.LookMount);

		if (outfitThing is not null)
		{
			var remotePlayer = new RemotePlayer(newClientId, packet.PlayerName, new Position(spawnX, spawnY, spawnZ), appearance, outfitThing, mountThing);
			world.RemotePlayers.Add(remotePlayer);
			if (world.Map is not null && world.Map.IsInside(remotePlayer.Position))
			{
				world.Map.GetTile(remotePlayer.Position).AddCreature(remotePlayer);
			}
		}

		// Broadcast spawn to all spectators at spawn point
		BroadcastToSpectators(PacketWriter.WritePacket(BuildMovePacket(
			newClientId, packet.PlayerName,
			spawnX, spawnY, spawnZ, 2,
			packet.LookType, packet.LookHead, packet.LookBody, packet.LookLegs,
			packet.LookFeet, packet.LookAddons, packet.LookMount, packet.IsMounted)),
			new Position(spawnX, spawnY, spawnZ));

		if (world.Player is not null)
		{
			var p = world.Player;
			conn.SendAsync(PacketWriter.WritePacket(BuildMovePacket(
				"HOST", p.Name,
				p.Position.X, p.Position.Y, p.Position.Z, p.Direction,
				(ushort)p.OutfitId, p.Appearance.LookHead, p.Appearance.LookBody,
				p.Appearance.LookLegs, p.Appearance.LookFeet, p.Appearance.LookAddons,
				(ushort)p.Appearance.LookMount, p.IsMounted)));
		}

		foreach (var other in world.RemotePlayers)
		{
			if (other.ClientId == newClientId) continue;
			conn.SendAsync(PacketWriter.WritePacket(BuildMovePacket(
				other.ClientId, other.Name,
				other.Position.X, other.Position.Y, other.Position.Z, other.Direction,
				(ushort)other.OutfitId, other.Appearance.LookHead, other.Appearance.LookBody,
				other.Appearance.LookLegs, other.Appearance.LookFeet, other.Appearance.LookAddons,
				(ushort)other.Appearance.LookMount, other.IsMounted)));
		}
	}

	private static PlayerMovePacket BuildMovePacket(
		string clientId, string playerName,
		int x, int y, int z, int direction,
		ushort lookType, byte lookHead, byte lookBody, byte lookLegs,
		byte lookFeet, byte lookAddons, ushort lookMount, bool isMounted) =>
		new PlayerMovePacket
		{
			ClientId = clientId,
			PlayerName = playerName,
			X = x,
			Y = y,
			Z = z,
			Direction = direction,
			LookType = lookType,
			LookHead = lookHead,
			LookBody = lookBody,
			LookLegs = lookLegs,
			LookFeet = lookFeet,
			LookAddons = lookAddons,
			LookMount = lookMount,
			IsMounted = isMounted
		};


	private void HandlePlayerMove(INyxConnection conn, PlayerMovePacket packet)
	{
		if (packet.ClientId == _clientId) return;

		var world = _world;

		var rp = world.RemotePlayers.Find(p => p.ClientId == packet.ClientId);
		if (rp is null)
		{
			var appearance = new CreatureOutfitAppearance(
				packet.LookType,
				packet.LookHead,
				packet.LookBody,
				packet.LookLegs,
				packet.LookFeet,
				packet.LookAddons,
				packet.LookMount
			);
			var outfitThing = world.ClientAssets?.Things.TryGetOutfit(packet.LookType);
			ThingType? mountThing = null;
			if (packet.IsMounted)
				mountThing = world.ClientAssets?.Things.TryGetOutfit(packet.LookMount);

			if (outfitThing is not null)
			{
				rp = new RemotePlayer(packet.ClientId, packet.PlayerName, new Position(packet.X, packet.Y, packet.Z), appearance, outfitThing, mountThing);
				world.RemotePlayers.Add(rp);
				if (world.Map is not null && world.Map.IsInside(rp.Position))
				{
					world.Map.GetTile(rp.Position).AddCreature(rp);
				}
			}
		}
		else
		{
			rp.Name = packet.PlayerName;
			rp.MoveTo(packet.X, packet.Y, packet.Direction);
			rp.IsMounted = packet.IsMounted;
		}

		if (_isHost)
		{
			// Broadcast movement to spectators around the movement target position
			BroadcastToSpectators(PacketWriter.WritePacket(packet), new Position(packet.X, packet.Y, packet.Z));
		}
	}

	private void HandlePlayerDisconnect(INyxConnection conn, PlayerDisconnectPacket packet)
	{
		var world = _world;

		var rp = world.RemotePlayers.Find(p => p.ClientId == packet.ClientId);
		if (rp is not null)
		{
			if (world.Map is not null && world.Map.IsInside(rp.Position))
			{
				world.Map.GetTile(rp.Position).RemoveCreature(rp);
			}
			world.RemotePlayers.Remove(rp);
		}
	}

	private void HandleItemUpdate(INyxConnection conn, ItemUpdatePacket packet)
	{
		var world = _world;
		if (world.Map is null) return;

		var pos = new Position(packet.X, packet.Y, packet.Z);
		if (packet.IsPlacement)
		{
			world.Map.AddItem(pos, Sandbox.Items.Item.Of(packet.ItemTypeId, packet.Count), world.ClientAssets?.Things);
		}
		else
		{
			if (world.Map.IsInside(pos))
			{
				world.Map.TryTakeTopItem(pos, out _, world.ClientAssets?.Things);
			}
		}

		if (_isHost)
		{
			// Broadcast item update to spectators around the item position
			BroadcastToSpectators(PacketWriter.WritePacket(packet), pos);
		}
	}

	private void HandleSpellCast(INyxConnection conn, SpellCastPacket packet)
	{
		var world = _world;
		if (world.SpellCatalog is null || world.Map is null) return;

		Position casterPos = world.Player?.Position ?? new Position(0, 0, 7);
		int casterDirection = world.Player?.Direction ?? 2;
		if (packet.CasterId != _clientId)
		{
			var rp = world.RemotePlayers.Find(p => p.ClientId == packet.CasterId);
			if (rp is not null)
			{
				casterPos = rp.Position;
				casterDirection = rp.Direction;
			}
		}

		var spell = world.SpellCatalog.Spells[packet.SpellId];
		if (spell.MouseTarget)
		{
			if (world.SpellCatalog.TryGetScript(spell.ScriptName, out var script) && script.IsMissileSpell && script.MissileId is { } missileId)
			{
				var flight = new SpellMissileFlight(casterPos.X, casterPos.Y, packet.TargetX, packet.TargetY, missileId);
				world.ActiveMissileEffects.Add(flight);
			}
		}
		else
		{
			if (world.SpellCatalog.TryGetScript(spell.ScriptName, out var script) && script.IsAreaSpell && script.Area is { } area && script.EffectId is { } effectId)
			{
				if (area.TryGetCasterAnchor(out var casterRow, out var casterCol))
				{
					int anchorX = spell.NeedTarget ? packet.TargetX : casterPos.X;
					int anchorY = spell.NeedTarget ? packet.TargetY : casterPos.Y;

					var waveDx = 0;
					var waveDy = 0;
					if (spell.Direction)
						(waveDx, waveDy) = casterDirection switch
						{
							0 => (0, -1),
							1 => (1, 0),
							2 => (0, 1),
							3 => (-1, 0),
							_ => (0, 0)
						};

					var rows = area.Cells.GetLength(0);
					var cols = area.Cells.GetLength(1);
					var list = new List<SpellTileHit>();

					for (var r = 0; r < rows; r++)
					{
						for (var c = 0; c < cols; c++)
						{
							var cell = (SpellAreaCell)area.Cells[r, c];
							if (cell is not (SpellAreaCell.Effect or SpellAreaCell.Caster))
								continue;

							var lateral = c - casterCol;
							var forward = casterRow - r;

							var (fwdX, fwdY) = casterDirection switch
							{
								0 => (0, -1),
								1 => (1, 0),
								2 => (0, 1),
								3 => (-1, 0),
								_ => (0, 0)
							};
							var (rightX, rightY) = casterDirection switch
							{
								0 => (1, 0),
								1 => (0, 1),
								2 => (-1, 0),
								3 => (0, -1),
								_ => (0, 0)
							};
							int dx = lateral * rightX + forward * fwdX;
							int dy = lateral * rightY + forward * fwdY;

							var tileX = anchorX + dx + waveDx;
							var tileY = anchorY + dy + waveDy;

							if (!world.Map.IsInside(new Position(tileX, tileY, 7)))
								continue;

							list.Add(new SpellTileHit(new Position(tileX, tileY, casterPos.Z), effectId));
						}
					}
					world.ActiveSpellEffects.AddHits(list);
				}
			}
		}

		if (_isHost)
		{
			// Broadcast spell cast to spectators around the caster position
			BroadcastToSpectators(PacketWriter.WritePacket(packet), casterPos);
		}
	}

	public void Send(byte[] data)
	{
		if (_isClient && _networkManager?.ClientConnection is not null)
		{
			_networkManager.ClientConnection.SendAsync(data);
		}
		else if (_isHost && _networkManager?.Server is not null)
		{
			// Host broadcasts locally if sending to others
			_networkManager.Server.BroadcastAsync(data);
		}
	}

	public void Broadcast(byte[] data)
	{
		if (_networkManager?.Server is not null)
		{
			_networkManager.Server.BroadcastAsync(data);
		}
	}

	// Proximity-based spectator broadcasting
	public void BroadcastToSpectators(byte[] data, Position origin, int rx = 18, int ry = 14, int rz = 2)
	{
		if (!_isHost || _networkManager?.Server is null) return;

		var spectators = GetSpectators(origin, rx, ry, rz);
		foreach (var conn in spectators)
		{
			conn.SendAsync(data);
		}
	}

	public List<INyxConnection> GetSpectators(Position origin, int rx = 18, int ry = 14, int rz = 2)
	{
		var list = new List<INyxConnection>();
		lock (_connections)
		{
			foreach (var conn in _connections)
			{
				string cid = conn.Id.ToString();
				var rp = _world.RemotePlayers.Find(p => p.ClientId == cid);
				if (rp is not null)
				{
					if (Math.Abs(rp.Position.X - origin.X) <= rx &&
						Math.Abs(rp.Position.Y - origin.Y) <= ry &&
						Math.Abs(rp.Position.Z - origin.Z) <= rz)
					{
						list.Add(conn);
					}
				}
			}
		}
		return list;
	}

	private void HandlePlayerChat(INyxConnection conn, PlayerChatPacket packet)
	{
		var world = _world;

		OnMessageReceived?.Invoke(packet);

		if (_isHost && conn is not null)
		{
			byte[] data = PacketWriter.WritePacket(packet);
			if (packet.ChatType == 0) // Speak
			{
				Position senderPos = world.Player?.Position ?? new Position(0, 0, 7);
				var rp = world.RemotePlayers.Find(p => p.ClientId == packet.SenderId);
				if (rp is not null) senderPos = rp.Position;

				BroadcastToSpectators(data, senderPos, rx: 18, ry: 14);
			}
			else if (packet.ChatType == 1) // Yell
			{
				Position senderPos = world.Player?.Position ?? new Position(0, 0, 7);
				var rp = world.RemotePlayers.Find(p => p.ClientId == packet.SenderId);
				if (rp is not null) senderPos = rp.Position;

				BroadcastToSpectators(data, senderPos, rx: 36, ry: 28);
			}
			else
			{
				Broadcast(data);
			}
		}
	}

	public void SendChatRequest(string message, byte chatType)
	{
		if (!IsActive)
		{
			var offlinePacket = new PlayerChatPacket
			{
				SenderId = "OFFLINE",
				SenderName = _world.Player?.Name ?? "Player",
				Message = message,
				ChatType = chatType
			};
			HandlePlayerChat(null!, offlinePacket);
			return;
		}

		var chatPacket = new PlayerChatPacket
		{
			SenderId = ClientId,
			SenderName = _world.Player?.Name ?? (IsHost ? "HOST" : "Player"),
			Message = message,
			ChatType = chatType
		};

		byte[] packetData = PacketWriter.WritePacket(chatPacket);
		if (IsClient)
		{
			Send(packetData);
		}
		else if (IsHost)
		{
			if (chatType == 0) // Speak
			{
				BroadcastToSpectators(packetData, _world.Player?.Position ?? new Position(0, 0, 7));
			}
			else if (chatType == 1) // Yell
			{
				BroadcastToSpectators(packetData, _world.Player?.Position ?? new Position(0, 0, 7), rx: 36, ry: 28);
			}
			else
			{
				Broadcast(packetData);
			}

			HandlePlayerChat(null!, chatPacket);
		}
	}

	public void HandleSpellCastLocal(SpellCastPacket packet) => HandleSpellCast(null!, packet);
	public void HandleItemUpdateLocal(ItemUpdatePacket packet) => HandleItemUpdate(null!, packet);

	public void Dispose()
	{
		if (_isHost)
		{
			StopHosting();
		}
		else if (_isClient)
		{
			DisconnectClient();
		}
		GC.SuppressFinalize(this);
	}
}
