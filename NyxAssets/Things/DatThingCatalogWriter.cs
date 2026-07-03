using NyxAssets.Data.Writers;

namespace NyxAssets.Things;

/// <summary>Writes a <see cref="ThingCatalog"/> in Asset Editor <c>.dat</c> binary format.</summary>
public sealed class DatThingCatalogWriter : IThingCatalogWriter
{
    public void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions formatOptions, uint? signatureOverride = null)
    {
        var extended = formatOptions.ResolveExtendedSpriteIds();
        var improved = formatOptions.ResolveImprovedAnimations();
        var outfitGroups = formatOptions.ResolveOutfitFrameGroups();

        var w = new LittleEndianStreamWriter(output);
        w.WriteU32(signatureOverride ?? catalog.DatSignature);
        w.WriteU16((ushort)catalog.ItemCount);
        w.WriteU16((ushort)catalog.OutfitCount);
        w.WriteU16((ushort)catalog.EffectCount);
        w.WriteU16((ushort)catalog.MissileCount);

        void WriteSection(uint minId, uint maxId, ThingKind kind, bool itemPath)
        {
            for (var id = minId; id <= maxId; id++)
            {
                var thing = catalog.GetExisting(kind, id);
                if (itemPath)
                    DatThingPropertySerializer.WriteItem(w, thing, catalog.DatFormat);
                else
                    DatThingPropertySerializer.WriteNonItem(w, thing, catalog.DatFormat);

                if (catalog.DatFormat <= DatThingFormat.V2_7_40__7_50)
                    ThingTextureEncoder.Write(w, thing, extended, improved, outfitGroups, includePatternZ: false);
                else
                    ThingTextureEncoder.Write(w, thing, extended, improved, outfitGroups, includePatternZ: true);
            }
        }

        WriteSection(ThingCatalog.FirstItemId, catalog.ItemCount, ThingKind.Item, itemPath: true);
        WriteSection(ThingCatalog.FirstOutfitId, catalog.OutfitCount, ThingKind.Outfit, itemPath: false);
        WriteSection(ThingCatalog.FirstEffectId, catalog.EffectCount, ThingKind.Effect, itemPath: false);
        WriteSection(ThingCatalog.FirstMissileId, catalog.MissileCount, ThingKind.Missile, itemPath: false);
    }
}
