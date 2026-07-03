using Silk.NET.Input;
using NyxDrawer.Appearance;
using NyxAssets.Things;
using Sandbox.Items;

namespace Sandbox;

/// <summary>Local player: tile grid + Nyx-style 32 px walk interpolation and walk animation phase.</summary>
internal sealed class Player : ICreature
{
	public const float SpriteSize = 32f;

	/// <summary>NyxClient <c>NyxDirection</c>: North=0, East, South, West.</summary>
	public int Direction { get; private set; } = (int)CreatureDirection.South;

	public string Name { get; set; }

	public Position Position { get; set; } = new(0, 0, 7);

	public int Health { get; set; } = 100;
	public int Mana { get; set; } = 100;
	public int Level { get; set; } = 1;
	public int Exp { get; set; } = 1;

	/// <summary>Tile used for map stack / draw order (destination while walking).</summary>
	public Position StackPosition => _walking ? new Position(_toTileX, _toTileY, Position.Z) : Position;

	public uint WalkAnimationPhase { get; private set; }

	public bool IsWalking => _walking;

	public float CameraCenterTileX
	{
		get
		{
			if (!_walking)
				return Position.X + 0.5f;
			var t = Math.Clamp(_walkElapsedMs / _stepDurationMs, 0f, 1f);
			return _fromTileX + t * (_toTileX - _fromTileX) + 0.5f;
		}
	}

	public float CameraCenterTileY
	{
		get
		{
			if (!_walking)
				return Position.Y + 0.5f;
			var t = Math.Clamp(_walkElapsedMs / _stepDurationMs, 0f, 1f);
			return _fromTileY + t * (_toTileY - _fromTileY) + 0.5f;
		}
	}

	public Player(
		string name,
		int startTileX,
		int startTileY,
		int startTileZ,
		CreatureOutfitAppearance appearance,
		ThingType outfitThing,
		ThingType? mountThing = null,
		float stepDurationMs = 150f)
	{
		Name = name;
		Position = new Position(startTileX, startTileY, startTileZ);
		Appearance = appearance;
		_outfit = outfitThing;
		MountThing = mountThing;
		IsMounted = appearance.HasMount && mountThing is not null;
		_stepDurationMs = Math.Max(40f, stepDurationMs);
		RefreshFootAnimPhases();
	}

	public PlayerEquipment Equipment { get; } = new();

	/// <summary>Backpack contents (opened from equipment backpack slot). Sized from equipped container via <see cref="SetBackpackStorage"/>.</summary>
	public ItemStorage Backpack { get; private set; } = new(ItemPlacementRules.DefaultContainerCapacity);

	/// <summary>Replaces the player backpack grid (e.g. when equipping a container with <c>max-slots</c> from ExtraProperties).</summary>
	internal void SetBackpackStorage(ItemStorage storage) => Backpack = storage;

	public CreatureOutfitAppearance Appearance { get; }
	public uint OutfitId => Appearance.LookType;
	public ThingType OutfitThing => _outfit;
	public ThingType? MountThing { get; }
	public bool IsMounted { get; private set; }

	public void Update(float deltaSeconds, IInputContext? input, NyxGameMap.GameMap map, bool blocksMovement = false)
	{
		if (_walking)
			AdvanceWalk(deltaSeconds, map);

		if (input is not null && input.Keyboards.Count > 0 && input.Keyboards[0] is { } kb)
		{
			if (MountThing is not null && kb.IsKeyPressed(Key.R) && !_mountKeyWasDown)
				ToggleMount();
			_mountKeyWasDown = kb.IsKeyPressed(Key.R);
		}

		if (_walking)
			return;

		if (blocksMovement)
			return;

		if (input is null || input.Keyboards.Count == 0)
			return;

		var keyboard = input.Keyboards[0];
		if (keyboard is null)
			return;
		int dx = 0, dy = 0;

		if (keyboard.IsKeyPressed(Key.W)) dy -= 1;
		if (keyboard.IsKeyPressed(Key.S)) dy += 1;
		if (keyboard.IsKeyPressed(Key.A)) dx -= 1;
		if (keyboard.IsKeyPressed(Key.D)) dx += 1;
		if (dx == 0 && dy == 0)
			return;

		if (Math.Abs(dx) > Math.Abs(dy))
			Direction = dx < 0 ? (int)CreatureDirection.West : (int)CreatureDirection.East;
		else
			Direction = dy < 0 ? (int)CreatureDirection.North : (int)CreatureDirection.South;

		var nx = Position.X + dx;
		var ny = Position.Y + dy;
		if (!map.IsInside(new Position(nx, ny, 7)))
			return;

		StartWalk(nx, ny);
	}

	public void GetDrawPosition(float cameraOriginTileX, float cameraOriginTileY, out float px, out float py)
	{
		var baseTileX = _walking ? _toTileX : Position.X;
		var baseTileY = _walking ? _toTileY : Position.Y;
		px = (baseTileX - cameraOriginTileX) * SpriteSize + _walkOffsetX;
		py = (baseTileY - cameraOriginTileY) * SpriteSize + _walkOffsetY;
	}

	private void StartWalk(int destTileX, int destTileY)
	{
		_fromTileX = Position.X;
		_fromTileY = Position.Y;
		_toTileX = destTileX;
		_toTileY = destTileY;
		_walking = true;
		_walkElapsedMs = 0f;
		_lastFootTickBucket = -1;
		_footStep = 0;
		UpdateWalkOffset(0);
	}

	private void AdvanceWalk(float deltaSeconds, NyxGameMap.GameMap map)
	{
		_walkElapsedMs += deltaSeconds * 1000f;
		var total = Math.Min(SpriteSize, _walkElapsedMs / _stepDurationMs * SpriteSize);

		UpdateWalkOffset(total);
		UpdateWalkAnimationPhase(total);

		if (_walkElapsedMs >= _stepDurationMs)
		{
			var nextPos = new Position(_toTileX, _toTileY, Position.Z);
			_walking = false;
			_walkOffsetX = 0;
			_walkOffsetY = 0;
			WalkAnimationPhase = 0;
			_footStep = 0;

			if (map.IsInside(nextPos))
			{
				var tile = map.GetTile(nextPos);
				var fcItem = FindFloorChangeItem(tile);
				if (!fcItem.IsEmpty)
				{
					var type = fcItem.GetItemType();
					var dir = type.FloorChangeDirection?.ToLowerInvariant();
					if (dir == "north")
					{
						nextPos = nextPos.Translate(0, -1, -1);
					}
					else if (dir == "south" || dir == "southalt")
					{
						nextPos = nextPos.Translate(0, 1, -1);
					}
					else if (dir == "east" || dir == "eastalt")
					{
						nextPos = nextPos.Translate(1, 0, -1);
					}
					else if (dir == "west")
					{
						nextPos = nextPos.Translate(-1, 0, -1);
					}
					else if (dir == "down" || (type.FloorChange && string.IsNullOrEmpty(dir)))
					{
						if (fcItem.ItemTypeId == 411)
						{
							nextPos = nextPos.Translate(0, 1, 1);
						}
						else
						{
							nextPos = nextPos.Translate(0, 0, 1);
						}
					}
				}
			}
			if (map.IsInside(Position))
			{
				map.GetTile(Position).RemoveCreature(this);
			}
			Position = nextPos;
			if (map.IsInside(Position))
			{
				map.GetTile(Position).AddCreature(this);
			}
		}
	}

	private NyxGameCore.Item FindFloorChangeItem(Tile tile)
	{
		if (!tile.Ground.IsEmpty)
		{
			var type = tile.Ground.GetItemType();
			if (type.FloorChange || !string.IsNullOrEmpty(type.FloorChangeDirection))
				return tile.Ground;
		}

		foreach (var item in tile.Items)
		{
			if (!item.IsEmpty)
			{
				var type = item.GetItemType();
				if (type.FloorChange || !string.IsNullOrEmpty(type.FloorChangeDirection))
					return item;
			}
		}

		return NyxGameCore.Item.Empty;
	}

	private void UpdateWalkOffset(float totalPixelsWalked)
	{
		var d = (int)Direction;
		_walkOffsetX = 0;
		_walkOffsetY = 0;
		if (d == 0)
			_walkOffsetY = SpriteSize - totalPixelsWalked;
		else if (d == 2)
			_walkOffsetY = totalPixelsWalked - SpriteSize;
		else if (d == 1)
			_walkOffsetX = totalPixelsWalked - SpriteSize;
		else if (d == 3)
			_walkOffsetX = SpriteSize - totalPixelsWalked;
	}

	private void UpdateWalkAnimationPhase(float totalPixelsWalked)
	{
		if (_footAnimPhases <= 0)
		{
			WalkAnimationPhase = 0;
			return;
		}

		var footDelay = _stepDurationMs / _footAnimPhases;
		if (footDelay < 10f)
			footDelay = 10f;

		var bucket = (int)(_walkElapsedMs / footDelay);
		if (bucket != _lastFootTickBucket && totalPixelsWalked < SpriteSize)
		{
			_lastFootTickBucket = bucket;
			_footStep++;
		}

		if (totalPixelsWalked < SpriteSize)
			WalkAnimationPhase = (uint)(1 + (_footStep % _footAnimPhases));
		else
			WalkAnimationPhase = 0;
	}

	public void ToggleMount()
	{
		if (!Appearance.HasMount || MountThing is null)
			return;
		IsMounted = !IsMounted;
		RefreshFootAnimPhases();
	}

	private void RefreshFootAnimPhases() =>
		_footAnimPhases = ComputeFootAnimPhases(_outfit, IsMounted ? MountThing : null);

	private static int ComputeFootAnimPhases(ThingType outfit, ThingType? mountOutfit)
	{
		var phases = ComputeFootAnimPhases(outfit);
		if (mountOutfit is null || mountOutfit.FrameGroups.Count == 0)
			return phases;

		var mountPhases = ComputeFootAnimPhases(mountOutfit);
		return Math.Min(phases, mountPhases);
	}

	private static int ComputeFootAnimPhases(ThingType outfit)
	{
		if (outfit.FrameGroups.Count == 0)
			return 1;
		if (outfit.FrameGroups.Count > 1)
			return Math.Max(1, (int)outfit.FrameGroups[1].Frames);

		var frames = (int)outfit.FrameGroups[0].Frames;
		return Math.Max(1, frames - 1);
	}

	private readonly ThingType _outfit;
	private readonly float _stepDurationMs;
	private int _footAnimPhases;
	private bool _mountKeyWasDown;

	private bool _walking;
	private int _fromTileX, _fromTileY, _toTileX, _toTileY;
	private float _walkElapsedMs;
	private float _walkOffsetX, _walkOffsetY;
	private int _footStep;
	private int _lastFootTickBucket = -1;
}
