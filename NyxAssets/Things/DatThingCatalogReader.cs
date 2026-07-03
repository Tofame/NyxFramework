using NyxAssets.Data.Readers;

namespace NyxAssets.Things;

/// <summary>Reads Asset Editor <c>.dat</c> binary format into a <see cref="ThingCatalog"/>.</summary>
public sealed class DatThingCatalogReader : IThingCatalogReader
{
    public ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options)
    {
        if (data.Length < 12)
            throw new InvalidDataException("DAT file too small.");

        var span = data.Span;
        var format = options.ResolveDatThingFormat();
        var extendedIds = options.ResolveExtendedSpriteIds();
        var improvedAnimations = options.ResolveImprovedAnimations();
        var outfitFrameGroups = options.ResolveOutfitFrameGroups();

        var reader = new LittleEndianSpanReader(span);
        var signature = reader.ReadU32();
        var items = reader.ReadU16();
        var outfits = reader.ReadU16();
        var effects = reader.ReadU16();
        var missiles = reader.ReadU16();

        var catalog = new ThingCatalog(signature, items, outfits, effects, missiles, format);

        ThingCatalog.LoadDatSection(ref reader, format, options, extendedIds, improvedAnimations, outfitFrameGroups, ThingCatalog.FirstItemId, items, ThingKind.Item, catalog.ItemsMutable);
        ThingCatalog.LoadDatSection(ref reader, format, options, extendedIds, improvedAnimations, outfitFrameGroups, ThingCatalog.FirstOutfitId, outfits, ThingKind.Outfit, catalog.OutfitsMutable);
        ThingCatalog.LoadDatSection(ref reader, format, options, extendedIds, improvedAnimations, outfitFrameGroups, ThingCatalog.FirstEffectId, effects, ThingKind.Effect, catalog.EffectsMutable);
        ThingCatalog.LoadDatSection(ref reader, format, options, extendedIds, improvedAnimations, outfitFrameGroups, ThingCatalog.FirstMissileId, missiles, ThingKind.Missile, catalog.MissilesMutable);

        if (reader.Remaining != 0)
            throw new InvalidDataException("Extra bytes after DAT payload (wrong client version or format?).");

        catalog.InitializeFastArrays();
        return catalog;
    }
}
