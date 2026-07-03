# Nyx `.dat` binary layout (thing metadata)

> [Documentation index](../README.md) · Format specs

NyxAssets’s `ThingCatalog` reads the same **linear** structure as **Asset Editor** `ThingTypeStorage.readBytes` + `MetadataReader` / `MetadataReader1`–`6`: a small fixed header, then **four sections** of thing types in a fixed order. Each thing is: **property flags** (byte stream ending with `0xFF`) followed by a **texture / sprite-index block**.

Endianness: **little-endian** for all multi-byte fields.

## File header (12 bytes)

| Offset | Type | Field |
|--------|------|--------|
| `0` | `uint32` | **datSignature** — client data version marker |
| `4` | `uint16` | **itemCount** — maximum item **id** (items are stored from id **100** upward) |
| `6` | `uint16` | **outfitCount** — maximum outfit id (outfits start at **1**) |
| `8` | `uint16` | **effectCount** — maximum effect id (effects start at **1**) |
| `10` | `uint16` | **missileCount** — maximum missile id (missiles start at **1**) |

Asset Editor defines the same offsets in `MetadataFilePosition` (`SIGNATURE`, `ITEMS_COUNT`, …).

### Id ranges used when loading

NyxAssets constants (`ThingCatalog`):

| Section | First id | Last id (inclusive) |
|---------|----------|----------------------|
| Items | `100` | `itemCount` |
| Outfits | `1` | `outfitCount` |
| Effects | `1` | `effectCount` |
| Missiles | `1` | `missileCount` |

The file stores **max ids**, not “count minus padding”; the loader iterates inclusive ranges as above (same as Asset Editor).

## After the header: four sequential sections

Order is **fixed**:

1. **Items** — ids `100` … `itemCount`
2. **Outfits** — ids `1` … `outfitCount`
3. **Effects** — ids `1` … `effectCount`
4. **Missiles** — ids `1` … `missileCount`

There is **no** per-section header beyond the global counts; the reader knows how many records to read from the counts.

## One `ThingType` record (repeated)

Each record is two parts.

### Part A — Property flags

- Read `uint8` **flag bytes** in a loop until a byte equals **`0xFF`** (`LastFlag`).
- Interpretation of each flag depends on **client / dat format** (Asset Editor selects `MetadataReader1` … `MetadataReader6` from client version; NyxAssets uses `DatThingFormat` the same way).
- Some flags consume extra following bytes (e.g. ground speed `uint16`, market name length-prefixed Latin-1 string).

If an unknown flag is seen for that format, NyxAssets throws (same idea as Asset Editor’s “unknown flag” error).

### Part B — Texture / sprite index block

Immediately after the closing `0xFF` of Part A, the **texture pattern** block is read. Layout depends on:

- **`DatThingFormat` V1–V2** — “legacy” layout: **pattern Z is forced to `1`** (Asset Editor `MetadataReader1` / `2` override).
- **`DatThingFormat` V3+** — “modern” layout: **pattern Z** is read from the file (`MetadataReader` base).

Other switches (aligned with Asset Editor `readTexturePatterns`):

- **`extendedSpriteIds`** — if true, each sprite index in the block is **`uint32`**; if false, **`uint16`**.
- **`improvedAnimations`** — if true, multi-frame things include animation metadata (`uint8` mode, `int32` loop count, `int8` start frame, per-frame min/max `uint32` durations). If false, durations are filled from defaults (Asset Editor uses settings; NyxAssets uses `ClientDataReadOptions` durations in ms).
- **`outfitFrameGroups`** — if true **and** the thing is an **outfit**, the block can contain **multiple frame groups** (leading `uint8` group count, then per-group type byte); otherwise a single implicit group.

The block describes:

- Width, height, exact size, layers, pattern X/Y/Z, frame count.
- A flat array of **sprite ids** (indices into the **`.spr`** file, **1-based**) of length `width × height × patternX × patternY × patternZ × frames × layers` (capped at 4096 indices in Asset Editor; same check in NyxAssets).

## End of file

After the last missile record, the reader must be at **EOF**. NyxAssets throws if **any bytes remain** (“extra bytes … wrong client version or format?”).

## Relationship to `.spr`

- `.dat` stores **references** (sprite ids) and **metadata** (flags, dimensions, animation).
- `.spr` / `.assets` stores **actual pixel data** for each id.
- The same **client build** (and extended / transparency flags) must be used for both files so counts, id widths, and pixel format line up.

## See also

- [spr-binary.md](spr-binary.md)
- [assets-binary.md](assets-binary.md)
- [guides/usage.md](../guides/usage.md)
- Asset Editor: `ThingTypeStorage.as`, `MetadataReader.as`, `MetadataFilePosition.as`
