namespace Sandbox.Items;

/// <summary>Stack merge rules using <see cref="ItemsManager"/>.</summary>
public static class ItemStacking
{
    public static bool CanMerge(Item incoming, Item existing)
    {
        if (incoming is ItemContainer || existing is ItemContainer)
            return false;

        if (incoming.IsEmpty || existing.IsEmpty || incoming.ItemTypeId == 0 || existing.ItemTypeId == 0)
            return false;

        if (incoming.ItemTypeId != existing.ItemTypeId)
            return false;

        return incoming.IsStackable();
    }

    /// <summary>Merges <paramref name="incoming"/> into <paramref name="existing"/>; leaves overflow in <paramref name="incoming"/>.</summary>
    public static void Merge(ref Item existing, ref Item incoming)
    {
        if (!CanMerge(incoming, existing))
            return;

        var max = existing.GetMaxStack();
        var total = (uint)existing.Count + incoming.Count;

        if (total <= max)
        {
            existing = OfId(existing.ItemTypeId, (ushort)total);
            incoming = Item.Empty;
            return;
        }

        existing = OfId(existing.ItemTypeId, max);
        incoming = OfId(incoming.ItemTypeId, (ushort)(total - max));
    }

    private static Item OfId(uint id, ushort count) => Item.Of(id, count);
}
