# NyxRender

Ultra-simple **GPU sprite renderer**: treat each sprite as a **32×32 texture with an ID**, with batching and grid-packed atlases.

## Documentation in this folder

| Doc | Content |
|-----|--------|
| [docs/HIGH_LEVEL_API.md](docs/HIGH_LEVEL_API.md) | Full **`SpriteRenderer`** / **`Sprite`** API (constructor, frame loop, load, draw, effects, stats). |
| [docs/SPRITE_CACHE.md](docs/SPRITE_CACHE.md) | **GPU cache options**, FPS presets (60/120/180), VRAM tuning. |
| [docs/TEXTURE_ATLAS_GUIDE.md](docs/TEXTURE_ATLAS_GUIDE.md) | Atlas strategies, uniform 32×32 packing, memory notes. |
| [docs/FIXES_SUMMARY.md](docs/FIXES_SUMMARY.md) | Fix log / summary notes. |
| [docs/REFACTOR_SUMMARY.md](docs/REFACTOR_SUMMARY.md) | Refactor notes (historical). |
| [docs/RENDERING_FIXES.md](docs/RENDERING_FIXES.md) | Rendering-related fix notes. |

---

## Core concept

Sprites are **32×32** pixels keyed by an **`int` id** in GPU atlases. You either **preload** (`LoadSprite` / `LoadSpriteRgba` / `LoadSprites`) or **upload on first draw** via `TryDraw(spriteId, rgba4096, x, y)` (same id later ignores new pixel data until you manage lifetime yourself).

```csharp
// After preload: draw by id using Sprite.Resident (no pixel payload)
renderer.BeginFrame();
renderer.Draw(Sprite.Resident(20), x: 100f, y: 200f);
renderer.EndFrame();

// Or lazy first-time upload + draw (id, RGBA span, position)
renderer.TryDraw(42, rgba4096, x: 100f, y: 200f);
```

## Key features

- **Small surface area**: `Draw` / `TryDraw` with `Sprite`, batched `DrawComposite`, lazy `TryDraw(id, span, …)`
- **GPU-oriented**: grid-packed atlases, batched rendering
- **High variety**: many distinct 32×32 sprites via multiple atlases

## Stats (`GetStats`)

`SpriteRendererStats` reports **loaded sprite count**, **atlas count**, **eviction count**, and approximate **atlas GPU memory**. A bounded **LRU cache** evicts unused sprites automatically (see `SpriteRendererCacheOptions`); you do not preload the whole `.spr`.

## Usage example

```csharp
var renderer = new SpriteRenderer(gl, screenWidth, screenHeight);

byte[] allSprites = File.ReadAllBytes("animals.dat");
renderer.LoadSprites(0, allSprites, 50000);

renderer.BeginFrame();

for (int i = 0; i < 10000; i++)
{
    var dogOutfit = new[] { 20, 35, 12, 14 };
    renderer.DrawComposite(dogOutfit.AsSpan(), x: GetRandomX(), y: GetRandomY());
}

renderer.EndFrame();

var stats = renderer.GetStats();
Console.WriteLine($"Sprites: {stats.LoadedSprites}, Atlases: {stats.AtlasCount}");
Console.WriteLine($"GPU (atlas textures, approx): {stats.MemoryUsageMB:F0} MB");
```

## Architecture (names)

1. **SpriteRenderer** — main API (`Draw`, `LoadSprite`, atlas-backed storage)
2. **Sprite atlas grid** — large textures holding many 32×32 slots
3. **SpriteBatch** — batches draw calls for efficiency

## Binary layout (simple RGB stream)

Your file can be modeled as: `[32×32 RGB][32×32 RGB]…`

```
Per sprite: 32 × 32 × 3 bytes = 3,072 bytes
200k sprites: 200,000 × 3,072 ≈ 586 MB
```

## Dependencies

- Silk.NET (OpenGL, windowing, maths)
- StbImageSharp
- **.NET 10.0**

For how this repo fits together, see the root [README.md](../README.md) in **NyxFramework**.
