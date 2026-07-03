---
name: gortex-sandbox-ui-actionbar-2-dirs
description: "Work in the Sandbox\UI\ActionBar +2 dirs area — 136 symbols across 5 files (87% cohesion)"
---

# Sandbox\UI\ActionBar +2 dirs

136 symbols | 5 files | 87% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\SandboxLayout.cs`
- `Sandbox\Spells\SpellActionBindings.cs`
- `Sandbox\UI\ActionBar\SandboxActionBar.cs`
- `Sandbox\UI\ActionBar\SandboxKeyBindCapture.cs`
- `Sandbox\UI\ActionBar\SandboxKeyBindDialog.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\SandboxLayout.cs` | WindowWidth, GameX, SandboxLayout, windowX, layout, ... |
| `Sandbox\Spells\SpellActionBindings.cs` | SlotCount, SetKey, key, _keys, slot, ... |
| `Sandbox\UI\ActionBar\SandboxActionBar.cs` | CloseContextMenu, ShowContextMenu, _missileEffects, _contextPopupLayer, _layout, ... |
| `Sandbox\UI\ActionBar\SandboxKeyBindCapture.cs` | BuildCandidates, SandboxKeyBindCapture, IsModifierKey, key, CandidateKeys |
| `Sandbox\UI\ActionBar\SandboxKeyBindDialog.cs` | IsOpen, onComplete, _title, PanelW, height, ... |

## Entry Points

- `Sandbox\UI\ActionBar\SandboxActionBar.cs::SandboxActionBar.<init>`

## Connected Communities

- **Sandbox +2 dirs · SandboxGameWorld** (1 cross-edges)
- **Sandbox\UI +2 dirs · SandboxEngineStats** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-147"
smart_context with task: "understand Sandbox\UI\ActionBar +2 dirs", format: "gcx"
find_usages with id: "Sandbox\UI\ActionBar\SandboxActionBar.cs::SandboxActionBar.<init>", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
