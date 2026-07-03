---
name: gortex-nyxassets-things-3-dirs
description: "Work in the NyxAssets\Things +3 dirs area — 267 symbols across 15 files (86% cohesion)"
---

# NyxAssets\Things +3 dirs

267 symbols | 15 files | 86% cohesion

## When to Use

Use this skill when working on files in:
- `NyxAssets\Data\Readers\ThingTextureDecoder.cs`
- `NyxAssets\Data\Writers\ThingTextureEncoder.cs`
- `NyxAssets\Things\ClientDataReadOptions.cs`
- `NyxAssets\Things\ClientDataVersion.cs`
- `NyxAssets\Things\DatThingCatalogReader.cs`
- `NyxAssets\Things\DatThingCatalogWriter.cs`
- `NyxAssets\Things\DatThingFormat.cs`
- `NyxAssets\Things\IThingCatalogReader.cs`
- `NyxAssets\Things\IThingCatalogWriter.cs`
- `NyxAssets\Things\JsonThingCatalogReader.cs`
- `NyxAssets\Things\JsonThingCatalogWriter.cs`
- `NyxAssets\Things\ThingCatalog.cs`
- `NyxAssets\Things\ThingKind.cs`
- `NyxAssets\Things\ThingType.cs`
- `Sandbox\Creatures\Npc.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxAssets\Data\Readers\ThingTextureDecoder.cs` | MaxSpriteIndicesPerThing, ThingTextureDecoder |
| `NyxAssets\Data\Writers\ThingTextureEncoder.cs` | ThingTextureEncoder |
| `NyxAssets\Things\ClientDataReadOptions.cs` | ItemsDefaultFrameDurationMs, TransparentSprites, OutfitsDefaultFrameDurationMs, ResolveImprovedAnimations, ResolveExtendedSpriteIds, ... |
| `NyxAssets\Things\ClientDataVersion.cs` | ClientDataVersion |
| `NyxAssets\Things\DatThingCatalogReader.cs` | Read, options, data, DatThingCatalogReader |
| `NyxAssets\Things\DatThingCatalogWriter.cs` | DatThingCatalogWriter, catalog, Write, formatOptions, output, ... |
| `NyxAssets\Things\DatThingFormat.cs` | v, SelectFromClientVersion, V6_10_10__10_56, V1_7_10__7_30, DatThingFormat, ... |
| `NyxAssets\Things\IThingCatalogReader.cs` | IThingCatalogReader |
| `NyxAssets\Things\IThingCatalogWriter.cs` | IThingCatalogWriter |
| `NyxAssets\Things\JsonThingCatalogReader.cs` | elem, dest, kind, JsonThingCatalogReader, key, ... |
| `NyxAssets\Things\JsonThingCatalogWriter.cs` | output, Write, options, WritePropertyValue, signatureOverride, ... |
| `NyxAssets\Things\ThingCatalog.cs` | PutEffect, TryGetOutfit, ThingCatalog.<init>, _outfits, datSignatureOverride, ... |
| `NyxAssets\Things\ThingKind.cs` | Missile, ThingKind, Item, Effect, Outfit |
| `NyxAssets\Things\ThingType.cs` | IsGround, HasOffset, Writable, ClothSlot, HasElevation, ... |
| `Sandbox\Creatures\Npc.cs` | tileX, appearance, mountThing, outfitThing, Npc.<init>, ... |

## Entry Points

- `NyxAssets\Things\ThingCatalog.cs::ThingCatalog.LoadItemsXml_L245`
- `NyxAssets\Things\JsonThingCatalogReader.cs::JsonThingCatalogReader.Read`

## Connected Communities

- **NyxAssets\Things · ThingFrameGroup** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-15"
smart_context with task: "understand NyxAssets\Things +3 dirs", format: "gcx"
find_usages with id: "NyxAssets\Things\ThingCatalog.cs::ThingCatalog.LoadItemsXml_L245", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
