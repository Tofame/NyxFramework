---
name: gortex-nyxgui-core-9-dirs
description: "Work in the NyxGUI\Core +9 dirs area — 408 symbols across 33 files (70% cohesion)"
---

# NyxGUI\Core +9 dirs

408 symbols | 33 files | 70% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Binding\NyxCollectionBinding.cs`
- `NyxGUI\Core\ICapturesPointer.cs`
- `NyxGUI\Core\INyxGuiPainter.cs`
- `NyxGUI\Core\NyxColor.cs`
- `NyxGUI\Core\NyxContainer.cs`
- `NyxGUI\Core\NyxElement.cs`
- `NyxGUI\Core\NyxGuiRootStack.cs`
- `NyxGUI\Core\NyxImageStyle.cs`
- `NyxGUI\Core\NyxLayoutBox.cs`
- `NyxGUI\Core\NyxLayoutResolver.cs`
- `NyxGUI\Core\NyxRadioGroup.cs`
- `NyxGUI\Core\NyxTooltipRouting.cs`
- `NyxGUI\Core\NyxWidget.cs`
- `NyxGUI\Core\NyxWidgetVisual.cs`
- `NyxGUI\Definitions\NyxGuiBuiltDocument.cs`
- `NyxGUI\Definitions\NyxGuiPropertyBag.cs`
- `NyxGUI\Definitions\NyxGuiTextUpdate.cs`
- `NyxGUI\DragDrop\NyxDragSource.cs`
- `NyxGUI\Elements\NyxCheckBox.cs`
- `NyxGUI\Elements\NyxImage.cs`
- `NyxGUI\Elements\NyxImageContainer.cs`
- `NyxGUI\Elements\NyxRadioButton.cs`
- `NyxGUI\Elements\NyxScrollablePanel.cs`
- `NyxGUI\Elements\NyxSeparator.cs`
- `NyxGUI\Elements\NyxTable.cs`
- `NyxGUI\Elements\ZoomCanvas.cs`
- `NyxGUI\Hosting\INyxGuiPaintSession.cs`
- `NyxGUI\Windows\NyxTooltip.cs`
- `Sandbox\NyxGUI_Extend\UIItem.cs`
- `Sandbox\NyxGUI_Extend\UIItemStackOverlay.cs`
- `Sandbox\UI\Inventory\ItemTooltip.cs`
- `Sandbox\UI\Inventory\UIInventory.cs`
- `Sandbox\UI\SandboxShell.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Binding\NyxCollectionBinding.cs` | itemFactory, NyxCollectionBinding.<init>, itemUpdater, container |
| `NyxGUI\Core\ICapturesPointer.cs` | ICapturesPointer |
| `NyxGUI\Core\INyxGuiPainter.cs` | INyxGuiPainter |
| `NyxGUI\Core\NyxColor.cs` | b, r, g, FromRgb |
| `NyxGUI\Core\NyxContainer.cs` | ClearChildren, IsRoot, OnRightButtonUp, child, child, ... |
| `NyxGUI\Core\NyxElement.cs` | InternalId, NyxElement, HasInvalidation, theme, painter, ... |
| `NyxGUI\Core\NyxGuiRootStack.cs` | Remove, FindActiveTooltipElement, root |
| `NyxGUI\Core\NyxImageStyle.cs` | opacity, ToPaintCommand, destination, defaultTint |
| `NyxGUI\Core\NyxLayoutBox.cs` | GetWidgetDependencies, rootWindowAnchorId |
| `NyxGUI\Core\NyxLayoutResolver.cs` | children, widgetsById, SortByDependencies, root, RelayoutSubtree, ... |
| `NyxGUI\Core\NyxRadioGroup.cs` | NyxRadioGroup, Groups, selected, button, Select, ... |
| `NyxGUI\Core\NyxTooltipRouting.cs` | painter, scope, NyxTooltipRouting, FindDeepestHovered, scope, ... |
| `NyxGUI\Core\NyxWidget.cs` | FixedHeight, internalId, NyxWidget, FixedWidth, SetFixedSize, ... |
| `NyxGUI\Core\NyxWidgetVisual.cs` | Image, HasBorder, BorderColor, BackgroundColor, BorderWidth, ... |
| `NyxGUI\Definitions\NyxGuiBuiltDocument.cs` | Root, OnLabelTextChanged, Suspend, T, Settings, ... |
| `NyxGUI\Definitions\NyxGuiPropertyBag.cs` | value, key, TryGetInt |
| `NyxGUI\Definitions\NyxGuiTextUpdate.cs` | ApplyText, text, element |
| `NyxGUI\DragDrop\NyxDragSource.cs` | Threshold, GhostTemplate, GetData, FadeSource, NyxDragSource |
| `NyxGUI\Elements\NyxCheckBox.cs` | painter, NyxCheckBox, Label, _isChecked, NyxCheckBox.<init>, ... |
| `NyxGUI\Elements\NyxImage.cs` | theme, id, NyxImage.<init>, painter, NyxImage, ... |
| `NyxGUI\Elements\NyxImageContainer.cs` | ScrollOffsetY, theme, NyxImageContainer.<init>, ContentWidth, ScrollOffsetX, ... |
| `NyxGUI\Elements\NyxRadioButton.cs` | theme, Group, LabelRect, DotRect, _isChecked, ... |
| `NyxGUI\Elements\NyxScrollablePanel.cs` | Paint, theme, painter |
| `NyxGUI\Elements\NyxSeparator.cs` | painter, theme, bounds, Paint, NyxSeparator.<init>, ... |
| `NyxGUI\Elements\NyxTable.cs` | NyxTable, RowHeight, id, ShowHeader, HeaderHeight, ... |
| `NyxGUI\Elements\ZoomCanvas.cs` | painter, theme, Paint |
| `NyxGUI\Hosting\INyxGuiPaintSession.cs` | INyxGuiPaintSession |
| `NyxGUI\Windows\NyxTooltip.cs` | painter, id, Update, theme, _delayMs, ... |
| `Sandbox\NyxGUI_Extend\UIItem.cs` | theme, SpriteBytes, Paint, Smooth, IconPixels, ... |
| `Sandbox\NyxGUI_Extend\UIItemStackOverlay.cs` | MarginBottom, TextColor, UIItemStackOverlay, MarginRight |
| `Sandbox\UI\Inventory\ItemTooltip.cs` | painter |
| `Sandbox\UI\Inventory\UIInventory.cs` | doc, LoadBackpack, loadOptions |
| `Sandbox\UI\SandboxShell.cs` | AdoptIntoShellRoot, child |

## Entry Points

- `NyxGUI\Elements\NyxImageContainer.cs::NyxImageContainer.Paint`

## Connected Communities

- **NyxGUI\Core · BuildLineRanges** (5 cross-edges)
- **NyxGUI\Core +1 dirs · InvalidateLayout** (4 cross-edges)
- **NyxGUI\Definitions +1 dirs · TryGetInt** (3 cross-edges)
- **NyxGUI\Core +3 dirs** (3 cross-edges)
- **NyxGUI\Core · WithOpacity** (2 cross-edges)
- **NyxGUIRender +4 dirs** (1 cross-edges)
- **NyxGUI\Core · Resolve** (1 cross-edges)
- **NyxGUI\Core · RelayoutContainer** (1 cross-edges)
- **NyxGUI\Definitions · CanSkipLayoutForTextChange** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-54"
smart_context with task: "understand NyxGUI\Core +9 dirs", format: "gcx"
find_usages with id: "NyxGUI\Elements\NyxImageContainer.cs::NyxImageContainer.Paint", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
