namespace Sandbox.Items;

/// <summary>OT-style container insert: merge into existing stacks (low index first), then earliest empty slots.</summary>
public static class ItemStoragePlacement
{
    /// <summary>Places as much of <paramref name="incoming"/> as possible; returns what did not fit.</summary>
    public static Item Insert(ItemStorage storage, Item incoming)
    {
        if (incoming.IsEmpty)
            return Item.Empty;

        if (ItemPlacementRules.IsForbiddenInsideItemStorage(incoming))
            return incoming;

        var remaining = incoming;
        MergeIntoExistingStacks(storage, ref remaining);
        FillEarliestEmptySlots(storage, ref remaining);
        return remaining;
    }

    /// <summary>Shifts all items to the lowest indices, clearing trailing slots.</summary>
    public static void CompactToLeft(ItemStorage storage)
    {
        var write = 0;
        for (var read = 0; read < storage.Capacity; read++)
        {
            var item = storage[read];
            if (item.IsEmpty)
                continue;

            if (write != read)
                storage[write] = item;

            write++;
        }

        for (var i = write; i < storage.Capacity; i++)
            storage[i] = Item.Empty;
    }

    /// <summary>Fills earliest empty slots only (no merge). For in-container moves and spillover.</summary>
    public static Item PlaceInEmptySlotsOnly(ItemStorage storage, Item incoming)
    {
        if (incoming.IsEmpty)
            return Item.Empty;

        if (ItemPlacementRules.IsForbiddenInsideItemStorage(incoming))
            return incoming;

        var remaining = incoming;
        FillEarliestEmptySlots(storage, ref remaining);
        return remaining;
    }

    /// <summary>Whether <paramref name="incoming"/> fits in empty slots only (low index first).</summary>
    public static bool CanPlaceInEmptySlotsOnly(ItemStorage storage, Item incoming)
    {
        if (incoming.IsEmpty)
            return true;

        if (ItemPlacementRules.IsForbiddenInsideItemStorage(incoming))
            return false;

        var needed = incoming.Count;
        var maxStack = incoming.GetMaxStack();

        for (var i = 0; i < storage.Capacity; i++)
        {
            if (!storage[i].IsEmpty)
                continue;

            var place = (ushort)Math.Min(needed, maxStack);
            needed -= place;
            if (needed == 0)
                return true;
        }

        return false;
    }

    private static void MergeIntoExistingStacks(ItemStorage storage, ref Item remaining)
    {
        for (var i = 0; i < storage.Capacity; i++)
        {
            if (remaining.IsEmpty)
                return;

            var slot = storage[i];
            if (!ItemStacking.CanMerge(remaining, slot))
                continue;

            var merged = slot;
            ItemStacking.Merge(ref merged, ref remaining);
            storage[i] = merged;
        }
    }

    private static void FillEarliestEmptySlots(ItemStorage storage, ref Item remaining)
    {
        for (var i = 0; i < storage.Capacity; i++)
        {
            if (remaining.IsEmpty)
                return;

            if (!storage[i].IsEmpty)
                continue;

            var max = remaining.GetMaxStack();
            var place = (ushort)Math.Min(remaining.Count, max);
            storage[i] = Item.Of(remaining.ItemTypeId, place);
            if (remaining.Count <= place)
            {
                remaining = Item.Empty;
                return;
            }

            remaining = Item.Of(remaining.ItemTypeId, (ushort)(remaining.Count - place));
        }
    }
}
