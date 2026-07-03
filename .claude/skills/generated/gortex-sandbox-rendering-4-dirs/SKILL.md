---
name: gortex-sandbox-rendering-4-dirs
description: "Work in the Sandbox\Rendering +4 dirs area — 173 symbols across 7 files (87% cohesion)"
---

# Sandbox\Rendering +4 dirs

173 symbols | 7 files | 87% cohesion

## When to Use

Use this skill when working on files in:
- `NyxDrawer\AssetDrawer.cs`
- `NyxDrawer\Creatures\CreatureDrawRequest.cs`
- `NyxRender\SpriteRenderer.cs`
- `Sandbox\NyxGameCore\Tile.cs`
- `Sandbox\Rendering\ClientDraw.cs`
- `Sandbox\Rendering\CreatureDrawState.cs`
- `Sandbox\Rendering\MapFloorDrawer.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxDrawer\AssetDrawer.cs` | AssetDrawer, Animator, Preloader, Items, Effects, ... |
| `NyxDrawer\Creatures\CreatureDrawRequest.cs` | Outfit, Mounted, WalkPhase, Appearance, CreatureDrawRequest, ... |
| `NyxRender\SpriteRenderer.cs` | Point |
| `Sandbox\NyxGameCore\Tile.cs` | FillStack, dest, TileStackEntry, EnumerateStack |
| `Sandbox\Rendering\ClientDraw.cs` | drawCreaturesAtTile, winH, map, playerPos, camXf, ... |
| `Sandbox\Rendering\CreatureDrawState.cs` | tilePos, sy, DrawCreatures, sx, elevPx |
| `Sandbox\Rendering\MapFloorDrawer.cs` | winW, tileCache, DrawCreaturesOnTile, AddElevation, drawCreaturesAtTile, ... |

## How to Explore

```
get_communities with id: "community-133"
smart_context with task: "understand Sandbox\Rendering +4 dirs", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
