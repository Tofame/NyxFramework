# NyxAssets

Small **.NET 10** library for reading **Nyx-style client data**: paired **`Nyx.dat`** (object definitions) and **`Nyx.spr`** (32×32 sprite pixels), using the same on-disk layout as **[Asset Editor](https://github.com/ottools/ObjectBuilder)** (`SpriteReader`, `MetadataReader*`, `ThingTypeStorage`).

NyxAssets reads **and writes** Nyx-style **`.dat` / `.spr`** (tier matrix in [docs/guides/supported-clients.md](docs/guides/supported-clients.md)).

## What you get

| Piece | Purpose |
|-------|--------|
| **`ISpriteSource`** | Format-agnostic interface for random-access sprite decoding. Both `SpriteArchive` and `AssetArchive` implement it. |
| **`IThingCatalogReader`** / **`IThingCatalogWriter`** | Interfaces for reading/writing thing definitions in any format. `DatThingCatalogReader`/`DatThingCatalogWriter` implement them for Asset Editor `.dat`. `JsonThingCatalogReader`/`JsonThingCatalogWriter` implement them for compact JSON. |
| **`ThingCatalog`** | Parsed `.dat` or `.json` metadata: items, outfits, effects, missiles + metadata + sprite id layouts. |
| **`SpriteArchive`** | Random access to `.spr`: lookup table + on-demand RLE decode. Implements `ISpriteSource`. |
| **`AssetArchive`** | Random access to `.assets`: page-based ZSTD-compressed archive with LRU caching. Implements `ISpriteSource`. |
| **`SpriteSheetCompiler`** | Writes a legacy `.spr` from raw sprite RGBA buffers. |
| **`AssetArchiveWriter`** | Compiles sprite buffers or converts a legacy `.spr` directly into a page-based ZSTD `.assets` archive. |
| **`ClientAssetBundle`** | Convenience wrapper: loads catalog and sprite sources under a unified API. |
| **`SpriteImageExporter`** | Raster export: decoded RGBA buffer → **PNG / JPEG / BMP** (via **SixLabors.ImageSharp**). |
| **`ThingSpriteSheetExporter`** | Full **spritesheet** per `ThingFrameGroup` or stacked per `ThingType` (Asset Editor sheet layout). |

## Where decoded textures “live”

The library **does not** build an in-memory atlas or texture list by default. It keeps the **raw `.spr` bytes** and decodes **when you ask** for a sprite id:

- **`DecodeSpriteById(id)`** / **`GetSpriteRgbaPixels(id)`** allocates a new `byte[4096]`.
- **`TryDecodeSpriteById(id, span)`** / **`TryCopySpriteRgba(id, span)`** writes into **your** span (reuse one buffer in a loop).

To get a **collection**, loop ids and store results yourself (see [docs/guides/usage.md](docs/guides/usage.md)).

**Raster files:** the project references **SixLabors.ImageSharp** for `NyxAssets.Utils.SpriteImageExporter` (PNG/JPEG/BMP from the same decoded buffer layout as `SpritePixelCodec`) and `NyxAssets.Utils.ThingSpriteSheetExporter` for full thing / frame-group sheets.

## Quick start

```csharp
using NyxAssets.Client;
using NyxAssets.Things;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

using var game = ClientAssetBundle.OpenFromFiles("Nyx.dat", "Nyx.spr", options); // memory-maps .spr
// Alternatively, load from ZSTD-compressed page-based assets:
// using var sprSource = AssetArchive.OpenReadOnlyFile("Nyx.assets");
// using var game = new ClientAssetBundle(ThingCatalog.LoadJson("things.json", options), sprSource);

byte[] pixels = game.DecodeSpriteById(1); // lookup + decode this id only; 1-based
```

Use `LoadFromFiles` instead of `OpenFromFiles` if you prefer the whole `.spr` in a `byte[]`.

## JSON format (human-readable alternative to `.dat`)

NyxAssets can read and write thing definitions as **JSON** — a compact, tool-friendly format. All `.dat` flags, frame groups, and sprite ids are preserved. Game-logic properties (name, weight, armor, etc.) live in a `"properties"` object via `ThingType.ExtraProperties`.

```csharp
// Convert .dat → .json
var catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);
catalog.ExportJson("things.json", options);

// Load from .json
var catalog = ThingCatalog.LoadJson("things.json", options);
```

See [docs/formats/things-json.md](docs/formats/things-json.md) for the JSON schema and [docs/development/json-mapper.md](docs/development/json-mapper.md) for how to extend JSON fields.

## Documentation

Full index: **[docs/README.md](docs/README.md)**

| Category | Doc | Content |
|----------|-----|--------|
| **Guides** | [guides/usage.md](docs/guides/usage.md) | Loading, decoding, export, scratch buffers. |
| | [guides/supported-clients.md](docs/guides/supported-clients.md) | `.dat` tiers, compile APIs, client-version switches. |
| **Formats** | [formats/dat-binary.md](docs/formats/dat-binary.md) | Legacy `.dat` layout. |
| | [formats/spr-binary.md](docs/formats/spr-binary.md) | Legacy `.spr` layout and RLE. |
| | [formats/assets-binary.md](docs/formats/assets-binary.md) | Modern `.assets` ZSTD pages. |
| | [formats/things-json.md](docs/formats/things-json.md) | JSON catalog schema. |
| **Development** | [development/overview.md](docs/development/overview.md) | How to extend NyxAssets. |
| | [development/json-mapper.md](docs/development/json-mapper.md) | `ThingTypeJsonMapper` — add JSON fields safely. |
| | [development/frame-resolver.md](docs/development/frame-resolver.md) | `ThingFrameResolver` — frame/sprite queries for editors. |
| | [development/custom-formats.md](docs/development/custom-formats.md) | Custom `ISpriteSource` / catalog readers. |

## Source layout (repo)

`.dat` parsing and serialization live under **`NyxAssets/Data/`**, split by direction:

- **`Data/Readers/`** — `LittleEndianSpanReader`, `ThingPropertyDecoder`, `ThingTextureDecoder` (`NyxAssets.Data.Readers`).
- **`Data/Writers/`** — `LittleEndianStreamWriter`, `DatThingPropertySerializer`, `ThingTextureEncoder` (`NyxAssets.Data.Writers`).

`.spr` I/O is grouped the same way under **`NyxAssets/Sprites/`**:

- **`Sprites/Readers/`** — `SpriteArchive` and `AssetArchive` (still in namespace `NyxAssets.Sprites` so callers keep a single `using NyxAssets.Sprites`).
- **`Sprites/Writers/`** — `SpriteSheetCompiler` and `AssetArchiveWriter` (same namespace).
- **`Sprites/SpritePixelCodec.cs`** — shared legacy compress/decompress helpers used by legacy paths.

`Things/` holds the domain model (`ThingCatalog`, `ThingType`, format/version options). `Client/` is the façade over both assets. **`Utils/`** — `SpriteImageExporter` and `ThingSpriteSheetExporter` (`NyxAssets.Utils`) for PNG/JPEG/BMP export (single sprites and full sheets).

## Build

```bash
dotnet build NyxAssets/NyxAssets.csproj
```

## Layout on disk (one-line summary)

- **`.spr`**: `[ signature | count ][ ptr[0] … ptr[N-1] ][ variable sprite blobs… ]` — each blob is 3 skipped bytes + `uint16` length + RLE payload → 32×32 RGBA.
- **`.assets`**: `[ magic | version | pageCount | spriteCount ][ SpriteIndexEntry[0..N-1] ][ PageEntry[0..M-1] ][ ZSTD compressed page data blobs… ]` — modern block-based format.
- **`.dat`**: `[ 12-byte header ][ for each thing: flags…0xFF ][ sprite index block ]` × four categories (items, outfits, effects, missiles).

Details: see the `docs/` files above.
