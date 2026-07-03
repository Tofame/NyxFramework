using NyxAssets.Things;
using NyxStructs;

namespace NyxGameCore;

/// <summary>One map cell: ground sprite + stacked items + creatures.</summary>
public sealed class Tile
{
	public Item Ground { get; private set; } = Item.Empty;

	public ushort GroundId => (ushort)Ground.ItemTypeId;

	private ValueList<Item> _items;
	private ValueList<uint> _zoneIds = default;
	private ValueList<ICreature> _creatures;

	public ArraySegment<Item> Items => _items.AsSpan();

	// Gameplay metadata from map loaders/editors
	public ArraySegment<uint> ZoneIds => _zoneIds.AsSpan();

	// Active creatures on the tile
	public ArraySegment<ICreature> Creatures => _creatures.AsSpan();

	public bool HasGround => !Ground.IsEmpty;
	public bool HasItems => _items.Count > 0;
	public bool HasCreatures => _creatures.Count > 0;
	public int ItemCount => _items.Count;
	public int CreatureCount => _creatures.Count;

	public void SetGround(Item ground) => Ground = ground;

	public void AddCreature(ICreature creature)
	{
		_creatures.Add(creature);
	}

	public bool RemoveCreature(ICreature creature)
	{
		return _creatures.Remove(creature);
	}

	public void AddItemDirect(Item item)
	{
		_items.Add(item);
	}

	private void RemoveItemAt(int index)
	{
		_items.RemoveAt(index);
	}

	public void AddItem(Item item, ThingCatalog? catalog = null)
	{
		if (item.IsEmpty)
			return;

		if (catalog != null)
		{
			var thing = catalog.TryGetItem(item.ItemTypeId);
			if (thing != null && thing.Stackable)
			{
				int count = _items.Count;
				for (var i = 0; i < count; i++)
				{
					if (_items[i].ItemTypeId == item.ItemTypeId)
					{
						const ushort maxStack = 100;
						var existing = _items[i];
						var total = (uint)existing.Count + item.Count;
						if (total <= maxStack)
						{
							existing.Count = (ushort)total;
							_items.SetAt(i, existing);
							item.Count = 0;
							return;
						}
						else
						{
							existing.Count = maxStack;
							_items.SetAt(i, existing);
							item.Count = (ushort)(total - maxStack);
							AddItem(item, catalog);
							return;
						}
					}
				}
			}
		}

		InsertSorted(item, catalog);
	}

	private void InsertSorted(Item item, ThingCatalog? catalog)
	{
		var priority = GetStackPriority(item.ItemTypeId, catalog);
		var append = priority <= 3;
		int count = _items.Count;

		for (var i = 0; i < count; i++)
		{
			var otherPriority = GetStackPriority(_items[i].ItemTypeId, catalog);
			if (append && otherPriority > priority || !append && otherPriority >= priority)
			{
				_items.Insert(i, item);
				return;
			}
		}

		_items.Add(item);
	}

	private static int GetStackPriority(uint itemId, ThingCatalog? catalog)
	{
		if (catalog == null)
			return 5; // Common

		var thing = catalog.TryGetItem(itemId);
		if (thing == null)
			return 5; // Common

		if (thing.IsGround)
			return 0;
		if (thing.IsGroundBorder)
			return 1;
		if (thing.IsOnBottom)
			return 2;
		if (thing.IsOnTop)
			return 3;
		return 5; // Common
	}

	/// <summary>Removes the visually topmost pickupable item.</summary>
	public bool TryTakeTopItem(out Item item, ThingCatalog? catalog = null)
	{
		item = Item.Empty;
		int count = _items.Count;
		if (count == 0)
			return false;

		for (var i = count - 1; i >= 0; i--)
		{
			if (GetStackPriority(_items[i].ItemTypeId, catalog) == 3)
			{
				item = _items[i];
				_items.RemoveAt(i);
				return true;
			}
		}

		for (var i = 0; i < count; i++)
		{
			if (GetStackPriority(_items[i].ItemTypeId, catalog) == 5)
			{
				item = _items[i];
				_items.RemoveAt(i);
				return true;
			}
		}

		for (var i = count - 1; i >= 0; i--)
		{
			if (GetStackPriority(_items[i].ItemTypeId, catalog) == 2)
			{
				item = _items[i];
				_items.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	/// <summary>Same priority as TryTakeTopItem without removing.</summary>
	public bool TryPeekTopItem(out int index, out Item item, ThingCatalog? catalog = null)
	{
		index = -1;
		item = Item.Empty;
		int count = _items.Count;
		if (count == 0)
			return false;

		for (var i = count - 1; i >= 0; i--)
		{
			if (GetStackPriority(_items[i].ItemTypeId, catalog) == 3)
			{
				index = i;
				item = _items[i];
				return true;
			}
		}

		for (var i = 0; i < count; i++)
		{
			if (GetStackPriority(_items[i].ItemTypeId, catalog) == 5)
			{
				index = i;
				item = _items[i];
				return true;
			}
		}

		for (var i = count - 1; i >= 0; i--)
		{
			if (GetStackPriority(_items[i].ItemTypeId, catalog) == 2)
			{
				index = i;
				item = _items[i];
				return true;
			}
		}

		return false;
	}

	public void SetItemAt(int index, Item item)
	{
		if (index < 0 || index >= _items.Count)
			return;

		_items.SetAt(index, item.IsEmpty ? Item.Empty : item);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public int FillStack(Span<TileStackEntry> dest)
	{
		int count = 0;
		if (!Ground.IsEmpty)
		{
			if (count < dest.Length)
				dest[count++] = new TileStackEntry((ushort)Ground.ItemTypeId, Ground.Count);
		}
		int itemCount = _items.Count;
		for (int i = 0; i < itemCount; i++)
		{
			if (count < dest.Length)
				dest[count++] = new TileStackEntry((ushort)_items[i].ItemTypeId, _items[i].Count);
		}
		return count;
	}

	/// <summary>Ground then items in stack order.</summary>
	public IEnumerable<TileStackEntry> EnumerateStack()
	{
		if (!Ground.IsEmpty)
			yield return new TileStackEntry((ushort)Ground.ItemTypeId, Ground.Count);

		int count = _items.Count;
		for (int i = 0; i < count; i++)
		{
			yield return new TileStackEntry((ushort)_items[i].ItemTypeId, _items[i].Count);
		}
	}

	public bool IsWalkable(ThingCatalog? catalog = null)
	{
		if (Ground.IsEmpty)
			return false;

		if (catalog != null)
		{
			var groundThing = catalog.TryGetItem(Ground.ItemTypeId);
			if (groundThing != null && groundThing.BlockPathfind)
				return false;

			int count = _items.Count;
			for (int i = 0; i < count; i++)
			{
				var thing = catalog.TryGetItem(_items[i].ItemTypeId);
				if (thing != null && thing.BlockPathfind)
					return false;
			}
		}

		return _creatures.Count == 0;
	}

	public bool BlocksMissile(ThingCatalog? catalog = null)
	{
		if (catalog != null)
		{
			if (!Ground.IsEmpty)
			{
				var groundThing = catalog.TryGetItem(Ground.ItemTypeId);
				if (groundThing != null && groundThing.BlockMissile)
					return true;
			}

			int count = _items.Count;
			for (int i = 0; i < count; i++)
			{
				var thing = catalog.TryGetItem(_items[i].ItemTypeId);
				if (thing != null && thing.BlockMissile)
					return true;
			}
		}

		return false;
	}

	public Item GetTopItem(ThingCatalog? catalog = null)
	{
		if (TryPeekTopItem(out _, out var item, catalog))
			return item;
		return Item.Empty;
	}

	public ICreature? GetTopCreature()
	{
		return _creatures.Count > 0 ? _creatures[_creatures.Count - 1] : null;
	}

	public bool ContainsItem(uint itemTypeId)
	{
		return FindItemIndex(itemTypeId) >= 0;
	}

	public int FindItemIndex(uint itemTypeId)
	{
		int count = _items.Count;
		for (int i = 0; i < count; i++)
		{
			if (_items[i].ItemTypeId == itemTypeId)
				return i;
		}
		return -1;
	}

	public Item? GetItemAt(int index)
	{
		if (index < 0 || index >= _items.Count)
			return null;
		return _items[index];
	}

	public void ClearItems()
	{
		_items = default;
	}

	public void ClearCreatures()
	{
		_creatures = default;
	}

	public void Clear()
	{
		Ground = Item.Empty;
		ClearItems();
		ClearCreatures();
	}

	public bool RemoveItem(Item item)
	{
		if (item == null || item.IsEmpty)
			return false;

		int count = _items.Count;
		for (int i = 0; i < count; i++)
		{
			if (ReferenceEquals(_items[i], item))
			{
				_items.RemoveAt(i);
				return true;
			}
		}

		for (int i = 0; i < count; i++)
		{
			if (_items[i].ItemTypeId == item.ItemTypeId)
			{
				var existing = _items[i];
				if (existing.Count > item.Count)
				{
					existing.Count -= item.Count;
					_items.SetAt(i, existing);
					return true;
				}
				else if (existing.Count == item.Count)
				{
					_items.RemoveAt(i);
					return true;
				}
			}
		}

		return false;
	}
}

/// <summary>One drawable thing on a Tile.</summary>
public readonly record struct TileStackEntry(ushort DatId, ushort Count);
