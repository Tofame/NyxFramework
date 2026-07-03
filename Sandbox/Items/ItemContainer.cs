namespace Sandbox.Items;

/// <summary>
/// Nyx item that owns an inner grid (backpack, bag, etc.). The equipped backpack reuses <see cref="Player.Backpack"/>
/// by passing the same <see cref="ItemStorage"/> instance.
/// </summary>
public sealed class ItemContainer : Item
{
    public ItemStorage Contents { get; }

    public int SlotCount => Contents.Capacity;

    /// <summary>Uses an existing storage grid (shared with player backpack, chests, …).</summary>
    public ItemContainer(uint itemTypeId, ushort count, ItemStorage contents)
        : base(itemTypeId, count)
    {
        ArgumentNullException.ThrowIfNull(contents);
        Contents = contents;
    }

    /// <summary>Standalone container (map loot, slots, …) — not tied to <see cref="Player.Backpack"/>.</summary>
    public ItemContainer(uint itemTypeId, ushort count, int innerCapacity)
        : this(itemTypeId, count, new ItemStorage(innerCapacity))
    {
    }

    /// <summary>All slots in index order (empty slots are <see cref="Item.Empty"/>).</summary>
    public IEnumerable<Item> GetItems()
    {
        for (var i = 0; i < Contents.Capacity; i++)
            yield return Contents[i];
    }

    public Item GetItemBySlotIndex(int index) => Contents[index];

    /// <summary>
    /// Adds a stack. When <paramref name="mergeWithExisting"/> is true, merges into matching stacks first (OT-style).
    /// Returns what did not fit.
    /// </summary>
    public Item AddItem(Item item, bool mergeWithExisting = true)
    {
        if (item.IsEmpty)
            return Item.Empty;

        if (ItemPlacementRules.IsForbiddenInsideItemStorage(item))
            return item;

        return mergeWithExisting
            ? ItemStoragePlacement.Insert(Contents, item)
            : ItemStoragePlacement.PlaceInEmptySlotsOnly(Contents, item);
    }

    /// <summary>Adds by dat id; <paramref name="count"/> defaults to 1.</summary>
    public Item AddItemById(uint itemTypeId, ushort count = 1, bool mergeWithExisting = true) =>
        AddItem(Item.Of(itemTypeId, count), mergeWithExisting);

    /// <summary>True when <paramref name="itemTypeId"/> exists in the loaded client and the stack was fully placed.</summary>
    public bool TryAddItemById(uint itemTypeId, ushort count = 1, bool mergeWithExisting = true)
    {
        var type = ItemsManager.Instance.Get(itemTypeId);
        if (type.IsNone || type.IsContainer)
            return false;

        return AddItemById(itemTypeId, count, mergeWithExisting).IsEmpty;
    }

    /// <summary>Removes <paramref name="count"/> of <paramref name="itemTypeId"/> from lowest slot index first.</summary>
    /// <returns>How many were actually removed.</returns>
    public ushort RemoveItemById(uint itemTypeId, ushort count = 1)
    {
        if (itemTypeId == 0 || count == 0)
            return 0;

        var remaining = count;
        for (var i = 0; i < Contents.Capacity && remaining > 0; i++)
        {
            var slot = Contents[i];
            if (slot.ItemTypeId != itemTypeId)
                continue;

            var take = (ushort)Math.Min(remaining, slot.Count);
            remaining -= take;
            if (take >= slot.Count)
                Contents[i] = Item.Empty;
            else
                Contents[i] = Item.Of(itemTypeId, (ushort)(slot.Count - take));
        }

        return (ushort)(count - remaining);
    }

    /// <summary>
    /// Removes one slot that exactly matches <paramref name="item"/> (type + count), otherwise removes
    /// <paramref name="item"/>.Count of that type across slots.
    /// </summary>
    public bool RemoveItem(Item item)
    {
        if (item.IsEmpty)
            return false;

        for (var i = 0; i < Contents.Capacity; i++)
        {
            if (Contents[i] != item)
                continue;

            Contents[i] = Item.Empty;
            return true;
        }

        return RemoveItemById(item.ItemTypeId, item.Count) > 0;
    }

    public override NyxGameCore.Item Clone()
    {
        var clonedStorage = new ItemStorage(Contents.Capacity);
        for (int i = 0; i < Contents.Capacity; i++)
        {
            if (!Contents[i].IsEmpty)
            {
                clonedStorage[i] = (Item)Contents[i].Clone();
            }
        }
        return new ItemContainer(ItemTypeId, Count, clonedStorage);
    }

    public override bool Equals(Item? other) =>
        other is ItemContainer oc &&
        ItemTypeId == oc.ItemTypeId &&
        Count == oc.Count &&
        ReferenceEquals(Contents, oc.Contents);

    public override int GetHashCode() =>
        HashCode.Combine(ItemTypeId, Count, System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Contents));
}
