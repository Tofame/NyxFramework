---
name: gortex-nyxgui-core-4-dirs
description: "Work in the NyxGUI\Core +4 dirs area — 123 symbols across 11 files (73% cohesion)"
---

# NyxGUI\Core +4 dirs

123 symbols | 11 files | 73% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\NyxAnchorEdge.cs`
- `NyxGUI\Core\NyxLayoutAnchor.cs`
- `NyxGUI\Core\NyxLayoutBox.cs`
- `NyxGUI\Core\NyxLayoutResolver.cs`
- `NyxGUI\Definitions\NyxGuiDefinitionProperties.cs`
- `Sandbox\UI\Inventory\UIContainer.cs`
- `Sandbox\UI\Inventory\UIInventory.cs`
- `Sandbox\UI\Inventory\UISlot.cs`
- `Sandbox\UI\Minimap\MinimapView.cs`
- `Sandbox\UI\Minimap\UIMinimap.cs`
- `Sandbox\UI\SandboxChat.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\NyxAnchorEdge.cs` | Top, CenterY, Right, Bottom, NyxAnchorEdge, ... |
| `NyxGUI\Core\NyxLayoutAnchor.cs` | other, edge, GetHashCode, obj, text, ... |
| `NyxGUI\Core\NyxLayoutBox.cs` | Left, NyxLayoutBox, Padding, Margin, FixedSize, ... |
| `NyxGUI\Core\NyxLayoutResolver.cs` | edge, ResolveY, edge, ResolveX, r, ... |
| `NyxGUI\Definitions\NyxGuiDefinitionProperties.cs` | TryApplyCenterAxisAnchor, TryApplyFillAnchor, box, value, box, ... |
| `Sandbox\UI\Inventory\UIContainer.cs` | RelayoutSlots, backpackSlotsHost, _outerHost, ViewportRows, GetPreferredWindowWidth, ... |
| `Sandbox\UI\Inventory\UIInventory.cs` | height, UpdateViewport, GetBackpackDockHeight, player, AttachPlayer, ... |
| `Sandbox\UI\Inventory\UISlot.cs` | frame, host, storage, RegisterContainer, normalBorderWidth, ... |
| `Sandbox\UI\Minimap\MinimapView.cs` | InvalidateCache |
| `Sandbox\UI\Minimap\UIMinimap.cs` | UpdateTitle, shell, renderer, UIMinimap.<init>, guiRoots, ... |
| `Sandbox\UI\SandboxChat.cs` | viewportHeight, gamePanel, guiRoots, gameWorld, SandboxChat.<init>, ... |

## Entry Points

- `Sandbox\UI\SandboxChat.cs::SandboxChat.<init>`
- `Sandbox\UI\Minimap\UIMinimap.cs::UIMinimap.<init>`

## Connected Communities

- **NyxGUI\Core +6 dirs** (7 cross-edges)
- **Sandbox\UI\Inventory · UIInventory** (4 cross-edges)
- **Sandbox\UI +1 dirs** (2 cross-edges)
- **Sandbox\UI\Inventory · InventorySlotView** (1 cross-edges)
- **Sandbox\UI\Inventory +1 dirs** (1 cross-edges)
- **Sandbox\UI\Minimap · UIMinimap** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-160"
smart_context with task: "understand NyxGUI\Core +4 dirs", format: "gcx"
find_usages with id: "Sandbox\UI\SandboxChat.cs::SandboxChat.<init>", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
