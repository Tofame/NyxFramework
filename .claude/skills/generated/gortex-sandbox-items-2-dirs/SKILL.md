---
name: gortex-sandbox-items-2-dirs
description: "Work in the Sandbox\Items +2 dirs area — 229 symbols across 14 files (89% cohesion)"
---

# Sandbox\Items +2 dirs

229 symbols | 14 files | 89% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\Items\EquipmentSlot.cs`
- `Sandbox\Items\EquipmentSlotMapping.cs`
- `Sandbox\Items\ExtraPropertiesExtensions.cs`
- `Sandbox\Items\Item.cs`
- `Sandbox\Items\ItemContainer.cs`
- `Sandbox\Items\ItemPlacementRules.cs`
- `Sandbox\Items\ItemStacking.cs`
- `Sandbox\Items\ItemStorage.cs`
- `Sandbox\Items\ItemStoragePlacement.cs`
- `Sandbox\Items\ItemType.cs`
- `Sandbox\Items\ItemsManager.cs`
- `Sandbox\Items\PlayerEquipment.cs`
- `Sandbox\SandboxGameWorld.cs`
- `Sandbox\UI\Inventory\ItemDragService.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\Items\EquipmentSlot.cs` | Ring, EquipmentSlot, Legs, Feet, RightHand, ... |
| `Sandbox\Items\EquipmentSlotMapping.cs` | clothSlot, value, slot, EquipmentSlotMapping, TryParse, ... |
| `Sandbox\Items\ExtraPropertiesExtensions.cs` | dict, dict, key, GetInt, GetFloat, ... |
| `Sandbox\Items\Item.cs` | Legs, GetHashCode, IconDisplaySignature, itemTypeId, Item, ... |
| `Sandbox\Items\ItemContainer.cs` | other, AddItemById, count, itemTypeId, index, ... |
| `Sandbox\Items\ItemPlacementRules.cs` | item, item, container, TryEnsureOpenable, slot, ... |
| `Sandbox\Items\ItemStacking.cs` | CanMerge, incoming, OfId, incoming, existing, ... |
| `Sandbox\Items\ItemStorage.cs` | ItemStorage, ItemStorage.<init>, Set, capacity, Clear, ... |
| `Sandbox\Items\ItemStoragePlacement.cs` | ItemStoragePlacement, storage, PlaceInEmptySlotsOnly, remaining, storage, ... |
| `Sandbox\Items\ItemType.cs` | ContainerCapacity, other, PrimaryPatternY, PrimaryLayers, obj, ... |
| `Sandbox\Items\ItemsManager.cs` | byDatIndex, ItemsManager.<init>, datItemId, assets, datItemId, ... |
| `Sandbox\Items\PlayerEquipment.cs` | TryEquip, slot, PlayerEquipment, Equip, _slots, ... |
| `Sandbox\SandboxGameWorld.cs` | player, ApplyDemoPlayerStartingItems |
| `Sandbox\UI\Inventory\ItemDragService.cs` | PlaceInEarliestEmpties, second, storage, SliceDraggedStack, first, ... |

## How to Explore

```
get_communities with id: "community-138"
smart_context with task: "understand Sandbox\Items +2 dirs", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
