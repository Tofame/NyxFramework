using System;
using System.Collections.Generic;
using System.IO;
using NyxGameCore;
using NyxGameMap.Formats;
using NyxAssets.Things;

namespace NyxGameMap;

public class GameMap
{
	private readonly Dictionary<(int CX, int CY, int CZ), SecSector> _sectors = new();
	private (int CX, int CY, int CZ) _lastSectorKey = (-1, -1, -1);
	private SecSector? _lastSector;

	public string SectorsDirectory { get; set; } = string.Empty;

	public GameMap()
	{
	}

	public bool IsInside(Position pos)
	{
		return pos.X >= 0 && pos.Y >= 0 && pos.Z >= 0 && pos.Z < 16;
	}

	public Tile GetTile(Position pos)
	{
		if (!IsInside(pos))
			return new Tile();

		int cx = pos.X / SecSector.SizeX;
		int cy = pos.Y / SecSector.SizeY;
		int cz = pos.Z;

		var key = (cx, cy, cz);
		SecSector? sector = _lastSector;
		if (key != _lastSectorKey || sector == null)
		{
			if (!_sectors.TryGetValue(key, out sector))
			{
				sector = TryLoadSector(cx, cy, cz);
				_sectors[key] = sector;
			}
			_lastSectorKey = key;
			_lastSector = sector;
		}

		int rx = pos.X % SecSector.SizeX;
		int ry = pos.Y % SecSector.SizeY;
		return sector.Tiles[rx, ry];
	}

	public virtual void SetGround(Position pos, ushort groundId)
	{
		if (!IsInside(pos))
			return;

		GetTile(pos).SetGround(new Item(groundId));
	}

	public virtual void PlaceDatItem(Position pos, ushort datItemId, ushort count = 1)
	{
		if (datItemId == 0)
			return;

		GetTile(pos).AddItem(new Item(datItemId, count));
	}

	private SecSector TryLoadSector(int cx, int cy, int cz)
	{
		if (!string.IsNullOrEmpty(SectorsDirectory) && Directory.Exists(SectorsDirectory))
		{
			string filename = SecSector.GetFileName(cx, cy, cz);
			string fullPath = Path.Combine(SectorsDirectory, filename);
			if (File.Exists(fullPath))
			{
				try
				{
					return SecSector.Read(fullPath);
				}
				catch
				{
					// Fallback
				}
			}
		}

		return new SecSector(cx, cy, cz);
	}

	public void SaveAll(string directory)
	{
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		foreach (var kvp in _sectors)
		{
			var sector = kvp.Value;
			string filename = SecSector.GetFileName(sector.ChunkX, sector.ChunkY, sector.ChunkZ);
			string fullPath = Path.Combine(directory, filename);
			sector.Write(fullPath);
		}
	}

	public bool IsWalkable(Position pos, ThingCatalog? catalog = null) => GetTile(pos).IsWalkable(catalog);
	public bool BlocksMissile(Position pos, ThingCatalog? catalog = null) => GetTile(pos).BlocksMissile(catalog);
	public bool HasCreatureAt(Position pos) => GetTile(pos).HasCreatures;
	public ArraySegment<ICreature> GetCreaturesAt(Position pos) => GetTile(pos).Creatures;
	public ArraySegment<Item> GetItemsAt(Position pos) => GetTile(pos).Items;
	public Item GetGroundAt(Position pos) => GetTile(pos).Ground;

	public Tile GetNeighborTile(Position pos, int direction)
	{
		return GetTile(pos.GetNeighbor(direction));
	}

	public IEnumerable<Tile> GetAdjacentTiles(Position pos)
	{
		for (int d = 0; d < 8; d++)
		{
			yield return GetTile(pos.GetNeighbor(d));
		}
	}

	public IEnumerable<(Position Position, Tile Tile)> GetTilesInArea(Position center, int radius)
	{
		for (int z = Math.Max(0, center.Z - 2); z <= Math.Min(15, center.Z + 2); z++)
		{
			for (int y = center.Y - radius; y <= center.Y + radius; y++)
			{
				for (int x = center.X - radius; x <= center.X + radius; x++)
				{
					var pos = new Position(x, y, z);
					yield return (pos, GetTile(pos));
				}
			}
		}
	}

	public bool MoveCreature(ICreature creature, Position from, Position to)
	{
		if (!IsInside(from) || !IsInside(to))
			return false;

		var fromTile = GetTile(from);
		var toTile = GetTile(to);

		if (fromTile.RemoveCreature(creature))
		{
			toTile.AddCreature(creature);
			return true;
		}
		return false;
	}

	public bool TeleportCreature(ICreature creature, Position to)
	{
		if (!IsInside(to))
			return false;

		var from = creature.Position;
		if (IsInside(from))
		{
			GetTile(from).RemoveCreature(creature);
		}
		GetTile(to).AddCreature(creature);
		return true;
	}

	public void CleanTile(Position pos)
	{
		if (IsInside(pos))
		{
			GetTile(pos).Clear();
		}
	}

	public void AddItem(Position pos, Item item, ThingCatalog? catalog = null)
	{
		if (IsInside(pos))
		{
			GetTile(pos).AddItem(item, catalog);
		}
	}

	public bool TryTakeTopItem(Position pos, out Item item, ThingCatalog? catalog = null)
	{
		item = Item.Empty;
		if (!IsInside(pos))
			return false;

		return GetTile(pos).TryTakeTopItem(out item, catalog);
	}

	public List<Position>? FindPath(Position start, Position end, ThingCatalog? catalog = null)
	{
		if (!IsInside(start) || !IsInside(end))
			return null;

		if (start == end)
			return new List<Position>();

		var queue = new Queue<Position>();
		var cameFrom = new Dictionary<Position, Position>();
		var visited = new HashSet<Position>();

		queue.Enqueue(start);
		visited.Add(start);

		bool found = false;
		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (current == end)
			{
				found = true;
				break;
			}

			for (int d = 0; d < 4; d++)
			{
				var next = current.GetNeighbor(d);
				if (!IsInside(next) || visited.Contains(next))
					continue;

				var tile = GetTile(next);
				if (next != end && !tile.IsWalkable(catalog))
					continue;

				visited.Add(next);
				cameFrom[next] = current;
				queue.Enqueue(next);
			}
		}

		if (!found)
			return null;

		var path = new List<Position>();
		var curr = end;
		while (curr != start)
		{
			path.Add(curr);
			curr = cameFrom[curr];
		}
		path.Reverse();
		return path;
	}
}
