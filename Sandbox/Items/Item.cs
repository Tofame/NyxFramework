namespace Sandbox.Items;

/// <summary>Runtime item stack: Nyx client item id + count. Base type for <see cref="ItemContainer"/>.</summary>
public class Item : NyxGameCore.Item, IEquatable<Item>
{
    private protected Item(uint itemTypeId, ushort count) : base(itemTypeId, count)
    {
    }

    /// <summary>Shared empty stack (do not mutate).</summary>
    public static new Item Empty { get; } = new(0, 0);

    /// <summary>Resolved type from <see cref="ItemsManager"/> (empty items yield <see cref="ItemType.None"/>).</summary>
    public ItemType GetItemType() => ItemsManager.Instance.Get(ItemTypeId);

    public int GetStackPriority() => GetItemType().StackPriority;

    public bool IsStackable() => GetItemType().Stackable;

    public ushort GetMaxStack() => GetItemType().MaxStack;

    /// <summary>Wear slot from client cloth / ExtraProperties; <see langword="null"/> if not equipment.</summary>
    public EquipmentSlot? GetRequiredEquipmentSlot() => GetItemType().RequiredEquipmentSlot;

    /// <summary>UI slot icon cache key (type + count only; never includes inner container grids).</summary>
    public virtual int IconDisplaySignature => HashCode.Combine(ItemTypeId, (int)Count);

    /// <summary>Clamps to <see cref="ItemType.MaxStack"/> from <see cref="ItemsManager"/>.</summary>
    public static Item Of(uint itemTypeId, ushort count = 1)
    {
        if (itemTypeId == 0 || count == 0)
            return Empty;

        var t = ItemsManager.Instance.Get(itemTypeId);
        if (t.IsNone)
            return Empty;

        var max = t.MaxStack;
        if (max == 0)
            return Empty;

        var clamped = count > max ? max : count;
        var finalCount = Math.Max((ushort)1, clamped);

        if (t.IsContainer)
            return new ItemContainer(itemTypeId, finalCount, t.ContainerCapacity);

        return new Item(itemTypeId, finalCount);
    }

    public override NyxGameCore.Item Clone() => Of(ItemTypeId, Count);

    public virtual bool Equals(Item? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (GetType() != other.GetType())
            return false;

        return ItemTypeId == other.ItemTypeId && Count == other.Count;
    }

    public override bool Equals(object? obj) => Equals(obj as Item);

    public override int GetHashCode() => HashCode.Combine(ItemTypeId, Count);

    public static bool operator ==(Item? left, Item? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(Item? left, Item? right) => !(left == right);
}

/// <summary>Well-known Nyx client ids used by the Sandbox demo map and fixtures.</summary>
public static class DemoItemIds
{
    public const uint Helmet = 2506;
    public const uint Legs = 2507;
    public const uint Armor = 2508;
    public const uint Katana = 2412;
    public const uint Shield = 2514;
    public const uint Backpack = 1988;
    public const uint GoldCoin = 2148;
    public const uint Egg = 2328;
}

public static class ItemExtensions
{
	public static ItemType GetItemType(this NyxGameCore.Item item)
	{
		return ItemsManager.Instance.Get(item.ItemTypeId);
	}
}
