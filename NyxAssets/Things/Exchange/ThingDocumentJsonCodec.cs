using System.Text.Json;
using NyxAssets.Sprites;

namespace NyxAssets.Things.Exchange;

/// <summary>
/// JSON envelope for a single thing export. Root object includes <c>type</c> (<c>item</c>, <c>outfit</c>, …)
/// plus all fields from <see cref="ThingJsonCodec"/> and optional embedded sprite pixels.
/// </summary>
public static class ThingDocumentJsonCodec
{
    public const string FormatName = "nyx-thing";
    public const int FormatVersion = 1;

    /// <summary>Reads a portable thing JSON document.</summary>
    public static ThingDocument Read(ReadOnlyMemory<byte> json)
    {
        using var doc = JsonDocument.Parse(json);
        return Read(doc.RootElement);
    }

    /// <summary>Reads a portable thing JSON document from a file.</summary>
    public static ThingDocument Read(string filePath) =>
        Read(File.ReadAllBytes(filePath).AsMemory());

    /// <summary>Reads a portable thing JSON document.</summary>
    public static ThingDocument Read(JsonElement root)
    {
        if (root.TryGetProperty("format", out var formatProp)
            && formatProp.GetString() is { } format
            && !string.Equals(format, FormatName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported document format '{format}'. Expected '{FormatName}'.");
        }

        if (!root.TryGetProperty("type", out var typeProp))
            throw new InvalidDataException("Missing required property 'type'.");

        var kind = ThingKindNames.FromName(typeProp.GetString() ?? throw new InvalidDataException("Property 'type' is empty."));
        var thing = ThingJsonCodec.Read(root, kind);

        uint clientVersion = 1098;
        if (root.TryGetProperty("clientVersion", out var clientVersionProp))
            clientVersion = clientVersionProp.GetUInt32();

        ushort obdVersion = 0;
        if (root.TryGetProperty("obdVersion", out var obdVersionProp))
            obdVersion = (ushort)obdVersionProp.GetUInt32();

        Dictionary<uint, byte[]>? sprites = null;
        if (root.TryGetProperty("sprites", out var spritesProp) && spritesProp.ValueKind == JsonValueKind.Array)
        {
            sprites = new Dictionary<uint, byte[]>();
            foreach (var spriteElem in spritesProp.EnumerateArray())
            {
                var id = spriteElem.GetProperty("id").GetUInt32();
                var data = spriteElem.GetProperty("data").GetString()
                    ?? throw new InvalidDataException($"Sprite {id} has empty data.");

                var encoding = spriteElem.TryGetProperty("encoding", out var encProp)
                    ? encProp.GetString() ?? "rgba"
                    : "rgba";

                sprites[id] = DecodeSpritePayload(id, encoding, data);
            }
        }

        return new ThingDocument
        {
            Thing = thing,
            ClientVersion = clientVersion,
            ObdVersion = obdVersion,
            SpritesRgba = sprites,
        };
    }

    /// <summary>Writes a portable thing JSON document.</summary>
    public static void Write(ThingDocument document, Stream output, bool indent = true, bool includeSprites = true)
    {
        var options = new JsonWriterOptions { Indented = indent };
        using var writer = new Utf8JsonWriter(output, options);
        Write(document, writer, includeSprites);
    }

    /// <summary>Writes a portable thing JSON document to a file.</summary>
    public static void Write(string filePath, ThingDocument document, bool indent = true, bool includeSprites = true)
    {
        using var fs = File.Create(filePath);
        Write(document, fs, indent, includeSprites);
    }

    /// <summary>Writes a portable thing JSON document.</summary>
    public static void Write(ThingDocument document, Utf8JsonWriter writer, bool includeSprites = true)
    {
        writer.WriteStartObject();
        writer.WriteString("format", FormatName);
        writer.WriteNumber("formatVersion", FormatVersion);
        writer.WriteString("type", ThingKindNames.ToName(document.Kind));
        writer.WriteNumber("clientVersion", document.ClientVersion);

        if (document.ObdVersion != 0)
            writer.WriteNumber("obdVersion", document.ObdVersion);

        ThingJsonCodec.Write(writer, document.Thing);

        if (includeSprites && document.SpritesRgba is { Count: > 0 })
        {
            writer.WriteStartArray("sprites");
            foreach (var (id, rgba) in document.SpritesRgba.OrderBy(static kv => kv.Key))
            {
                writer.WriteStartObject();
                writer.WriteNumber("id", id);
                writer.WriteString("encoding", "rgba");
                writer.WriteString("data", Convert.ToBase64String(rgba));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    /// <summary>Converts an OBD document to JSON (metadata + embedded sprites).</summary>
    public static ThingDocument FromObd(ReadOnlyMemory<byte> obdBytes, ClientDataReadOptions? options = null) =>
        ObdThingCodec.Read(obdBytes, options);

    /// <summary>Converts JSON metadata + sprites to OBD bytes.</summary>
    public static byte[] ToObd(ThingDocument document, ClientDataReadOptions options, ushort? obdVersion = null) =>
        ObdThingCodec.Write(document, options, obdVersion);

    private static byte[] DecodeSpritePayload(uint spriteId, string encoding, string base64Data)
    {
        var bytes = Convert.FromBase64String(base64Data);
        return encoding.ToLowerInvariant() switch
        {
            "rgba" when bytes.Length == SpritePixelCodec.RgbaBufferLength => bytes,
            "rgba" => throw new InvalidDataException($"Sprite {spriteId}: rgba encoding requires {SpritePixelCodec.RgbaBufferLength} bytes."),
            "spr-rle" or "rle" =>
                DecompressRleSprite(spriteId, bytes),
            _ => throw new InvalidDataException($"Sprite {spriteId}: unsupported encoding '{encoding}'."),
        };
    }

    private static byte[] DecompressRleSprite(uint spriteId, byte[] compressed)
    {
        var rgba = new byte[SpritePixelCodec.RgbaBufferLength];
        try
        {
            ObdSpritePixels.DecompressFromObd(compressed, transparent: true, rgba);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Sprite {spriteId}: failed to decode RLE payload.", ex);
        }

        return rgba;
    }
}
