---
name: gortex-nyxgui-elements-3-dirs
description: "Work in the NyxGUI\Elements +3 dirs area — 132 symbols across 9 files (91% cohesion)"
---

# NyxGUI\Elements +3 dirs

132 symbols | 9 files | 91% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\NyxGuiKey.cs`
- `NyxGUI\Core\NyxGuiRootStack.cs`
- `NyxGUI\Elements\NyxComboBox.cs`
- `NyxGUI\Elements\NyxImageContainer.cs`
- `NyxGUI\Elements\NyxSlider.cs`
- `NyxGUI\Elements\NyxTextArea.cs`
- `NyxGUI\Elements\NyxTextBox.cs`
- `NyxGUI\Events\NyxEventArgs.cs`
- `Sandbox\UI\SandboxNyxGUIKeyboard.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\NyxGuiKey.cs` | Right, Enter, Tab, Down, Backspace, ... |
| `NyxGUI\Core\NyxGuiRootStack.cs` | character, key, ProcessKeyboard |
| `NyxGUI\Elements\NyxComboBox.cs` | x, HitTest, y |
| `NyxGUI\Elements\NyxImageContainer.cs` | x, OnMouseWheel, delta, y |
| `NyxGUI\Elements\NyxSlider.cs` | button, y, OnMouseDown, x |
| `NyxGUI\Elements\NyxTextArea.cs` | LineStartIndex, CharWidth, _text, ScrollOffsetY, _scrollOffsetY, ... |
| `NyxGUI\Elements\NyxTextBox.cs` | HandleKey, c, EnsureCaretVisible, key, InsertCharacter, ... |
| `NyxGUI\Events\NyxEventArgs.cs` | NyxKeyEventArgs, type, key, NyxKeyEventArgs.<init>, source, ... |
| `Sandbox\UI\SandboxNyxGUIKeyboard.cs` | keyboard, keyboard, roots, ShouldTrigger, key, ... |

## Entry Points

- `Sandbox\UI\SandboxNyxGUIKeyboard.cs::SandboxNyxGUIKeyboard.Update`
- `NyxGUI\Elements\NyxTextArea.cs::NyxTextArea.HandleKey`
- `NyxGUI\Elements\NyxTextBox.cs::NyxTextBox.HandleKey`

## Connected Communities

- **Sandbox\UI +2 dirs · IsKeyPressed** (6 cross-edges)
- **NyxGUI\Core +6 dirs** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-167"
smart_context with task: "understand NyxGUI\Elements +3 dirs", format: "gcx"
find_usages with id: "Sandbox\UI\SandboxNyxGUIKeyboard.cs::SandboxNyxGUIKeyboard.Update", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
