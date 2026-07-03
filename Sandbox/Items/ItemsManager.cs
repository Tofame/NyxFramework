using NyxAssets.Client;
using NyxAssets.Things;

namespace Sandbox.Items;

/// <summary>
/// Loads all item types from the client <c>.dat</c> (or JSON catalog) into a single array.
/// Items are referenced by their numeric dat-id.
/// </summary>
public sealed class ItemsManager
{
    private readonly ItemType[] _byDatIndex;

    private ItemsManager(ItemType[] byDatIndex)
    {
        _byDatIndex = byDatIndex;
    }

    /// <summary>Set during Sandbox startup after client assets load.</summary>
    public static ItemsManager Instance { get; private set; } = null!;

    public int TypeSlotCount => _byDatIndex.Length;

    /// <summary>All slots indexed by <c>datId - <see cref="ThingCatalog.FirstItemId"/></c>; unused ids are <see cref="ItemType.None"/>.</summary>
    public ReadOnlySpan<ItemType> All => _byDatIndex;

    public static void Initialize(ClientAssetBundle assets)
    {
        var table = LoadFromCatalog(assets.Things);

        var loaded = 0;
        for (var i = 0; i < table.Length; i++)
        {
            if (!table[i].IsNone)
                loaded++;
        }

        Console.WriteLine($"Items: {loaded} type(s) (ids {ThingCatalog.FirstItemId}…{assets.Things.ItemCount}).");

        Instance = new ItemsManager(table);
    }

    public ItemType Get(uint datItemId)
    {
        if (datItemId < ThingCatalog.FirstItemId)
            return ItemType.None;

        var index = (int)(datItemId - ThingCatalog.FirstItemId);
        if ((uint)index >= (uint)_byDatIndex.Length)
            return ItemType.None;

        var t = _byDatIndex[index];
        return t.IsNone ? ItemType.None : t;
    }

    public Item CreateStack(uint datItemId, ushort count = 1) =>
        Item.Of(datItemId, count);

    private static ItemType[] LoadFromCatalog(ThingCatalog things)
    {
        if (things.ItemCount < ThingCatalog.FirstItemId)
            return [];

        var size = (int)(things.ItemCount - ThingCatalog.FirstItemId + 1);
        var table = new ItemType[size];

        foreach (var thing in things.EnumerateItems())
        {
            var index = (int)(thing.Id - ThingCatalog.FirstItemId);
            if ((uint)index >= (uint)table.Length)
                continue;

            table[index] = ItemType.FromThing(thing);
        }

        return table;
    }
}
