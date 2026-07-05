# Format specification: Object Builder `.obd`

> **Implementation:** `ObdThingCodec` and helpers in `Things/Exchange/`.  
> **Reference:** [Object Builder `OBDEncoder`](https://github.com/ottools/ObjectBuilder/blob/master/src/otlib/obd/OBDEncoder.as)  
> **Guide:** [development/thing-exchange.md](../development/thing-exchange.md)

**OBD** (Object Builder Data) is a single-thing binary format used by [Object Builder](https://github.com/ottools/ObjectBuilder) for export/import. Files use the `.obd` extension and are registered as a native file type in the AIR application.

NyxAssets reads and writes OBD v1, v2, and v3, matching Object Builder’s on-disk layout.

---

## 1. File structure

```
.obd file
└── LZMA-compressed payload (Adobe Flash ByteArray.compress(LZMA))
    ├── 5 bytes   LZMA properties
    ├── 8 bytes   uncompressed size (int64 LE)
    └── N bytes   compressed data
```

After LZMA decompression, the payload begins with a **version marker** (uint16 LE):

| First uint16 | Interpretation |
|--------------|----------------|
| `200` | OBD v2 |
| `300` | OBD v3 |
| `≥ 710` | OBD v1 — value is **client version**, not OBD version |
| other | unsupported |

---

## 2. OBD v2 / v3 (common header)

After version marker:

| Offset | Type | Field |
|--------|------|-------|
| +0 | uint16 | OBD version (`200` or `300`) — only when marker is 200/300 |
| +2 | uint16 | Client version (e.g. `860`, `1098`) |
| +4 | uint8 | Category: `1`=item, `2`=outfit, `3`=effect, `4`=missile |
| +5 | uint32 | Texture patterns offset (absolute byte offset in decompressed payload) |

Then **property flags** (see §4) until terminator byte `0xFF`.

At the texture offset:

- **Outfits + v3 only:** `uint8` frame group count, then per group:
  - `uint8` group id (Object Builder convention: `1` when only one group)
- Per frame group (all kinds):
  - Dimensions, patterns, animation block, embedded sprites (§5)

v2 and v3 share the same property stream and frame-group layout; they differ in **sprite encoding** and v3 **outfit frame groups**.

---

## 3. OBD v1 (legacy)

After client version (first uint16):

| Field | Type | Notes |
|-------|------|-------|
| Category | UTF-8 string | Flash `readUTF`: uint16 LE length + bytes (`"item"`, `"outfit"`, …) |
| Properties | flag stream | Uses `DatThingFormat` for client version (MetadataReader1–6) |
| Frame group | binary | Single group; patternZ always read |
| Sprites | id + RLE | uint32 id, uint32 size, `size` bytes RLE payload |

---

## 4. Property flag stream (v2 / v3)

Sequential uint8 flags until `0xFF` (`LAST_FLAG`). Non-terminal flags may carry extra bytes (speed, offsets, market name, …).

Matches Object Builder `OBDEncoder.readProperties` / NyxAssets `ObdPropertyCodec`. Flag bytes align with modern `.dat` MetadataReader6, plus:

| Flag | Meaning |
|------|---------|
| `0xFC` | `hasCharges` |
| `0xFD` | `floorChange` |
| `0xFE` | `usable` |

Market item flag `0x22` includes: category, tradeAs, showAs, name (uint16 length + Latin-1), restrict profession, restrict level.

**Thing id is not stored** in OBD.

---

## 5. Frame group block

Per frame group (after optional v3 outfit group header):

| Field | Type |
|-------|------|
| width | uint8 |
| height | uint8 |
| exactSize | uint8 (if width>1 or height>1; else 32) |
| layers | uint8 |
| patternX | uint8 |
| patternY | uint8 |
| patternZ | uint8 |
| frames | uint8 |

If `frames > 1` (v2/v3):

| Field | Type |
|-------|------|
| animationMode | uint8 |
| loopCount | int32 |
| startFrame | int8 |
| per frame | uint32 min ms, uint32 max ms |

Then **`totalSprites`** embedded sprite records (order matches Asset Editor sprite index layout).

---

## 6. Embedded sprites

### OBD v2

For each sprite slot:

| Field | Size |
|-------|------|
| spriteId | uint32 |
| pixels | **4096 bytes** Flash `BitmapData` order: **A, R, G, B** per pixel |

NyxAssets converts to/from Nyx RGBA (`R, G, B, A`) in `ObdSpritePixels`.

### OBD v1 / v3

For each sprite slot:

| Field | Size |
|-------|------|
| spriteId | uint32 |
| dataSize | uint32 |
| payload | `dataSize` bytes RLE (same family as `.spr`) |

**Constraint (Object Builder):** `dataSize ≤ 4096`. Dense artwork may fail export in v3; use v2 (fixed ARGB) instead.

---

## 7. Version selection guide

| Choose | When |
|--------|------|
| **v3 (`300`)** | Default; outfits with idle/walk frame groups; modern Object Builder |
| **v2 (`200`)** | Simple items/effects; large or dense sprites that exceed v3 RLE size limit |
| **v1 (`100`)** | Legacy clients; category string format; single frame group |

Object Builder UI picks v3 automatically for outfits when the client supports frame groups.

---

## 8. NyxAssets API

```csharp
using NyxAssets.Things.Exchange;

var options = new ClientDataReadOptions
{
    ClientVersion = new ClientDataVersion(1098),
    TransparentSprites = true,
};

ThingDocument doc = ObdThingCodec.Read("export.obd", options);
byte[] obd = ObdThingCodec.Write(doc, options, ObdVersions.Version3);
```

**Write requirements:**

- `doc.SpritesRgba` must contain every sprite id from frame groups
- Each RGBA buffer is 4096 bytes

**Read result:**

- `doc.ObdVersion` — detected version
- `doc.ClientVersion` — from file header
- `doc.Thing.Id` — always `0` (assign on catalog import)

---

## 9. Verified fixture

`NyxAssets/Tests/Fixtures/Obd/item_test.obd`:

| Property | Value |
|----------|-------|
| OBD version | v2 (`200`) |
| Client | 8.60 (`860`) |
| Kind | item |
| Layout | 2×2, 1 layer, 1 frame |
| Sprites | 4 (ids 182952–182955) |

Regression test: `ObdFixtureTests.ItemTestObd_RealFile_LoadsItemWithSprites`.

---

## 10. Related formats

| Format | Scope | Sprites |
|--------|-------|---------|
| `.dat` | Full catalog | ids only |
| `.spr` / `.assets` | Full sprite archive | all sprites |
| `.obd` | **One thing** | embedded |
| `nyx-thing` JSON | **One thing** | optional base64 |

See [nyx-thing-json.md](nyx-thing-json.md) and [development/thing-exchange.md](../development/thing-exchange.md).
