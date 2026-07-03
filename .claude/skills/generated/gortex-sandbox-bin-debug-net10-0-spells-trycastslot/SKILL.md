---
name: gortex-sandbox-bin-debug-net10-0-spells-trycastslot
description: "Work in the Sandbox\bin\Debug\net10.0\Spells · TryCastSlot area — 108 symbols across 9 files (89% cohesion)"
---

# Sandbox\bin\Debug\net10.0\Spells · TryCastSlot

108 symbols | 9 files | 89% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\bin\Debug\net10.0\Spells\ActiveMissileEffects.cs`
- `Sandbox\bin\Debug\net10.0\Spells\ActiveSpellEffects.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellAreaCell.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellAreaPattern.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellCastActions.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellCaster.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellCatalog.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellDefinition.cs`
- `Sandbox\bin\Debug\net10.0\Spells\SpellMissileFlight.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\bin\Debug\net10.0\Spells\ActiveMissileEffects.cs` | camXf, MinDurationMs, winH, _entries, Draw, ... |
| `Sandbox\bin\Debug\net10.0\Spells\ActiveSpellEffects.cs` | camXf, drawer, _entries, AddHits, winW, ... |
| `Sandbox\bin\Debug\net10.0\Spells\SpellAreaCell.cs` | Effect, SpellAreaCell, None, CasterNoEffect, Caster |
| `Sandbox\bin\Debug\net10.0\Spells\SpellAreaPattern.cs` | TryGetCasterAnchor, casterRow, casterCol |
| `Sandbox\bin\Debug\net10.0\Spells\SpellCastActions.cs` | BuildTooltipText, aimGameY, input, npcs, gameWorld, ... |
| `Sandbox\bin\Debug\net10.0\Spells\SpellCaster.cs` | hits, forward, dy, SpellTileHit, TryFindTargetTile, ... |
| `Sandbox\bin\Debug\net10.0\Spells\SpellCatalog.cs` | path, scriptName, TryGetScript, script, LoadDefinitions |
| `Sandbox\bin\Debug\net10.0\Spells\SpellDefinition.cs` | SelfTarget, Words, ScriptName, NeedTarget, Direction, ... |
| `Sandbox\bin\Debug\net10.0\Spells\SpellMissileFlight.cs` | SpellMissileFlight |

## Connected Communities

- **Sandbox\bin\Debug\net10.0\Spells · ParseSpell** (2 cross-edges)
- **Sandbox\Spells +2 dirs** (1 cross-edges)
- **Sandbox\bin\Debug\net10.0\Spells · TryGetMouseTile** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-178"
smart_context with task: "understand Sandbox\bin\Debug\net10.0\Spells · TryCastSlot", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
