# Format Specification: Modern Page-Based ZSTD `.assets`

> [Documentation index](../README.md) · Format specs

The `.assets` format is a modern, high-performance sprite archive format designed as a successor to Nyx's legacy `.spr` format. It completely eliminates legacy color-key padding, uses page-level ZSTD compression for better compression ratios, and supports lazy page loading via an LRU cache.

## File Layout

All multi-byte integer values are stored in **little-endian** byte order.

```
┌─────────────────────────────────────────────────────────────┐
│ HEADER (16 bytes)                                           │
│   magic ('ASST') + version + pageCount + spriteCount        │
├─────────────────────────────────────────────────────────────┤
│ SPRITE INDEX (8 bytes × spriteCount)                        │
│   entry[i] = pageId (4 bytes) + localIndex (4 bytes)        │
├─────────────────────────────────────────────────────────────┤
│ PAGE TABLE (20 bytes × pageCount)                           │
│   offset (8B) + compressed (4B) + uncompressed (4B) + count │
├─────────────────────────────────────────────────────────────┤
│ COMPRESSED PAGE BLOBS (variable size)                       │
│   each page contains ZSTD-compressed Sprite structs         │
└─────────────────────────────────────────────────────────────┘
```

---

## 1. Header

The header contains basic file metadata. It is exactly 16 bytes.

```cpp
struct AssetsHeader
{
    uint32_t magic;         // 'ASST' (0x54535341)
    uint32_t version;       // Format version (currently 1)
    uint32_t pageCount;     // Total number of pages
    uint32_t spriteCount;   // Total number of sprites in the archive
};
```

---

## 2. Sprite Index

Immediately following the header, the Sprite Index contains mapping data to locate which page and local index a sprite belongs to. It has `spriteCount` elements.

```cpp
struct SpriteIndexEntry
{
    uint32_t pageId;        // 0-based page ID
    uint32_t localIndex;    // 0-based index of the sprite within that page
};
```

* Sprite ID `id` (1-based) corresponds to index `id - 1` in this table.
* Total index size is `spriteCount * 8` bytes.

---

## 3. Page Table

Immediately following the Sprite Index, the Page Table describes the locations and sizes of all compressed pages. It has `pageCount` elements.

```cpp
struct PageEntry
{
    uint64_t offset;            // Absolute byte offset of the compressed page in the file
    uint32_t compressedSize;    // Size of the ZSTD-compressed page blob in bytes
    uint32_t uncompressedSize;  // Size of the page payload after decompression
    uint32_t spriteCount;       // Number of sprites packed inside this page
};
```

* Total page table size is `pageCount * 20` bytes.

---

## 4. Page Payload (Decompressed)

Once decompressed, the page payload is a packed sequence of variable-sized `Sprite` structures.

```cpp
struct Sprite
{
    uint16_t width;             // Pixel width
    uint16_t height;            // Pixel height
    uint8_t rgba[width * height * 4]; // Opaque or transparent pixels (RGBA8888)
};
```

* **Empty Sprites**: If a sprite is empty or fully transparent, it is written with `width = 0` and `height = 0` (consuming exactly 4 bytes in the payload and no pixel bytes).
* **Finding a Sprite**: Since sprites inside a page are packed sequentially and have variable sizes, a reader traverses the page payload starting from offset `0` and skips `4 + width * height * 4` bytes for each sprite until it reaches the target `localIndex`.

---

## 5. Runtime and Caching Strategy

The reader (`AssetArchive`) supports two runtime strategies:

### Strategy A: Lazy Loading with LRU Cache (Default)
Rather than loading the entire archive or keeping all sprites decompressed in memory:
1. **Startup**: The header, sprite index table, and page table are loaded into memory (~2.5 MB for 300,000 sprites). The file stream is kept open for random access.
2. **LRU Page Cache**: An LRU (Least Recently Used) cache holds up to a configurable number of decompressed page payloads in memory (default: `64` pages).
3. **On-Demand Decode**:
   * Look up the sprite ID in the `SpriteIndex` to get `pageId` and `localIndex`.
   * Check the LRU cache for `pageId`.
   * If cache misses: seek to the page's `offset` in the file, read `compressedSize` bytes, decompress the page using ZSTD into a `uncompressedSize` buffer, and add it to the cache (evicting the oldest page if the limit is exceeded).
   * Traverse the decompressed page payload to copy the target sprite's pixels.

### Strategy B: Preload All Pages (In-Memory Loading)
If `preloadPages = true` (or `inMemoryLoading = true` in Sandbox configuration) is set:
1. **Startup**: The entire archive file is read into memory, and all pages are decompressed *upfront* during load time.
2. **Bypass Cache**: All uncompressed page payloads are held in a static array.
3. **O(1) Sprite Decoding**:
   * Look up the sprite ID in the `SpriteIndex` to get `pageId` and `localIndex`.
   * Fetch the uncompressed page directly from memory without ZSTD decoding, cache lookups, locks, or evictions.
   * Traverse the page payload to copy the pixels.
   * This is ideal for machines with sufficient memory that require zero disk reads and zero runtime decompression overhead.
