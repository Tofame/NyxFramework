# Single-thing exchange (`Things/Exchange/`)

> **See also:** [formats/nyx-thing-json.md](../formats/nyx-thing-json.md) (JSON schema) · [formats/obd-binary.md](../formats/obd-binary.md) (OBD layout) · [json-mapper.md](json-mapper.md) (shared `ThingType` fields)

NyxAssets can export and import **one** item, outfit, effect, or missile at a time — with optional embedded sprite pixels — in formats suitable for tools and Object Builder interchange.

This is separate from the **full catalog** APIs (`ThingCatalog.ExportJson` / `LoadJson`, binary `.dat`), which read/write every thing in a client file at once.

---

## Why this exists

| Use case | Full catalog (`things.json`) | Single-thing exchange |
|----------|------------------------------|------------------------|
| Replace entire client metadata | Yes | No |
| Share one outfit between projects | Awkward (splice arrays) | Yes |
| Import from Object Builder `.obd` | No | Yes |
| Version-control one item change | Large diff | Small, focused file |
| Portable file with sprite pixels | No (ids only) | Yes (JSON or OBD) |

The exchange layer sits on top of the same domain model (`ThingType`, `ThingFrameGroup`) and reuses `ThingTypeJsonMapper` for JSON field names.

---

## Architecture

```
                         ThingDocument
                    (ThingType + metadata
                     + optional SpritesRgba)
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
 ThingDocumentJsonCodec   ObdThingCodec      ThingDocument
  (nyx-thing JSON)         (.obd binary)      .ImportInto(catalog)
         │                    │
         │                    ├── FlashLzmaCodec      (LZMA wrapper)
         │                    ├── ObdPropertyCodec    (OBD flag stream)
         │                    ├── ObdTextureCodec     (frame groups + sprites)
         │                    └── ObdSpritePixels     (ARGB ↔ RGBA, RLE)
         │
         └── ThingJsonCodec → ThingTypeJsonMapper
```

### Public entry points

| Type | Namespace | Role |
|------|-----------|------|
| **`ThingDocument`** | `NyxAssets.Things.Exchange` | In-memory portable thing + optional pixels |
| **`ThingDocumentJsonCodec`** | `NyxAssets.Things.Exchange` | Read/write `nyx-thing` JSON files |
| **`ObdThingCodec`** | `NyxAssets.Things.Exchange` | Read/write Object Builder `.obd` files |
| **`ThingJsonCodec`** | `NyxAssets.Things` | Read/write one `ThingType` JSON object (no envelope) |
| **`ObdVersions`** | `NyxAssets.Things.Exchange` | OBD version constants (`100`, `200`, `300`) |
| **`ThingKindNames`** | `NyxAssets.Things.Exchange` | `"item"` / `"outfit"` / `"effect"` / `"missile"` strings |

Internal helpers (`FlashLzmaCodec`, `ObdPropertyCodec`, `ObdTextureCodec`, `ObdSpritePixels`) are not public API; they mirror [Object Builder `OBDEncoder`](https://github.com/ottools/ObjectBuilder).

---

## `ThingDocument`

The central model wrapping everything needed to move one thing between NyxAssets, JSON, and OBD.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Thing` | `ThingType` | Metadata, flags, frame groups, `ExtraProperties` |
| `ClientVersion` | `uint` | Client build number (e.g. `1098`, `860`). Stored in OBD; optional in JSON |
| `ObdVersion` | `ushort` | `0` for JSON-only; `100`/`200`/`300` when loaded from or targeting OBD |
| `SpritesRgba` | `Dictionary<uint, byte[]>?` | Decoded 32×32 RGBA (`4096` bytes each), keyed by sprite id |

### Factory methods

```csharp
// Metadata only — sprite ids in frame groups, no pixel payloads
ThingDocument doc = ThingDocument.FromThing(thing, clientVersion: 1098);

// With embedded pixels from an open sprite archive
using var sprites = SpriteArchive.OpenReadOnlyFile("Nyx.spr", options);
ThingDocument portable = ThingDocument.FromThing(thing, sprites, options, embedSprites: true);
```

### Import into a catalog

```csharp
doc.ImportInto(catalog);                  // uses Thing.Id as-is
doc.ImportInto(catalog, assignId: 5000); // override id (required for OBD — see below)
```

`ImportInto` calls `PutItem` / `PutOutfit` / `PutEffect` / `PutMissile` on the matching catalog bucket. Catalog rules still apply (e.g. new outfits must use the next contiguous id when the bucket is empty).

### Helper

- **`EnumerateSpriteIds()`** — all unique ids referenced across frame groups
- **`CollectUniqueSpriteIds(thing)`** (internal) — used by codecs to validate embedded sprites

---

## JSON exchange (`ThingDocumentJsonCodec`)

### Format identifier

- **`format`:** `"nyx-thing"`
- **`formatVersion`:** `1`
- **`type`:** required — `"item"`, `"outfit"`, `"effect"`, or `"missile"`

All other thing fields use the same names as [formats/things-json.md](../formats/things-json.md) (`id`, flags, `frameGroups`, `properties`, …). See [formats/nyx-thing-json.md](../formats/nyx-thing-json.md) for the full schema and examples.

### Read / write

```csharp
using NyxAssets.Things.Exchange;

// Read
ThingDocument loaded = ThingDocumentJsonCodec.Read("coin.json");
ThingDocument fromBytes = ThingDocumentJsonCodec.Read(jsonBytes.AsMemory());

// Write
ThingDocumentJsonCodec.Write("coin.json", doc);
ThingDocumentJsonCodec.Write(doc, stream, indent: true, includeSprites: true);
```

Set **`includeSprites: false`** to emit metadata only (sprite ids in `frameGroups`, no `sprites` array).

### OBD ↔ JSON shortcuts

```csharp
ThingDocument fromObd = ThingDocumentJsonCodec.FromObd(obdBytes, options);
byte[] obdBytes = ThingDocumentJsonCodec.ToObd(doc, options, ObdVersions.Version3);
```

---

## OBD exchange (`ObdThingCodec`)

Object Builder **Object Builder Data** (`.obd`) files: LZMA-compressed payloads containing thing properties, frame layout, and **embedded sprite pixels**. Compatible with exports from [Object Builder / Wos-ObjectBuilder](https://github.com/ottools/ObjectBuilder).

### Read / write

```csharp
using NyxAssets.Things.Exchange;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

// Import
ThingDocument doc = ObdThingCodec.Read(@"C:\exports\item_test.obd", options);

// Export (requires SpritesRgba for every sprite id in frame groups)
byte[] obd = ObdThingCodec.Write(doc, options, ObdVersions.Version3);
ObdThingCodec.Write("outfit.obd", doc, options);
```

On read, **`options`** is optional — defaults are derived from the OBD header’s client version when omitted. Pass explicit options when you need a specific `TransparentSprites` or animation default.

### OBD versions

| Constant | Value | Read | Write | Sprite encoding |
|----------|-------|------|-------|-----------------|
| `ObdVersions.Version1` | `100` | Yes | Yes | RLE + length prefix (legacy; category as UTF string) |
| `ObdVersions.Version2` | `200` | Yes | Yes | Fixed 4096-byte Flash ARGB per sprite |
| `ObdVersions.Version3` | `300` | Yes | Yes | RLE + length prefix; outfit frame groups |

**Default on write:** v3 (`300`), or the document’s `ObdVersion` if already set. Outfits with multiple frame groups should use v3.

Full binary layout: [formats/obd-binary.md](../formats/obd-binary.md).

### Important: OBD does not store thing id

Object Builder assigns the id when you import into a `.dat`. After `ObdThingCodec.Read`, **`Thing.Id` is `0`**. Always pass an id when merging into your catalog:

```csharp
var doc = ObdThingCodec.Read("item_test.obd", options);
doc.ImportInto(catalog, assignId: 35000);
```

### Sprite pixel requirements for export

- Every id in `Thing.FrameGroups[*].SpriteIds` must exist in `SpritesRgba`.
- **v2:** each value is 4096 bytes RGBA (converted internally to Object Builder ARGB).
- **v3 / v1:** values are RLE-compressed for OBD; compressed size must be **≤ 4096 bytes** (Object Builder limit). Dense full-tile sprites may exceed this — use **v2** for those, or simplify artwork.

---

## End-to-end workflows

### 1 — Object Builder → Nyx catalog (metadata + pixels in memory)

```csharp
var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

var doc = ObdThingCodec.Read("item_test.obd", options);
doc.ImportInto(catalog, assignId: 35000);

// doc.SpritesRgba holds decoded pixels if you need to inject into a custom sprite source
```

Injecting pixels into `.spr` / `.assets` is **not** handled by exchange — you still need your own sprite allocation strategy (new ids, `SpriteSheetCompiler`, etc.).

### 2 — Catalog thing → portable JSON for editing

```csharp
var thing = catalog.GetOutfit(128);
using var bundle = ClientAssetBundle.OpenFromFiles("Nyx.dat", "Nyx.spr", options);

var doc = ThingDocument.FromThing(thing, bundle.Sprites, options);
ThingDocumentJsonCodec.Write("outfit-128.json", doc);
```

### 3 — JSON → OBD for Object Builder

```csharp
var doc = ThingDocumentJsonCodec.Read("outfit-128.json");
byte[] obd = ObdThingCodec.Write(doc, options, ObdVersions.Version3);
File.WriteAllBytes("outfit-128.obd", obd);
```

### 4 — Metadata-only JSON (references existing `.spr` ids)

```csharp
var doc = ThingDocument.FromThing(catalog.GetItem(2160));
ThingDocumentJsonCodec.Write("coin.json", doc, includeSprites: false);
// frameGroups[].spriteIds point at your client spr file
```

---

## Source files in `Things/Exchange/`

| File | Visibility | Responsibility |
|------|------------|----------------|
| `ThingDocument.cs` | public | Document model, `FromThing`, `ImportInto` |
| `ThingDocumentJsonCodec.cs` | public | `nyx-thing` JSON read/write, OBD↔JSON helpers |
| `ObdThingCodec.cs` | public | OBD v1–v3 read/write orchestration |
| `ObdVersions.cs` | public | Version constants |
| `ThingKindNames.cs` | public | Kind ↔ string ↔ OBD category byte |
| `FlashLzmaCodec.cs` | internal | Adobe Flash / Object Builder LZMA (via `Ecng.Lzma`) |
| `ObdPropertyCodec.cs` | internal | OBD property-flag stream (matches `OBDEncoder.readProperties`) |
| `ObdTextureCodec.cs` | internal | Frame groups + embedded sprite blobs |
| `ObdSpritePixels.cs` | internal | Flash ARGB ↔ Nyx RGBA; RLE via `SpritePixelCodec` |

Related **public** file outside the folder:

| File | Responsibility |
|------|----------------|
| `Things/ThingJsonCodec.cs` | Thin facade over `ThingTypeJsonMapper` for single-object JSON fields |

---

## Dependencies

- **`Ecng.Lzma`** — Flash-compatible LZMA compress/decompress for `.obd` outer wrapper
- **`System.Text.Json`** — JSON envelope (BCL)
- Existing **`SpritePixelCodec`** — RLE sprite payloads (shared with `.spr`)

---

## Testing

Regression tests live in `NyxAssets/Tests/`:

| Test class | Coverage |
|------------|----------|
| `ThingDocumentExchangeTests` | Synthetic JSON/OBD round-trips, catalog import |
| `ObdFixtureTests` | Real Object Builder export `Fixtures/Obd/item_test.obd` |

Run:

```bash
dotnet test NyxAssets/Tests/NyxAssets.Tests.csproj
```

The fixture file is a **2×2 item** exported from Object Builder (OBD v2, client 8.60, four sprites). It is copied to the test output directory automatically.

---

## Limitations and pitfalls

1. **No thing id in OBD** — assign on `ImportInto`.
2. **OBD v3 sprites are 4096-byte ARGB** (not `.spr` RLE) — matches Object Builder `OBDEncoder.encodeV3`.
3. **Animation block required when `frames > 1`** — Object Builder always reads animation metadata in that case, even if `isAnimation` was false in the model. NyxAssets fills default timings on export.
4. **Default export version** — v2 for items/effects/missiles; v3 only for multi-group outfits.
5. **Catalog id rules** — `PutOutfit` / `PutItem` still enforce contiguous append rules; importing id `128` into an empty catalog may fail — use the next free id or pre-populate the catalog.
6. **Sprite injection** — exchange decodes/encodes pixels but does not patch `.spr` / `.assets`; id remapping is your responsibility.
7. **Client version** — OBD stores the authoring client; property flags on v1 exports use the matching `DatThingFormat` tier. Mismatching client versions may mis-decode rare flags.
8. **v1 OBD write** — uses legacy `DatThingPropertySerializer` paths; prefer v2/v3 for new exports unless targeting very old Object Builder workflows.

---

## Adding fields to exchanged JSON

Single-thing JSON uses **`ThingTypeJsonMapper`**. To add a new `ThingType` property to JSON/OBD metadata:

1. Add the property on `ThingType`.
2. Register it in `ThingTypeJsonMapper` ([json-mapper.md](json-mapper.md)).
3. If the field also exists in OBD v2/v3 property stream, update `ObdPropertyCodec` to match Object Builder flag bytes.
4. Extend `ThingDocumentExchangeTests` or `ObdFixtureTests`.

Do **not** duplicate field lists in `ThingDocumentJsonCodec`.

---

## Related documentation

- [formats/things-json.md](../formats/things-json.md) — full catalog JSON (array of things)
- [formats/nyx-thing-json.md](../formats/nyx-thing-json.md) — single-thing JSON schema
- [formats/obd-binary.md](../formats/obd-binary.md) — OBD binary layout
- [custom-formats.md](custom-formats.md) — custom catalog readers/writers
- [guides/usage.md](../guides/usage.md) — usage examples including exchange
