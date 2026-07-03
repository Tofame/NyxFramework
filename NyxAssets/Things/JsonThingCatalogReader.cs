using System.Text.Json;

namespace NyxAssets.Things;

/// <summary>
/// Reads a <see cref="ThingCatalog"/> from JSON format.
/// </summary>
public sealed class JsonThingCatalogReader : IThingCatalogReader
{
    public ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options)
    {
        using var doc = JsonDocument.Parse(data);
        var root = doc.RootElement;

        var catalog = new ThingCatalog();

        ReadSection(root, "items", ThingKind.Item, thing => catalog.PutItem(thing, rebuildArrays: false));
        ReadSection(root, "outfits", ThingKind.Outfit, thing => catalog.PutOutfit(thing, rebuildArrays: false));
        ReadSection(root, "effects", ThingKind.Effect, thing => catalog.PutEffect(thing, rebuildArrays: false));
        ReadSection(root, "missiles", ThingKind.Missile, thing => catalog.PutMissile(thing, rebuildArrays: false));

        catalog.InitializeFastArrays();
        return catalog;
    }

    private static void ReadSection(JsonElement root, string propertyName, ThingKind kind, Action<ThingType> put)
    {
        if (!root.TryGetProperty(propertyName, out var elements))
            return;

        foreach (var elem in elements.EnumerateArray())
            put(ThingTypeJsonMapper.ReadThing(elem, kind));
    }
}
