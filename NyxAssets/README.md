# NyxAssets

**.NET 10** library for reading and writing **Nyx-style client data**: `Nyx.dat` (object definitions) and `Nyx.spr` / `Nyx.assets` (sprites). On-disk layouts match **[Asset Editor](https://github.com/ottools/ObjectBuilder)**.

## Install

```bash
dotnet add package NyxAssets
```

Requires **.NET 10**. You need paired client files (`*.dat` + `*.spr` or `*.assets`) at runtime.

---

## Namespaces & entry points

| Namespace | Start here | Role |
|-----------|------------|------|
| `NyxAssets.Client` | **`ClientAssetBundle`** | Load `.dat` + sprites together; decode & export |
| `NyxAssets.Things` | **`ThingCatalog`**, **`ThingType`** | Items, outfits, effects, missiles from `.dat` / JSON |
| `NyxAssets.Things.Exchange` | **`ThingDocument`**, **`ObdThingCodec`** | Single-thing JSON + Object Builder `.obd` |
| `NyxAssets.Things.Frames` | **`ThingFrameResolver`** | Direction, walk phase, stack count → sprite ids |
| `NyxAssets.Sprites` | **`SpriteArchive`**, **`ISpriteSource`** | `.spr` / `.assets` random-access decode |
| `NyxAssets.Utils` | **`SpriteImageExporter`** | Decoded pixels → PNG / JPEG / BMP |

**Typical flow:** `ClientAssetBundle` → `GetItem` / `GetOutfit` → `ThingFrameResolver` → `DecodeSpriteById` → optional `SpriteImageExporter`.

**Full API reference:** [docs/API.md](docs/API.md) in this package, or on GitHub:  
https://github.com/Tofame/NyxFramework/blob/main/NyxAssets/docs/API.md

---

## Quick start

```csharp
using NyxAssets.Client;
using NyxAssets.Things;
using NyxAssets.Sprites;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

// Opens .dat (memory) + .spr (memory-mapped). Dispose when done.
using ClientAssetBundle bundle = ClientAssetBundle.OpenFromFiles("Nyx.dat", "Nyx.spr", options);

// Metadata
Console.WriteLine($"Items up to id {bundle.Things.ItemCount}, sprites: {bundle.Sprites.SpriteCount}");

// Look up a thing definition
ThingType coin = bundle.GetItem(2148);

// Decode one sprite by id (ids are 1-based, from ThingFrameGroup.SpriteIds)
byte[] rgba = bundle.DecodeSpriteById(100);   // allocates byte[4096]
// same as: bundle.GetSpriteRgba(100)  or  bundle.Sprites.DecodeSpriteById(100)

// Or decode without allocating (preferred in loops):
Span<byte> scratch = stackalloc byte[SpritePixelCodec.RgbaBufferLength];
bundle.TryDecodeSpriteById(100, scratch);
```

`DecodeSpriteById` lives on **`ClientAssetBundle`**, **`SpriteArchive`**, **`AssetArchive`**, and the **`ISpriteSource`** interface (`bundle.Sprites`). Returns 32×32 RGBA (`4096` bytes, `R,G,B,A` per pixel). See [Sprite decoding](#sprite-decoding) below.

Use `LoadFromFiles` instead of `OpenFromFiles` if you want the whole `.spr` in a `byte[]` (no dispose needed for the map).

---

## Common examples

### 1 — Load only `.dat` or only `.spr`

```csharp
using NyxAssets.Things;
using NyxAssets.Sprites;

var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098), TransparentSprites = true };

ThingCatalog catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);
ThingType outfit = catalog.GetOutfit(128);

using SpriteArchive spr = SpriteArchive.OpenReadOnlyFile("Nyx.spr", options);
uint count = spr.SpriteCount;
byte[] pixels = spr.DecodeSpriteById(1);
```

### 2 — `.dat` + `.assets` (binary catalog + modern sprites)

```csharp
using var bundle = ClientAssetBundle.OpenAssetsFromFiles("Nyx.dat", "Nyx.assets", options);
// or auto by extension on the sprite path:
using var bundle = ClientAssetBundle.OpenFromFilesAuto("Nyx.dat", "Nyx.assets", options);
```

`OpenFromFilesAuto` only auto-detects **`.spr` vs `.assets`** — the catalog path must still be **`.dat`**.

### 3 — `things.json` + `.assets` (no `.dat`, no `.spr`)

Use this when you ship a JSON thing catalog and a ZSTD sprite archive only. There is no single helper yet — load each side, then construct the bundle:

```csharp
using NyxAssets.Client;
using NyxAssets.Sprites;
using NyxAssets.Things;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

ThingCatalog catalog = ThingCatalog.LoadJson("things.json", options);
AssetArchive sprites = AssetArchive.OpenReadOnlyFile("Nyx.assets");

using ClientAssetBundle bundle = new ClientAssetBundle(catalog, sprites, disposeSprites: true);

ThingType coin = bundle.GetItem(2148);
byte[] rgba = bundle.DecodeSpriteById(100);
```

- **`LoadJson`** — metadata + sprite id layouts from JSON.
- **`OpenReadOnlyFile`** — memory-maps `.assets`; **`disposeSprites: true`** so `Dispose()` releases the map.
- Pass **`preloadPages: true`** to `OpenReadOnlyFile` if you want all ZSTD pages decompressed up front.

In-memory variant (no file map to dispose):

```csharp
ThingCatalog catalog = ThingCatalog.LoadJson(jsonBytes.AsMemory(), options);
AssetArchive sprites = AssetArchive.Load(assetsBytes.AsMemory());
using var bundle = new ClientAssetBundle(catalog, sprites, disposeSprites: false);
```

### 4 — Item with stack count → correct sprite

```csharp
using NyxAssets.Things.Frames;

ThingType coin = bundle.GetItem(2148);
ThingFrameSelection frame = ThingFrameResolver.GetItemFrame(coin, new ItemFrameRequest { StackCount = 37 });

foreach (ThingFrameSelection.SpriteSlot slot in frame.EnumerateSpriteSlots())
{
    bundle.TryDecodeSpriteById(slot.SpriteId, scratch);
}
```

### 5 — Outfit facing + walking animation

```csharp
ThingType player = bundle.GetOutfit(128);
var request = new OutfitFrameRequest
{
    Direction = (int)Direction4.South,
    WalkPhase = 2,
    AddonMask = 0xFF,
    Mounted = false,
};
ThingFrameSelection frame = ThingFrameResolver.GetOutfitFrame(player, request);
uint[] spriteIds = frame.GetSpriteIds();
```

### 6 — Export sprite or spritesheet to PNG

```csharp
using NyxAssets.Utils;

// Single sprite
bundle.TryExportSpritePng(spriteId: 100, filePath: "sprite.png");

// Whole thing (all frame groups, Asset Editor layout)
ThingType item = bundle.GetItem(2148);
bundle.TryExportThingSpriteSheetPng(item, "item_sheet.png");
```

### 7 — `.dat` ↔ JSON

```csharp
var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098), TransparentSprites = true };

// Export
ThingCatalog catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);
catalog.ExportJson("things.json", options);

// Import
ThingCatalog fromJson = ThingCatalog.LoadJson("things.json", options);
```

### 8 — Build or convert sprite archives

```csharp
using NyxAssets.Sprites;

// .spr → .assets
AssetArchiveWriter.ConvertSprToAssets("Nyx.spr", "Nyx.assets", extendedSpriteIds: true, transparentPixels: true);

// Write .spr from RGBA buffers (index 0 unused; index 1 = sprite id 1)
SpriteSheetCompiler.WriteToStream(outputStream, sprSignature: 0x12345678, extendedSpriteIds: true, transparentPixels: true, rgbaPerSpriteIdOneBased);
```

### 9 — Add or remove sprites and things in memory

```csharp
using NyxAssets.Client;
using NyxAssets.Sprites;
using NyxAssets.Things;

var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098), TransparentSprites = true };
using var bundle = ClientAssetBundle.OpenFromFiles("Nyx.dat", "Nyx.spr", options);

// Add or replace a sprite by id (1-based) and write the updated archive back out.
bundle.PutSprite(42, rgbaBuffer);

// Remove an existing sprite slot (returns false when the slot did not exist)
bundle.RemoveSprite(7);

// Add or replace a thing definition. New ids must be contiguous with the section bounds.
var newItem = new ThingType { Id = bundle.Things.ItemCount + 1, Kind = ThingKind.Item };
newItem.FrameGroups.Add(new ThingFrameGroup { SpriteIds = new uint[] { 42 }, Width = 1, Height = 1, ExactSize = 32, Layers = 1, PatternX = 1, PatternY = 1, PatternZ = 1, Frames = 1 });
bundle.PutItem(newItem);

// Remove an existing definition by id.
bundle.RemoveItem(100);
```

`PutSprite` / `RemoveSprite` are available on `ClientAssetBundle`, `SpriteArchive`, `AssetArchive`, and `ISpriteSource` implementations that support mutation. `RemoveItem` / `RemoveOutfit` / `RemoveEffect` / `RemoveMissile` are available on `ThingCatalog` and `ClientAssetBundle`.

---

## Sprite decoding

| Method | Type | Description |
|--------|------|-------------|
| `byte[] DecodeSpriteById(uint spriteId)` | `ClientAssetBundle`, `ISpriteSource` | Allocates `byte[4096]`. Throws if invalid. |
| `byte[] GetSpriteRgba(uint spriteId)` | `ClientAssetBundle` | Alias for `DecodeSpriteById`. |
| `byte[] GetSpriteRgbaPixels(uint spriteId)` | `SpriteArchive` | Alias for `DecodeSpriteById`. |
| `bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)` | all above | Writes into your buffer (≥ 4096 bytes). Returns `false` if invalid. |
| `bool IsEmptySprite(uint spriteId)` | `ISpriteSource` | Slot has no pixel data. |
| `uint SpriteCount { get; }` | `ISpriteSource` | Valid ids: **1 … SpriteCount**. |

Constants: `SpritePixelCodec.RgbaBufferLength` = `4096`, `SpritePixelCodec.SpriteEdgeLength` = `32`.

---

## API at a glance

Method signatures for the types you use most. Parameter names reflect the real API.

### `ClientAssetBundle` (`NyxAssets.Client`)

```csharp
// Construction / load
ClientAssetBundle(ThingCatalog things, ISpriteSource sprites, bool disposeSprites = false)
static ClientAssetBundle Load(ReadOnlyMemory<byte> dat, ReadOnlyMemory<byte> spr, ClientDataReadOptions options)
static ClientAssetBundle LoadFromFiles(string datPath, string sprPath, ClientDataReadOptions options)
static ClientAssetBundle OpenFromFiles(string datPath, string sprPath, ClientDataReadOptions options)
static ClientAssetBundle OpenAssetsFromFiles(string datPath, string assetsPath, ClientDataReadOptions options, bool preloadPages = false)
static ClientAssetBundle OpenFromFilesAuto(string datPath, string spritePath, ClientDataReadOptions options, bool preloadSprites = false)

ThingCatalog Things { get; }
ISpriteSource Sprites { get; }

// Decode
bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)
byte[] DecodeSpriteById(uint spriteId)
byte[] GetSpriteRgba(uint spriteId)

// Catalog shortcuts (throw KeyNotFoundException)
ThingType GetItem(uint id)
ThingType GetOutfit(uint id)
ThingType GetEffect(uint id)
ThingType GetMissile(uint id)

// Export
bool TryExportSpritePng(uint spriteId, string filePath)
bool TryExportSpriteJpeg(uint spriteId, string filePath, int quality = 90)
bool TryExportSpriteBmp(uint spriteId, string filePath)
bool TryExportFrameGroupSpriteSheetPng(ThingFrameGroup group, string filePath)
bool TryExportThingSpriteSheetPng(ThingType thing, string filePath)
// ... Jpeg/Bmp variants on same types

void Dispose()
```

### `ThingCatalog` (`NyxAssets.Things`)

```csharp
static ThingCatalog Load(ReadOnlyMemory<byte> datFile, ClientDataReadOptions options)
static ThingCatalog LoadJson(ReadOnlyMemory<byte> jsonData, ClientDataReadOptions options)
static ThingCatalog LoadJson(string filePath, ClientDataReadOptions options)

ThingType? TryGetItem(uint id) / ThingType GetItem(uint id)
ThingType? TryGetOutfit(uint id) / ThingType GetOutfit(uint id)
ThingType? TryGetEffect(uint id) / ThingType GetEffect(uint id)
ThingType? TryGetMissile(uint id) / ThingType GetMissile(uint id)

IEnumerable<ThingType> EnumerateItems()
IEnumerable<ThingType> EnumerateOutfits()
IEnumerable<ThingType> EnumerateEffects()
IEnumerable<ThingType> EnumerateMissiles()

void PutItem(ThingType thing, bool rebuildArrays = true)   // + PutOutfit, PutEffect, PutMissile
void WriteDatTo(Stream output, ClientDataReadOptions formatOptions, uint? datSignatureOverride = null)
void ExportJson(string filePath, ClientDataReadOptions options, uint? signatureOverride = null, string? itemsXmlPath = null)
void LoadItemsXml(string filePath)

uint DatSignature { get; set; }
uint ItemCount { get; }   // inclusive last id, not count of defined items
uint OutfitCount { get; }
uint EffectCount { get; }
uint MissileCount { get; }
DatThingFormat DatFormat { get; set; }
```

### `ThingType` / `ThingFrameGroup`

```csharp
// ThingType — flags (IsGround, Stackable, Rotatable, …), FrameGroups, ExtraProperties
ThingFrameGroup? GetFrameGroup(int index)
uint[] GetSpriteIdsForOutfit(uint? innerWidth = null, …, int frameGroupIndex = 0)

// ThingFrameGroup — layout + sprite id list
uint GetSpriteId(uint layer, uint patternX, uint patternY, uint patternZ, uint frame)
bool TryGetSpriteId(uint layer, uint patternX, uint patternY, uint patternZ, uint frame, out uint spriteId)
uint[] GetSpriteIds(uint? innerWidth = null, …)
uint[] SpriteIds { get; set; }
uint Width, Height, Layers, PatternX, PatternY, PatternZ, Frames { get; set; }
```

### `ThingFrameResolver` (`NyxAssets.Things.Frames`)

```csharp
ThingFrameSelection GetOutfitFrame(ThingType outfit, OutfitFrameRequest request = default)
ThingFrameSelection GetItemFrame(ThingType item, ItemFrameRequest request = default)
ThingFrameSelection GetEffectFrame(ThingType effect, EffectFrameRequest request = default)
ThingFrameSelection GetMissileFrame(ThingType missile, MissileFrameRequest request = default)
IEnumerable<ThingFrameSelection> EnumerateOutfitAddonFrames(ThingType outfit, OutfitFrameRequest request = default)

uint GetEffectFrameIndex(ThingType effect, float elapsedMs, int ticksPerFrame = 75)
uint GetCyclicFrameIndex(ThingType thing, float elapsedMs, int ticksPerFrame = 333)
```

Request structs: **`OutfitFrameRequest`** (`Direction`, `WalkPhase`, `AddonMask`, `Mounted`), **`ItemFrameRequest`** (`StackCount`, `PatternX`, `PatternY`, `Frame`), **`EffectFrameRequest`** (`Frame`, `TileX`, `TileY`), **`MissileFrameRequest`** (`Direction`, `TileDeltaX`, `TileDeltaY`).

### `ISpriteSource` / `SpriteArchive` / `AssetArchive` (`NyxAssets.Sprites`)

```csharp
// ISpriteSource
uint SpriteCount { get; }
bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)
byte[] DecodeSpriteById(uint spriteId)
bool IsEmptySprite(uint spriteId)
void Dispose()

// SpriteArchive
static SpriteArchive Load(ReadOnlyMemory<byte> sprFile, ClientDataReadOptions options, bool preloadSprites = false)
static SpriteArchive OpenReadOnlyFile(string sprPath, ClientDataReadOptions options, bool preloadSprites = false)

// AssetArchive
static AssetArchive Load(ReadOnlyMemory<byte> fileData, bool preloadPages = false)
static AssetArchive OpenReadOnlyFile(string filePath, bool preloadPages = false)
void SetMaxCachedPages(int count)
```

### `ClientDataReadOptions`

```csharp
required ClientDataVersion ClientVersion { get; init; }
bool TransparentSprites { get; init; }
bool? ExtendedSpriteIds { get; init; }
bool? ImprovedAnimations { get; init; }
bool? OutfitFrameGroups { get; init; }
DatThingFormat? DatThingFormatOverride { get; init; }
```

---

## What you get (types)

| Type | Purpose |
|------|---------|
| **`ClientAssetBundle`** | `.dat` + sprite source in one handle |
| **`ThingCatalog`** | All items, outfits, effects, missiles |
| **`ThingType`** / **`ThingFrameGroup`** | One definition + sprite layout |
| **`ThingFrameResolver`** | Game-style frame → sprite id resolution |
| **`SpriteArchive`** | Legacy `.spr` (RLE, lookup table) |
| **`AssetArchive`** | Modern `.assets` (ZSTD pages) |
| **`SpriteSheetCompiler`** / **`AssetArchiveWriter`** | Write / convert sprite files |
| **`SpriteImageExporter`** / **`ThingSpriteSheetExporter`** | PNG/JPEG/BMP export |
| **`ThingDocument`** / **`ObdThingCodec`** | Single-thing JSON + `.obd` import/export |

---

## Single-thing exchange

Import one Object Builder export or share one definition between projects:

```csharp
using NyxAssets.Things.Exchange;

var doc = ObdThingCodec.Read("item_test.obd");
doc.ImportInto(catalog, assignId: 35000);

ThingDocumentJsonCodec.Write("item.json", doc);
```

See [docs/development/thing-exchange.md](docs/development/thing-exchange.md).

---

## Documentation

| Doc | Description |
|-----|-------------|
| **[docs/API.md](docs/API.md)** | Every public type and method (detailed) |
| [docs/guides/usage.md](docs/guides/usage.md) | Longer usage guide |
| [docs/guides/supported-clients.md](docs/guides/supported-clients.md) | Client version / `.dat` tiers |
| [docs/development/frame-resolver.md](docs/development/frame-resolver.md) | Frame resolver examples |
| [docs/development/thing-exchange.md](docs/development/thing-exchange.md) | Single-thing JSON + OBD import/export |

On NuGet: open **docs/API.md** from the package folder in your IDE, or browse on GitHub:  
https://github.com/Tofame/NyxFramework/tree/main/NyxAssets/docs

---

## Build from source

```bash
dotnet build NyxAssets/NyxAssets.csproj
```

Part of [NyxFramework](https://github.com/Tofame/NyxFramework).
