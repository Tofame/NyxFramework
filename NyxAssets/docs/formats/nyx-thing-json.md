# Format specification: `nyx-thing` JSON (single thing)

> **Implementation:** `ThingDocumentJsonCodec`, `ThingJsonCodec` → `ThingTypeJsonMapper`.  
> **Guide:** [development/thing-exchange.md](../development/thing-exchange.md)

The `nyx-thing` format is a **single-object** JSON document for exporting one item, outfit, effect, or missile. It extends the per-entry schema from [things-json.md](things-json.md) with a type envelope and optional embedded sprite pixels.

---

## 1. Root object

```json
{
  "format": "nyx-thing",
  "formatVersion": 1,
  "type": "item",
  "clientVersion": 1098,
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
    "weight": "0.10"
  },
  "sprites": [
    {
      "id": 12345,
      "encoding": "rgba",
      "data": "<base64>"
    }
  ]
}
```

### Envelope fields (exchange-specific)

| Field | Required | Description |
|-------|----------|-------------|
| `format` | recommended | Must be `"nyx-thing"` when present; other values are rejected |
| `formatVersion` | recommended | Currently `1` |
| `type` | **yes** | `"item"`, `"outfit"`, `"effect"`, or `"missile"` |
| `clientVersion` | no | Client build number (default `1098` on read if omitted) |
| `obdVersion` | no | Source OBD version (`100`/`200`/`300`) when converted from `.obd` |
| `sprites` | no | Embedded sprite pixels (see §3). Omitted for metadata-only exports |

All [things-json.md](things-json.md) thing fields (`id`, boolean flags, numerics, `frameGroups`, `properties`) appear at the **same level** as the envelope — not nested under a `thing` key.

---

## 2. Thing fields

Identical to one element of the `items` / `outfits` / `effects` / `missiles` arrays in a catalog `things.json`. See [things-json.md](things-json.md) for:

- Boolean flags (`pickupable`, `cloth`, …)
- Numeric properties (`groundSpeed`, `offsetX`, …)
- `frameGroups` (dimensions, patterns, animation, `spriteIds`)
- `properties` (game/server metadata via `ExtraProperties`)

The writer omits default values (same compaction rules as the catalog JSON writer).

---

## 3. Embedded sprites (`sprites` array)

Optional. When present, each entry provides pixel data for one sprite id referenced in `frameGroups`.

```json
"sprites": [
  {
    "id": 42,
    "encoding": "rgba",
    "data": "AAAAAAAA…"
  }
]
```

| Field | Description |
|-------|-------------|
| `id` | Sprite id (matches an entry in `frameGroups[].spriteIds`) |
| `encoding` | `"rgba"` (default) — 4096-byte base64 RGBA, 32×32, `R,G,B,A` per pixel |
| `encoding` | `"spr-rle"` or `"rle"` — base64 RLE payload (same codec as `.spr`; decompressed to RGBA on read) |
| `data` | Base64-encoded bytes |

On **write**, encoding is always `"rgba"`. Sprites are sorted by id for stable output.

When **`includeSprites: false`**, the writer omits the entire `sprites` array; only `spriteIds` in frame groups remain.

---

## 4. Examples by kind

### Item (metadata only)

```json
{
  "format": "nyx-thing",
  "formatVersion": 1,
  "type": "item",
  "clientVersion": 1098,
  "id": 2160,
  "pickupable": true,
  "stackable": true,
  "frameGroups": [{ "spriteIds": [5001] }],
  "properties": { "name": "Gold Coin" }
}
```

### Outfit (with pixels)

```json
{
  "format": "nyx-thing",
  "formatVersion": 1,
  "type": "outfit",
  "clientVersion": 1098,
  "id": 128,
  "cloth": true,
  "clothSlot": 1,
  "frameGroups": [
    {
      "groupTypeId": 0,
      "patternX": 4,
      "patternY": 4,
      "spriteIds": [100, 101, 102]
    }
  ],
  "sprites": [
    { "id": 100, "encoding": "rgba", "data": "…" }
  ]
}
```

### Effect (animated)

```json
{
  "format": "nyx-thing",
  "formatVersion": 1,
  "type": "effect",
  "id": 3,
  "frameGroups": [
    {
      "frames": 4,
      "isAnimation": true,
      "animationMode": 0,
      "loopCount": 0,
      "startFrame": 0,
      "frameTimings": [
        { "min": 100, "max": 100 },
        { "min": 100, "max": 100 },
        { "min": 100, "max": 100 },
        { "min": 100, "max": 100 }
      ],
      "spriteIds": [200, 201, 202, 203]
    }
  ]
}
```

### Missile (distance effect)

```json
{
  "format": "nyx-thing",
  "formatVersion": 1,
  "type": "missile",
  "id": 1,
  "frameGroups": [
    {
      "patternX": 3,
      "spriteIds": [300, 301, 302]
    }
  ]
}
```

---

## 5. Comparison with catalog JSON

| | `things.json` | `nyx-thing` |
|--|---------------|-------------|
| Scope | Entire client catalog | One thing |
| Root keys | `items`, `outfits`, … arrays | `type` + flat thing fields |
| `format` | none | `"nyx-thing"` |
| Embedded sprites | no | optional `sprites` array |
| API | `ThingCatalog.LoadJson` / `ExportJson` | `ThingDocumentJsonCodec.Read` / `Write` |

You can convert between them manually: one `nyx-thing` file corresponds to one array element (plus envelope and optional sprites).

---

## 6. API

```csharp
using NyxAssets.Things.Exchange;

ThingDocument doc = ThingDocumentJsonCodec.Read("thing.json");
ThingDocumentJsonCodec.Write("thing.json", doc, indent: true, includeSprites: true);
```

See [development/thing-exchange.md](../development/thing-exchange.md) for OBD conversion and catalog import.
