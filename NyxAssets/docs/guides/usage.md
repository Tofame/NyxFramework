# NyxAssets example usage

> Part of [NyxAssets documentation](../README.md) · [Development guide](../development/overview.md)

## Reference the library

From another project in the same solution:

```xml
<ItemGroup>
  <ProjectReference Include="..\NyxAssets\NyxAssets.csproj" />
</ItemGroup>
```

Or pack NyxAssets as a NuGet package and add a normal package reference.

## Usings

```csharp
using NyxAssets.Client;
using NyxAssets.Utils;
using NyxAssets.Things;
using NyxAssets.Sprites;
```

## Load `.dat` + `.spr` together

Use the **Asset Editor client build number** for your client (e.g. `1098` → 10.98). That drives which `.dat` flag decoder is used and default extended sprite ids / frame groups.

### Full in-memory load (simplest)

Reads **both** files into `byte[]`:

```csharp
var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true, // must match how your client’s .spr was built
};

using var assets = ClientAssetBundle.LoadFromFiles(
    @"C:\Nyx\Nyx.dat",
    @"C:\Nyx\Nyx.spr",
    options);

Console.WriteLine($"DAT signature: {assets.Things.DatSignature}");
Console.WriteLine($"SPR signature: {assets.Sprites.Signature}, sprites: {assets.Sprites.SpriteCount}");
```

(`LoadFromFiles` returns a bundle that does **not** need disposal for the `.spr`; calling `Dispose()` is harmless.)

### Memory-mapped `.spr` (no huge `byte[]` for the sheet)

Same API, but the **`.spr` file is opened read-only via memory mapping**; only pages you read are touched. **Dispose** when finished to release the map.

```csharp
using var assets = ClientAssetBundle.OpenFromFiles(
    @"C:\Nyx\Nyx.dat",
    @"C:\Nyx\Nyx.spr",
    options);
```

## Load `things.json` + `.assets` (no `.dat`)

When you only ship JSON definitions and a ZSTD sprite archive — no binary `.dat` or legacy `.spr`:

```csharp
var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

ThingCatalog catalog = ThingCatalog.LoadJson(@"C:\Nyx\things.json", options);
AssetArchive sprites = AssetArchive.OpenReadOnlyFile(@"C:\Nyx\Nyx.assets");

using var assets = new ClientAssetBundle(catalog, sprites, disposeSprites: true);

Console.WriteLine($"Items up to id {assets.Things.ItemCount}, sprites: {assets.Sprites.SpriteCount}");
```

`OpenFromFilesAuto` does **not** support JSON catalogs yet — it always loads the first path as binary `.dat`. Use the pattern above, or `.dat` + `.assets` via `OpenAssetsFromFiles` / `OpenFromFilesAuto`.

## Decode exactly one sprite by id (lookup table)

The `.spr` file stores a **per–sprite-id file offset**. NyxAssets never decodes “all textures” unless you loop all ids yourself.

```csharp
Span<byte> rgba = stackalloc byte[SpritePixelCodec.RgbaBufferLength];

if (assets.TryDecodeSpriteById(42, rgba))
{
    // use rgba (32×32, A R G B per pixel)
}
```

Or allocate:

```csharp
byte[] rgba = assets.DecodeSpriteById(42);
```

Lower-level, without the bundle:

```csharp
// Load legacy RLE spr (lazy lookup + RLE decoding on demand):
using var spr = SpriteArchive.OpenReadOnlyFile(@"C:\Nyx\Nyx.spr", options);
// Or preload and decompress all RLE sprites into memory at startup (bypasses runtime decoding):
// using var spr = SpriteArchive.OpenReadOnlyFile(@"C:\Nyx\Nyx.spr", options, preloadSprites: true);
spr.TryDecodeSpriteById(42, destinationSpan);

// Or load modern page-based ZSTD assets:
// using var spr = AssetArchive.OpenReadOnlyFile(@"C:\Nyx\Nyx.assets"); // lazy cache loading
// Or preload and decompress all pages into memory at startup (bypasses cache):
// using var spr = AssetArchive.OpenReadOnlyFile(@"C:\Nyx\Nyx.assets", preloadPages: true);
// spr.TryDecodeSpriteById(42, destinationSpan);
```

## Export decoded sprites (PNG / JPEG / BMP)

Decoded pixels are **4096 bytes** per id (`R, G, B, A` per pixel — same as `SpritePixelCodec.UncompressToRgba`). Use **`NyxAssets.Utils.SpriteImageExporter`** (SixLabors.ImageSharp):

```csharp
byte[] pixels = assets.DecodeSpriteById(50);
SpriteImageExporter.WritePng(pixels, @"C:\export\sprite_50.png");
SpriteImageExporter.WriteJpeg(pixels, @"C:\export\sprite_50.jpg", quality: 90);
SpriteImageExporter.WriteBmp(pixels, @"C:\export\sprite_50.bmp");

// Or decode + write in one call (false if id missing / empty slot)
assets.TryExportSpritePng(50, @"C:\export\sprite_50.png");
```

## Export full spritesheets (items, outfits, effects, missiles)

Asset Editor lays out every sprite id for a **frame group** in a 2D grid (pattern × animation × layers, etc.), then stacks **multiple frame groups** (e.g. outfit idle + walking) vertically. NyxAssets mirrors that in **`ThingSpriteSheetExporter`** and on **`ClientAssetBundle`**:

```csharp
ThingType outfit = assets.GetOutfit(128);

// One PNG per frame group (same layout as Asset Editor’s single-group sheet)
foreach (var group in outfit.FrameGroups)
{
    var path = $@"C:\export\outfit_{outfit.Id}_group_{group.GroupTypeId}.png";
    assets.TryExportFrameGroupSpriteSheetPng(group, path);
}

// Or one tall image with every group stacked (like Asset Editor “total” sheet)
assets.TryExportThingSpriteSheetPng(outfit, $@"C:\export\outfit_{outfit.Id}_full.png");

// Same for items / effects / missiles — any ThingType
assets.TryExportThingSpriteSheetPng(assets.GetItem(100), @"C:\export\item_100_full.png");
```

JPEG/BMP variants exist on both `ThingSpriteSheetExporter` and `ClientAssetBundle` (`TryExportThingSpriteSheetJpeg`, `TryExportFrameGroupSpriteSheetBmp`, …).

## Query things (metadata + sprite id lists)

```csharp
ThingType item = assets.GetItem(100); // first item id

foreach (var group in item.FrameGroups)
{
    Console.WriteLine($"Group: {group.Width}x{group.Height}, frames={group.Frames}");
    foreach (uint spriteId in group.SpriteIds)
        Console.WriteLine($"  uses .spr id {spriteId}");
}
```

Other getters on `ClientAssetBundle` / `ThingCatalog`:

- `GetOutfit(uint id)`
- `GetEffect(uint id)`
- `GetMissile(uint id)` — missiles / “distance effects” in `.dat` terms

`ThingCatalog` also exposes `TryGet*` variants and `EnumerateItems()` (and outfits / effects / missiles).

## Decode one sprite (on demand)

Sprites are **not** stored as decoded RGBA inside `SpriteArchive`. You request one id at a time; decoded pixels go into **your buffer**.

### New array per sprite

```csharp
byte[] rgba = assets.GetSpriteRgba(1); // 1-based id, always 4096 bytes (32×32×4)
```

### Reuse a scratch buffer (less GC)

```csharp
Span<byte> scratch = stackalloc byte[SpritePixelCodec.RgbaBufferLength];

for (uint id = 1; id <= assets.Sprites.SpriteCount; id++)
{
    if (!assets.Sprites.TryCopySpriteRgba(id, scratch))
        continue; // empty slot

    // use scratch: upload to GPU, copy to atlas, etc.
}
```

## Build your own “collection of textures”

The library intentionally does **not** allocate a `List<Texture2D>` or similar (that would be engine-specific and memory-heavy). You choose the collection type.

### Example: all non-empty sprites → `Dictionary<uint, byte[]>`

```csharp
var textures = new Dictionary<uint, byte[]>(capacity: (int)assets.Sprites.SpriteCount);

for (uint id = 1; id <= assets.Sprites.SpriteCount; id++)
{
    if (assets.Sprites.IsEmptySprite(id))
        continue;

    textures[id] = assets.Sprites.GetSpriteRgbaPixels(id);
}
```

### Example: only sprites referenced by one item

```csharp
ThingType thing = assets.GetItem(3031);
var seen = new HashSet<uint>();

foreach (var group in thing.FrameGroups)
{
    foreach (uint spriteId in group.SpriteIds)
    {
        if (!seen.Add(spriteId))
            continue;

        byte[] rgba = assets.GetSpriteRgba(spriteId);
        // ...
    }
}
```

## Load `.dat` or `.spr` separately

```csharp
var catalog = ThingCatalog.Load(datMemory, options);
var sprites = SpriteArchive.Load(sprMemory, options);
```

## Overrides (advanced)

If you know the format better than the heuristics from `ClientVersion`:

```csharp
var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
    ExtendedSpriteIds = true,
    ImprovedAnimations = true,
    OutfitFrameGroups = true,
    DatThingFormatOverride = DatThingFormat.V6_10_10__10_56,
};
```

See `DatThingFormatRules` for how defaults are derived from `ClientVersion`.

## Compile (write) `.spr`

Build a list where index `0` is unused and each sprite id maps to `4096` bytes RGBA (or `null` for an empty slot):

```csharp
using NyxAssets.Sprites;

var slots = new List<byte[]?>(capacity: 200_000) { null }; // index 0 unused
for (var i = 1; i < 200_000; i++)
    slots.Add(null); // or your RGBA buffer per id

using var fs = File.Create("out.spr");
SpriteSheetCompiler.WriteToStream(
    fs,
    sprSignature: 0x12345678,
    extendedSpriteIds: true,
    transparentPixels: true,
    rgbaPerSpriteIdOneBased: slots);
```

## Compile (write) `.assets`

Write a new page-based ZSTD `.assets` archive directly from C# code:

```csharp
using NyxAssets.Sprites;

var writer = new AssetArchiveWriter();
// Add sprites (width, height, raw RGBA bytes)
writer.AddSprite(32, 32, rgbaBuffer1);
writer.AddSprite(0, 0, ReadOnlySpan<byte>.Empty); // empty sprite
writer.AddSprite(32, 32, rgbaBuffer2);

// Save to disk (ZSTD-compressed, 2048 sprites per page, compression level 3)
writer.Save("Nyx.assets", compressionLevel: 3, spritesPerPage: 2048);
```

## Extending a loaded `.dat` (new item, outfit, effect, missiles)

The `.dat` header stores, for each section, the **inclusive last id** Asset Editor iterates up to (items from `100` … `ItemCount`, outfits from `1` … `OutfitCount`, etc.). NyxAssets matches that.

After `ThingCatalog.Load`, register new definitions with **`PutItem`**, **`PutOutfit`**, **`PutEffect`**, **`PutMissile`**. Each `ThingType` must have **`Kind`** set correctly, a valid **`Id`**, and at least one **`FrameGroups`** entry (sprite layout + ids pointing into your `.spr`). Appends must be **contiguous**: the next id in a section must be exactly one past the current bound (e.g. new item id = `catalog.ItemCount + 1` once `ItemCount >= 100`).

```csharp
// Example: one new item, outfit, effect, and three missiles (after loading catalog + options)
var newItem = new ThingType { Id = catalog.ItemCount + 1, Kind = ThingKind.Item, IsGround = true, GroundSpeed = 100 };
newItem.FrameGroups.Add(/* one ThingFrameGroup with SpriteIds, dimensions, … */);

catalog.PutItem(newItem);

var newOutfit = new ThingType { Id = catalog.OutfitCount + 1, Kind = ThingKind.Outfit };
newOutfit.FrameGroups.Add(/* … */);
catalog.PutOutfit(newOutfit);

// same idea: PutEffect, then PutMissile three times with ids MissileCount+1 … MissileCount+3

using var outDat = File.Create("patched.dat");
catalog.WriteDatTo(outDat, options, datSignatureOverride: null);
```

If you add **new sprite ids** at the end of the `.spr`, extend the sprite sheet (see above) **before** or **together** with patching `.dat`, and point new `ThingType` sprite indices at those ids.

## Compile (write) `.dat` (V1–V6)

After loading a catalog, you can write it back with the same texture layout flags you use at runtime; property bytes follow Asset Editor’s `MetadataWriter` for that catalog’s `DatFormat`:

```csharp
using var outDat = File.Create("out.dat");
catalog.WriteDatTo(outDat, options, datSignatureOverride: null);
```

## Using custom asset formats

NyxAssets supports alternative asset formats through interfaces. Implement `ISpriteSource` for custom sprite storage, or `IThingCatalogReader` / `IThingCatalogWriter` for custom thing definitions.

### Built-in JSON format (human-readable `.dat` alternative)

```csharp
using NyxAssets.Client;
using NyxAssets.Things;

var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098) };

// Convert .dat to JSON
var catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);
catalog.ExportJson("things.json", options);

// Load from JSON
var jsonCatalog = ThingCatalog.LoadJson("things.json", options);

// Use JSON catalog with .spr
using var sprites = SpriteArchive.OpenReadOnlyFile("Nyx.spr", options);
using var bundle = new ClientAssetBundle(jsonCatalog, sprites, disposeSprites: true);
```

JSON preserves all `.dat` flags, frame groups, and sprite ids. Game-logic properties (name, weight, armor, etc.) live in `"properties"` via `ThingType.ExtraProperties`. See [formats/things-json.md](../formats/things-json.md) for the schema and [development/json-mapper.md](../development/json-mapper.md) to extend fields.

### Custom sprite source (e.g., PNG atlas)

```csharp
using NyxAssets.Client;
using NyxAssets.Sprites;

using var sprites = new PngAtlasSpriteSource("atlas.png", "manifest.json");
using var bundle = new ClientAssetBundle(catalog, sprites);

byte[] pixels = bundle.DecodeSpriteById(42);
```

### Custom thing reader (e.g., JSON)

```csharp
using NyxAssets.Things;

var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098) };
var reader = new JsonThingCatalogReader();
var catalog = reader.Read(File.ReadAllBytes("things.json"), options);

// Use with built-in .spr
using var sprites = SpriteArchive.OpenReadOnlyFile("Nyx.spr", options);
using var bundle = new ClientAssetBundle(catalog, sprites, disposeSprites: true);
```

See [development/custom-formats.md](../development/custom-formats.md) for full implementation guides and examples.

## See also

- [supported-clients.md](supported-clients.md)
- [formats/spr-binary.md](../formats/spr-binary.md)
- [formats/dat-binary.md](../formats/dat-binary.md)
- [formats/things-json.md](../formats/things-json.md)
- [formats/assets-binary.md](../formats/assets-binary.md)
- [development/custom-formats.md](../development/custom-formats.md)
- [README.md](../README.md)
