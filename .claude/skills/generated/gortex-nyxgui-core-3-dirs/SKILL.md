---
name: gortex-nyxgui-core-3-dirs
description: "Work in the NyxGUI\Core +3 dirs area — 225 symbols across 15 files (85% cohesion)"
---

# NyxGUI\Core +3 dirs

225 symbols | 15 files | 85% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\NyxContainer.cs`
- `NyxGUI\Core\NyxElement.cs`
- `NyxGUI\Core\NyxGuiFocus.cs`
- `NyxGUI\Core\NyxGuiRootStack.cs`
- `NyxGUI\Core\NyxPointerInput.cs`
- `NyxGUI\Core\NyxRect.cs`
- `NyxGUI\Definitions\NyxGuiBuiltDocument.cs`
- `NyxGUI\Elements\NyxButton.cs`
- `NyxGUI\Elements\NyxCheckBox.cs`
- `NyxGUI\Elements\NyxRadioButton.cs`
- `NyxGUI\Elements\NyxTextBox.cs`
- `NyxGUI\Elements\ZoomCanvas.cs`
- `NyxGUI\Events\NyxEvent.cs`
- `NyxGUI\Events\NyxEventArgs.cs`
- `NyxGUI\Events\NyxEventHandler.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\NyxContainer.cs` | FindDeepestHit, OnRightButtonDown, OnMouseDown, x, x, ... |
| `NyxGUI\Core\NyxElement.cs` | button, HitTestSubtree, y, x, x, ... |
| `NyxGUI\Core\NyxGuiFocus.cs` | HasEditableTextEntry, CapturesGlobalShortcuts, NyxGuiFocus, SetFocus, Clear, ... |
| `NyxGUI\Core\NyxGuiRootStack.cs` | wheelDelta, y, y, FindDeepestHit, ProcessMouse, ... |
| `NyxGUI\Core\NyxPointerInput.cs` | element, y, FindCapturingInChildren, x, x, ... |
| `NyxGUI\Core\NyxRect.cs` | Contains, px, py |
| `NyxGUI\Definitions\NyxGuiBuiltDocument.cs` | handler, WireEvent, element, eventName |
| `NyxGUI\Elements\NyxButton.cs` | OnMouseUp, x, button, y |
| `NyxGUI\Elements\NyxCheckBox.cs` | OnMouseUp, y, button, x |
| `NyxGUI\Elements\NyxRadioButton.cs` | y, button, x, OnMouseUp |
| `NyxGUI\Elements\NyxTextBox.cs` | OnMouseUp, button, y, x |
| `NyxGUI\Elements\ZoomCanvas.cs` | ZoomCanvas.<init>, args, args, HandleMouseDownEvent, sender, ... |
| `NyxGUI\Events\NyxEvent.cs` | Drop, TextInput, TextChanged, NyxEventType, MouseMove, ... |
| `NyxGUI\Events\NyxEventArgs.cs` | NyxClickEventArgs.<init>, x, NyxMouseEventArgs.<init>, relatedTarget, Button, ... |
| `NyxGUI\Events\NyxEventHandler.cs` | element, eventType, AddHandler, handler |

## How to Explore

```
get_communities with id: "community-45"
smart_context with task: "understand NyxGUI\Core +3 dirs", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
