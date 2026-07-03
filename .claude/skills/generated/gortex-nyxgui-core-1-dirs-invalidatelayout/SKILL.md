---
name: gortex-nyxgui-core-1-dirs-invalidatelayout
description: "Work in the NyxGUI\Core +1 dirs · InvalidateLayout area — 164 symbols across 7 files (85% cohesion)"
---

# NyxGUI\Core +1 dirs · InvalidateLayout

164 symbols | 7 files | 85% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\NyxElement.cs`
- `NyxGUI\Core\NyxElementLayoutExtensions.cs`
- `NyxGUI\Core\NyxImageBorders.cs`
- `NyxGUI\Core\NyxLayoutAnchor.cs`
- `NyxGUI\Core\NyxLayoutEnums.cs`
- `NyxGUI\Core\NyxThickness.cs`
- `NyxGUI\Layout\NyxStackLayout.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\NyxElement.cs` | InvalidateLayout |
| `NyxGUI\Core\NyxElementLayoutExtensions.cs` | padding, edge, AnchorFill, NyxElementLayoutExtensions, T, ... |
| `NyxGUI\Core\NyxImageBorders.cs` | Uniform, border |
| `NyxGUI\Core\NyxLayoutAnchor.cs` | edge, widgetId, WidgetEdge |
| `NyxGUI\Core\NyxLayoutEnums.cs` | Vertical, Center, End, Start, Stretch, ... |
| `NyxGUI\Core\NyxThickness.cs` | Left, GetHashCode, Uniform, NyxThickness, bottom, ... |
| `NyxGUI\Layout\NyxStackLayout.cs` | finalRect, container, container, Spacing, Orientation, ... |

## Connected Communities

- **NyxGUI\Core +6 dirs** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-43"
smart_context with task: "understand NyxGUI\Core +1 dirs · InvalidateLayout", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
