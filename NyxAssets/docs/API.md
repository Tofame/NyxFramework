# NyxAssets API reference

Complete reference for every **public** type and member in the NyxAssets NuGet package (.NET 10).

> **New to NyxAssets?** Start with the package [README](../README.md) — install, quick start, common examples, and an **API at a glance** with method signatures. This document is the full reference.

For usage walkthroughs see [guides/usage.md](guides/usage.md). For on-disk formats see [formats/](formats/). For extending the library see [development/overview.md](development/overview.md).

---

## Table of contents

- [Sprite decoding (core API)](#sprite-decoding-core-api)
- [Public member index](#public-member-index)
- [Namespaces](#namespaces)
- [NyxAssets.Client](#nyxassetsclient)
- [NyxAssets.Things](#nyxassetsthings)
- [NyxAssets.Things.Exchange](#nyxassetsthingsexchange)
- [NyxAssets.Things.Frames](#nyxassetsthingsframes)
- [NyxAssets.Sprites](#nyxassetssprites)
- [NyxAssets.Utils](#nyxassetsutils)
- [Internal types (not public API)](#internal-types-not-public-api)
- [Typical call flows](#typical-call-flows)

---

## Sprite decoding (core API)

Sprite decoding is the most common operation in NyxAssets. The same three methods appear on **`ISpriteSource`**, **`SpriteArchive`**, **`AssetArchive`**, and **`ClientAssetBundle`** (which delegates to `Sprites`).

### Where to call from

| Type | When to use |
|------|-------------|
| `ClientAssetBundle` | You already loaded `.dat` + `.spr`/`.assets` together — use `bundle.DecodeSpriteById(...)` or `bundle.Sprites.DecodeSpriteById(...)`. |
| `SpriteArchive` / `AssetArchive` | You only have a sprite file, or you built a custom `ISpriteSource`. |
| `ISpriteSource` | Library/extension code that must work with any sprite backend. |

### Sprite ids are 1-based

Ids match Asset Editor / client convention: **first sprite is id `1`**, not `0`. Valid range: `1 … SpriteCount`. Id `0` always fails decode.

Sprite ids come from `ThingFrameGroup.SpriteIds`, `ThingFrameSelection.EnumerateSpriteSlots()`, or `ThingType.EnumerateSpriteIdsForOutfit()`.

### Pixel buffer layout

Every decoded legacy `.spr` sprite is **32×32 RGBA**:

| Constant | Value |
|----------|-------|
| `SpritePixelCodec.SpriteEdgeLength` | `32` |
| `SpritePixelCodec.RgbaBufferLength` | `4096` (32×32×4) |

Byte order per pixel: **`R, G, B, A`** (OpenGL `GL_RGBA`). Row-major: pixel `(x, y)` starts at index `(y * 32 + x) * 4`.

`.assets` archives may store variable-sized sprites internally, but `DecodeSpriteById` still writes into a **4096-byte** buffer for API consistency with `.spr`.

---

### `TryDecodeSpriteById`

```csharp
bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)
```

**Purpose:** Decode exactly **one** sprite by id into **your** buffer — no allocation. Preferred in hot loops.

| Aspect | Detail |
|--------|--------|
| **Parameters** | `spriteId` — 1-based index. `rgbaDestination` — must be ≥ 4096 bytes for `.spr`; for `.assets`, must fit actual pixel bytes (still typically 4096). |
| **Returns** | `true` if decoded (including **empty** sprites, which zero-fill the buffer). `false` if id out of range, corrupt blob, or decode error. |
| **Throws** | `ArgumentException` if buffer too small. `ObjectDisposedException` on disposed memory-mapped archives. |
| **Empty sprites** | Lookup address `0` or zero-length payload → buffer cleared to zeros, returns `true`. Check beforehand with `IsEmptySprite`. |
| **Performance** | Reuse one `Span<byte>` or `stackalloc byte[4096]` across many ids. Only that sprite's compressed blob is read (random access). |

**Implementations:**

- **`SpriteArchive`** — Reads lookup table offset, RLE-decompresses via `SpritePixelCodec.UncompressToRgba`. Alias: `TryCopySpriteRgba` (identical).
- **`AssetArchive`** — Decompresses ZSTD page (LRU-cached), copies raw RGBA from page payload.
- **`ClientAssetBundle`** — Forwards to `Sprites.TryDecodeSpriteById`.

```csharp
Span<byte> scratch = stackalloc byte[SpritePixelCodec.RgbaBufferLength];
if (bundle.TryDecodeSpriteById(spriteId, scratch))
{
    // use scratch — 4096 bytes RGBA
}
```

---

### `DecodeSpriteById`

```csharp
byte[] DecodeSpriteById(uint spriteId)
```

**Purpose:** Convenience wrapper — allocates `new byte[4096]`, decodes, returns the array.

| Aspect | Detail |
|--------|--------|
| **Returns** | Fresh `byte[4096]` with decoded pixels. |
| **Throws** | `InvalidDataException` if `TryDecodeSpriteById` would return `false`. Message: `"Sprite {id} is missing or invalid."` |
| **When to use** | One-off tools, prototyping, or when you need to own the array long-term. |
| **When to avoid** | Tight loops decoding hundreds of sprites — use `TryDecodeSpriteById` with a reused buffer instead. |

**Aliases** (same behavior, different names):

| Type | Alias |
|------|-------|
| `ClientAssetBundle` | `GetSpriteRgba(uint spriteId)` |
| `SpriteArchive` | `GetSpriteRgbaPixels(uint spriteId)` |

```csharp
// These are equivalent on ClientAssetBundle:
byte[] a = bundle.DecodeSpriteById(42);
byte[] b = bundle.GetSpriteRgba(42);
byte[] c = bundle.Sprites.DecodeSpriteById(42);
```

---

### `IsEmptySprite`

```csharp
bool IsEmptySprite(uint spriteId)
```

**Purpose:** Check whether a slot has no pixel data before decoding.

| Returns | Meaning |
|---------|---------|
| `true` | Id out of range, address `0`, zero-length block, or zero width/height (`.assets`). |
| `false` | Slot has compressed/raw pixel data. |

Empty slots still decode successfully via `TryDecodeSpriteById` (all-zero buffer).

---

### `SpriteCount`

```csharp
uint SpriteCount { get; }
```

Number of sprites in the archive. Valid decode ids: **`1` through `SpriteCount`** inclusive.

---

### End-to-end: thing → sprite id → pixels

```csharp
using NyxAssets.Client;
using NyxAssets.Things;
using NyxAssets.Things.Frames;
using NyxAssets.Sprites;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

using var bundle = ClientAssetBundle.OpenFromFiles("Nyx.dat", "Nyx.spr", options);

// Path A: direct id (you already know the .spr id)
byte[] pixels = bundle.DecodeSpriteById(100);

// Path B: resolve game frame → ids → decode
var item = bundle.GetItem(2148);
var frame = ThingFrameResolver.GetItemFrame(item, new ItemFrameRequest { StackCount = 5 });
Span<byte> buf = stackalloc byte[SpritePixelCodec.RgbaBufferLength];
foreach (var slot in frame.EnumerateSpriteSlots())
    bundle.TryDecodeSpriteById(slot.SpriteId, buf);
```

---

## Namespaces

| Namespace | Role |
|-----------|------|
| `NyxAssets.Client` | High-level bundle over catalog + sprite source |
| `NyxAssets.Things` | `.dat` / JSON thing definitions, catalog, options |
| `NyxAssets.Things.Frames` | Game-style frame/sprite resolution (direction, stacks, missiles) |
| `NyxAssets.Sprites` | `.spr` / `.assets` I/O, RLE codec, compilers |
| `NyxAssets.Utils` | PNG/JPEG/BMP export and spritesheet compositing |

---

## NyxAssets.Client

### `ClientAssetBundle`

High-level entry: one loaded `ThingCatalog` plus one `ISpriteSource` for the same client build. Implements `IDisposable`.

#### Properties

| Member | Description |
|--------|-------------|
| `ThingCatalog Things { get; }` | Parsed object definitions from `.dat` or JSON. |
| `ISpriteSource Sprites { get; }` | Random-access sprite archive (`.spr` or `.assets`). |

#### Constructors

| Method | Description |
|--------|-------------|
| `ClientAssetBundle(ThingCatalog things, ISpriteSource sprites, bool disposeSprites = false)` | Wraps an existing catalog and sprite source. When `disposeSprites` is `true`, `Dispose()` calls `Sprites.Dispose()`. |

#### Static factory methods

| Method | Description |
|--------|-------------|
| `Load(ReadOnlyMemory<byte> dat, ReadOnlyMemory<byte> spr, ClientDataReadOptions options)` | Loads both blobs into managed memory (`byte[]` for each). Returns a bundle that does **not** dispose the sprite source. |
| `LoadFromFiles(string datPath, string sprPath, ClientDataReadOptions options)` | Reads entire `.dat` and `.spr` files into memory, then calls `Load`. |
| `OpenFromFiles(string datPath, string sprPath, ClientDataReadOptions options)` | Reads whole `.dat` into memory; memory-maps `.spr` (no giant `byte[]` for the sheet). **Dispose** the bundle to release the map. |
| `OpenAssetsFromFiles(string datPath, string assetsPath, ClientDataReadOptions options, bool preloadPages = false)` | Loads `.dat` and opens a ZSTD page-based `.assets` archive. |
| `OpenFromFilesAuto(string datPath, string spritePath, ClientDataReadOptions options, bool preloadSprites = false)` | Chooses `SpriteArchive` or `AssetArchive` by sprite extension. **Catalog must be `.dat`** — for JSON + `.assets`, see [example 3](../README.md#3--thingsjson--assets-no-dat-no-spr). |

#### Sprite decode

Delegates to `Sprites` (`ISpriteSource`). **Full documentation:** [Sprite decoding (core API)](#sprite-decoding-core-api).

| Method | Summary |
|--------|---------|
| `bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)` | No-allocation decode into your buffer (≥ 4096 bytes). |
| `byte[] DecodeSpriteById(uint spriteId)` | Allocates and returns `byte[4096]`. Throws `InvalidDataException` on failure. |
| `byte[] GetSpriteRgba(uint spriteId)` | Alias for `DecodeSpriteById`. |

Equivalent via property: `bundle.Sprites.DecodeSpriteById(id)`.

#### Raster export (single sprite)

| Method | Description |
|--------|-------------|
| `bool TryExportSpritePng(uint spriteId, string filePath)` | Decode + write PNG. |
| `bool TryExportSpriteJpeg(uint spriteId, string filePath, int quality = 90)` | Decode + write JPEG. |
| `bool TryExportSpriteBmp(uint spriteId, string filePath)` | Decode + write BMP. |

#### Raster export (spritesheets)

| Method | Description |
|--------|-------------|
| `bool TryExportFrameGroupSpriteSheetPng(ThingFrameGroup group, string filePath)` | One frame group as Asset Editor–layout PNG. |
| `bool TryExportFrameGroupSpriteSheetJpeg(ThingFrameGroup group, string filePath, int quality = 90)` | Same, JPEG. |
| `bool TryExportFrameGroupSpriteSheetBmp(ThingFrameGroup group, string filePath)` | Same, BMP. |
| `bool TryExportThingSpriteSheetPng(ThingType thing, string filePath)` | All frame groups stacked vertically. |
| `bool TryExportThingSpriteSheetJpeg(ThingType thing, string filePath, int quality = 90)` | Same, JPEG. |
| `bool TryExportThingSpriteSheetBmp(ThingType thing, string filePath)` | Same, BMP. |

#### Catalog lookups

| Method | Description |
|--------|-------------|
| `ThingType GetItem(uint id)` | Throws `KeyNotFoundException` if missing. |
| `ThingType GetOutfit(uint id)` | Throws `KeyNotFoundException` if missing. |
| `ThingType GetEffect(uint id)` | Throws `KeyNotFoundException` if missing. |
| `ThingType GetMissile(uint id)` | Throws `KeyNotFoundException` if missing. |

#### IDisposable

| Method | Description |
|--------|-------------|
| `void Dispose()` | Disposes `Sprites` when constructed with `disposeSprites: true`. |

---

## NyxAssets.Things

### `ThingCatalog`

All object types loaded from a client `.dat` or JSON catalog.

#### Constants

| Name | Value | Meaning |
|------|-------|---------|
| `FirstItemId` | `100` | First valid item id |
| `FirstOutfitId` | `1` | First valid outfit id |
| `FirstEffectId` | `1` | First valid effect id |
| `FirstMissileId` | `1` | First valid missile id |

#### Properties

| Property | Description |
|----------|-------------|
| `uint DatSignature { get; set; }` | 4-byte signature from `.dat` header. |
| `uint ItemCount { get; }` | Inclusive last item id (not a count of defined items). |
| `uint OutfitCount { get; }` | Inclusive last outfit id. |
| `uint EffectCount { get; }` | Inclusive last effect id. |
| `uint MissileCount { get; }` | Inclusive last missile id. |
| `DatThingFormat DatFormat { get; set; }` | Which `.dat` flag decoder tier applies. |

#### Constructors

| Method | Description |
|--------|-------------|
| `ThingCatalog()` | Empty catalog for JSON load or manual construction. |

#### Static load

| Method | Description |
|--------|-------------|
| `static ThingCatalog Load(ReadOnlyMemory<byte> datFile, ClientDataReadOptions options)` | Parses binary `.dat` via `DatThingCatalogReader`. |
| `static ThingCatalog LoadJson(ReadOnlyMemory<byte> jsonData, ClientDataReadOptions options)` | Parses JSON catalog. |
| `static ThingCatalog LoadJson(string filePath, ClientDataReadOptions options)` | Reads file then `LoadJson`. |

#### Mutation (build / edit catalogs)

| Method | Description |
|--------|-------------|
| `void PutItem(ThingType thing, bool rebuildArrays = true)` | Register or replace an item. New ids must be contiguous append (`ItemCount + 1`). |
| `void PutOutfit(ThingType thing, bool rebuildArrays = true)` | Register or replace an outfit. |
| `void PutEffect(ThingType thing, bool rebuildArrays = true)` | Register or replace an effect. |
| `void PutMissile(ThingType thing, bool rebuildArrays = true)` | Register or replace a missile. |

#### Lookup

| Method | Description |
|--------|-------------|
| `ThingType? TryGetItem(uint id)` | Returns `null` if id is out of range or undefined. |
| `ThingType GetItem(uint id)` | Throws `KeyNotFoundException` if missing. |
| `ThingType? TryGetOutfit(uint id)` | Returns `null` if missing. |
| `ThingType GetOutfit(uint id)` | Throws if missing. |
| `ThingType? TryGetEffect(uint id)` | Returns `null` if missing. |
| `ThingType GetEffect(uint id)` | Throws if missing. |
| `ThingType? TryGetMissile(uint id)` | Returns `null` if missing. |
| `ThingType GetMissile(uint id)` | Throws if missing. |
| `(int RedrawW, int RedrawH) GetMaxLyingItemRedrawSpan()` | Max `(width−1, height−1)` among `IsLyingObject` items; cached after first call. |

#### Enumeration

| Method | Description |
|--------|-------------|
| `IEnumerable<ThingType> EnumerateItems()` | Defined items from `FirstItemId` … `ItemCount`. |
| `IEnumerable<ThingType> EnumerateOutfits()` | Defined outfits. |
| `IEnumerable<ThingType> EnumerateEffects()` | Defined effects. |
| `IEnumerable<ThingType> EnumerateMissiles()` | Defined missiles. |

#### Export / merge

| Method | Description |
|--------|-------------|
| `void WriteDatTo(Stream output, ClientDataReadOptions formatOptions, uint? datSignatureOverride = null)` | Writes binary `.dat`. |
| `void ExportJson(Stream output, ClientDataReadOptions options, uint? signatureOverride = null, string? itemsXmlPath = null)` | Writes JSON; optionally merges `items.xml` first. |
| `void ExportJson(string filePath, ClientDataReadOptions options, uint? signatureOverride = null, string? itemsXmlPath = null)` | File convenience wrapper. |
| `void LoadItemsXml(string filePath)` | Merges TFS-style `items.xml` into `ExtraProperties`. |
| `void LoadItemsXml(Stream input)` | Same from stream. |

---

### `ThingType`

One object definition: metadata flags + frame groups + sprite layout.

#### Identity

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `uint` | Client id in its section |
| `Kind` | `ThingKind` | Item, outfit, effect, or missile |

#### Item / tile flags

| Property | Description |
|----------|-------------|
| `IsGround` | Ground tile |
| `GroundSpeed` | Movement speed when ground |
| `IsGroundBorder` | Ground border decoration |
| `IsOnBottom` | Drawn on bottom stack |
| `IsOnTop` | Drawn on top stack |
| `IsContainer` | Container item |
| `Stackable` | Stack count affects pattern (4×2 grid) |
| `ForceUse` | Force use action |
| `MultiUse` | Multi-use item |
| `HasCharges` | Has charges |
| `Writable` | Writable (sign, book, …) |
| `WritableOnce` | Writable once |
| `MaxTextLength` | Max text length when writable |
| `IsFluidContainer` | Fluid container |
| `IsFluid` | Fluid (single tile) |
| `IsUnpassable` | Blocks movement |
| `IsUnmoveable` | Cannot be moved |
| `BlockMissile` | Blocks missiles |
| `BlockPathfind` | Blocks pathfinding |
| `NoMoveAnimation` | No move animation |
| `Pickupable` | Can be picked up |
| `Hangable` | Can hang on walls |
| `IsVertical` | Vertical hangable |
| `IsHorizontal` | Horizontal hangable |
| `Rotatable` | Rotatable (pattern X) |
| `HasLight` | Emits light |
| `LightLevel` | Light intensity |
| `LightColor` | Light color index |
| `DontHide` | Don't hide when behind |
| `IsTranslucent` | Translucent rendering |
| `FloorChange` | Floor change tile |
| `HasOffset` | Sprite draw offset |
| `OffsetX` | Draw offset X |
| `OffsetY` | Draw offset Y |
| `HasElevation` | Elevation offset |
| `Elevation` | Elevation value |
| `IsLyingObject` | Lying object (corpses, …) |
| `AnimateAlways` | Always animates |
| `MiniMap` | Shows on minimap |
| `MiniMapColor` | Minimap color |
| `IsLensHelp` | Lens help flag |
| `LensHelp` | Lens help id |
| `IsFullGround` | Full ground tile |
| `IgnoreLook` | Ignore look description |
| `Cloth` | Equipment slot item |
| `ClothSlot` | Slot index |
| `IsMarketItem` | Market item |
| `MarketName` | Market display name |
| `MarketCategory` | Market category |
| `MarketTradeAs` | Trade-as id |
| `MarketShowAs` | Show-as id |
| `MarketRestrictProfession` | Profession restriction |
| `MarketRestrictLevel` | Level restriction |
| `HasDefaultAction` | Default action set |
| `DefaultAction` | Default action id |
| `Wrappable` | Can be wrapped |
| `Unwrappable` | Can be unwrapped |
| `BottomEffect` | Bottom effect layer |
| `DontCenterOutfit` | Don't center outfit sprite |
| `Usable` | Usable item |

#### Collections

| Property | Description |
|----------|-------------|
| `List<ThingFrameGroup> FrameGroups { get; }` | One or more frame groups (idle/walking for outfits). |
| `Dictionary<string, string> ExtraProperties { get; }` | Game-logic / JSON / items.xml properties. |

#### Methods

| Method | Description |
|--------|-------------|
| `ThingFrameGroup? GetFrameGroup(int index)` | Returns group or `null`. |
| `IEnumerable<uint> EnumerateSpriteIdsForOutfit(...)` | Outfit-only: enumerates `.spr` ids for a slice of the layout tensor. Optional args pin one axis (`null` = all). Params: `innerWidth`, `innerHeight`, `layer`, `patternX`, `patternY`, `patternZ`, `frame`, `frameGroupIndex` (default 0). |
| `uint[] GetSpriteIdsForOutfit(...)` | Array form of `EnumerateSpriteIdsForOutfit`. |

---

### `ThingFrameGroup`

One frame group: dimensions, patterns, animation, and flat `SpriteIds` list (Asset Editor `FrameGroup`).

#### Properties

| Property | Default | Description |
|----------|---------|-------------|
| `GroupTypeId` | — | Frame group type id (outfits) |
| `Width` | `1` | Inner tile width (32 px cells) |
| `Height` | `1` | Inner tile height |
| `ExactSize` | `32` | Exact pixel size hint |
| `Layers` | `1` | Layer count |
| `PatternX` | `1` | Pattern axis X (often direction) |
| `PatternY` | `1` | Pattern axis Y (often addons) |
| `PatternZ` | `1` | Pattern axis Z (often mount) |
| `Frames` | `1` | Animation frame count |
| `IsAnimation` | — | Has animation timings |
| `AnimationMode` | — | Animation mode byte |
| `LoopCount` | — | Loop count |
| `StartFrame` | — | Start frame index |
| `FrameTimings` | — | Per-frame min/max ms |
| `SpriteIds` | `[]` | Flat sprite id list |

#### Layout helpers

| Method | Description |
|--------|-------------|
| `uint GetTotalSpriteSlots()` | `Width × Height × PatternX × PatternY × PatternZ × Frames × Layers` |
| `uint GetSpriteSheetTextureColumns()` | Asset Editor `getTotalX` |
| `uint GetSpriteSheetTextureRows()` | Asset Editor `getTotalY` |
| `uint GetSpriteSheetTextureCount()` | Asset Editor `getTotalTextures` |

#### Index / id resolution

| Method | Description |
|--------|-------------|
| `uint GetSpriteIndex(uint innerWidth, uint innerHeight, uint layer, uint patternX, uint patternY, uint patternZ, uint frame)` | Flat index into `SpriteIds`. |
| `uint GetSpriteIndex(uint layer, uint patternX, uint patternY, uint patternZ, uint frame)` | 1×1 inner cell shortcut. |
| `uint GetTextureIndex(uint layer, uint patternX, uint patternY, uint patternZ, uint frame)` | Texture-slot index (no inner W/H walk). |
| `uint GetSpriteId(...)` | `SpriteIds[GetSpriteIndex(...)]`; throws if out of range. |
| `uint GetSpriteId(uint layer, ...)` | 1×1 shortcut. |
| `bool TryGetSpriteId(..., out uint spriteId)` | Non-throwing variant. |
| `bool TryGetSpriteId(uint layer, ..., out uint spriteId)` | 1×1 shortcut. |
| `IEnumerable<uint> EnumerateSpriteIds(...)` | All ids matching optional pin filters (`null` = iterate axis). |
| `uint[] GetSpriteIds(...)` | Materialized array from `EnumerateSpriteIds`. |

---

### `AnimationFrameTiming`

| Member | Description |
|--------|-------------|
| `AnimationFrameTiming(uint minimumMilliseconds, uint maximumMilliseconds)` | Constructor. |
| `uint MinimumMilliseconds { get; }` | Min frame duration. |
| `uint MaximumMilliseconds { get; }` | Max frame duration. |

---

### `ThingKind` (enum)

| Value | Meaning |
|-------|---------|
| `Item = 1` | Item section |
| `Outfit = 2` | Outfit section |
| `Effect = 3` | Effect section |
| `Missile = 4` | Missile / distance effect section |

---

### `ClientDataVersion`

| Member | Description |
|--------|-------------|
| `ClientDataVersion(uint Value)` | Record struct; e.g. `1098` → client 10.98. |
| `uint Value { get; }` | Raw build number. |

---

### `ClientDataReadOptions`

How to interpret a `.dat` / `.spr` pair for a given client build.

#### Properties

| Property | Description |
|----------|-------------|
| `ClientDataVersion ClientVersion { get; init; }` | **Required.** Client build number. |
| `bool? ExtendedSpriteIds { get; init; }` | When null, derived from client version (≥ 960). |
| `bool? ImprovedAnimations { get; init; }` | When null, derived (≥ 1050). |
| `bool? OutfitFrameGroups { get; init; }` | When null, derived (≥ 1057). |
| `bool TransparentSprites { get; init; }` | `.spr` payloads include alpha byte per pixel. |
| `uint ItemsDefaultFrameDurationMs { get; init; }` | Default `150`. |
| `uint OutfitsDefaultFrameDurationMs { get; init; }` | Default `300`. |
| `uint EffectsDefaultFrameDurationMs { get; init; }` | Default `100`. |
| `DatThingFormat? DatThingFormatOverride { get; init; }` | Override `.dat` tier selection. |

#### Resolve methods

| Method | Description |
|--------|-------------|
| `bool ResolveExtendedSpriteIds()` | Effective extended sprite ids flag. |
| `bool ResolveImprovedAnimations()` | Effective improved animations flag. |
| `bool ResolveOutfitFrameGroups()` | Effective outfit frame groups flag. |
| `DatThingFormat ResolveDatThingFormat()` | Effective `.dat` format tier. |
| `uint ResolveDefaultFrameDurationMs(ThingKind kind)` | Default animation ms for kind when `.dat` omits timings. |

---

### `DatThingFormat` (enum)

| Value | Client range (approx.) |
|-------|------------------------|
| `V1_7_10__7_30 = 1` | ≤ 7.30 |
| `V2_7_40__7_50 = 2` | 7.40 – 7.50 |
| `V3_7_55__7_72 = 3` | 7.55 – 7.72 |
| `V4_7_80__8_54 = 4` | 7.80 – 8.54 |
| `V5_8_60__9_86 = 5` | 8.60 – 9.86 |
| `V6_10_10__10_56 = 6` | ≥ 10.10 |

---

### `DatThingFormatRules` (static)

| Method | Description |
|--------|-------------|
| `DatThingFormat SelectFromClientVersion(ClientDataVersion v)` | Maps build number → format tier. |
| `bool UsesExtendedSpriteIdsByDefault(ClientDataVersion v)` | `v.Value >= 960`. |
| `bool UsesImprovedAnimationsByDefault(ClientDataVersion v)` | `v.Value >= 1050`. |
| `bool UsesOutfitFrameGroupsByDefault(ClientDataVersion v)` | `v.Value >= 1057`. |

---

### `IThingCatalogReader`

| Method | Description |
|--------|-------------|
| `ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options)` | Parse bytes → catalog. |

**Implementations:** `DatThingCatalogReader`, `JsonThingCatalogReader`.

---

### `DatThingCatalogReader`

| Method | Description |
|--------|-------------|
| `ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options)` | Parses Asset Editor `.dat` binary layout. |

---

### `JsonThingCatalogReader`

| Method | Description |
|--------|-------------|
| `ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options)` | Parses JSON catalog (`items`, `outfits`, `effects`, `missiles` arrays). |

---

### `IThingCatalogWriter`

| Method | Description |
|--------|-------------|
| `void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions options, uint? signatureOverride = null)` | Serialize catalog. |

**Implementations:** `DatThingCatalogWriter`, `JsonThingCatalogWriter`.

---

### `DatThingCatalogWriter`

| Method | Description |
|--------|-------------|
| `void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions formatOptions, uint? signatureOverride = null)` | Writes binary `.dat` for catalog's `DatFormat`. |

---

### `JsonThingCatalogWriter`

| Method | Description |
|--------|-------------|
| `void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions options, uint? signatureOverride = null)` | Writes indented JSON (signature param unused). |

---

### `ItemsXmlMerger` (static)

| Method | Description |
|--------|-------------|
| `void MergeFromFile(ThingCatalog catalog, string filePath)` | Loads TFS-style `items.xml` and merges attributes into matching items' `ExtraProperties`. |
| `void Merge(ThingCatalog catalog, Stream input)` | Same from stream. |

---

## NyxAssets.Things.Exchange

Single-thing import/export: one item, outfit, effect, or missile with optional embedded 32×32 RGBA sprite pixels. Supports **nyx-thing JSON** and Object Builder **`.obd`**.

→ Full guide: [development/thing-exchange.md](development/thing-exchange.md)  
→ Formats: [formats/nyx-thing-json.md](formats/nyx-thing-json.md), [formats/obd-binary.md](formats/obd-binary.md)

### `ThingDocument`

| Member | Description |
|--------|-------------|
| `ThingType Thing { get; init; }` | Metadata, frame groups, `ExtraProperties`. |
| `uint ClientVersion { get; init; }` | Client build (default `1098`). |
| `ushort ObdVersion { get; init; }` | `0` for JSON; `100`/`200`/`300` when from/to OBD. |
| `Dictionary<uint, byte[]>? SpritesRgba { get; init; }` | Decoded 4096-byte RGBA per sprite id. |
| `ThingKind Kind => Thing.Kind` | Item / outfit / effect / missile. |
| `IEnumerable<uint> EnumerateSpriteIds()` | All sprite ids in frame groups. |
| `void ImportInto(ThingCatalog catalog, uint? assignId = null)` | `Put*` into catalog; optional id override (needed for OBD). |
| `static ThingDocument FromThing(ThingType thing, uint clientVersion = 1098)` | Metadata-only document. |
| `static ThingDocument FromThing(ThingType thing, ISpriteSource sprites, ClientDataReadOptions options, bool embedSprites = true)` | Resolves pixels from sprite source. |

### `ThingDocumentJsonCodec` (static)

| Constant / method | Description |
|-------------------|-------------|
| `FormatName` | `"nyx-thing"` |
| `FormatVersion` | `1` |
| `Read(ReadOnlyMemory<byte> json)` / `Read(string filePath)` / `Read(JsonElement root)` | Parse JSON → `ThingDocument`. Requires `type`. |
| `Write(ThingDocument document, Stream output, bool indent = true, bool includeSprites = true)` | Write JSON. |
| `Write(string filePath, ThingDocument document, …)` | Write file. |
| `Write(ThingDocument document, Utf8JsonWriter writer, bool includeSprites = true)` | Write to existing writer. |
| `FromObd(ReadOnlyMemory<byte> obdBytes, ClientDataReadOptions? options = null)` | OBD bytes → document. |
| `ToObd(ThingDocument document, ClientDataReadOptions options, ushort? obdVersion = null)` | Document → OBD bytes. |

### `ObdThingCodec` (static)

| Method | Description |
|--------|-------------|
| `Read(ReadOnlyMemory<byte> obdBytes, ClientDataReadOptions? options = null)` | LZMA-decompress and parse OBD v1–v3. |
| `Read(string filePath, ClientDataReadOptions? options = null)` | Read file. |
| `Write(ThingDocument document, ClientDataReadOptions options, ushort? obdVersion = null)` | Requires `SpritesRgba`; returns LZMA-compressed bytes. |
| `Write(string filePath, ThingDocument document, ClientDataReadOptions options, ushort? obdVersion = null)` | Write file. |

### `ObdVersions` (static)

| Constant | Value |
|----------|-------|
| `Version1` | `100` |
| `Version2` | `200` |
| `Version3` | `300` |
| `IsSupported(ushort version)` | True for 100/200/300. |

### `ThingKindNames` (static)

| Method | Description |
|--------|-------------|
| `ToName(ThingKind kind)` | `"item"`, `"outfit"`, `"effect"`, `"missile"`. |
| `FromName(string name)` | Parse type string (case-insensitive). |

### `ThingJsonCodec` (static, namespace `NyxAssets.Things`)

| Method | Description |
|--------|-------------|
| `Read(JsonElement elem, ThingKind kind)` | Parse one thing object (no envelope). |
| `Write(Utf8JsonWriter writer, ThingType thing)` | Write thing fields into current JSON object. |

---

## NyxAssets.Things.Frames

Frame resolution turns **game/editor parameters** (which way a creature faces, how many coins are stacked, where a missile is aimed) into a **`ThingFrameSelection`**: the exact frame group, pattern axes, and animation frame inside a `ThingType`. From there you call `EnumerateSpriteSlots()` to get 1-based `.spr` ids and decode pixels.

```
OutfitFrameRequest  ──►  ThingFrameResolver.GetOutfitFrame(...)
ItemFrameRequest    ──►  ThingFrameResolver.GetItemFrame(...)
EffectFrameRequest  ──►  ThingFrameResolver.GetEffectFrame(...)
MissileFrameRequest ──►  ThingFrameResolver.GetMissileFrame(...)
                              │
                              ▼
                     ThingFrameSelection
                              │
                              ▼
              EnumerateSpriteSlots() → SpriteId per layer/cell
                              │
                              ▼
              ISpriteSource.TryDecodeSpriteById(spriteId, rgba)
```

Request structs are **immutable inputs** (`init` properties). They carry no behavior — all logic lives on `ThingFrameResolver`. Defaults are set by parameterless constructors so `new OutfitFrameRequest()` is valid.

See also [development/frame-resolver.md](development/frame-resolver.md) for NyxDrawer parity notes.

---

### `Direction4` (enum)

Four-way creature facing used by outfits and mounts. Values match NyxClient / OTC convention:

| Value | Int | Facing |
|-------|-----|--------|
| `North` | 0 | Up |
| `East` | 1 | Right |
| `South` | 2 | Down (default in `OutfitFrameRequest`) |
| `West` | 3 | Left |

Maps to **`PatternX`** on outfit frame groups that have `PatternX == 4`. Pass `(int)Direction4.South` as `OutfitFrameRequest.Direction`.

---

### `Direction8` (enum)

Eight-way aim for missiles / distance effects (`NyxDirection` in OTC):

| Value | Int |
|-------|-----|
| `North` | 0 |
| `East` | 1 |
| `South` | 2 |
| `West` | 3 |
| `NorthEast` | 4 |
| `SouthEast` | 5 |
| `SouthWest` | 6 |
| `NorthWest` | 7 |

Used by `MissileFrameRequest.Direction` or derived from tile delta via `MissileDirectionPatterns.DirectionFromTileDelta`.

---

### `OutfitFrameRequest` (struct)

**Purpose:** Describes *which slice* of an outfit (or mount) you want — standing vs walking, facing, visible addon layers, and mounted posture.

**Used by:**

- `ThingFrameResolver.GetOutfitFrame`
- `ThingFrameResolver.EnumerateOutfitAddonFrames`
- `ThingFrameResolver.GetMountFrame`
- `ThingFrameResolver.EnumerateMountedOutfitFrames`

**Defaults** (parameterless constructor): `Direction = South`, `AddonMask = 0xFF` (all addons), `FrameGroupIndex = -1` (auto), `WalkPhase = 0`, `Mounted = false`.

#### Properties

| Property | Type | Default | What it controls |
|----------|------|---------|------------------|
| `Direction` | `int` | `2` (South) | Creature facing 0–3 → normalized into `PatternX` via `% PatternX`. Use `Direction4` enum values. |
| `WalkPhase` | `uint` | `0` | **`0`** = idle/standing. **`> 0`** = walking animation step. When the outfit has two frame groups (idle + walk), `WalkPhase > 0` selects group 1 and picks frame `(WalkPhase - 1) % Frames`. With one group, walk phases map to frames 1…`Frames−1`. |
| `AddonMask` | `byte` | `0xFF` | Bitmask of **addon pattern rows** (`PatternY`). Base body (`PatternY = 0`) is **always** drawn. Bit 0 → pattern Y 1, bit 1 → Y 2, etc. `0xFF` = all addons. `0b00000011` = base + first two addon rows only. Only affects `EnumerateOutfitAddonFrames` / mounted enumeration — `GetOutfitFrame` always uses `PatternY = 0`. |
| `Mounted` | `bool` | `false` | When `true` and the frame group has `PatternZ > 1`, sets `PatternZ = min(1, PatternZ − 1)` (mounted sprite row). Matches NyxClient mount drawing. |
| `FrameGroupIndex` | `int` | `-1` | **`-1`** = resolver picks idle vs walking from `WalkPhase`. **`≥ 0`** = force that index in `ThingType.FrameGroups`; `WalkPhase` is then `% Frames` within that group. |

#### Example — standing south, all addons, mounted

```csharp
var request = new OutfitFrameRequest
{
    Direction = (int)Direction4.South,
    WalkPhase = 0,
    AddonMask = 0xFF,
    Mounted = true,
};

var selection = ThingFrameResolver.GetOutfitFrame(outfit, request);
// selection.PatternX = south, PatternY = 0, PatternZ = mounted row, Frame = 0

foreach (var slot in selection.EnumerateSpriteSlots())
    bundle.TryDecodeSpriteById(slot.SpriteId, rgba);
```

#### Example — walking, preview each addon layer separately

```csharp
var walkRequest = new OutfitFrameRequest { Direction = (int)Direction4.East, WalkPhase = 3 };

foreach (var slice in ThingFrameResolver.EnumerateOutfitAddonFrames(outfit, walkRequest))
{
    // slice.PatternY = 0 (base), 1 (first addon), … — one selection per visible addon row
}
```

#### Example — mount + rider together

```csharp
foreach (var (thing, slice, isMount) in ThingFrameResolver.EnumerateMountedOutfitFrames(outfit, mount, walkRequest))
{
    // isMount == true entries are mount layers (drawn first in NyxDrawer)
}
```

---

### `ItemFrameRequest` (struct)

**Purpose:** Describes which **item tile sprite** to show — stack pile appearance, rotation, or animation frame.

**Used by:** `ThingFrameResolver.GetItemFrame`

**Defaults:** `StackCount = 1`, `Frame = 0`, patterns null (auto).

#### Properties

| Property | Type | Default | What it controls |
|----------|------|---------|------------------|
| `Frame` | `uint` | `0` | Animation frame index (`% Frames`). Used for animated items. |
| `StackCount` | `int` | `1` | For **stackable** items with a **4×2 pattern grid** (`PatternX=4`, `PatternY=2`), maps count → pile sprite via `ItemStackPatterns.Resolve`. Counts 1–4 use row 0; 5+ use row 1 with column by bracket (5–9, 10–24, 25–49, 50+). Ignored when `PatternX` is set. |
| `PatternX` | `uint?` | `null` | When set, **overrides** stack resolution (e.g. `Rotatable` items: 0=N, 1=E, 2=S, 3=W). Takes precedence over `StackCount`. |
| `PatternY` | `uint?` | `null` | Explicit pattern Y override. When null with null `PatternX`, stack rules or `0` apply. |
| `PatternZ` | `uint` | `0` | Third pattern axis (rare on items; passed through to selection). |

#### Stack pile mapping (when 4×2 grid + stackable)

| Stack count | PatternX | PatternY |
|-------------|----------|----------|
| 1 | 0 | 0 |
| 2 | 1 | 0 |
| 3 | 2 | 0 |
| 4 | 3 | 0 |
| 5–9 | 0 | 1 |
| 10–24 | 1 | 1 |
| 25–49 | 2 | 1 |
| 50+ | 3 | 1 |

#### Example — gold pile by count

```csharp
var selection = ThingFrameResolver.GetItemFrame(coinItem, new ItemFrameRequest { StackCount = 37 });
// → patternX = 2, patternY = 1 (medium-large pile)
```

#### Example — rotatable item (explicit pattern)

```csharp
var selection = ThingFrameResolver.GetItemFrame(chair, new ItemFrameRequest { PatternX = 2, PatternY = 0 });
// StackCount ignored when PatternX is set
```

---

### `EffectFrameRequest` (struct)

**Purpose:** Describes a **magic effect** frame — which animation step and which tile-dependent pattern variant.

**Used by:** `ThingFrameResolver.GetEffectFrame`

There is no parameterless constructor; unset fields default to `0`.

#### Properties

| Property | Type | Default | What it controls |
|----------|------|---------|------------------|
| `Frame` | `uint` | `0` | Animation frame (`% Frames`). Often computed first with `GetEffectFrameIndex(effect, elapsedMs)` (default 75 ms per tick). |
| `TileX` | `int` | `0` | World tile X → `PatternX = PositiveMod(TileX, frameGroup.PatternX)`. Effects with multiple pattern columns vary visually by map position. |
| `TileY` | `int` | `0` | World tile Y → `PatternY = PositiveMod(TileY, frameGroup.PatternY)`. |

#### Example — timed + tile-varied effect

```csharp
var frame = ThingFrameResolver.GetEffectFrameIndex(effect, elapsedMs, ticksPerFrame: 75);
var selection = ThingFrameResolver.GetEffectFrame(effect, new EffectFrameRequest
{
    Frame = frame,
    TileX = casterTileX,
    TileY = casterTileY,
});
```

For **`AnimateAlways`** items/effects, use `GetCyclicFrameIndex(thing, elapsedMs)` instead of effect-specific timing.

---

### `MissileFrameRequest` (struct)

**Purpose:** Describes **missile / distance effect** aim — which of the 8 directional sprites to use.

**Used by:** `ThingFrameResolver.GetMissileFrame`

#### Properties

| Property | Type | Default | What it controls |
|----------|------|---------|------------------|
| `Direction` | `Direction8?` | `null` | When set, maps directly to `(PatternX, PatternY)` via `MissileDirectionPatterns.GetPattern`. |
| `TileDeltaX` | `int` | `0` | When `Direction` is null, combined with `TileDeltaY` to derive eight-way aim (`DirectionFromTileDelta`). Positive X = east. |
| `TileDeltaY` | `int` | `0` | Tile delta from source to target. Positive Y = south in world coords; angle uses `-dy` internally (screen-up = north). |

Missile selections always use **`Frame = 0`** (single-frame missiles).

#### Pattern grid (3×3 layout in `.dat`)

```
 NW(0,0)  N(1,0)  NE(2,0)
 W(0,1)   ·       E(2,1)
 SW(0,2)  S(1,2)  SE(2,2)
```

#### Example — explicit direction

```csharp
var selection = ThingFrameResolver.GetMissileFrame(fireball, new MissileFrameRequest
{
    Direction = Direction8.NorthEast,
});
```

#### Example — aim from caster to target tile

```csharp
var selection = ThingFrameResolver.GetMissileFrame(fireball, new MissileFrameRequest
{
    TileDeltaX = targetX - sourceX,
    TileDeltaY = targetY - sourceY,
});

// Optional: travel duration for animation
var durationMs = MissileDirectionPatterns.DurationMsFromTileDelta(dx, dy); // 150 * sqrt(dx² + dy²)
```

---

### `ThingFrameSelection` (struct)

**Purpose:** The **output** of frame resolution — a resolved slice into one `ThingFrameGroup`. Read-only snapshot; use `with` expressions to copy with changes (e.g. addon enumeration sets `PatternY`).

**Produced by:** all `ThingFrameResolver.Get*Frame` methods.

#### Properties

| Property | Description |
|----------|-------------|
| `FrameGroup` | The active `ThingFrameGroup` (dimensions, patterns, `SpriteIds`). |
| `FrameGroupIndex` | Index into `ThingType.FrameGroups` (0 = idle, 1 = walk for dual-group outfits). |
| `PatternX` | Resolved first pattern axis (direction, missile aim column, stack/rotation, …). |
| `PatternY` | Resolved second pattern axis (addon row, stack row, …). |
| `PatternZ` | Resolved third pattern axis (mount row, …). |
| `Frame` | Resolved animation frame index. |

#### Nested `SpriteSlot`

One drawable 32×32 cell within the selection (multi-tile items and multi-layer outfits yield multiple slots).

| Property | Description |
|----------|-------------|
| `InnerWidth` | Inner tile X index (0 … `Width−1`). |
| `InnerHeight` | Inner tile Y index. |
| `Layer` | Layer index (0 … `Layers−1`; outfit addons often use separate layers). |
| `SpriteId` | 1-based id into `.spr` / `.assets`. |

#### Methods

| Method | Description |
|--------|-------------|
| `IEnumerable<SpriteSlot> EnumerateSpriteSlots()` | Walks inner W×H × layers; skips missing/zero sprite ids. **Primary API** for turning a selection into decode calls. |
| `uint[] GetSpriteIds()` | Flat array of all non-zero sprite ids (order matches enumeration). |

#### Example — selection to pixels

```csharp
var selection = ThingFrameResolver.GetOutfitFrame(outfit, new OutfitFrameRequest { Direction = (int)Direction4.North });
Span<byte> buf = stackalloc byte[4096];

foreach (var slot in selection.EnumerateSpriteSlots())
{
    bundle.TryDecodeSpriteById(slot.SpriteId, buf);
    // slot.Layer, slot.InnerWidth/Height tell you compositing order within the thing
}
```

---

### `ThingFrameResolver` (static)

High-level frame/sprite resolution (mirrors NyxClient / NyxDrawer conventions).

| Method | Description |
|--------|-------------|
| `uint NormalizeDirection(int direction, uint patternCount)` | Maps direction into `0 … patternCount−1`. |
| `uint PositiveMod(int value, uint count)` | Positive modulo for tile coords. |
| `bool IsAddonPatternVisible(int patternY, byte addonMask)` | Pattern Y=0 always visible; Y≥1 needs addon bit. |
| `uint GetMountedPatternZ(ThingFrameGroup frameGroup, bool mounted)` | `min(1, PatternZ−1)` when mounted. |
| `void ResolveWalkingFrame(ThingType outfit, uint walkPhase, out ThingFrameGroup frameGroup, out uint frame, out int frameGroupIndex)` | Idle vs walking group + frame. |
| `ThingFrameSelection GetOutfitFrame(ThingType outfit, OutfitFrameRequest request = default)` | One outfit slice. |
| `IEnumerable<ThingFrameSelection> EnumerateOutfitAddonFrames(ThingType outfit, OutfitFrameRequest request = default)` | One selection per visible addon row. |
| `ThingFrameSelection GetMountFrame(ThingType mount, OutfitFrameRequest request = default)` | Mount frame for same walk phase. |
| `IEnumerable<(ThingType Thing, ThingFrameSelection Selection, bool IsMount)> EnumerateMountedOutfitFrames(ThingType outfit, ThingType? mount, OutfitFrameRequest request = default)` | Mount layers then rider addon slices. |
| `ThingFrameSelection GetItemFrame(ThingType item, ItemFrameRequest request = default)` | Item patterns (stack grid or overrides). |
| `ThingFrameSelection GetEffectFrame(ThingType effect, EffectFrameRequest request = default)` | Effect frame + tile patterns. |
| `uint GetEffectFrameIndex(ThingType effect, float elapsedMs, int ticksPerFrame = 75)` | Animation frame from elapsed time. |
| `uint GetCyclicFrameIndex(ThingType thing, float elapsedMs, int ticksPerFrame = 333)` | Cyclic animation for `AnimateAlways`. |
| `ThingFrameSelection GetMissileFrame(ThingType missile, MissileFrameRequest request = default)` | Missile aim → pattern X/Y. |

---

### `ItemStackPatterns` (static)

NyxClient stack pile grid (4×2 patterns for stackable items).

| Method | Description |
|--------|-------------|
| `bool UsesStackCountGrid(ThingFrameGroup frameGroup, bool stackable)` | True when stackable and 4×2 patterns. |
| `bool UsesStackCountGrid(uint patternXCount, uint patternYCount, bool stackable)` | Overload with raw counts. |
| `void Resolve(ThingFrameGroup frameGroup, bool stackable, int count, out uint patternX, out uint patternY)` | Maps stack count → pattern coords. |
| `void Resolve(uint patternXCount, uint patternYCount, bool stackable, int count, out uint patternX, out uint patternY)` | Overload with raw counts. |

---

### `MissileDirectionPatterns` (static)

| Method | Description |
|--------|-------------|
| `(uint PatternX, uint PatternY) GetPattern(Direction8 direction)` | Eight-way → pattern coords. |
| `(uint PatternX, uint PatternY) GetPattern(int direction8)` | Raw 0–7 index. |
| `Direction8 DirectionFromTileDelta(int dx, int dy)` | Tile delta → eight-way direction (45° sectors). |
| `float DurationMsFromTileDelta(int dx, int dy)` | Travel duration: `150 × sqrt(dx² + dy²)`. |

---

## NyxAssets.Sprites

### `ISpriteSource`

Format-agnostic contract for random-access sprite decoding. Extends `IDisposable`.

**Implementations:** `SpriteArchive` (`.spr`), `AssetArchive` (`.assets`).

#### Members

| Member | Summary |
|--------|---------|
| `uint SpriteCount { get; }` | Valid ids: `1 … SpriteCount`. |
| `bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)` | See [TryDecodeSpriteById](#trydecodespritebyid). |
| `byte[] DecodeSpriteById(uint spriteId)` | See [DecodeSpriteById](#decodespritebyid). |
| `bool IsEmptySprite(uint spriteId)` | See [IsEmptySprite](#isemptysprite). |
| `void Dispose()` | Release memory-mapped file handles (`SpriteArchive`, `AssetArchive` when opened with `OpenReadOnlyFile`). No-op for in-memory `Load`. |

---

### `SpriteArchive`

Random access to legacy `.spr` (Asset Editor `SpriteReader`).

#### Constants / properties

| Member | Description |
|--------|-------------|
| `uint Signature { get; }` | File signature. |
| `uint SpriteCount { get; }` | Sprite count from header. |
| `bool UsesExtendedSpriteIds { get; }` | 32-bit vs 16-bit sprite count header. |
| `bool TransparentPixels { get; }` | RLE includes alpha channel. |
| `bool IsMemoryMapped { get; }` | True when opened via `OpenReadOnlyFile`. |

#### Static load

| Method | Description |
|--------|-------------|
| `static SpriteArchive Load(ReadOnlyMemory<byte> sprFile, ClientDataReadOptions options, bool preloadSprites = false)` | Full file in memory. |
| `static SpriteArchive Load(ReadOnlyMemory<byte> sprFile, bool extendedSpriteIds, bool transparentPixels, bool preloadSprites = false)` | Explicit header flags. |
| `static SpriteArchive OpenReadOnlyFile(string sprPath, ClientDataReadOptions options, bool preloadSprites = false)` | Memory-mapped file. |
| `static SpriteArchive OpenReadOnlyFile(string sprPath, bool extendedSpriteIds, bool transparentPixels, bool preloadSprites = false)` | Explicit flags. |

#### Instance methods — decode

Same semantics as [Sprite decoding (core API)](#sprite-decoding-core-api).

| Method | Notes |
|--------|-------|
| `bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)` | RLE decode from `.spr` lookup table. |
| `bool TryCopySpriteRgba(uint spriteId, Span<byte> rgbaDestination)` | **Identical** to `TryDecodeSpriteById`. |
| `byte[] DecodeSpriteById(uint spriteId)` | Allocates 4096 bytes. |
| `byte[] GetSpriteRgbaPixels(uint spriteId)` | Alias for `DecodeSpriteById`. |
| `bool IsEmptySprite(uint spriteId)` | Lookup address `0` or zero-length payload. |
| `void Dispose()` | Releases memory map when `IsMemoryMapped == true`. |

---

### `AssetArchive`

ZSTD page-based `.assets` sprite archive.

#### Constants / properties

| Member | Description |
|--------|-------------|
| `const uint MagicSignature` | `0x54535341` (`'ASST'`) |
| `uint Signature { get; }` | File magic |
| `uint Version { get; }` | Archive version (currently `1`) |
| `uint PageCount { get; }` | ZSTD page count |
| `uint SpriteCount { get; }` | Total sprites |

#### Structs

**`SpriteIndexEntry`**

| Field | Description |
|-------|-------------|
| `uint PageId` | Page containing sprite |
| `uint LocalIndex` | Index within page |

**`PageEntry`**

| Field | Description |
|-------|-------------|
| `ulong Offset` | Byte offset in file |
| `uint CompressedSize` | ZSTD compressed size |
| `uint UncompressedSize` | Decompressed page size |
| `uint SpriteCount` | Sprites in page |

#### Static load

| Method | Description |
|--------|-------------|
| `static AssetArchive Load(ReadOnlyMemory<byte> fileData, bool preloadPages = false)` | Full file in memory. |
| `static AssetArchive OpenReadOnlyFile(string filePath, bool preloadPages = false)` | Memory-mapped. |

#### Instance methods

#### Instance methods — decode

Same semantics as [Sprite decoding (core API)](#sprite-decoding-core-api). Buffer for `TryDecodeSpriteById` must fit `width × height × 4` bytes (typically 4096).

| Method | Notes |
|--------|-------|
| `void SetMaxCachedPages(int count)` | LRU page cache size (default 64 decompressed ZSTD pages). |
| `bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)` | Decompresses owning page if not cached. |
| `byte[] DecodeSpriteById(uint spriteId)` | Allocates 4096-byte buffer. |
| `bool IsEmptySprite(uint spriteId)` | Zero width/height in page entry. |
| `void Dispose()` | Releases memory map. |

---

### `SpritePixelCodec` (static)

RLE codec for 32×32 `.spr` payloads.

#### Constants

| Name | Value |
|------|-------|
| `SpriteEdgeLength` | `32` |
| `RgbaBufferLength` | `4096` (32×32×4) |

#### Methods

| Method | Description |
|--------|-------------|
| `void UncompressToRgba(ReadOnlySpan<byte> compressed, bool transparent, Span<byte> destinationRgba)` | RLE → 32×32 RGBA (`R,G,B,A` per pixel). |
| `bool IsRgbaPixelZero(ReadOnlySpan<byte> rgba, int pixelIndex)` | All four channels zero. |
| `byte[] CompressRgba(ReadOnlySpan<byte> rgba, bool transparent)` | RGBA → RLE payload. |

---

### `SpriteSheetCompiler` (static)

Builds legacy `.spr` files.

| Method | Description |
|--------|-------------|
| `void WriteToStream(Stream output, uint sprSignature, bool extendedSpriteIds, bool transparentPixels, IReadOnlyList<byte[]?> rgbaPerSpriteIdOneBased)` | Writes `.spr`. Index `0` unused; `1…Count−1` are sprite ids. `null` or all-transparent → empty slot. |

---

### `AssetArchiveWriter`

Compiles sprites into ZSTD `.assets` format.

| Method | Description |
|--------|-------------|
| `void AddSprite(ushort width, ushort height, ReadOnlySpan<byte> rgba)` | Append one sprite (`width×height×4` RGBA after 4-byte header). Zero size → empty slot. |
| `void AddRange(IEnumerable<byte[]> sprites)` | Append pre-built sprite byte arrays. |
| `void Save(string path, int compressionLevel = 3, uint spritesPerPage = 2048)` | Write archive to disk. |
| `static void ConvertSprToAssets(string sprPath, string assetsPath, bool extendedSpriteIds, bool transparentPixels, int compressionLevel = 3, uint spritesPerPage = 2048)` | One-shot `.spr` → `.assets` conversion. |

---

## NyxAssets.Utils

### `SpriteImageExporter` (static)

Writes decoded 32×32 RGBA buffers to raster files (SixLabors.ImageSharp).

#### Constants

| Name | Value |
|------|-------|
| `DefaultJpegQuality` | `90` |

#### Image construction

| Method | Description |
|--------|-------------|
| `Image<Rgba32> CreateImageFromSpriteBuffer(ReadOnlySpan<byte> nyxAssetsRgba32x32)` | Buffer → new `Image<Rgba32>` (caller disposes). |
| `void BlitSpriteBufferOnto(Image<Rgba32> dest, int destX, int destY, ReadOnlySpan<byte> nyxAssetsRgba32x32)` | Source-over alpha composite one sprite onto dest. |

#### Save existing image

| Method | Description |
|--------|-------------|
| `void SavePng(Image<Rgba32> image, Stream destination)` | |
| `void SaveJpeg(Image<Rgba32> image, Stream destination, int quality = 90)` | |
| `void SaveBmp(Image<Rgba32> image, Stream destination)` | |

#### Write from 32×32 buffer

| Method | Description |
|--------|-------------|
| `void WritePng(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination)` | |
| `void WriteJpeg(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination, int quality = 90)` | |
| `void WriteBmp(ReadOnlySpan<byte> nyxAssetsRgba32x32, Stream destination)` | |
| `void WritePng(ReadOnlySpan<byte> nyxAssetsRgba32x32, string filePath)` | |
| `void WriteJpeg(ReadOnlySpan<byte> nyxAssetsRgba32x32, string filePath, int quality = 90)` | |
| `void WriteBmp(ReadOnlySpan<byte> nyxAssetsRgba32x32, string filePath)` | |

#### Decode + write from archive

| Method | Description |
|--------|-------------|
| `bool TryDecodeAndWritePng(ISpriteSource archive, uint spriteId, string filePath)` | Returns `false` if decode fails. |
| `bool TryDecodeAndWriteJpeg(ISpriteSource archive, uint spriteId, string filePath, int quality = 90)` | |
| `bool TryDecodeAndWriteBmp(ISpriteSource archive, uint spriteId, string filePath)` | |

---

### `ThingSpriteSheetExporter` (static)

Asset Editor–style spritesheets (one frame group or all groups stacked).

#### Frame group → file/stream

| Method | Description |
|--------|-------------|
| `bool TryWriteFrameGroupSpriteSheetPng(ISpriteSource archive, ThingFrameGroup group, Stream destination)` | |
| `bool TryWriteFrameGroupSpriteSheetJpeg(...)` | |
| `bool TryWriteFrameGroupSpriteSheetBmp(...)` | |
| `bool TryWriteFrameGroupSpriteSheetPng(ISpriteSource archive, ThingFrameGroup group, string filePath)` | |
| `bool TryWriteFrameGroupSpriteSheetJpeg(..., string filePath, int quality = 90)` | |
| `bool TryWriteFrameGroupSpriteSheetBmp(..., string filePath)` | |

#### Whole thing → file/stream

| Method | Description |
|--------|-------------|
| `bool TryWriteThingSpriteSheetPng(ISpriteSource archive, ThingType thing, Stream destination)` | All frame groups stacked vertically. |
| `bool TryWriteThingSpriteSheetJpeg(...)` | |
| `bool TryWriteThingSpriteSheetBmp(...)` | |
| `bool TryWriteThingSpriteSheetPng(ISpriteSource archive, ThingType thing, string filePath)` | |
| `bool TryWriteThingSpriteSheetJpeg(..., string filePath, int quality = 90)` | |
| `bool TryWriteThingSpriteSheetBmp(..., string filePath)` | |

---

## Public member index

Alphabetical index of every public member. Method names link to their primary section.

### A

| Member | Type |
|--------|------|
| `AddRange` | `AssetArchiveWriter` → [AssetArchiveWriter](#assetarchivewriter) |
| `AddSprite` | `AssetArchiveWriter` |
| `AnimationFrameTiming` | [struct](#animationframetiming) |
| `AssetArchive` | [class](#assetarchive) |
| `AssetArchiveWriter` | [class](#assetarchivewriter) |

### B

| Member | Type |
|--------|------|
| `BlitSpriteBufferOnto` | `SpriteImageExporter` → [Utils](#spriteimageexporter-static) |

### C

| Member | Type |
|--------|------|
| `ClientAssetBundle` | [class](#clientassetbundle) |
| `ClientDataReadOptions` | [class](#clientdatareadoptions) |
| `ClientDataVersion` | [struct](#clientdataversion) |
| `CompressRgba` | `SpritePixelCodec` |
| `ConvertSprToAssets` | `AssetArchiveWriter` |

### D

| Member | Type |
|--------|------|
| `DatSignature` | `ThingCatalog` property |
| `DatThingCatalogReader` | [class](#datthingcatalogreader) |
| `DatThingCatalogWriter` | [class](#datthingcatalogwriter) |
| `DatThingFormat` | [enum](#datthingformat-enum) |
| `DatThingFormatRules` | [static class](#datthingformatrules-static) |
| **`DecodeSpriteById`** | **`ISpriteSource`, `SpriteArchive`, `AssetArchive`, `ClientAssetBundle`** → **[Sprite decoding](#decodespritebyid)** |
| `DefaultJpegQuality` | `SpriteImageExporter` const (`90`) |
| `Direction4` | [enum](#direction4-enum) |
| `Direction8` | [enum](#direction8-enum) |
| `DirectionFromTileDelta` | `MissileDirectionPatterns` |
| `Dispose` | `ClientAssetBundle`, `ISpriteSource`, `SpriteArchive`, `AssetArchive` |

### E

| Member | Type |
|--------|------|
| `EffectCount` | `ThingCatalog` property |
| `EffectFrameRequest` | [struct](#effectframerequest-struct) |
| `EnumerateEffects` | `ThingCatalog` |
| `EnumerateItems` | `ThingCatalog` |
| `EnumerateMissiles` | `ThingCatalog` |
| `EnumerateMountedOutfitFrames` | `ThingFrameResolver` |
| `EnumerateOutfitAddonFrames` | `ThingFrameResolver` |
| `EnumerateOutfits` | `ThingCatalog` |
| `EnumerateSpriteIds` | `ThingFrameGroup`, `ThingType` |
| `EnumerateSpriteIdsForOutfit` | `ThingType` |
| `EnumerateSpriteSlots` | `ThingFrameSelection` |
| `ExportJson` | `ThingCatalog` |
| `ExtraProperties` | `ThingType` property |

### F

| Member | Type |
|--------|------|
| `FirstEffectId`, `FirstItemId`, `FirstMissileId`, `FirstOutfitId` | `ThingCatalog` constants |
| `FrameGroups` | `ThingType` property |

### G

| Member | Type |
|--------|------|
| `GetCyclicFrameIndex` | `ThingFrameResolver` |
| `GetEffect` | `ThingCatalog`, `ClientAssetBundle` |
| `GetEffectFrame` | `ThingFrameResolver` |
| `GetEffectFrameIndex` | `ThingFrameResolver` |
| `GetFrameGroup` | `ThingType` |
| `GetItem` | `ThingCatalog`, `ClientAssetBundle` |
| `GetItemFrame` | `ThingFrameResolver` |
| `GetMaxLyingItemRedrawSpan` | `ThingCatalog` |
| `GetMissile` | `ThingCatalog`, `ClientAssetBundle` |
| `GetMissileFrame` | `ThingFrameResolver` |
| `GetMountFrame` | `ThingFrameResolver` |
| `GetMountedPatternZ` | `ThingFrameResolver` |
| `GetOutfit` | `ThingCatalog`, `ClientAssetBundle` |
| `GetOutfitFrame` | `ThingFrameResolver` |
| `GetPattern` | `MissileDirectionPatterns`, `ItemStackPatterns` |
| `GetSpriteId` / `GetSpriteIds` | `ThingFrameGroup` |
| `GetSpriteIdsForOutfit` | `ThingType` |
| `GetSpriteIndex` | `ThingFrameGroup` |
| `GetSpriteRgba` | `ClientAssetBundle` → alias for **`DecodeSpriteById`** |
| `GetSpriteRgbaPixels` | `SpriteArchive` → alias for **`DecodeSpriteById`** |
| `GetTextureIndex` | `ThingFrameGroup` |
| `GetTotalSpriteSlots` | `ThingFrameGroup` |

### I

| Member | Type |
|--------|------|
| `IsAddonPatternVisible` | `ThingFrameResolver` |
| `IsEmptySprite` | `ISpriteSource` → [Sprite decoding](#isemptysprite) |
| `IsRgbaPixelZero` | `SpritePixelCodec` |
| `ISpriteSource` | [interface](#ispritesource) |
| `IThingCatalogReader` | [interface](#ithingcatalogreader) |
| `IThingCatalogWriter` | [interface](#ithingcatalogwriter) |
| `ItemCount`, `ItemFrameRequest`, `ItemsXmlMerger` | [Things](#nyxassetsthings) |

### J

| Member | Type |
|--------|------|
| `JsonThingCatalogReader` | [class](#jsonthingcatalogreader) |
| `JsonThingCatalogWriter` | [class](#jsonthingcatalogwriter) |

### L

| Member | Type |
|--------|------|
| `Load` | `ThingCatalog`, `SpriteArchive`, `AssetArchive`, `ClientAssetBundle` |
| `LoadFromFiles` | `ClientAssetBundle` |
| `LoadItemsXml` | `ThingCatalog` |
| `LoadJson` | `ThingCatalog` |

### M

| Member | Type |
|--------|------|
| `MagicSignature` | `AssetArchive` const |
| `Merge` / `MergeFromFile` | `ItemsXmlMerger` |
| `MissileCount`, `MissileDirectionPatterns`, `MissileFrameRequest` | [Things.Frames](#nyxassetsthingsframes) |

### N

| Member | Type |
|--------|------|
| `NormalizeDirection` | `ThingFrameResolver` |

### O

| Member | Type |
|--------|------|
| `OpenAssetsFromFiles` | `ClientAssetBundle` |
| `OpenFromFiles` | `ClientAssetBundle`, `SpriteArchive`, `AssetArchive` |
| `OpenFromFilesAuto` | `ClientAssetBundle` |
| `OutfitCount`, `OutfitFrameRequest` | [Things / Frames](#nyxassetsthings) |

### P

| Member | Type |
|--------|------|
| `PageEntry`, `PageCount` | `AssetArchive` |
| `PositiveMod` | `ThingFrameResolver` |
| `PutEffect`, `PutItem`, `PutMissile`, `PutOutfit` | `ThingCatalog` |

### R

| Member | Type |
|--------|------|
| `Read` | `DatThingCatalogReader`, `JsonThingCatalogReader`, `IThingCatalogReader` |
| `Resolve` | `ItemStackPatterns` |
| `ResolveDatThingFormat`, `ResolveDefaultFrameDurationMs`, `ResolveExtendedSpriteIds`, `ResolveImprovedAnimations`, `ResolveOutfitFrameGroups` | `ClientDataReadOptions` |
| `ResolveWalkingFrame` | `ThingFrameResolver` |
| `RgbaBufferLength` | `SpritePixelCodec` const (`4096`) |

### S

| Member | Type |
|--------|------|
| `Save` | `AssetArchiveWriter` |
| `SaveBmp`, `SaveJpeg`, `SavePng` | `SpriteImageExporter` |
| `SelectFromClientVersion` | `DatThingFormatRules` |
| `SetMaxCachedPages` | `AssetArchive` |
| `Signature` | `SpriteArchive`, `AssetArchive` |
| `SpriteArchive` | [class](#spritearchive) |
| `SpriteCount` | `ISpriteSource` |
| `SpriteEdgeLength` | `SpritePixelCodec` const (`32`) |
| `SpriteIndexEntry` | `AssetArchive` struct |
| `SpritePixelCodec` | [static class](#spritepixelcodec-static) |
| `SpriteSheetCompiler` | [static class](#spritesheetcompiler-static) |
| `Sprites` | `ClientAssetBundle` property → `ISpriteSource` |

### T

| Member | Type |
|--------|------|
| `ThingCatalog` | [class](#thingcatalog) |
| `ThingFrameGroup` | [class](#thingframegroup) |
| `ThingFrameResolver` | [static class](#thingframeresolver-static) |
| `ThingFrameSelection` | [struct](#thingframeselection-struct) |
| `ThingKind` | [enum](#thingkind-enum) |
| `ThingSpriteSheetExporter` | [static class](#thingspritesheetexporter-static) |
| `ThingType` | [class](#thingtype) |
| `Things` | `ClientAssetBundle` property → `ThingCatalog` |
| **`TryDecodeSpriteById`** | **`ISpriteSource`, `SpriteArchive`, `AssetArchive`, `ClientAssetBundle`** → **[Sprite decoding](#trydecodespritebyid)** |
| `TryCopySpriteRgba` | `SpriteArchive` (alias for `TryDecodeSpriteById`) |
| `TryDecodeAndWriteBmp/Jpeg/Png` | `SpriteImageExporter` |
| `TryExportSpriteBmp/Jpeg/Png` | `ClientAssetBundle` |
| `TryExportFrameGroupSpriteSheet*` | `ClientAssetBundle` |
| `TryExportThingSpriteSheet*` | `ClientAssetBundle` |
| `TryGetEffect/Item/Missile/Outfit` | `ThingCatalog` |
| `TryGetSpriteId` | `ThingFrameGroup` |
| `TryWriteFrameGroupSpriteSheet*`, `TryWriteThingSpriteSheet*` | `ThingSpriteSheetExporter` |

### U

| Member | Type |
|--------|------|
| `UncompressToRgba` | `SpritePixelCodec` |
| `UsesExtendedSpriteIdsByDefault`, `UsesImprovedAnimationsByDefault`, `UsesOutfitFrameGroupsByDefault`, `UsesStackCountGrid` | `DatThingFormatRules`, `ItemStackPatterns` |

### W

| Member | Type |
|--------|------|
| `Write` | `DatThingCatalogWriter`, `JsonThingCatalogWriter`, `IThingCatalogWriter` |
| `WriteBmp`, `WriteJpeg`, `WritePng` | `SpriteImageExporter` |
| `WriteDatTo` | `ThingCatalog` |
| `WriteToStream` | `SpriteSheetCompiler` |

---

## Internal types (not public API)

These types are `internal` and not stable extension points. Use the public facades above.

| Type | Role |
|------|------|
| `BinaryLittleEndian` | Span primitive reads |
| `LittleEndianSpanReader` | Sequential `.dat` reader |
| `ThingPropertyDecoder` | `.dat` flag stream |
| `ThingTextureDecoder` | `.dat` sprite index block |
| `LittleEndianStreamWriter` | Sequential binary writer |
| `DatThingPropertySerializer` | `.dat` flag serialization |
| `ThingTextureEncoder` | `.dat` texture block writer |
| `AssetPageLayout` | Sprite offsets inside decompressed asset pages |
| `ThingTypeJsonMapper` | JSON field mapping (use `JsonThingCatalogReader` / `Writer`) |

To add JSON fields, follow [development/json-mapper.md](development/json-mapper.md).

---

## Typical call flows

### Load and decode one sprite

```csharp
var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098), TransparentSprites = true };
using var bundle = ClientAssetBundle.OpenFromFiles("Nyx.dat", "Nyx.spr", options);
Span<byte> rgba = stackalloc byte[4096];
bundle.TryDecodeSpriteById(1, rgba);
```

### Resolve outfit frame → sprite ids → pixels

```csharp
var outfit = bundle.GetOutfit(128);
var selection = ThingFrameResolver.GetOutfitFrame(outfit, new OutfitFrameRequest { Direction = (int)Direction4.South });
foreach (var slot in selection.EnumerateSpriteSlots())
{
    var pixels = bundle.DecodeSpriteById(slot.SpriteId);
}
```

### Export catalog to JSON

```csharp
var catalog = ThingCatalog.Load(datBytes, options);
catalog.ExportJson("things.json", options);
```

### Load JSON catalog + `.assets` (no `.dat`)

```csharp
var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098), TransparentSprites = true };

ThingCatalog catalog = ThingCatalog.LoadJson("things.json", options);
using AssetArchive sprites = AssetArchive.OpenReadOnlyFile("Nyx.assets");
using var bundle = new ClientAssetBundle(catalog, sprites, disposeSprites: true);

ThingType item = bundle.GetItem(2148);
byte[] rgba = bundle.DecodeSpriteById(item.FrameGroups[0].SpriteIds[0]);
```
