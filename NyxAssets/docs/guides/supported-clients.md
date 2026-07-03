# Supported client data (not network protocols)

> [Documentation index](../README.md) · [Usage guide](../guides/usage.md)

NyxAssets reads and writes **binary client asset files** in the same families as **Asset Editor** / classic Nyx **`.dat`** and **`.spr`**. It does **not** implement Nyx **login / game / RSA protocol** stacks (those are separate from `.dat` / `.spr`).

## `.dat` (thing / object definitions)

**Read support** — all six metadata tiers used by Asset Editor (`ThingTypeStorage.load`):

| Tier | Client build range (Asset Editor `version.value`) |
|------|--------------------------------------------------------|
| `DatThingFormat.V1_7_10__7_30` | ≤ 730 |
| `DatThingFormat.V2_7_40__7_50` | 731 – 750 |
| `DatThingFormat.V3_7_55__7_72` | 751 – 772 |
| `DatThingFormat.V4_7_80__8_54` | 773 – 854 |
| `DatThingFormat.V5_8_60__9_86` | 855 – 986 |
| `DatThingFormat.V6_10_10__10_56` | ≥ 987 |

**Write support (`ThingCatalog.WriteDatTo`)** — all six tiers (matches `MetadataWriter1`–`MetadataWriter6` item vs non-item flag layouts). Output uses the same `DatFormat` the catalog was loaded with.

Per-build switches (same defaults as Asset Editor when you pass `ClientDataVersion`):

| Feature | Default when |
|---------|----------------|
| 32-bit sprite ids in `.dat` / extended `.spr` header | client ≥ **960** |
| Per-frame animation durations in `.dat` | client ≥ **1050** |
| Multiple outfit frame groups | client ≥ **1057** |

You can override all of these on `ClientDataReadOptions`.

## `.spr` (32×32 sprites)

- **Read / write** use the same header, **lookup table**, and **RLE blob** layout as Asset Editor (`SpriteReader` / `SpriteStorage.compile`).
- **Transparency**: `transparentSprites` / `transparentPixels` controls whether each coloured pixel in the blob includes an alpha byte (must match the client you target).

## Compile APIs

| API | Status |
|-----|--------|
| `SpriteSheetCompiler.WriteToStream` | Implemented (Asset Editor–style RGB prefix `FF 00 FF` + compressed payload). |
| `SpritePixelCodec.CompressRgba` | Implemented (inverse of `UncompressToRgba`). |
| `ThingCatalog.WriteDatTo` | **V1–V6 `.dat`**; texture block uses legacy vs modern layout by tier (same rule as read path). |

## References in this repo

- Asset Editor ActionScript: `otlib/things/ThingTypeStorage.as`, `MetadataReader*.as`, `MetadataWriter*.as`, `otlib/sprites/SpriteStorage.as`, `Sprite.as`.
- NyxAssets layout docs: [formats/dat-binary.md](../formats/dat-binary.md), [formats/spr-binary.md](../formats/spr-binary.md).
