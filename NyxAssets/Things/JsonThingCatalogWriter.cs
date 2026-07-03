using System.Text.Json;

namespace NyxAssets.Things;

/// <summary>
/// Writes a <see cref="ThingCatalog"/> to JSON format.
/// </summary>
public sealed class JsonThingCatalogWriter : IThingCatalogWriter
{
    public void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions options, uint? signatureOverride = null)
    {
        using var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        WriteThingArray(writer, "items", catalog.EnumerateItems());
        WriteThingArray(writer, "outfits", catalog.EnumerateOutfits());
        WriteThingArray(writer, "effects", catalog.EnumerateEffects());
        WriteThingArray(writer, "missiles", catalog.EnumerateMissiles());

        writer.WriteEndObject();
    }

    private static void WriteThingArray(Utf8JsonWriter writer, string propertyName, IEnumerable<ThingType> things)
    {
        writer.WriteStartArray(propertyName);
        foreach (var thing in things)
            ThingTypeJsonMapper.WriteThing(writer, thing);
        writer.WriteEndArray();
    }
}
