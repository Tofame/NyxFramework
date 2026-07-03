---
name: gortex-sandbox-creatures-4-dirs
description: "Work in the Sandbox\Creatures +4 dirs area — 139 symbols across 10 files (80% cohesion)"
---

# Sandbox\Creatures +4 dirs

139 symbols | 10 files | 80% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\Creatures\CreatureDirection.cs`
- `Sandbox\Creatures\Npc.cs`
- `Sandbox\Creatures\Player.cs`
- `Sandbox\Creatures\RemotePlayer.cs`
- `Sandbox\SandboxGameWorld.cs`
- `Sandbox\Spells\ActiveMissileEffects.cs`
- `Sandbox\Spells\ActiveSpellEffects.cs`
- `Sandbox\Spells\SpellCastInput.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellCastInput.cs`
- `Sandbox\bin\Release\net10.0\Spells\SpellCastInput.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\Creatures\CreatureDirection.cs` | CreatureDirection, West, East, North, South |
| `Sandbox\Creatures\Npc.cs` | GetDrawPosition, px, py, cameraOriginTileX, cameraOriginTileY |
| `Sandbox\Creatures\Player.cs` | _walkOffsetY, deltaSeconds, _toTileX, _footAnimPhases, Update, ... |
| `Sandbox\Creatures\RemotePlayer.cs` | deltaSeconds, StartWalk, destY, cameraOriginTileX, destY, ... |
| `Sandbox\SandboxGameWorld.cs` | input, shaderTime, renderer, SendLocalPlayerMoveUpdate, viewportBounds, ... |
| `Sandbox\Spells\ActiveMissileEffects.cs` | Prune, MinDurationMs, drawer, Entry, camYf, ... |
| `Sandbox\Spells\ActiveSpellEffects.cs` | winH, assets, Draw, camYf, deltaSeconds, ... |
| `Sandbox\Spells\SpellCastInput.cs` | GetCameraOrigin, winW, winH, camYf, player, ... |
| `Sandbox\bin\Debug\net10.0\Spells\SpellCastInput.cs` | player, GetCameraOrigin, winH, winW, camYf, ... |
| `Sandbox\bin\Release\net10.0\Spells\SpellCastInput.cs` | GetCameraOrigin, camYf, camXf, player, winW, ... |

## Connected Communities

- **Sandbox\Creatures +2 dirs** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-139"
smart_context with task: "understand Sandbox\Creatures +4 dirs", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
