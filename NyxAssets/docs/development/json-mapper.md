# ThingTypeJsonMapper

`ThingTypeJsonMapper` (`Things/ThingTypeJsonMapper.cs`) is the **single source of truth** for mapping between `ThingType` and JSON. Both `JsonThingCatalogReader` and `JsonThingCatalogWriter` delegate to it — they only handle the catalog envelope (`items`, `outfits`, `effects`, `missiles` arrays).

**Why it exists:** Previously, reader and writer each maintained a long manual list of property names. Adding a field to `ThingType` required editing two files and was easy to forget. The mapper keeps names and read/write rules in one place.

For the **JSON schema** (what keys mean to tools and game data authors), see [formats/things-json.md](../formats/things-json.md).

---

## Architecture

```
JsonThingCatalogReader                JsonThingCatalogWriter
        │                                      │
        │  ReadThing(elem, kind)               │  WriteThing(writer, thing)
        └──────────────┬───────────────────────┘
                       ▼
              ThingTypeJsonMapper
                       │
       ┌───────────────┼───────────────┐
       ▼               ▼               ▼
 ReadScalarFields  ReadFrameGroups  ReadExtraProperties
 WriteScalarFields WriteFrameGroups WriteExtraProperties
       │
       ▼
 BoolFields[]  UIntFields[]  IntFields[]  (+ marketName)
```

### Scalar fields (descriptor tables)

Most `ThingType` properties are registered in static arrays:

| Array | C# type | JSON write rule | JSON read rule |
|-------|---------|-----------------|----------------|
| `BoolFields` | `bool` | Written only when `true` | Set when key present |
| `UIntFields` | `uint` | Written only when `!= 0` | Set when key present |
| `IntFields` | `int` | Written only when `!= 0` | Set when key present |

Each entry is a small record: **JSON name**, **getter**, **setter**.

```csharp
new("stackable", t => t.Stackable, (t, v) => t.Stackable = v),
```

`marketName` is handled separately (nullable string — written when non-null).

### Frame groups

`ReadFrameGroup` / `WriteFrameGroup` handle the `frameGroups` array:

- Dimensions, patterns, layers, `spriteIds`
- Animation block: `isAnimation`, `animationMode`, `loopCount`, `startFrame`, `frameTimings`
- Defaults omitted on write when they match Asset Editor defaults (`width`/`height`/`layers`/… = 1, `exactSize` = 32)

### Extra properties (`properties` object)

Game/server metadata (name, weight, slot type, etc.) lives in `ThingType.ExtraProperties` — a `Dictionary<string, string>` at runtime.

**Flat key:**

```json
"properties": { "weight": "4200" }
```

**Nested key** (dot notation in the dictionary, nested object in JSON):

```csharp
thing.ExtraProperties["abilities.regeneration"] = "50";
```

```json
"properties": {
  "abilities": {
    "value": "true",
    "regeneration": "50"
  }
}
```

The writer groups keys by the segment before the first `.`. The reader flattens nested objects back into dot keys (or a top-level `value` key for the parent).

Values are stored as strings in `ExtraProperties` but written as native JSON types when possible (`bool`, integer, float, else string).

---

## Public API (internal to the assembly)

| Method | Purpose |
|--------|---------|
| `ReadThing(JsonElement elem, ThingKind kind)` | Parse one thing object; `id` and section kind required. |
| `WriteThing(Utf8JsonWriter writer, ThingType thing)` | Emit one thing object (opens/closes its own JSON object). |

Catalog reader/writer call these per array element. You normally **do not** call the mapper from outside NyxAssets unless you add a new catalog format that reuses the same thing JSON shape.

---

## How to add a new field

### 1. Add the property to `ThingType`

```csharp
public bool MyNewFlag { get; set; }
```

If the field is also serialized in `.dat`, update `ThingPropertyDecoder` / `DatThingPropertySerializer` separately (different code path).

### 2. Register it in `ThingTypeJsonMapper`

Pick the correct descriptor array and use **camelCase** JSON name consistent with existing keys:

**Boolean flag:**

```csharp
// In BoolFields:
new("myNewFlag", t => t.MyNewFlag, (t, v) => t.MyNewFlag = v),
```

**Unsigned integer:**

```csharp
// In UIntFields:
new("myNewStat", t => t.MyNewStat, (t, v) => t.MyNewStat = v),
```

**Signed integer:**

```csharp
// In IntFields:
new("myOffset", t => t.MyOffset, (t, v) => t.MyOffset = v),
```

**String** (single field): add a dedicated block in `ReadScalarFields` / `WriteScalarFields` next to `marketName`.

**Do not** edit `JsonThingCatalogReader.cs` or `JsonThingCatalogWriter.cs` for scalar fields.

### 3. Document the schema

Add the field description to [formats/things-json.md](../formats/things-json.md) under the appropriate section (boolean flags, numeric properties, etc.).

### 4. Add a test

Extend `TestJsonCatalogRoundtrip_PreservesThingProperties` in `Tests/ThingCatalogTests.cs` (or add a focused test):

```csharp
item.MyNewFlag = true;
// ... export JSON, load JSON, assert readItem.MyNewFlag
```

Run:

```bash
dotnet test NyxAssets/Tests/NyxAssets.Tests.csproj
```

---

## Adding frame group fields

Frame groups are **not** table-driven yet. To add a new `ThingFrameGroup` JSON field:

1. Add the property on `ThingFrameGroup`.
2. Update `ReadFrameGroup` and `WriteFrameGroup` in `ThingTypeJsonMapper.cs`.
3. Document in [formats/things-json.md](../formats/things-json.md) § Frame groups.
4. Extend the JSON round-trip test.

Follow existing omit-default-on-write behavior when the default matches Asset Editor.

---

## Adding extra-property behavior

Extra properties are intentionally open-ended. Most server keys do not need mapper changes — any key in `ExtraProperties` round-trips through `ReadExtraProperty` / `WriteExtraProperties`.

Change those methods only if you need new **nesting rules** or value coercion behavior.

---

## Catalog-level JSON (not in the mapper)

These stay in `JsonThingCatalogReader` / `JsonThingCatalogWriter`:

- Root object keys: `items`, `outfits`, `effects`, `missiles`
- Batch load optimization: `Put*(thing, rebuildArrays: false)` then `InitializeFastArrays()`
- Future: top-level metadata (`datSignature`, format version) would be added at catalog reader/writer level, not in `ThingTypeJsonMapper`

---

## Checklist for JSON-related PRs

- [ ] Property added to `ThingType` (if applicable)
- [ ] Descriptor added to `ThingTypeJsonMapper` (or frame group read/write updated)
- [ ] [formats/things-json.md](../formats/things-json.md) updated
- [ ] Round-trip test passes
- [ ] No duplicate field lists in reader/writer

---

## See also

- [overview.md](overview.md) — full development guide
- [formats/things-json.md](../formats/things-json.md) — consumer-facing schema
- [custom-formats.md](custom-formats.md) — implementing alternate catalog formats
