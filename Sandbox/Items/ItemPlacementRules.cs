namespace Sandbox.Items;

/// <summary>Where items may be equipped, stored in grids, or opened as containers.</summary>
public static class ItemPlacementRules
{
    public const int DefaultContainerCapacity = 20;

    /// <summary>Container items must not go inside another container grid (no backpack-in-backpack).</summary>
    public static bool IsForbiddenInsideItemStorage(Item item)
    {
        if (item.IsEmpty)
            return false;

        if (item is ItemContainer)
            return true;

        return item.GetItemType().IsContainer;
    }

    /// <summary>Empty clears any slot. Backpack equipment slot accepts container items only.</summary>
    public static bool CanEquipInSlot(Item item, EquipmentSlot slot)
    {
        if (item.IsEmpty)
            return true;

        if (slot == EquipmentSlot.Backpack)
            return item is ItemContainer || IsContainerItem(item);

        if (item is ItemContainer || IsContainerItem(item))
            return false;

        var required = item.GetRequiredEquipmentSlot();
        return required == slot;
    }

    public static bool IsContainerItem(Item item) =>
        !item.IsEmpty && item.GetItemType().IsContainer;

    /// <summary>Plain container stacks become <see cref="ItemContainer"/> with a new empty inner grid.</summary>
    public static bool TryEnsureOpenable(ref Item item, out ItemContainer container)
    {
        container = null!;
        if (!IsContainerItem(item))
            return false;

        if (item is ItemContainer existing)
        {
            container = existing;
            return true;
        }

        var capacity = item.GetItemType().ContainerCapacity;
        container = new ItemContainer(item.ItemTypeId, item.Count, capacity);
        item = container;
        return true;
    }
}
