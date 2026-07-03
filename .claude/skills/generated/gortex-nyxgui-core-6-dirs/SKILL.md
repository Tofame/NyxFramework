---
name: gortex-nyxgui-core-6-dirs
description: "Work in the NyxGUI\Core +6 dirs area — 345 symbols across 26 files (79% cohesion)"
---

# NyxGUI\Core +6 dirs

345 symbols | 26 files | 79% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\INyxTextEntry.cs`
- `NyxGUI\Core\NyxColor.cs`
- `NyxGUI\Core\NyxFormattedText.cs`
- `NyxGUI\Core\NyxGuiTheme.cs`
- `NyxGUI\Core\NyxTextAlign.cs`
- `NyxGUI\Core\NyxTextLayout.cs`
- `NyxGUI\Core\NyxTextRun.cs`
- `NyxGUI\Core\NyxWidgetStates.cs`
- `NyxGUI\Definitions\NyxGuiBuiltDocument.cs`
- `NyxGUI\Definitions\NyxGuiDefinitionProperties.cs`
- `NyxGUI\DragDrop\NyxDragGhost.cs`
- `NyxGUI\Elements\NyxComboBox.cs`
- `NyxGUI\Elements\NyxContextMenu.cs`
- `NyxGUI\Elements\NyxExtendedLabel.cs`
- `NyxGUI\Elements\NyxLabel.cs`
- `NyxGUI\Elements\NyxProgressBar.cs`
- `NyxGUI\Elements\NyxTextArea.cs`
- `NyxGUI\Elements\NyxTextBox.cs`
- `NyxGUI\Styling\NyxTheme.cs`
- `Sandbox\UI\CreatureInformationDrawer.cs`
- `Sandbox\UI\Inventory\ItemTooltip.cs`
- `Sandbox\UI\SandboxBestiary.cs`
- `Sandbox\UI\SandboxFileDialog.cs`
- `Sandbox\UI\SandboxJoinServer.cs`
- `Sandbox\UI\SandboxLanDialog.cs`
- `Sandbox\UI\SandboxTaskList.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\INyxTextEntry.cs` | INyxTextEntry |
| `NyxGUI\Core\NyxColor.cs` | value, a, A, B, c, ... |
| `NyxGUI\Core\NyxFormattedText.cs` | markup, Parse, NyxFormattedText, defaultColor, defaultColor, ... |
| `NyxGUI\Core\NyxGuiTheme.cs` | TextPrimary, ScrollTrack, InputBorder, GraphAxis, TableRowAlt, ... |
| `NyxGUI\Core\NyxTextAlign.cs` | TopLeft, Center, NyxTextAlign, TopRight, TopCenter |
| `NyxGUI\Core\NyxTextLayout.cs` | lines, painter, DefaultCharWidth, text, NyxTextLayout, ... |
| `NyxGUI\Core\NyxTextRun.cs` | Color, text, color, underline, Underline, ... |
| `NyxGUI\Core\NyxWidgetStates.cs` | GetStateTable, stateName |
| `NyxGUI\Definitions\NyxGuiBuiltDocument.cs` | TryGetLabel, id |
| `NyxGUI\Definitions\NyxGuiDefinitionProperties.cs` | raw, wrap, ParseTextAlign |
| `NyxGUI\DragDrop\NyxDragGhost.cs` | Paint, theme, painter |
| `NyxGUI\Elements\NyxComboBox.cs` | theme, painter, Paint |
| `NyxGUI\Elements\NyxContextMenu.cs` | NyxContextMenu.<init>, internalId |
| `NyxGUI\Elements\NyxExtendedLabel.cs` | theme, Align, LineHeight, NyxExtendedLabel, Wrap, ... |
| `NyxGUI\Elements\NyxLabel.cs` | NyxLabel.<init>, Paint, Wrap, NyxLabel, Text, ... |
| `NyxGUI\Elements\NyxProgressBar.cs` | theme, Paint, painter |
| `NyxGUI\Elements\NyxTextArea.cs` | visual, text, lineAlign, painter, theme, ... |
| `NyxGUI\Elements\NyxTextBox.cs` | y, DeleteSelection, visual, x, MaxLength, ... |
| `NyxGUI\Styling\NyxTheme.cs` | CreateDefault |
| `Sandbox\UI\CreatureInformationDrawer.cs` | speakerName, gamePanel, CreatureInformationDrawer, Dispose, DurationMs, ... |
| `Sandbox\UI\Inventory\ItemTooltip.cs` | tooltipW, painter, Paint, tooltipH, descLines, ... |
| `Sandbox\UI\SandboxBestiary.cs` | _armor, BestiaryDetailPane, _class, exp, Show, ... |
| `Sandbox\UI\SandboxFileDialog.cs` | _resultLabel, _window, _renderer, SandboxFileDialog, _toggleKey, ... |
| `Sandbox\UI\SandboxJoinServer.cs` | _scanHeader, width, Visible, _statusLabel, PanelH, ... |
| `Sandbox\UI\SandboxLanDialog.cs` | RefreshUI, guiRoots, keyboard, Close, _title, ... |
| `Sandbox\UI\SandboxTaskList.cs` | Exp, settings, SandboxTaskList.<init>, shell, Name, ... |

## Entry Points

- `Sandbox\UI\CreatureInformationDrawer.cs::CreatureInformationDrawer.Update`
- `Sandbox\UI\SandboxJoinServer.cs::SandboxJoinServer.<init>`
- `Sandbox\UI\SandboxLanDialog.cs::SandboxLanDialog.<init>`
- `NyxGUI\Elements\NyxTextBox.cs::NyxTextBox.Paint`
- `NyxGUI\Elements\NyxExtendedLabel.cs::NyxExtendedLabel.Paint`

## Connected Communities

- **Sandbox\UI +2 dirs · SandboxEngineStats** (5 cross-edges)
- **NyxGUI\Core · BuildLineRanges** (2 cross-edges)
- **NyxGUI\Elements +3 dirs** (2 cross-edges)
- **Sandbox\UI +2 dirs · IsKeyPressed** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-164"
smart_context with task: "understand NyxGUI\Core +6 dirs", format: "gcx"
find_usages with id: "Sandbox\UI\CreatureInformationDrawer.cs::CreatureInformationDrawer.Update", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
