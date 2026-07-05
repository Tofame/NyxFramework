# Custom asset formats

> **Developers:** JSON serialization is centralized in `ThingTypeJsonMapper` — see [json-mapper.md](json-mapper.md). Consumer schema: [formats/things-json.md](../formats/things-json.md).  
> **Single things:** Import/export one item/outfit/effect/missile via [thing-exchange.md](thing-exchange.md) (`ThingDocument`, `.obd`, `nyx-thing` JSON).

NyxAssets ships with Asset Editor `.dat` / `.spr` parsers, but the library is designed to support **alternative asset formats** through three interfaces:

- **`ISpriteSource`** — decode sprites by id (any backing storage)
- **`IThingCatalogReader`** — read raw bytes → `ThingCatalog`
- **`IThingCatalogWriter`** — write `ThingCatalog` → stream

This guide shows how to implement each interface for your own format.

## Why custom formats?

You might want:

- **JSON / TOML / XML** thing definitions (human-readable, version-control friendly)
- **PNG atlas** sprites (one large image + JSON manifest instead of `.spr` lookup table)
- **SQLite / Redis** backing (query sprites by id from a database)
- **Custom binary** format (your own compression, different sprite sizes, etc.)

The domain model (`ThingType`, `ThingFrameGroup`, `ThingKind`, `AnimationFrameTiming`) is format-agnostic — you populate it from any source.

## Built-in JSON format

NyxAssets ships with a **JSON reader and writer** as the primary alternative to `.dat`. All `.dat` flags, frame groups, sprite ids, and animation metadata are preserved. Game-logic properties (name, weight, armor, slotType, etc.) are stored in a `"properties"` object via `ThingType.ExtraProperties`. Native JSON types (numbers and booleans) are automatically used where applicable instead of wrapping every value in a string.

### Convert `.dat` → `.json`

```csharp
var catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);

// Optionally merge SERVER items.xml metadata during conversion
catalog.ExportJson("things.json", options, itemsXmlPath: "items.xml");
```

### Load from `.json`

```csharp
var catalog = ThingCatalog.LoadJson("things.json", options);
```

### Use `.json` as the source of truth

```csharp
var catalog = ThingCatalog.LoadJson("things.json", options);
// Loading legacy RLE-compressed sprite archive:
using var sprites = SpriteArchive.OpenReadOnlyFile("Nyx.spr", options);
// Or loading modern ZSTD page-based sprite archive:
// using var sprites = AssetArchive.OpenReadOnlyFile("Nyx.assets");
using var bundle = new ClientAssetBundle(catalog, sprites, disposeSprites: true);
```

### JSON schema

```json
{
  "items": [
    {
      "id": 2133,
      "pickupable": true,
      "frameGroups": [
        {
          "spriteIds": [42]
        }
      ],
      "properties": {
        "name": "ruby necklace",
        "weight": "570",
        "slotType": "necklace",
        "armor": "0"
      }
    }
  ],
  "outfits": [
    {
      "id": 128,
      "cloth": true,
      "frameGroups": [
        {
          "patternX": 4,
          "patternY": 4,
          "spriteIds": [100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115]
        }
      ]
    }
  ],
  "effects": [
    {
      "id": 1,
      "frameGroups": [
        {
          "frames": 4,
          "isAnimation": true,
          "animationMode": 0,
          "loopCount": 0,
          "startFrame": 0,
          "frameTimings": [
            { "min": 100, "max": 150 }
          ],
          "spriteIds": [200, 201, 202, 203]
        }
      ]
    }
  ],
  "missiles": [
    {
      "id": 1,
      "frameGroups": [
        {
          "spriteIds": [300]
        }
      ]
    }
  ]
}
```

The writer only outputs non-default values — `width`, `height`, `layers`, etc. are omitted when they equal their defaults (1, 1, 1, etc.), keeping the file compact.

### `ThingType.ExtraProperties`

The `ExtraProperties` dictionary on `ThingType` holds arbitrary key-value string pairs. The JSON reader/writer maps the `"properties"` object to this dictionary. This is where game-logic attributes live:

| Key | Example | Description |
|-----|---------|-------------|
| `name` | `"ruby necklace"` | Display name |
| `weight` | `"570"` | Item weight |
| `slotType` | `"necklace"` | Equipment slot |
| `armor` | `"0"` | Armor value |
| `attack` | `"24"` | Attack value |
| `description` | `"A shiny ruby necklace."` | Item description |
| `max-slots` | `"8"` | Container capacity |

Any key is allowed — the dictionary is open-ended. Sandbox's `ItemType.FromThing` reads these properties automatically.

### Round-trip: `.dat` → `.json` → `.dat`

```csharp
var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098) };

var catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);
catalog.ExportJson("things.json", options);

var reloaded = ThingCatalog.LoadJson("things.json", options);
using var outDat = File.Create("roundtrip.dat");
reloaded.WriteDatTo(outDat, options);
```

### Exporting `.dat` to JSON (Sandbox export mode)

The Sandbox project includes a built-in export mode. Set `exportJson = true` under `[client]` in `Sandbox/config.toml` and run the project.

To merge attributes (e.g. name, weight, attack, armor, description) from Forgotten Server's `items.xml` during this export, make sure to also set the `itemsXml` key under `[client]` in `Sandbox/config.toml`:

```toml
[client]
assetsDir = 'C:\Users\Tofame\Desktop\blacktek-client\data\things\1098'
itemsXml = 'c:\Users\Tofame\Desktop\forgottenserver-10.98-main\data\items\items.xml'
exportJson = true
```

This will:
1. Load your `Nyx.dat` file (path from `assetsDir` in `config.toml`)
2. Optionally merge properties from `items.xml` if `itemsXml` is configured
3. Export all items, outfits, effects, and missiles to `Sandbox/resources/things.json`
4. Print a summary: `Exported 1234 items, 567 outfits, 89 effects, 45 missiles to ...`
5. Exit immediately

After export, change `exportJson = false` in `Sandbox/config.toml` and run the project normally:

```powershell
dotnet run --project Sandbox
```

Sandbox will now load `things.json` instead of `Nyx.dat`. You can edit the JSON file to customize item properties, add new items, or remove unwanted entries.

### What gets exported

The JSON writer preserves **all** `.dat` data:

- **All thing types** — items, outfits, effects, missiles
- **All flags** — `isGround`, `stackable`, `pickupable`, `cloth`, `clothSlot`, etc.
- **Frame groups** — dimensions, patterns, animation metadata, sprite id arrays
- **Extra properties** — any key-value pairs in `ThingType.ExtraProperties` (game-logic attributes like `name`, `weight`, `armor`)

### What doesn't get exported

- **Sprite pixels** — the `.spr` file is separate; JSON only references sprite ids
- **Client version** — derived from `ClientDataReadOptions` at load time, not stored in JSON
- **Binary signature** — not needed; format is inferred from client version

### Customizing exported JSON

After export, you can:

- **Add game-logic properties** to items:
  ```json
  {
    "id": 2160,
    "pickupable": true,
    "stackable": true,
    "frameGroups": [
      {
        "spriteIds": [12345]
      }
    ],
    "properties": {
      "name": "Gold Coin",
      "weight": "0.10",
      "description": "Shiny gold."
    }
  }
  ```

- **Remove unused items** — delete entries from the `"items"` array
- **Add new items** — append new objects to the `"items"` array with your own sprite ids
- **Edit flags** — change `"pickupable": false` to `"pickupable": true`, etc.

### Programmatic export

You can also export from C# code:

```csharp
using NyxAssets.Things;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

var catalog = ThingCatalog.Load(File.ReadAllBytes("Nyx.dat"), options);

// Export to file
catalog.ExportJson("things.json", options);

// Or to stream
using var fs = File.Create("things.json");
catalog.ExportJson(fs, options);
```

### Loading from exported JSON

When `Sandbox/resources/assets/things.json` exists, Sandbox automatically uses it instead of `Nyx.dat`:

```csharp
// Sandbox does this automatically:
var jsonCatalog = ThingCatalog.LoadJson("things.json", options);
// Checks if Nyx.assets exists, otherwise falls back to Nyx.spr:
var sprSource = AssetArchive.OpenReadOnlyFile("Nyx.assets");
var bundle = new ClientAssetBundle(jsonCatalog, sprSource, disposeSprites: true);
```

If `things.json` doesn't exist, Sandbox falls back to loading `Nyx.dat` + `Nyx.spr`/`Nyx.assets` (original behavior).

## Implementing `ISpriteSource`

`ISpriteSource` is the contract for **random-access sprite decoding**. Implement it to back sprite lookups with anything that can produce 32×32 RGBA pixels.

### Interface

```csharp
public interface ISpriteSource : IDisposable
{
    uint SpriteCount { get; }
    bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination);
    byte[] DecodeSpriteById(uint spriteId);
    bool IsEmptySprite(uint spriteId);
}
```

### Example: PNG atlas

Suppose you have a single `atlas.png` (4096×4096) and a `manifest.json` mapping sprite ids to `(x, y)` coordinates:

```csharp
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NyxAssets.Sprites;

public sealed class PngAtlasSpriteSource : ISpriteSource
{
    private readonly Image<Rgba32> _atlas;
    private readonly Dictionary<uint, (int X, int Y)> _locations;

    public PngAtlasSpriteSource(string atlasPath, string manifestPath)
    {
        _atlas = Image.Load<Rgba32>(atlasPath);
        var json = File.ReadAllText(manifestPath);
        _locations = JsonSerializer.Deserialize<Dictionary<uint, (int X, int Y)>>(json)!;
        SpriteCount = (uint)_locations.Count;
    }

    public uint SpriteCount { get; }

    public bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)
    {
        if (rgbaDestination.Length < SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException("Buffer must be at least 4096 bytes.", nameof(rgbaDestination));

        if (!_locations.TryGetValue(spriteId, out var loc))
            return false;

        var edge = SpritePixelCodec.SpriteEdgeLength;
        for (var y = 0; y < edge; y++)
        {
            for (var x = 0; x < edge; x++)
            {
                var pixel = _atlas[loc.X + x, loc.Y + y];
                var o = (y * edge + x) * 4;
                rgbaDestination[o] = pixel.R;
                rgbaDestination[o + 1] = pixel.G;
                rgbaDestination[o + 2] = pixel.B;
                rgbaDestination[o + 3] = pixel.A;
            }
        }
        return true;
    }

    public byte[] DecodeSpriteById(uint spriteId)
    {
        var buf = new byte[SpritePixelCodec.RgbaBufferLength];
        if (!TryDecodeSpriteById(spriteId, buf))
            throw new InvalidDataException($"Sprite {spriteId} is missing.");
        return buf;
    }

    public bool IsEmptySprite(uint spriteId) => !_locations.ContainsKey(spriteId);

    public void Dispose() => _atlas.Dispose();
}
```

### Example: in-memory dictionary

For testing or small projects, a simple dictionary works:

```csharp
public sealed class DictionarySpriteSource : ISpriteSource
{
    private readonly Dictionary<uint, byte[]> _sprites = new();

    public DictionarySpriteSource() { }

    public void Add(uint spriteId, byte[] rgba)
    {
        if (rgba.Length < SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException("Expected 4096 bytes.");
        _sprites[spriteId] = rgba;
    }

    public uint SpriteCount => (uint)_sprites.Count;

    public bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)
    {
        if (!_sprites.TryGetValue(spriteId, out var rgba))
            return false;
        rgba.AsSpan().CopyTo(rgbaDestination);
        return true;
    }

    public byte[] DecodeSpriteById(uint spriteId)
    {
        if (!_sprites.TryGetValue(spriteId, out var rgba))
            throw new InvalidDataException($"Sprite {spriteId} is missing.");
        return rgba;
    }

    public bool IsEmptySprite(uint spriteId) => !_sprites.ContainsKey(spriteId);

    public void Dispose() { }
}
```

## Implementing `IThingCatalogReader`

`IThingCatalogReader` parses raw bytes into a `ThingCatalog`. Implement it to support JSON, XML, or custom binary formats.

### Interface

```csharp
public interface IThingCatalogReader
{
    ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options);
}
```

### Example: JSON reader

Suppose your thing definitions are in `things.json`:

```json
{
  "signature": 12345678,
  "items": [
    {
      "id": 100,
      "isGround": true,
      "groundSpeed": 100,
      "frameGroups": [
        {
          "width": 1, "height": 1,
          "layers": 1, "patternX": 1, "patternY": 1, "patternZ": 1, "frames": 1,
          "spriteIds": [42]
        }
      ]
    }
  ],
  "outfits": [],
  "effects": [],
  "missiles": []
}
```

```csharp
using System.Text.Json;
using NyxAssets.Things;

public sealed class JsonThingCatalogReader : IThingCatalogReader
{
    public ThingCatalog Read(ReadOnlyMemory<byte> data, ClientDataReadOptions options)
    {
        var json = JsonSerializer.Deserialize<JsonCatalog>(data.Span)!;

        var catalog = new ThingCatalog();
        catalog.DatSignature = json.Signature;
        catalog.DatFormat = DatThingFormat.V6_10_10__10_56;

        foreach (var item in json.Items)
            catalog.PutItem(BuildThing(item, ThingKind.Item));

        foreach (var outfit in json.Outfits)
            catalog.PutOutfit(BuildThing(outfit, ThingKind.Outfit));

        foreach (var effect in json.Effects)
            catalog.PutEffect(BuildThing(effect, ThingKind.Effect));

        foreach (var missile in json.Missiles)
            catalog.PutMissile(BuildThing(missile, ThingKind.Missile));

        return catalog;
    }

    private static ThingType BuildThing(JsonThing json, ThingKind kind)
    {
        var thing = new ThingType
        {
            Id = json.Id,
            Kind = kind,
            IsGround = json.IsGround,
            GroundSpeed = json.GroundSpeed,
        };

        foreach (var fg in json.FrameGroups)
        {
            var group = new ThingFrameGroup
            {
                Width = fg.Width,
                Height = fg.Height,
                Layers = fg.Layers,
                PatternX = fg.PatternX,
                PatternY = fg.PatternY,
                PatternZ = fg.PatternZ,
                Frames = fg.Frames,
                SpriteIds = fg.SpriteIds,
            };
            thing.FrameGroups.Add(group);
        }

        return thing;
    }

    private record JsonCatalog(
        uint Signature,
        JsonThing[] Items,
        JsonThing[] Outfits,
        JsonThing[] Effects,
        JsonThing[] Missiles
    );

    private record JsonThing(
        uint Id,
        bool IsGround,
        uint GroundSpeed,
        JsonFrameGroup[] FrameGroups
    );

    private record JsonFrameGroup(
        uint Width, uint Height,
        uint Layers, uint PatternX, uint PatternY, uint PatternZ, uint Frames,
        uint[] SpriteIds
    );
}
```

## Implementing `IThingCatalogWriter`

`IThingCatalogWriter` serializes a `ThingCatalog` to a stream. Implement it to write JSON, XML, or custom formats.

### Interface

```csharp
public interface IThingCatalogWriter
{
    void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions options, uint? signatureOverride = null);
}
```

### Example: JSON writer

```csharp
using System.Text.Json;
using NyxAssets.Things;

public sealed class JsonThingCatalogWriter : IThingCatalogWriter
{
    public void Write(ThingCatalog catalog, Stream output, ClientDataReadOptions options, uint? signatureOverride = null)
    {
        var json = new JsonCatalog(
            Signature: signatureOverride ?? catalog.DatSignature,
            Items: catalog.EnumerateItems().Select(ToJsonThing).ToArray(),
            Outfits: catalog.EnumerateOutfits().Select(ToJsonThing).ToArray(),
            Effects: catalog.EnumerateEffects().Select(ToJsonThing).ToArray(),
            Missiles: catalog.EnumerateMissiles().Select(ToJsonThing).ToArray()
        );

        JsonSerializer.Serialize(output, json, new JsonSerializerOptions { WriteIndented = true });
    }

    private static JsonThing ToJsonThing(ThingType thing) => new(
        Id: thing.Id,
        IsGround: thing.IsGround,
        GroundSpeed: thing.GroundSpeed,
        FrameGroups: thing.FrameGroups.Select(fg => new JsonFrameGroup(
            Width: fg.Width, Height: fg.Height,
            Layers: fg.Layers, PatternX: fg.PatternX, PatternY: fg.PatternY, PatternZ: fg.PatternZ, Frames: fg.Frames,
            SpriteIds: fg.SpriteIds
        )).ToArray()
    );

    private record JsonCatalog(
        uint Signature,
        JsonThing[] Items,
        JsonThing[] Outfits,
        JsonThing[] Effects,
        JsonThing[] Missiles
    );

    private record JsonThing(
        uint Id,
        bool IsGround,
        uint GroundSpeed,
        JsonFrameGroup[] FrameGroups
    );

    private record JsonFrameGroup(
        uint Width, uint Height,
        uint Layers, uint PatternX, uint PatternY, uint PatternZ, uint Frames,
        uint[] SpriteIds
    );
}
```

## Using custom formats with `ClientAssetBundle`

`ClientAssetBundle` has a **public constructor** that accepts any `ISpriteSource`:

```csharp
public ClientAssetBundle(ThingCatalog things, ISpriteSource sprites, bool disposeSprites = false)
```

### Example: JSON things + PNG atlas sprites

```csharp
using NyxAssets.Client;
using NyxAssets.Things;

var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098) };

var reader = new JsonThingCatalogReader();
var catalog = reader.Read(File.ReadAllBytes("things.json"), options);

using var sprites = new PngAtlasSpriteSource("atlas.png", "manifest.json");
using var bundle = new ClientAssetBundle(catalog, sprites);

var item = bundle.GetItem(100);
byte[] pixels = bundle.DecodeSpriteById(item.FrameGroups[0].SpriteIds[0]);
```

### Example: custom reader + built-in `.spr`

```csharp
using NyxAssets.Client;
using NyxAssets.Sprites;
using NyxAssets.Things;

var options = new ClientDataReadOptions { ClientVersion = new ClientDataVersion(1098) };

var reader = new JsonThingCatalogReader();
var catalog = reader.Read(File.ReadAllBytes("things.json"), options);

using var sprites = SpriteArchive.OpenReadOnlyFile("Nyx.spr", options);
using var bundle = new ClientAssetBundle(catalog, sprites, disposeSprites: true);
```

## Exporting with custom sprite sources

`SpriteImageExporter` and `ThingSpriteSheetExporter` accept any `ISpriteSource`:

```csharp
using NyxAssets.Utils;

using var sprites = new PngAtlasSpriteSource("atlas.png", "manifest.json");
SpriteImageExporter.TryDecodeAndWritePng(sprites, 42, "sprite_42.png");

ThingType thing = /* ... */;
ThingSpriteSheetExporter.TryWriteThingSpriteSheetPng(sprites, thing, "thing_sheet.png");
```

## Tips for custom format implementations

1. **Reuse `SpritePixelCodec` constants** — `SpriteEdgeLength` (32) and `RgbaBufferLength` (4096) define the expected decoded sprite size.

2. **`ThingCatalog` has a public parameterless constructor** — build it manually, set `DatSignature` / `DatFormat`, then call `PutItem` / `PutOutfit` / `PutEffect` / `PutMissile` to populate.

3. **`ClientDataReadOptions` is optional for custom formats** — your reader can ignore it if your format doesn't need version-dependent parsing.

4. **Implement `IDisposable` on `ISpriteSource`** — even if you have nothing to dispose, the interface requires it (for memory-mapped files, large images, etc.).

5. **Test with `ClientAssetBundle`** — if your custom source works with the bundle, it works with all exporters and drawers in the ecosystem.

## See also

- [guides/usage.md](../guides/usage.md) — loading, decoding, exporting with the built-in `.dat` / `.spr` format.
- [formats/spr-binary.md](../formats/spr-binary.md) — `.spr` layout (for reference when implementing your own sprite storage).
- [formats/dat-binary.md](../formats/dat-binary.md) — `.dat` layout (for reference when implementing your own thing definitions).
- [json-mapper.md](json-mapper.md) — extending built-in JSON serialization.
- [README.md](../README.md) — documentation index.
