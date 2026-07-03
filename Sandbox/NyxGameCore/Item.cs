namespace NyxGameCore;

public class Item
{
	public uint ItemTypeId { get; set; }
	public ushort Count { get; set; }

	public Item(uint itemTypeId, ushort count = 1)
	{
		ItemTypeId = itemTypeId;
		Count = count;
	}

	public bool IsEmpty => ItemTypeId == 0 || Count == 0;

	public static readonly Item Empty = new(0, 0);

	public bool IsSameType(Item other) => ItemTypeId == other.ItemTypeId;

	public virtual Item Clone() => new(ItemTypeId, Count);

	public bool Merge(Item other, ushort maxStack = 100)
	{
		if (IsEmpty || other.IsEmpty || !IsSameType(other))
			return false;

		uint total = (uint)Count + other.Count;
		if (total <= maxStack)
		{
			Count = (ushort)total;
			other.Count = 0;
			return true;
		}
		else
		{
			other.Count = (ushort)(total - maxStack);
			Count = maxStack;
			return false;
		}
	}

	public virtual bool Split(ushort splitCount, out Item splitStack)
	{
		splitStack = Empty;
		if (IsEmpty || splitCount == 0 || splitCount >= Count)
			return false;

		Count -= splitCount;
		splitStack = Clone();
		splitStack.Count = splitCount;
		return true;
	}

	public override string ToString() => $"Item {ItemTypeId} (Count: {Count})";
}
