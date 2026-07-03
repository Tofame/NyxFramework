---
name: gortex-sandbox-ui-inventory-1-dirs
description: "Work in the Sandbox\UI\Inventory +1 dirs area — 237 symbols across 8 files (86% cohesion)"
---

# Sandbox\UI\Inventory +1 dirs

237 symbols | 8 files | 86% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\Items\ItemStacking.cs`
- `Sandbox\UI\Inventory\ItemDragService.cs`
- `Sandbox\UI\Inventory\ItemMoveAmountDialog.cs`
- `Sandbox\UI\Inventory\MapItemSurface.cs`
- `Sandbox\UI\Inventory\StackAmountTyping.cs`
- `Sandbox\UI\Inventory\UIContainer.cs`
- `Sandbox\UI\Inventory\UIInventory.cs`
- `Sandbox\UI\Inventory\UISlot.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\Items\ItemStacking.cs` | ItemStacking |
| `Sandbox\UI\Inventory\ItemDragService.cs` | DragThresholdPx, x, TryDropOnMapAtPointer, _pressX, sync, ... |
| `Sandbox\UI\Inventory\ItemMoveAmountDialog.cs` | initialAmount, callback, maxCount, Show |
| `Sandbox\UI\Inventory\MapItemSurface.cs` | camXf, PlaceItem, TryEnsureTopContainer, TryPickTile, MapItemSurface.<init>, ... |
| `Sandbox\UI\Inventory\StackAmountTyping.cs` | HasDigits, maxCount, digit, ParseOrZero, value, ... |
| `Sandbox\UI\Inventory\UIContainer.cs` | UIContainer.<init>, slotHost, columns, storage |
| `Sandbox\UI\Inventory\UIInventory.cs` | camXf, guiRoots, camYf, Update, input, ... |
| `Sandbox\UI\Inventory\UISlot.cs` | NotifyContainerChanged, Host, x, PickSlotAt, Host, ... |

## Entry Points

- `Sandbox\UI\Inventory\UIInventory.cs::UIInventory.Update`

## Connected Communities

- **Sandbox\Items +2 dirs** (3 cross-edges)
- **Sandbox\UI\Inventory · ItemMoveAmountDialog** (3 cross-edges)
- **NyxGUI\Core +6 dirs** (1 cross-edges)
- **Sandbox\UI\Inventory · UIContainerWindow** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-150"
smart_context with task: "understand Sandbox\UI\Inventory +1 dirs", format: "gcx"
find_usages with id: "Sandbox\UI\Inventory\UIInventory.cs::UIInventory.Update", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
