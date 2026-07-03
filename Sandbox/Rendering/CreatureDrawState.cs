using NyxDrawer;
using NyxDrawer.Creatures;

namespace Sandbox;

/// <summary>
/// Mutable state bag for the per-tile creature draw callback.
/// Allocated once; fields are set before each MapFloorDrawer.Draw call so the
/// captured delegate is never re-allocated.
/// </summary>
internal sealed class CreatureDrawState
{
	private List<Npc>? _npcs;
	private readonly Dictionary<int, List<Npc>> _npcLookup = new();
	private readonly List<List<Npc>> _listPool = new();
	private int _listPoolIdx;

	public List<Npc>? Npcs
	{
		get => _npcs;
		set
		{
			_npcs = value;
			foreach (var kvp in _npcLookup)
			{
				kvp.Value.Clear();
			}
			_listPoolIdx = 0;
			if (value is not null)
			{
				foreach (var npc in value)
				{
					var key = (npc.Position.Y << 16) | (npc.Position.X & 0xFFFF);
					if (!_npcLookup.TryGetValue(key, out var list))
					{
						if (_listPoolIdx < _listPool.Count)
						{
							list = _listPool[_listPoolIdx++];
						}
						else
						{
							list = new List<Npc>();
							_listPool.Add(list);
							_listPoolIdx++;
						}
						_npcLookup[key] = list;
					}
					list.Add(npc);
				}
			}
		}
	}

	private List<RemotePlayer>? _remotePlayers;
	private readonly Dictionary<int, List<RemotePlayer>> _remotePlayerLookup = new();
	private readonly List<List<RemotePlayer>> _remotePool = new();
	private int _remotePoolIdx;

	public List<RemotePlayer>? RemotePlayers
	{
		get => _remotePlayers;
		set
		{
			_remotePlayers = value;
			foreach (var kvp in _remotePlayerLookup)
			{
				kvp.Value.Clear();
			}
			_remotePoolIdx = 0;
			if (value is not null)
			{
				foreach (var rp in value)
				{
					var key = (rp.Position.Y << 16) | (rp.Position.X & 0xFFFF);
					if (!_remotePlayerLookup.TryGetValue(key, out var list))
					{
						if (_remotePoolIdx < _remotePool.Count)
						{
							list = _remotePool[_remotePoolIdx++];
							list.Clear();
						}
						else
						{
							list = new List<RemotePlayer>();
							_remotePool.Add(list);
							_remotePoolIdx++;
						}
						_remotePlayerLookup[key] = list;
					}
					list.Add(rp);
				}
			}
		}
	}

	public Player? Player;
	public AssetDrawer? Drawer;
	public float CamXf;
	public float CamYf;

	public void DrawCreatures(Position tilePos, float sx, float sy, int elevPx)
	{
		if (Drawer is null) return;

		var dz = Player is not null ? Player.Position.Z - tilePos.Z : 0;
		var floorOffset = dz * 32f;

		var key = (tilePos.Y << 16) | (tilePos.X & 0xFFFF);
		if (_npcLookup.TryGetValue(key, out var list) && list.Count > 0)
		{
			for (var i = 0; i < list.Count; i++)
			{
				var npc = list[i];
				if (npc.Position.Z != tilePos.Z)
					continue;
				npc.GetDrawPosition(CamXf, CamYf, out var npx, out var npy);
				Drawer.Creatures.Draw(new CreatureDrawRequest
				{
					Outfit     = npc.OutfitThing,
					Mount      = npc.MountThing,
					Mounted    = npc.IsMounted,
					Appearance = npc.Appearance,
					AnchorX    = npx - floorOffset - elevPx,
					AnchorY    = npy - floorOffset - elevPx,
					Direction  = npc.Direction,
					WalkPhase  = 0,
				});
			}
		}

		if (_remotePlayerLookup.TryGetValue(key, out var rList) && rList.Count > 0)
		{
			for (var i = 0; i < rList.Count; i++)
			{
				var rp = rList[i];
				if (rp.Position.Z != tilePos.Z)
					continue;
				rp.GetDrawPosition(CamXf, CamYf, out var rpx, out var rpy);
				Drawer.Creatures.Draw(new CreatureDrawRequest
				{
					Outfit     = rp.OutfitThing,
					Mount      = rp.MountThing,
					Mounted    = rp.IsMounted,
					Appearance = rp.Appearance,
					AnchorX    = rpx - floorOffset - elevPx,
					AnchorY    = rpy - floorOffset - elevPx,
					Direction  = rp.Direction,
					WalkPhase  = rp.WalkAnimationPhase,
				});
			}
		}

		if (Player is null || Player.StackPosition.X != tilePos.X || Player.StackPosition.Y != tilePos.Y || Player.StackPosition.Z != tilePos.Z)
			return;

		Player.GetDrawPosition(CamXf, CamYf, out var ppx, out var ppy);
		Drawer.Creatures.Draw(new CreatureDrawRequest
		{
			Outfit     = Player.OutfitThing,
			Mount      = Player.MountThing,
			Mounted    = Player.IsMounted,
			Appearance = Player.Appearance,
			AnchorX    = ppx - floorOffset - elevPx,
			AnchorY    = ppy - floorOffset - elevPx,
			Direction  = Player.Direction,
			WalkPhase  = Player.WalkAnimationPhase,
		});
	}
}
