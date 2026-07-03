# SpriteRenderer GPU cache

NyxRender does **not** load your whole `.spr` into VRAM. It keeps a **working set**: sprite ids you have drawn recently, packed into one or more **2048×2048 atlases**. When the cache is full or sprites go unused, the oldest entries are **evicted**. The next draw re-uploads pixels (from `TryDraw(id, rgba, …)` or NyxDrawer decoding `.spr`).

Policy is configured with **`SpriteRendererCacheOptions`** passed to the renderer constructor:

```csharp
var renderer = new SpriteRenderer(gl, width, height, new SpriteRendererCacheOptions(
    maxResidentSprites: 8192,
    maxAtlasCount: 8,
    maxIdleFrames: 3600));
```

Omit the third argument to use **`SpriteRendererCacheOptions.Default`** (same numbers as above).

---

## The three options (what they actually do)

### `MaxResidentSprites`

**Meaning:** Maximum number of **distinct sprite ids** that can have a GPU atlas slot at once.

- Each id is one 32×32 image in an atlas (one “slot”).
- Not the size of your `.spr` catalog (that can be 700k+ ids on disk).
- When you try to upload sprite id **60** and the cache already holds `MaxResidentSprites` ids, the renderer **evicts the least recently drawn** id(s) until there is room, then uploads **60**.

**Think:** “How many different tiles/outfit cells/effect frames do I want to keep hot in VRAM?”

**Scale:** One full atlas fits **4096** slots (`2048 ÷ 32 = 64`, `64×64 = 4096`). So:

| MaxResidentSprites | Roughly equivalent |
|--------------------|--------------------|
| 4096 | ~1 full atlas of unique ids |
| 8192 (default) | ~2 atlases worth |
| 12288 | ~3 atlases worth |
| 16384 | ~4 atlases worth |

You can set this **higher** than `MaxAtlasCount × 4096` in theory, but then you depend on eviction reusing slots inside existing atlases. In practice, keep `MaxResidentSprites` ≤ `MaxAtlasCount × 4096` unless you know you churn heavily and reuse slots.

---

### `MaxAtlasCount`

**Meaning:** Maximum number of **2048×2048 RGBA atlas textures** the renderer may allocate.

- Each atlas is about **16 MB** of GPU memory (`2048 × 2048 × 4` bytes).
- `GetStats().MemoryUsageMB` reports **`AtlasCount × 16 MB`** as an upper bound (even if atlases are half empty).
- New atlases are created only when **all existing atlases are full** and you still need a new slot.
- Eviction **frees slots inside** atlases; it does **not** destroy empty atlases (they stay allocated until you dispose the renderer).

**Think:** “Hard cap on 2D sprite VRAM budget for this renderer.”

| MaxAtlasCount | Approx. atlas VRAM cap |
|---------------|-------------------------|
| 4 | ~64 MB |
| 6 | ~96 MB |
| 8 (default) | ~128 MB |
| 12 | ~192 MB |
| 16 | ~256 MB |

This is only the **sprite atlas** budget. Your game also uses framebuffer, UI, post-processing, etc.

---

### `MaxIdleFrames`

**Meaning:** Evict a sprite if it has **not been drawn** for this many **`BeginFrame` calls** (not seconds).

**Important:** This value is in **frames**, not seconds.

```
idle time (seconds) ≈ MaxIdleFrames ÷ target FPS
```

Examples at **MaxIdleFrames = 3600** (default):

| Target FPS | Idle time before eviction |
|------------|---------------------------|
| 60 | ~60 s |
| 120 | ~30 s |
| 180 | ~20 s |

Set to **`0`** to disable idle eviction. The cache still respects **`MaxResidentSprites`** and **`MaxAtlasCount`**; only the “haven’t drawn this in a while” rule is off.

**When eviction runs:** Mostly at **`EndFrame()`** (idle + over-cap cleanup). Also **while uploading** a new sprite if the cache is full (LRU makes room immediately).

---

## How the pieces work together

```
.spr (700k ids)          GPU cache (your options)
     │                         │
     │  decode on demand       │  MaxResidentSprites ids max
     └──────────► TryDraw ────►│  packed into ≤ MaxAtlasCount atlases
                               │  idle > MaxIdleFrames → evict at EndFrame
```

1. You draw item **60** → decode once → upload to atlas → draw.
2. You draw many other items for a while → cache fills; LRU evicts old ids if needed.
3. You don’t draw **60** for a long time (in **frames**) → **60** is evicted at `EndFrame`.
4. You draw **60** again → decode + upload again (small hitch possible).

**Preload** (`NyxDrawer.Preloader`, `LoadSpriteRgba`) only warms the cache early; it is **not** required. Eviction applies to preloaded ids too.

---

## Presets by target frame rate

Goal for these presets:

- Keep roughly **~45–60 seconds** of idle time at your target FPS (adjust if you want snappier VRAM reclaim).
- Reasonable default VRAM for a Nyx-style 2D client on a **mid-range** PC.
- `MaxResidentSprites` ≤ `MaxAtlasCount × 4096`.

### 60 FPS

```csharp
SpriteRendererCacheOptions.For60Fps   // same as Default
```

Good starting point: 8192 ids, 8 atlases, 3600 idle frames (~60 s at 60 FPS).

### 120 FPS

At 120 FPS you get **twice as many frames per second**, so the same `MaxIdleFrames` means **half the wall-clock idle time**. Double the frame count to keep ~60 s idle:

```csharp
SpriteRendererCacheOptions.For120Fps
```

If VRAM is tight, use `new SpriteRendererCacheOptions(maxIdleFrames: 3600)` instead (~30 s idle at 120 FPS).

### 180 FPS

```csharp
SpriteRendererCacheOptions.For180Fps   // ~60 s idle at 180 FPS
```

For ~30 s idle at 180 FPS, use `5400`.

---

## “I have a very good PC” — should I use 6 atlases / 12288 / 600?

Those numbers mean different things; don’t treat them as one bundle.

| Setting | Example | What it implies |
|---------|---------|-----------------|
| `maxAtlasCount: 6` | ~96 MB atlas cap | **Conservative** VRAM, not “high-end max” |
| `maxResidentSprites: 12288` | ~3 atlases of unique ids | Fine if you often see **many** distinct sprites |
| `maxIdleFrames: 600` | At 60 FPS ≈ **10 s** idle | **Aggressive** eviction (frees VRAM quickly, more re-uploads) |

So **6 + 12288 + 600** on a strong PC is a mixed profile: a **large id working set**, a **moderate atlas budget**, and **short** idle retention. That can make sense if you want to cap VRAM at ~96 MB but still cache many ids briefly.

### High-end PC — what to raise instead

If you have plenty of VRAM (e.g. 8 GB+ dedicated, 2D game not hogging the rest):

```csharp
SpriteRendererCacheOptions.HighVram60Fps   // 16k ids, 12 atlases, ~60 s idle @ 60 FPS
SpriteRendererCacheOptions.HighVram120Fps  // same, idle scaled for 120 FPS
```

**Raise `MaxAtlasCount`** when you hit evictions every frame in busy scenes (stats: `EvictedTotal` climbing fast, visible stutter) and GPU memory is still fine.

**Raise `MaxResidentSprites`** when you routinely have more than ~8000 **different** sprite ids on screen or in a short walking path (rare ground items, many outfits).

**Raise `MaxIdleFrames`** (scaled by FPS) when sprites you still care about disappear from cache too soon and cause hitches when you pan back.

**Lower `MaxIdleFrames`** (e.g. 600–1800 at 60 FPS) when you want to **minimize VRAM** and accept more decode/upload work — not because the PC is weak, but because you prefer a smaller working set.

There is **no benefit** to setting limits **lower** than you need on a powerful machine; you only increase re-uploads and CPU decode. Pick limits from **VRAM budget** and **how long off-screen sprites should stay hot**, not from “modesty.”

---

## Choosing values (checklist)

1. **Pick target FPS** and desired idle seconds →  
   `MaxIdleFrames = idleSeconds × FPS`  
   (or use presets above).

2. **Pick atlas VRAM budget** →  
   `MaxAtlasCount = budgetMB ÷ 16` (round down).

3. **Pick id working set** →  
   `MaxResidentSprites` between **4096** and **`MaxAtlasCount × 4096`**  
   - Quiet game / small map: 4096–8192  
   - Busy town, many items: 8192–16384  

4. **Tune with `GetStats()`** after playing a heavy scene:

   | Stat | If too high / growing fast |
   |------|----------------------------|
   | `LoadedSprites` | Near cap → raise `MaxResidentSprites` or accept more churn |
   | `AtlasCount` | At `MaxAtlasCount` → raise cap or lower idle/count |
   | `EvictedTotal` | Spikes every frame → cache too small for scene |
   | `MemoryUsageMB` | `AtlasCount × 16` — compare to your GPU budget |

---

## API reminders

```csharp
// Default policy
var renderer = new SpriteRenderer(gl, w, h);

// Custom policy
var renderer = new SpriteRenderer(gl, w, h, myOptions);

// Inspect
var s = renderer.GetStats();
// s.LoadedSprites, s.AtlasCount, s.EvictedTotal, s.FrameIndex, s.MemoryUsageMB

// Manual trim (e.g. before loading a zone)
renderer.EvictDownTo(2048);
```

---

## See also

- [HIGH_LEVEL_API.md](HIGH_LEVEL_API.md) — draw/load/frame loop
- [TEXTURE_ATLAS_GUIDE.md](TEXTURE_ATLAS_GUIDE.md) — atlas layout
- [NyxDrawer README](../../NyxDrawer/README.md) — draw without preloading the whole `.spr`
