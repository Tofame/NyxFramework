---
name: gortex-nyxrender-4-dirs
description: "Work in the NyxRender +4 dirs area — 280 symbols across 9 files (86% cohesion)"
---

# NyxRender +4 dirs

280 symbols | 9 files | 86% cohesion

## When to Use

Use this skill when working on files in:
- `NyxDrawer\Appearance\OutfitColorLayout.cs`
- `NyxRender\EffectDrawBatch.cs`
- `NyxRender\GraphicsDevice.cs`
- `NyxRender\Shaders\ShaderRegistry.cs`
- `NyxRender\SpriteRenderer.cs`
- `NyxRender\SpriteResidentLru.cs`
- `Sandbox\SandboxGameWorld.cs`
- `Sandbox\SandboxUIManager.cs`
- `Sandbox\UI\Screens\GameplayScreen.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxDrawer\Appearance\OutfitColorLayout.cs` | Body, Legs, legs, feet, body, ... |
| `NyxRender\EffectDrawBatch.cs` | head, x, body, EffectDrawGroup.<init>, MaskUv, ... |
| `NyxRender\GraphicsDevice.cs` | obj, Color, R, GetHashCode, A, ... |
| `NyxRender\Shaders\ShaderRegistry.cs` | LoadSecondaryTextureFromFile, imagePath |
| `NyxRender\SpriteRenderer.cs` | _lru, Color, paletteFromMask, feet, _runs, ... |
| `NyxRender\SpriteResidentLru.cs` | spriteId, AllocNode, TryPeekOldest, Add, spriteId |
| `Sandbox\SandboxGameWorld.cs` | npcs, InitializeEntities, player, config, things |
| `Sandbox\SandboxUIManager.cs` | windowWidth, windowHeight, Draw |
| `Sandbox\UI\Screens\GameplayScreen.cs` | renderer, winW, winH, deltaTime, Draw |

## Connected Communities

- **NyxRender · SpriteResidentLru** (6 cross-edges)
- **NyxDrawer\Drawing +2 dirs** (3 cross-edges)
- **NyxRender +1 dirs · Sprite** (2 cross-edges)
- **Sandbox +2 dirs · Initialize** (2 cross-edges)
- **NyxRender +1 dirs · RegisterAll** (2 cross-edges)
- **NyxRender +2 dirs** (2 cross-edges)
- **NyxRender +1 dirs · SpriteBatch** (1 cross-edges)
- **NyxRender · EffectDrawBatch** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-119"
smart_context with task: "understand NyxRender +4 dirs", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
