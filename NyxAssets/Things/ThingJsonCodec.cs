using System.Text.Json;

namespace NyxAssets.Things;

/// <summary>Public entry point for serializing a single <see cref="ThingType"/> to/from JSON field objects.</summary>
public static class ThingJsonCodec
{
    public static ThingType Read(JsonElement elem, ThingKind kind) =>
        ThingTypeJsonMapper.ReadThing(elem, kind);

    public static void Write(Utf8JsonWriter writer, ThingType thing) =>
        ThingTypeJsonMapper.WriteThingFields(writer, thing);
}
