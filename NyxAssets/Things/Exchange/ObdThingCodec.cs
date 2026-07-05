using System.Text;
using NyxAssets.Data.Readers;
using NyxAssets.Data.Writers;
using NyxAssets.Sprites;

namespace NyxAssets.Things.Exchange;

/// <summary>Reads and writes Object Builder <c>.obd</c> single-thing files (OBD v1–v3).</summary>
public static class ObdThingCodec
{
    /// <summary>Loads a thing document from LZMA-compressed OBD bytes.</summary>
    public static ThingDocument Read(ReadOnlyMemory<byte> obdBytes, ClientDataReadOptions? options = null)
    {
        var payload = FlashLzmaCodec.Decompress(obdBytes.Span);
        var reader = new LittleEndianSpanReader(payload);
        var marker = reader.ReadU16();

        if (marker == ObdVersions.Version2)
            return ReadV2(ref reader, options);

        if (marker == ObdVersions.Version3)
            return ReadV3(ref reader, options);

        if (marker >= 710)
            return ReadV1(ref reader, marker, options);

        throw new InvalidDataException($"Unsupported OBD version marker {marker}.");
    }

    /// <summary>Loads a thing document from an OBD file path.</summary>
    public static ThingDocument Read(string filePath, ClientDataReadOptions? options = null) =>
        Read(File.ReadAllBytes(filePath).AsMemory(), options);

    /// <summary>
    /// Writes LZMA-compressed OBD bytes. Requires <see cref="ThingDocument.SpritesRgba"/> for all sprite ids
    /// referenced by the thing's frame groups.
    /// </summary>
    public static byte[] Write(ThingDocument document, ClientDataReadOptions options, ushort? obdVersion = null)
    {
        var version = obdVersion ?? ResolveDefaultObdVersion(document);
        if (!ObdVersions.IsSupported(version))
            throw new ArgumentOutOfRangeException(nameof(obdVersion), "Unsupported OBD version.");

        if (document.SpritesRgba is null || document.SpritesRgba.Count == 0)
            throw new InvalidOperationException("OBD export requires embedded sprite pixels in SpritesRgba.");

        foreach (var spriteId in ThingDocument.CollectUniqueSpriteIds(document.Thing))
        {
            if (!document.SpritesRgba.ContainsKey(spriteId))
                throw new InvalidOperationException($"Sprite {spriteId} is missing from SpritesRgba.");
        }

        EnsureObdAnimationMetadata(document.Thing, options);

        using var body = new MemoryStream();
        var writer = new LittleEndianStreamWriter(body);

        switch (version)
        {
            case ObdVersions.Version1:
                WriteV1(writer, document, options);
                break;
            case ObdVersions.Version2:
                WriteV2(writer, document, options);
                break;
            case ObdVersions.Version3:
                WriteV3(writer, document, options);
                break;
            default:
                throw new InvalidOperationException($"Unsupported OBD version {version}.");
        }

        return FlashLzmaCodec.Compress(body.ToArray());
    }

    /// <summary>Writes an OBD file.</summary>
    public static void Write(string filePath, ThingDocument document, ClientDataReadOptions options, ushort? obdVersion = null) =>
        File.WriteAllBytes(filePath, Write(document, options, obdVersion));

    private static ushort ResolveDefaultObdVersion(ThingDocument document)
    {
        if (document.ObdVersion is ObdVersions.Version1 or ObdVersions.Version2 or ObdVersions.Version3)
            return document.ObdVersion;

        // Object Builder defaults: v3 only for multi-group outfits; v2 for everything else.
        if (document.Thing.Kind == ThingKind.Outfit && document.Thing.FrameGroups.Count > 1)
            return ObdVersions.Version3;

        return ObdVersions.Version2;
    }

    private static ThingDocument ReadV1(ref LittleEndianSpanReader reader, ushort clientVersion, ClientDataReadOptions? options)
    {
        var readOptions = options ?? CreateDefaultOptions(clientVersion);
        var category = ReadLengthPrefixedLatin1(ref reader);
        var kind = ThingKindNames.FromName(category);
        var thing = new ThingType { Id = 0, Kind = kind };

        var format = readOptions.ResolveDatThingFormat();
        ThingPropertyDecoder.Read(ref reader, thing, format);

        var sprites = new Dictionary<uint, byte[]>();
        ObdTextureCodec.ReadV1(
            ref reader,
            thing,
            readOptions.TransparentSprites,
            readOptions.ResolveDefaultFrameDurationMs(kind),
            sprites);

        return new ThingDocument
        {
            Thing = thing,
            ClientVersion = clientVersion,
            ObdVersion = ObdVersions.Version1,
            SpritesRgba = sprites,
        };
    }

    private static ThingDocument ReadV2(ref LittleEndianSpanReader reader, ClientDataReadOptions? options)
    {
        var clientVersion = reader.ReadU16();
        var readOptions = options ?? CreateDefaultOptions(clientVersion);
        var kind = ReadCategory(ref reader);
        reader.ReadU32();

        var thing = new ThingType { Id = 0, Kind = kind };
        ObdPropertyCodec.Read(ref reader, thing);

        var sprites = new Dictionary<uint, byte[]>();
        ObdTextureCodec.ReadV2V3(
            ref reader,
            thing,
            ObdVersions.Version2,
            readOptions.TransparentSprites,
            readOptions.ResolveDefaultFrameDurationMs(kind),
            sprites);

        return new ThingDocument
        {
            Thing = thing,
            ClientVersion = clientVersion,
            ObdVersion = ObdVersions.Version2,
            SpritesRgba = sprites,
        };
    }

    private static ThingDocument ReadV3(ref LittleEndianSpanReader reader, ClientDataReadOptions? options)
    {
        var clientVersion = reader.ReadU16();
        var readOptions = options ?? CreateDefaultOptions(clientVersion);
        var kind = ReadCategory(ref reader);
        reader.ReadU32();

        var thing = new ThingType { Id = 0, Kind = kind };
        ObdPropertyCodec.Read(ref reader, thing);

        var sprites = new Dictionary<uint, byte[]>();
        ObdTextureCodec.ReadV2V3(
            ref reader,
            thing,
            ObdVersions.Version3,
            readOptions.TransparentSprites,
            readOptions.ResolveDefaultFrameDurationMs(kind),
            sprites);

        return new ThingDocument
        {
            Thing = thing,
            ClientVersion = clientVersion,
            ObdVersion = ObdVersions.Version3,
            SpritesRgba = sprites,
        };
    }

    private static void WriteV1(LittleEndianStreamWriter writer, ThingDocument document, ClientDataReadOptions options)
    {
        writer.WriteU16((ushort)document.ClientVersion);
        WriteLengthPrefixedLatin1(writer, ThingKindNames.ToName(document.Thing.Kind));

        var format = options.ResolveDatThingFormat();
        if (document.Thing.Kind == ThingKind.Item)
            DatThingPropertySerializer.WriteItem(writer, document.Thing, format);
        else
            DatThingPropertySerializer.WriteNonItem(writer, document.Thing, format);

        ObdTextureCodec.WriteV1(writer, document.Thing, options.TransparentSprites, document.SpritesRgba!);
    }

    private static void WriteV2(LittleEndianStreamWriter writer, ThingDocument document, ClientDataReadOptions options)
    {
        writer.WriteU16(ObdVersions.Version2);
        writer.WriteU16((ushort)document.ClientVersion);
        writer.WriteU8(ThingKindNames.ToObdCategory(document.Thing.Kind));

        var texturesPosition = writer.Position;
        writer.WriteU32(0);
        ObdPropertyCodec.Write(writer, document.Thing);

        var pos = writer.Position;
        writer.WriteU32At(texturesPosition, (uint)pos);

        ObdTextureCodec.WriteV2V3(
            writer,
            document.Thing,
            ObdVersions.Version2,
            options.TransparentSprites,
            document.SpritesRgba!);
    }

    private static void WriteV3(LittleEndianStreamWriter writer, ThingDocument document, ClientDataReadOptions options)
    {
        writer.WriteU16(ObdVersions.Version3);
        writer.WriteU16((ushort)document.ClientVersion);
        writer.WriteU8(ThingKindNames.ToObdCategory(document.Thing.Kind));

        var texturesPosition = writer.Position;
        writer.WriteU32(0);
        ObdPropertyCodec.Write(writer, document.Thing);

        var pos = writer.Position;
        writer.WriteU32At(texturesPosition, (uint)pos);

        ObdTextureCodec.WriteV2V3(
            writer,
            document.Thing,
            ObdVersions.Version3,
            options.TransparentSprites,
            document.SpritesRgba!);
    }

    private static ThingKind ReadCategory(ref LittleEndianSpanReader reader)
    {
        var value = reader.ReadU8();
        return value switch
        {
            1 => ThingKind.Item,
            2 => ThingKind.Outfit,
            3 => ThingKind.Effect,
            4 => ThingKind.Missile,
            _ => throw new InvalidDataException($"Invalid OBD category byte {value}."),
        };
    }

    private static string ReadLengthPrefixedLatin1(ref LittleEndianSpanReader reader)
    {
        var len = reader.ReadU16();
        return Encoding.Latin1.GetString(reader.ReadBytes(len));
    }

    private static void WriteLengthPrefixedLatin1(LittleEndianStreamWriter writer, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        writer.WriteU16((ushort)bytes.Length);
        writer.WriteBytes(bytes);
    }

    private static ClientDataReadOptions CreateDefaultOptions(uint clientVersion) =>
        new() { ClientVersion = new ClientDataVersion(clientVersion), TransparentSprites = true };

    /// <summary>
    /// Object Builder decoders read an animation block whenever <c>frames &gt; 1</c>, even if
    /// <c>isAnimation</c> was false in the editor model.
    /// </summary>
    private static void EnsureObdAnimationMetadata(ThingType thing, ClientDataReadOptions options)
    {
        foreach (var fg in thing.FrameGroups)
        {
            if (fg.Frames <= 1)
                continue;

            fg.IsAnimation = true;
            if (fg.FrameTimings is null || fg.FrameTimings.Length != fg.Frames)
            {
                var ms = options.ResolveDefaultFrameDurationMs(thing.Kind);
                fg.FrameTimings = new AnimationFrameTiming[fg.Frames];
                for (var i = 0u; i < fg.Frames; i++)
                    fg.FrameTimings[i] = new AnimationFrameTiming(ms, ms);
            }
        }
    }
}
