namespace Sandbox.Items;

/// <summary>Equipped items on the local player (one stack per <see cref="EquipmentSlot"/>).</summary>
public sealed class PlayerEquipment
{
    private readonly Dictionary<EquipmentSlot, Item> _slots = new();

    public Item this[EquipmentSlot slot] => _slots.TryGetValue(slot, out var item) ? item : Item.Empty;

    public void Equip(EquipmentSlot slot, Item item)
    {
        if (!ItemPlacementRules.CanEquipInSlot(item, slot))
            return;

        _slots[slot] = item.IsEmpty ? Item.Empty : item;
    }

    public bool TryEquip(EquipmentSlot slot, Item item)
    {
        if (!ItemPlacementRules.CanEquipInSlot(item, slot))
            return false;

        _slots[slot] = item.IsEmpty ? Item.Empty : item;
        return true;
    }

    public void Clear() => _slots.Clear();

    public IEnumerable<KeyValuePair<EquipmentSlot, Item>> EnumerateEquipped()
    {
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            var item = this[slot];
            if (!item.IsEmpty)
                yield return new KeyValuePair<EquipmentSlot, Item>(slot, item);
        }
    }
}
