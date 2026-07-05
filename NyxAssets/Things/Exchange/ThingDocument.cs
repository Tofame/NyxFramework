using NyxAssets.Sprites;

namespace NyxAssets.Things.Exchange;

/// <summary>
/// A portable single-thing export: metadata, frame layout, optional embedded 32×32 RGBA sprite pixels.
/// Used by JSON (<c>type</c> envelope) and Object Builder <c>.obd</c> interchange.
/// </summary>
public sealed class ThingDocument
{
    public required ThingType Thing { get; init; }

    /// <summary>Client build the data was authored for (Object Builder stores this in OBD).</summary>
    public uint ClientVersion { get; init; } = 1098;

    /// <summary>OBD format version when loaded from or targeting <c>.obd</c>; zero for JSON-only documents.</summary>
    public ushort ObdVersion { get; init; }

    /// <summary>Decoded 4096-byte RGBA pixels keyed by sprite id. Populated by OBD import or JSON with embedded sprites.</summary>
    public Dictionary<uint, byte[]>? SpritesRgba { get; init; }

    public ThingKind Kind => Thing.Kind;

    public IEnumerable<uint> EnumerateSpriteIds()
    {
        foreach (var fg in Thing.FrameGroups)
        {
            foreach (var id in fg.SpriteIds)
                yield return id;
        }
    }

    /// <summary>Inserts or replaces this thing in a catalog bucket matching <see cref="Kind"/>.</summary>
    /// <param name="assignId">When set, overrides <see cref="ThingType.Id"/> before insert (OBD files do not store ids).</param>
    public void ImportInto(ThingCatalog catalog, uint? assignId = null)
    {
        if (assignId.HasValue)
            Thing.Id = assignId.Value;

        switch (Thing.Kind)
        {
            case ThingKind.Item:
                catalog.PutItem(Thing);
                break;
            case ThingKind.Outfit:
                catalog.PutOutfit(Thing);
                break;
            case ThingKind.Effect:
                catalog.PutEffect(Thing);
                break;
            case ThingKind.Missile:
                catalog.PutMissile(Thing);
                break;
            default:
                throw new InvalidOperationException($"Unsupported kind {Thing.Kind}.");
        }
    }

    /// <summary>Builds a metadata-only document (sprite ids in frame groups, no pixel payloads).</summary>
    public static ThingDocument FromThing(ThingType thing, uint clientVersion = 1098) =>
        new()
        {
            Thing = thing,
            ClientVersion = clientVersion,
        };

    /// <summary>Builds a document with embedded RGBA pixels resolved from a sprite source.</summary>
    public static ThingDocument FromThing(
        ThingType thing,
        ISpriteSource sprites,
        ClientDataReadOptions options,
        bool embedSprites = true)
    {
        Dictionary<uint, byte[]>? rgbaSprites = null;
        if (embedSprites)
        {
            rgbaSprites = new Dictionary<uint, byte[]>();
            var scratch = new byte[SpritePixelCodec.RgbaBufferLength];
            foreach (var spriteId in CollectUniqueSpriteIds(thing))
            {
                if (!sprites.TryDecodeSpriteById(spriteId, scratch))
                    throw new InvalidDataException($"Sprite {spriteId} is missing from the sprite source.");

                rgbaSprites[spriteId] = scratch.ToArray();
            }
        }

        return new ThingDocument
        {
            Thing = thing,
            ClientVersion = options.ClientVersion.Value,
            SpritesRgba = rgbaSprites,
        };
    }

    internal static HashSet<uint> CollectUniqueSpriteIds(ThingType thing)
    {
        var ids = new HashSet<uint>();
        foreach (var fg in thing.FrameGroups)
        {
            foreach (var id in fg.SpriteIds)
                ids.Add(id);
        }

        return ids;
    }
}
