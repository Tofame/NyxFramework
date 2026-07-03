# Format Specification: Legacy Nyx `.spr`

> [Documentation index](../README.md) · Format specs

The legacy `.spr` format is the sprite archive format historically used by the Nyx client. It supports random access but has significant limitations, including legacy padding bytes and an inefficient RLE compression algorithm.

## File Layout

All multi-byte integer values are stored in **little-endian** byte order.

```
┌─────────────────────────────────────────────────────────────┐
│ HEADER (6 or 8 bytes)                                        │
│   signature (4 bytes) + spriteCount (2 or 4 bytes)          │
├─────────────────────────────────────────────────────────────┤
│ LOOKUP TABLE (4 bytes × spriteCount)                       │
│   entry[i] = file address offset for sprite (i+1), or 0     │
├─────────────────────────────────────────────────────────────┤
│ SPRITE DATA BLOBS (variable size)                           │
│   each entry: 3-byte RGB key + u16 length + RLE payload     │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Header

There are two header variants:
* **Standard (Short Sprite Count)**: Used by clients below version 9.60.
* **Extended (Long Sprite Count)**: Used by client versions $\ge$ 9.60 (or when `extendedSpriteIds` is enabled).

| Field | Size (Short) | Size (Extended) | Type | Description |
|---|---|---|---|---|
| `signature` | 4 bytes | 4 bytes | `uint32_t` | A client-specific signature. |
| `spriteCount`| 2 bytes | 4 bytes | `uint16_t` / `uint32_t` | Total count of sprite entries in the archive. |

---

## 2. Lookup Table

The Lookup Table starts immediately after the header. It contains `spriteCount` entries.
Each entry is a 4-byte `uint32_t` offset (little-endian) representing the absolute byte position in the file where the sprite blob starts.

* An offset of `0` indicates an empty or fully transparent sprite.
* The lookup table is 0-indexed, corresponding to 1-based sprite IDs: sprite ID `id` corresponds to lookup index `id - 1`.
* Total lookup size is `spriteCount * 4` bytes.

---

## 3. Sprite Data Blob

If a lookup table offset is non-zero, seek to that address to read the sprite data blob.

| Offset | Size | Type | Name | Description |
|---|---|---|---|---|
| `+0` | 3 bytes | `uint8_t[3]` | `rgbKey` | Legacy color key / padding bytes. Historically set to magenta `(255, 0, 255)` for alpha-keying. Obsolete on modern systems but must be skipped. |
| `+3` | 2 bytes | `uint16_t` | `payloadLength` | Byte length of the compressed RLE payload following this field. |
| `+5` | `payloadLength` | `uint8_t[]` | `payload` | The compressed Run-Length Encoded (RLE) pixel payload. |

---

## 4. Run-Length Encoding (RLE) & Chunk Stream

Each decompressed sprite represents a $32 \times 32$ image ($1024$ pixels total). The payload consists of alternating transparency runs and colored runs:

1. **`transparentRunCount`** (`uint16_t`): The number of fully transparent pixels to output.
2. **`coloredRunCount`** (`uint16_t`): The number of colored pixels to read from the stream.
   * If `transparentSprites = false` (legacy mode), read $3 \times \text{coloredRunCount}$ bytes (RGB), forcing Alpha to 255.
   * If `transparentSprites = true` (alpha channel support), read $4 \times \text{coloredRunCount}$ bytes (RGBA).

This pattern repeats until the decompressed output buffer is filled (1024 pixels, or 4096 bytes of raw RGBA).

---

## 5. Random Access & Loading Options

The lookup table allows the reader to jump straight to one sprite's blob by ID without scanning the file from the start.

NyxAssets offers two primary runtime loading strategies:

1. **Lazy Loading (Default)**:
   * **In-Memory**: `SpriteArchive.Load(ReadOnlyMemory<byte>, ClientDataReadOptions)` keeps the raw compressed file bytes in managed memory, decoding sprites on demand.
   * **Memory-Mapped**: `SpriteArchive.OpenReadOnlyFile(path, ClientDataReadOptions)` memory-maps the file, allowing the OS to page file data ranges on demand and decompressing sprites on the fly.
2. **Preloading (`preloadSprites = true` / `inMemoryLoading = true` in config)**:
   * Decodes all RLE-compressed sprites *upfront* at startup into an in-memory array (`byte[][]`).
   * Subsequent lookups via `TryCopySpriteRgba` bypass all file seeks, RLE parsing, and decoding logic, returning raw RGBA buffers in $O(1)$ from RAM. This is ideal for completely eliminating runtime CPU decompression stutters.

---

## Downsides of `.spr`

1. **Legacy Magenta Bytes**: Every non-empty sprite blob contains an obsolete 3-byte RGB pad, which wastes 3 bytes per sprite (almost 1 MB wasted space for 300,000 sprites).
2. **Inefficient RLE**: RLE is ideal for flat, block-colored legacy drawings but scales poorly for modern sprites containing detailed gradients and transparencies.
3. **No Page-Level Clustering**: Reading many sprites requires many individual small file seeks. Lack of block compression means overall compression ratio is much lower than modern algorithm standards (e.g., ZSTD, Brotli).
