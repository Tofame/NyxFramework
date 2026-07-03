namespace Sandbox.Items;

/// <summary>Fixed-capacity item grid (backpack, chest, depot).</summary>
public sealed class ItemStorage
{
    private readonly Item[] _slots;

    public ItemStorage(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        Capacity = capacity;
        _slots = new Item[capacity];
        for (var i = 0; i < capacity; i++)
            _slots[i] = Item.Empty;
    }

    public int Capacity { get; }

    public Item this[int index]
    {
        get
        {
            if ((uint)index >= (uint)Capacity)
                return Item.Empty;

            var slot = _slots[index];
            return slot.IsEmpty ? Item.Empty : slot;
        }
        set
        {
            if ((uint)index >= (uint)Capacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            _slots[index] = value.IsEmpty ? Item.Empty : value;
        }
    }

    public void Set(int index, Item item) => this[index] = item;

    public void Clear()
    {
        for (var i = 0; i < Capacity; i++)
            _slots[i] = Item.Empty;
    }
}
