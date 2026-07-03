---
name: gortex-sandbox-nyxgamecore-3-dirs
description: "Work in the Sandbox\NyxGameCore +3 dirs area — 110 symbols across 5 files (78% cohesion)"
---

# Sandbox\NyxGameCore +3 dirs

110 symbols | 5 files | 78% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\Creatures\Player.cs`
- `Sandbox\NyxGameCore\ICreature.cs`
- `Sandbox\NyxGameCore\Position.cs`
- `Sandbox\NyxGameMap\Formats\SecFormat.cs`
- `Sandbox\NyxGameMap\GameMap.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\Creatures\Player.cs` | GetDrawPosition, py, cameraOriginTileY, cameraOriginTileX, px |
| `Sandbox\NyxGameCore\ICreature.cs` | ICreature |
| `Sandbox\NyxGameCore\Position.cs` | DistanceManhattan, dz, y, z, dy, ... |
| `Sandbox\NyxGameMap\Formats\SecFormat.cs` | ChunkX, SizeX, cx, ChunkZ, SizeY, ... |
| `Sandbox\NyxGameMap\GameMap.cs` | TryLoadSector, to, center, to, GetGroundAt, ... |

## Entry Points

- `Sandbox\NyxGameMap\Formats\SecFormat.cs::SecSector.Read`
- `Sandbox\NyxGameMap\GameMap.cs::GameMap.FindPath`

## How to Explore

```
get_communities with id: "community-132"
smart_context with task: "understand Sandbox\NyxGameCore +3 dirs", format: "gcx"
find_usages with id: "Sandbox\NyxGameMap\Formats\SecFormat.cs::SecSector.Read", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
