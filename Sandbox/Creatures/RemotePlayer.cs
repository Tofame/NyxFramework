using NyxDrawer.Appearance;
using NyxAssets.Things;
using System;

namespace Sandbox;

internal sealed class RemotePlayer : ICreature
{
	public string ClientId { get; }
	public string Name { get; set; }
	public Position Position { get; set; }
	public int Direction { get; set; }
	public CreatureOutfitAppearance Appearance { get; set; }
	public ThingType OutfitThing { get; set; }
	public ThingType? MountThing { get; set; }
	public bool IsMounted { get; set; }
	public uint OutfitId => Appearance.LookType;
	
	public bool IsWalking => _walking;
	private bool _walking;
	private int _fromTileX, _fromTileY, _toTileX, _toTileY;
	private float _walkElapsedMs;
	private float _walkOffsetX, _walkOffsetY;
	private readonly float _stepDurationMs = 150f;
	private int _footStep;
	private int _lastFootTickBucket = -1;
	public uint WalkAnimationPhase { get; private set; }
	private int _footAnimPhases;

	private bool _hasPendingMove;
	private int _pendingDestX, _pendingDestY, _pendingDir;

	public RemotePlayer(string clientId, string name, Position position, CreatureOutfitAppearance appearance, ThingType outfitThing, ThingType? mountThing = null)
	{
		ClientId = clientId;
		Name = name;
		Position = position;
		Appearance = appearance;
		OutfitThing = outfitThing;
		MountThing = mountThing;
		IsMounted = appearance.HasMount && mountThing is not null;
		RefreshFootAnimPhases();
	}

	public void Update(float deltaSeconds, NyxGameMap.GameMap map)
	{
		if (_walking)
		{
			_walkElapsedMs += deltaSeconds * 1000f;
			var total = Math.Min(Player.SpriteSize, _walkElapsedMs / _stepDurationMs * Player.SpriteSize);

			_walkOffsetX = 0;
			_walkOffsetY = 0;
			if (Direction == 0)
				_walkOffsetY = Player.SpriteSize - total;
			else if (Direction == 2)
				_walkOffsetY = total - Player.SpriteSize;
			else if (Direction == 1)
				_walkOffsetX = total - Player.SpriteSize;
			else if (Direction == 3)
				_walkOffsetX = Player.SpriteSize - total;

			if (_footAnimPhases > 0)
			{
				var footDelay = _stepDurationMs / _footAnimPhases;
				if (footDelay < 10f) footDelay = 10f;
				var bucket = (int)(_walkElapsedMs / footDelay);
				if (bucket != _lastFootTickBucket && total < Player.SpriteSize)
				{
					_lastFootTickBucket = bucket;
					_footStep++;
				}
				if (total < Player.SpriteSize)
					WalkAnimationPhase = (uint)(1 + (_footStep % _footAnimPhases));
				else
					WalkAnimationPhase = 0;
			}

			if (_walkElapsedMs >= _stepDurationMs)
			{
				if (map.IsInside(Position))
				{
					map.GetTile(Position).RemoveCreature(this);
				}
				Position = new Position(_toTileX, _toTileY, Position.Z);
				if (map.IsInside(Position))
				{
					map.GetTile(Position).AddCreature(this);
				}
				_walking = false;
				_walkOffsetX = 0;
				_walkOffsetY = 0;
				WalkAnimationPhase = 0;
				_footStep = 0;

				if (_hasPendingMove)
				{
					_hasPendingMove = false;
					StartWalk(_pendingDestX, _pendingDestY, _pendingDir);
				}
			}
		}
	}

	public void MoveTo(int destX, int destY, int dir)
	{
		if (_walking)
		{
			_pendingDestX = destX;
			_pendingDestY = destY;
			_pendingDir = dir;
			_hasPendingMove = true;
			return;
		}

		StartWalk(destX, destY, dir);
	}

	private void StartWalk(int destX, int destY, int dir)
	{
		Direction = dir;
		if (Position.X == destX && Position.Y == destY)
		{
			_walking = false;
			return;
		}

		_fromTileX = Position.X;
		_fromTileY = Position.Y;
		_toTileX = destX;
		_toTileY = destY;
		_walking = true;
		_walkElapsedMs = 0f;
		_lastFootTickBucket = -1;
		_footStep = 0;

		// Pre-initialize offsets for t=0 so Draw() the same frame
		// does not briefly snap the sprite to the destination tile.
		_walkOffsetX = 0;
		_walkOffsetY = 0;
		if (dir == 0)       _walkOffsetY =  Player.SpriteSize;  // North: start below dest
		else if (dir == 2)  _walkOffsetY = -Player.SpriteSize;  // South: start above dest
		else if (dir == 1)  _walkOffsetX = -Player.SpriteSize;  // East:  start left of dest
		else if (dir == 3)  _walkOffsetX =  Player.SpriteSize;  // West:  start right of dest
	}

	public void GetDrawPosition(float cameraOriginTileX, float cameraOriginTileY, out float px, out float py)
	{
		var baseTileX = _walking ? _toTileX : Position.X;
		var baseTileY = _walking ? _toTileY : Position.Y;
		px = (baseTileX - cameraOriginTileX) * Player.SpriteSize + _walkOffsetX;
		py = (baseTileY - cameraOriginTileY) * Player.SpriteSize + _walkOffsetY;
	}

	private void RefreshFootAnimPhases()
	{
		_footAnimPhases = ComputeFootAnimPhases(OutfitThing, IsMounted ? MountThing : null);
	}

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
}
