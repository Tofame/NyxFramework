using Sandbox.Items;
using NyxGameMap;
using System;
using NyxAssets.Things;

namespace Sandbox;

/// <summary>Map tile hit-test and item place/take for drag-and-drop.</summary>
internal sealed class MapItemSurface
{
    private readonly GameMap _map;
	private readonly ThingCatalog? _catalog;
    private readonly Action<int, int, int, ushort, ushort, bool>? _onItemNetworkSync;

    /// <param name="map">The tile map to operate on.</param>
    /// <param name="onItemNetworkSync">
    ///     Optional callback invoked after a place/take so the caller can broadcast the
    ///     change over the network. Arguments: (x, y, z, itemTypeId, count, isPlacement).
    /// </param>
	public MapItemSurface(GameMap map, ThingCatalog? catalog, Action<int, int, int, ushort, ushort, bool>? onItemNetworkSync = null)
	{
		_map = map;
		_catalog = catalog;
		_onItemNetworkSync = onItemNetworkSync;
	}

    public bool TryPickTile(float gameX, float gameY, float camXf, float camYf, out Position tilePos)
    {
        int tileX = (int)Math.Floor(camXf + gameX / Player.SpriteSize);
        int tileY = (int)Math.Floor(camYf + gameY / Player.SpriteSize);
        tilePos = new Position(tileX, tileY, 7);
        return _map.IsInside(tilePos);
    }

    public bool TryTakeTopItem(Position tilePos, out Item item)
    {
        if (_map.IsInside(tilePos) && _map.GetTile(tilePos).TryTakeTopItem(out var baseItem, _catalog))
        {
            item = baseItem as Item ?? Item.Of(baseItem.ItemTypeId, baseItem.Count);
            _onItemNetworkSync?.Invoke(tilePos.X, tilePos.Y, tilePos.Z, (ushort)item.ItemTypeId, item.Count, false);
            return true;
        }

        item = Item.Empty;
        return false;
    }

    public void PlaceItem(Position tilePos, Item item)
    {
        if (item.IsEmpty || !_map.IsInside(tilePos))
            return;

        _map.GetTile(tilePos).AddItem(item, _catalog);
        _onItemNetworkSync?.Invoke(tilePos.X, tilePos.Y, tilePos.Z, (ushort)item.ItemTypeId, item.Count, true);
    }

    /// <summary>Upgrades the top container stack on the tile to <see cref="ItemContainer"/> when needed.</summary>
    public bool TryEnsureTopContainer(Position tilePos, out ItemContainer container)
    {
        container = null!;
        if (!_map.IsInside(tilePos))
            return false;

        var tile = _map.GetTile(tilePos);
        if (!tile.TryPeekTopItem(out var index, out var baseItem, _catalog))
            return false;

        var item = baseItem as Item ?? Item.Of(baseItem.ItemTypeId, baseItem.Count);
        if (!ItemPlacementRules.TryEnsureOpenable(ref item, out container))
            return false;

        tile.SetItemAt(index, item);
        return true;
    }

    public bool TryPeekTopItem(Position tilePos, out Item item)
    {
        item = Item.Empty;
        if (!_map.IsInside(tilePos))
            return false;

        if (_map.GetTile(tilePos).TryPeekTopItem(out _, out var baseItem, _catalog))
        {
            item = baseItem as Item ?? Item.Of(baseItem.ItemTypeId, baseItem.Count);
            return true;
        }
        return false;
    }
}
