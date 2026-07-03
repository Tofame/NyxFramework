# NyxRender — `SpriteRenderer` API guide

This document matches the **current** implementation in [`SpriteRenderer.cs`](../SpriteRenderer.cs) and [`Sprite.cs`](../Sprite.cs). Older revisions of this file described a **different** API (string keys, `Vector2`, configurable atlas size in the constructor); that design was never shipped.

---

## What you get

- **Fixed tile size:** every sprite is **32×32** (RGB or RGBA after load).
- **Integer sprite ids:** you assign stable `int` keys; the renderer maps them to **atlas UVs** (internal atlas texture size is **2048×2048**, fixed).
- **Batching:** draws that share an atlas are submitted as one GPU batch per atlas per frame (plain sprites via `SpriteBatch`; outfit/sprite *effects* via `EffectDrawBatch`).
- **No string lookups:** ids are your game’s or your asset pipeline’s problem.

Rough capacity: one full atlas holds **64×64 = 4096** sprites of 32×32. Many atlases are created automatically as slots fill.

---

## Construction & viewport

```csharp
var renderer = new SpriteRenderer(gl, viewportWidth, viewportHeight);
```

- **`gl`:** active Silk.NET `GL` context.
- **`viewportWidth` / `viewportHeight`:** initial orthographic size in **pixels** (origin top-left, +X right, +Y down — matches typical 2D UI/game screens).
- **`UpdateViewport(width, height)`:** call when the window or drawable size changes (same convention as the constructor).

There is **no** public `atlasSize` parameter; atlas dimensions are internal constants.

---

## Frame loop

Every frame:

1. **`BeginFrame()`** or **`BeginFrame(float timeSeconds)`** — clears batch state. Use the overload that passes **elapsed time in seconds** when you use animated shader effects (rainbow, snow, etc.).
2. Issue draws (any order is fine for correctness; batching groups by atlas).
3. **`EndFrame()`** — flushes `SpriteBatch` per atlas, then `EffectDrawBatch` (effects/outfits).

Re-entrant frames are not allowed (`BeginFrame` twice without `EndFrame` throws).

---

## Loading (`Load*`)

All loading **uploads** into an atlas slot and registers the id. Duplicate id → **`ArgumentException`**.

| Method | Payload |
|--------|---------|
| `LoadSprite(int spriteId, byte[] rgbData)` | **32×32×3** RGB → RGBA (A = 255). |
| `LoadSpriteRgba(int spriteId, ReadOnlySpan<byte> rgbaData)` | **32×32×4** RGBA. |
| `LoadSprites(int startSpriteId, byte[] binaryData, int spriteCount)` | Packed **RGB** sprites, `spriteCount × 3072` bytes minimum. |

There is **no** `LoadSprite(string, …)` or load-from-path on `SpriteRenderer`; use `Texture` + your own decode if you load files, then feed RGBA into `LoadSpriteRgba` or draw via `TryDraw` (below).

---

## The `Sprite` value

`Sprite` pairs **`int Id`** with **optional** **4096-byte RGBA** (`ReadOnlyMemory<byte>`).

- **`Sprite.Resident(id)`** — no pixels; **draw** assumes the id is **already** in the atlas (preload or prior `TryDraw` with pixels).
- **`Sprite.FromRgba(id, span)`** — copies pixels once into managed memory; first `Draw`/`TryDraw` uploads.

`Draw(in Sprite, …)` **throws** if the sprite cannot be resolved. **`TryDraw(in Sprite, …)`** returns `false` instead.

**`Color`** tint parameters use **`NyxRender.GraphicsDevice.Color`** (`byte` RGBA).

---

## Drawing — plain sprites

### Resident id (after `Load*`)

```csharp
renderer.BeginFrame(0f);
renderer.Draw(Sprite.Resident(20), 100f, 200f);
renderer.Draw(Sprite.Resident(20), 100f, 200f, new GraphicsDevice.Color(255, 255, 255, 180)); // tint
renderer.EndFrame();
```

### Lazy upload by id + span (**`TryDraw`**)

First time this **id** appears, **`rgba4096`** must be **exactly** `Sprite.Rgba32Length` (4096) bytes; it is uploaded. **Later draws with the same id ignore new span contents** (cached GPU slot). If atlas allocation fails, **`TryDraw`** returns `false`.

```csharp
Span<byte> rgba = stackalloc byte[Sprite.Rgba32Length];
// ... fill rgba ...

renderer.BeginFrame(0f);
renderer.TryDraw(42, rgba, 100f, 200f);
renderer.EndFrame();
```

Parameter order: **`(spriteId, rgba4096, x, y)`** and overload with **`Color tint`**.

### Layered ids at the same position

**`DrawComposite`** draws each id in order at the **same** `(x, y)` (each must already be resident):

```csharp
renderer.DrawComposite(new[] { 20, 35, 12, 14 }.AsSpan(), x, y);
// or IEnumerable<int> overload
```

---

## Drawing — effects & outfits

These paths use **`renderer.Shaders`** (`ShaderRegistry`). Your app must **register** required programs (see Sandbox / `ShaderRegistry` usage) before calling:

- **`TryDrawOutfitLayers`** — base + mask sprite ids, head/body/legs/feet **`Color`** palette, optional shader name (default **`outfit_default`**). **Both** ids must be resident and in the **same** atlas or the call fails. Use **`LoadSpritePair`** to guarantee co-location.
- **`LoadSpritePair(int idA, ReadOnlySpan<byte> rgbaA, int idB, ReadOnlySpan<byte> rgbaB)`** — loads two sprites (e.g. outfit base + mask) into the same atlas atomically. If one is already resident the other is loaded into that atlas. Pass `ReadOnlySpan<byte>.Empty` for already-resident sprites.
- **`TryDrawSpriteEffect`** — single sprite with a named effect shader (e.g. outlines). Supports lazy upload via the `(spriteId, rgba4096, …)` overload, mirroring **`TryDraw`**.

Failed preconditions → `false`, not throw (unlike plain `Draw`).

---

## Introspection

- **`IsSpriteResident(int spriteId)`** — id has an atlas slot.
- **`GetStats()`** → `SpriteRendererStats`: **`LoadedSprites`**, **`AtlasCount`**, **`MemoryUsageMB`**.

**`MemoryUsageMB`** is **approximate**: it assumes **every** atlas texture is **2048×2048×RGBA** (full size), not the sum of only used 32×32 tiles. Use it as an upper-bound / order-of-magnitude figure.

---

## Best practices

1. **One `BeginFrame` → many draws → one `EndFrame` per presented frame** — avoids redundant state changes.
2. **Choose ids deliberately** (monotonic, content hash, or asset table). Collisions overwrite semantics: **one id = one 32×32 image**.
3. **Prefer `Load*` at level load** when you know all tiles; use **`TryDraw(id, span, …)`** for streaming or tooling where the first visible frame can pay the upload.
4. **Resize:** call **`UpdateViewport`** when the window size changes so projection matches the drawable.
5. **Effects:** pass **time** into **`BeginFrame(time)`** for animated shaders.
6. **GPU cache eviction:** bounded LRU atlas cache via **`SpriteRendererCacheOptions`**. See **[SPRITE_CACHE.md](SPRITE_CACHE.md)** for what each option means, 60/120/180 FPS presets, and high-end PC tuning.

---

## Example: many preloaded sprites

```csharp
byte[] file = File.ReadAllBytes("sprites.rgb"); // your packed RGB stream
using var renderer = new SpriteRenderer(gl, width, height);
renderer.LoadSprites(startSpriteId: 0, file, spriteCount: 50_000);

renderer.BeginFrame(0f);
for (var i = 0; i < 10_000; i++)
{
    var ids = new[] { 20, 35, 12, 14 };
    renderer.DrawComposite(ids.AsSpan(), x: (i % 40) * 32f, y: (i / 40) * 32f);
}
renderer.EndFrame();

var stats = renderer.GetStats();
```

---

## See also

- [`README.md`](../README.md) — short overview and binary layout.
- [`TEXTURE_ATLAS_GUIDE.md`](TEXTURE_ATLAS_GUIDE.md) — atlas packing notes (verify against code if details differ).
- [`SpriteRenderer.cs`](../SpriteRenderer.cs) — source of truth for signatures.
