using NyxGui;
using NyxGuiRender;
using Sandbox.Items;
using Sandbox.NyxGUI_Extend;
using Silk.NET.Input;

namespace Sandbox.UI.Inventory;

/// <summary>Drag-and-drop between inventory slots and map tiles.</summary>
internal sealed class ItemDragService
{
    private const int DragThresholdPx = 4;

    private readonly UISlotHost _slots;
    private MapItemSurface? _map;
    private UISlot? _sourceSlot;
    private Position? _sourceMapTile;
    private UISlot? _pendingSource;
    private UISlot? _hoverSlot;
    private Item _payload = Item.Empty;
    private int _pointerX;
    private int _pointerY;
    private int _pressX;
    private int _pressY;
    private bool _active;
    private bool _pending;
	private bool _tempRemoved;

    private readonly StackAmountTyping _typedDrop = new();

    public ItemDragService(UISlotHost slots) => _slots = slots;

    public bool IsActive => _active;

    public void SetMap(MapItemSurface map) => _map = map;

    public void HandleStackDigitInput(IKeyboard keyboard)
    {
        if (!_active || !_payload.IsStackable() || _payload.Count <= 1)
            return;

        if (StackDigitInput.TryPoll(keyboard, out var digit))
            _typedDrop.AppendDigit(digit, _payload.Count);
    }

    public void UpdatePointer(int x, int y)
    {
        (_pointerX, _pointerY) = (x, y);

        if (_pending && !_active && _pendingSource is not null)
        {
            var dx = x - _pressX;
            var dy = y - _pressY;
            if (dx * dx + dy * dy > DragThresholdPx * DragThresholdPx)
            {
                var slot = _pendingSource;
                _pending = false;
                _pendingSource = null;
                TryBeginDragFromSlot(slot);
            }
        }

        if (!_active)
            return;

        var pick = _slots.PickSlotAt(x, y);
        if (ReferenceEquals(pick, _hoverSlot))
            return;

        _hoverSlot?.SetDropHighlight(false);
        _hoverSlot = pick;
        _hoverSlot?.SetDropHighlight(true);
    }

    public void PressSlot(UISlot slot, int x, int y)
    {
        if (_active || _slots.MoveAmount.IsOpen || slot.Item.IsEmpty)
            return;

        _pendingSource = slot;
        _pressX = x;
        _pressY = y;
        _pending = true;
    }

    public void PressMapTile(Position tilePos)
    {
        if (_active || _pending || _map is null)
            return;

        if (!_map.TryPeekTopItem(tilePos, out var item) || item.IsEmpty)
            return;

        _payload = item;
        _sourceMapTile = tilePos;
        _active = true;
    }

    public void ReleasePointer()
    {
        if (_active)
        {
            if (_slots.MoveAmount.IsOpen)
                return;

			RemoveFromSourceTemporarily();

            var target = _slots.PickSlotAt(_pointerX, _pointerY);
            if (target is not null)
                TryDropOnSlot(target);
            else if (_slots.TryPickContainerAt(_pointerX, _pointerY, out var storage, out var sync))
                TryDropOnContainer(storage, sync);
            else
                TryDropOnMapAtPointer();

            ClearHover();
            return;
        }

        _pending = false;
        _pendingSource = null;
    }

    private bool TryBeginDragFromSlot(UISlot? slot)
    {
        if (slot is null || _active || slot.Item.IsEmpty)
            return false;

        _typedDrop.Clear();
        _sourceSlot = slot;
        _payload = slot.Item;
        _active = true;
        return true;
    }

    public void CancelDrag()
    {
        if (!_active)
            return;

        if (_tempRemoved)
        {
			if (_sourceMapTile is { } mapTile)
				_map?.PlaceItem(mapTile, _payload);
			else
				ReturnRemainder(_payload);
        }

        Reset();
    }

	private void RemoveFromSourceTemporarily()
	{
		if (!_active || _tempRemoved)
			return;

		if (_sourceSlot is not null)
		{
			_sourceSlot.SetItem(Item.Empty);
			_sourceSlot.NotifyContainerChanged?.Invoke();
		}
		else if (_sourceMapTile is { } tilePos)
		{
			_map?.TryTakeTopItem(tilePos, out _);
		}

		_tempRemoved = true;
	}

    public bool TryDropOnSlot(UISlot target)
    {
        if (!_active)
            return false;

        if (_sourceSlot is not null && ReferenceEquals(_sourceSlot, target))
        {
            CancelDrag();
            return true;
        }

        if (ShouldPromptDropAmount(target))
        {
            if (_typedDrop.TryGetAmount(_payload.Count) is ushort preset)
            {
                ApplyDropOnSlot(target, preset);
                return true;
            }

            PromptDropAmount(amount =>
            {
                if (amount is ushort n)
                    ApplyDropOnSlot(target, n);
                else
                    CancelDrag();
            });
            return true;
        }

        ApplyDropOnSlot(target, _payload.Count);
        return true;
    }

    private bool TryDropOnContainer(ItemStorage storage, Action? sync)
    {
        if (!_active)
            return false;

        if (ShouldPromptContainerDrop())
        {
            if (_typedDrop.TryGetAmount(_payload.Count) is ushort preset)
            {
                ApplyDropOnContainer(storage, sync, preset);
                return true;
            }

            PromptDropAmount(amount =>
            {
                if (amount is ushort n)
                    ApplyDropOnContainer(storage, sync, n);
                else
                    CancelDrag();
            });
            return true;
        }

        ApplyDropOnContainer(storage, sync, _payload.Count);
        return true;
    }

    private bool ShouldPromptContainerDrop() =>
        _payload.IsStackable() && _payload.Count > 1;

    private bool ShouldPromptDropAmount(UISlot? target) =>
        ShouldPromptStackAmount(target?.Item ?? Item.Empty);

    private bool ShouldPromptStackAmount(Item dest) =>
        _payload.IsStackable() &&
        _payload.Count > 1 &&
        (dest.IsEmpty || ItemStacking.CanMerge(_payload, dest));

    private void ApplyDropOnSlot(UISlot target, ushort amount)
    {
        if (_payload.IsEmpty)
        {
            Reset();
            return;
        }

        amount = (ushort)Math.Min(amount, _payload.Count);
        if (amount == 0)
        {
            CancelDrag();
            return;
        }

        if (target.ContainerStorage is not null && DraggedChunkForbiddenIntoStorage(amount))
        {
            CancelDrag();
            return;
        }

        var placed = SliceDraggedStack(amount);
        if (placed.IsEmpty)
        {
            CancelDrag();
            return;
        }

        var dest = target.ReadItem?.Invoke() ?? target.Item;
        if (!ValidateEquipmentDrop(target, placed, dest))
        {
            CancelDrag();
            return;
        }

        var returned = Item.Of(_payload.ItemTypeId, (ushort)(_payload.Count - amount));

        if (!dest.IsEmpty && !ItemStacking.CanMerge(placed, dest))
        {
            if (amount < _payload.Count || _sourceMapTile is not null)
            {
                CancelDrag();
                return;
            }

            if (TryInternalContainerSwap(target, placed, dest))
            {
                FinalizeContainerStorage(target);
                Reset();
                return;
            }

            target.SetItem(placed);
            _sourceSlot?.SetItem(dest);
            target.NotifyContainerChanged?.Invoke();
            Reset();
            return;
        }

        if (dest.IsEmpty && IsInternalContainerMove(target.ContainerStorage))
        {
            var storage = target.ContainerStorage!;
            if (!returned.IsEmpty && !ItemStoragePlacement.CanPlaceInEmptySlotsOnly(storage, returned))
            {
                CancelDrag();
                return;
            }

            target.SetItem(placed);
            if (!returned.IsEmpty)
                ItemStoragePlacement.PlaceInEmptySlotsOnly(storage, returned);

            FinalizeContainerStorage(target);
            Reset();
            return;
        }

        if (target.ContainerStorage is { } container && !IsInternalContainerMove(container))
        {
            ApplyExternalContainerInsert(target, container, placed, returned);
            return;
        }

        var spill = ComputeSlotDropSpill(placed, dest, returned);
        if (!CanAcceptContainerSpill(target.ContainerStorage, spill))
        {
            CancelDrag();
            return;
        }

        if (dest.IsEmpty)
            target.SetItem(placed);
        else
        {
            var merged = dest;
            var overflow = placed;
            ItemStacking.Merge(ref merged, ref overflow);
            target.SetItem(merged);
            returned = CombineStacks(returned, overflow);
        }

        ReturnRemainder(returned);
        if (IsInternalContainerMove(target.ContainerStorage))
            FinalizeContainerStorage(target);
        else
            target.NotifyContainerChanged?.Invoke();

        Reset();
    }

    private bool TryInternalContainerSwap(UISlot target, Item placed, Item displaced)
    {
        if (!IsInternalContainerMove(target.ContainerStorage))
            return false;

        var storage = target.ContainerStorage!;
        if (!displaced.IsEmpty && !ItemStoragePlacement.CanPlaceInEmptySlotsOnly(storage, displaced))
            return false;

        target.SetItem(placed);
        if (!displaced.IsEmpty)
            ItemStoragePlacement.PlaceInEmptySlotsOnly(storage, displaced);

        return true;
    }

    private static bool CanPlaceInEarliestEmpties(ItemStorage storage, Item first, Item second)
    {
        if (second.IsEmpty)
            return ItemStoragePlacement.CanPlaceInEmptySlotsOnly(storage, first);

        if (first.IsEmpty)
            return ItemStoragePlacement.CanPlaceInEmptySlotsOnly(storage, second);

        return ItemStoragePlacement.CanPlaceInEmptySlotsOnly(storage,
            Item.Of(first.ItemTypeId, (ushort)(first.Count + second.Count)));
    }

    private static void PlaceInEarliestEmpties(ItemStorage storage, Item first, Item second)
    {
        if (!first.IsEmpty)
            ItemStoragePlacement.PlaceInEmptySlotsOnly(storage, first);

        if (!second.IsEmpty)
            ItemStoragePlacement.PlaceInEmptySlotsOnly(storage, second);
    }

    private void ApplyDropOnContainer(ItemStorage storage, Action? sync, ushort amount)
    {
        if (_payload.IsEmpty)
        {
            Reset();
            return;
        }

        amount = (ushort)Math.Min(amount, _payload.Count);
        if (amount == 0)
        {
            CancelDrag();
            return;
        }

        if (DraggedChunkForbiddenIntoStorage(amount))
        {
            CancelDrag();
            return;
        }

        var placed = Item.Of(_payload.ItemTypeId, amount);
        var returned = Item.Of(_payload.ItemTypeId, (ushort)(_payload.Count - amount));

        if (IsInternalContainerMove(storage))
        {
            if (!CanPlaceInEarliestEmpties(storage, placed, returned))
            {
                CancelDrag();
                return;
            }

            PlaceInEarliestEmpties(storage, placed, returned);
            ItemStoragePlacement.CompactToLeft(storage);
        }
        else
        {
            var leftover = ItemStoragePlacement.Insert(storage, placed);
            returned = CombineStacks(returned, leftover);
            ReturnRemainder(returned);
        }

        sync?.Invoke();
        Reset();
    }

    private bool TryDropOnMapAtPointer()
    {
        if (_map is null || !_slots.TryGetMapTileAtPointer(_pointerX, _pointerY, out var tilePos))
        {
            CancelDrag();
            return false;
        }

        if (_sourceMapTile is { } src && src == tilePos)
        {
            CancelDrag();
            return true;
        }

        if (ShouldPromptStackAmount(Item.Empty))
        {
            if (_typedDrop.TryGetAmount(_payload.Count) is ushort preset)
            {
                ApplyDropOnMap(tilePos, preset);
                return true;
            }

            PromptDropAmount(amount =>
            {
                if (amount is ushort n)
                    ApplyDropOnMap(tilePos, n);
                else
                    CancelDrag();
            });
            return true;
        }

        ApplyDropOnMap(tilePos, _payload.Count);
        return true;
    }

    private void PromptDropAmount(Action<ushort?> onChosen)
    {
        ClearHover();
        _slots.PromptDropAmount(_payload.Count, _typedDrop.TryGetAmount(_payload.Count), onChosen);
    }

    private void ApplyDropOnMap(Position tilePos, ushort amount)
    {
        if (_map is null || _payload.IsEmpty)
        {
            Reset();
            return;
        }

        amount = (ushort)Math.Min(amount, _payload.Count);
        if (amount == 0)
        {
            CancelDrag();
            return;
        }

        Item placed = _payload is ItemContainer container && amount == _payload.Count
            ? container
            : Item.Of(_payload.ItemTypeId, amount);

        _map.PlaceItem(tilePos, placed);
        ReturnRemainder(Item.Of(_payload.ItemTypeId, (ushort)(_payload.Count - amount)));
        CompactSourceContainerIfNeeded();
        Reset();
    }

    private void ApplyExternalContainerInsert(UISlot target, ItemStorage storage, Item placed, Item returned)
    {
        var leftover = ItemStoragePlacement.Insert(storage, placed);
        returned = CombineStacks(returned, leftover);
        target.NotifyContainerChanged?.Invoke();
        ReturnRemainder(returned);
        Reset();
    }

    private void CompactSourceContainerIfNeeded() =>
        FinalizeContainerStorage(_sourceSlot);

    private void FinalizeContainerStorage(UISlot? slot)
    {
        if (slot?.ContainerStorage is not { } storage)
            return;

        ItemStoragePlacement.CompactToLeft(storage);
        slot.NotifyContainerChanged?.Invoke();
    }

    private void ReturnRemainder(Item returned)
    {
        if (returned.IsEmpty)
            return;

        if (_sourceSlot is not null)
        {
            if (_sourceSlot.ContainerStorage is { } storage)
            {
                returned = ItemStoragePlacement.PlaceInEmptySlotsOnly(storage, returned);
                FinalizeContainerStorage(_sourceSlot);
                if (!returned.IsEmpty)
                    _sourceSlot.SetItem(returned);
            }
            else
                _sourceSlot.SetItem(returned);

            return;
        }

        if (_sourceMapTile is { } tile)
            _map?.PlaceItem(tile, returned);
    }

    private bool IsInternalContainerMove(ItemStorage? storage) =>
        _sourceSlot?.ContainerStorage is { } source &&
        storage is not null &&
        ReferenceEquals(source, storage);

    private bool CanAcceptContainerSpill(ItemStorage? storage, Item spill) =>
        spill.IsEmpty ||
        !IsInternalContainerMove(storage) ||
        ItemStoragePlacement.CanPlaceInEmptySlotsOnly(storage!, spill);

    private static Item ComputeSlotDropSpill(Item placed, Item dest, Item notPlaced)
    {
        if (dest.IsEmpty)
            return notPlaced;

        if (!ItemStacking.CanMerge(placed, dest))
            return notPlaced;

        var merged = dest;
        var overflow = placed;
        ItemStacking.Merge(ref merged, ref overflow);
        return CombineStacks(notPlaced, overflow);
    }

    private static Item CombineStacks(Item primary, Item extra)
    {
        if (extra.IsEmpty)
            return primary;

        if (primary.IsEmpty)
            return extra;

        var merged = primary;
        var incoming = extra;
        ItemStacking.Merge(ref merged, ref incoming);
        return incoming.IsEmpty ? merged : incoming;
    }

    private Item SliceDraggedStack(ushort amount)
    {
        if (_payload is ItemContainer container)
            return amount >= container.Count ? container : Item.Empty;

        return Item.Of(_payload.ItemTypeId, amount);
    }

    private bool ValidateEquipmentDrop(UISlot target, Item placed, Item destOccupant)
    {
        if (target.BoundEquipmentSlot is not { } slot)
            return true;

        if (!ItemPlacementRules.CanEquipInSlot(placed, slot))
            return false;

        if (!destOccupant.IsEmpty &&
            _sourceSlot?.BoundEquipmentSlot is { } sourceSlot &&
            !ItemPlacementRules.CanEquipInSlot(destOccupant, sourceSlot))
            return false;

        return true;
    }

    /// <summary>True when this drag slice may not be placed into any <see cref="ItemStorage"/> grid.</summary>
    private bool DraggedChunkForbiddenIntoStorage(ushort amount)
    {
        if (_payload is ItemContainer)
            return true;

        return ItemPlacementRules.IsForbiddenInsideItemStorage(Item.Of(_payload.ItemTypeId, amount));
    }

    public void DrawGhost(NyxGuiRenderer painter, NyxGuiTheme theme, ItemIconRasterizer icons)
    {
        if (!_active || _payload.IsEmpty)
            return;

        const int size = 32;
        var dest = new NyxRect(_pointerX - size / 2, _pointerY - size / 2, size, size);
        var displayCount = GetDisplayCount();
        var rgba = icons.GetOrCreateCached(_payload.ItemTypeId, displayCount);
        if (rgba is null)
            return;

        painter.DrawSprite32(dest, rgba, smooth: false);

        if (_payload.IsStackable() && displayCount > 1)
        {
            var font = _sourceSlot?.ItemIcon.ResolveEffectiveFont();
            UIItemStackOverlay.Paint(painter, dest, displayCount, font);
        }
    }

    private void ClearHover()
    {
        _hoverSlot?.SetDropHighlight(false);
        _hoverSlot = null;
    }

    private ushort GetDisplayCount() =>
        _typedDrop.TryGetAmount(_payload.Count) ?? _payload.Count;

    private void Reset()
    {
        ClearHover();
        _typedDrop.Clear();
        _active = false;
        _sourceSlot = null;
        _sourceMapTile = null;
        _payload = Item.Empty;
        _pending = false;
        _pendingSource = null;
		_tempRemoved = false;
    }
}

