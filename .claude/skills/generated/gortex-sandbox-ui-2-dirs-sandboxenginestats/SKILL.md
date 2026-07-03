---
name: gortex-sandbox-ui-2-dirs-sandboxenginestats
description: "Work in the Sandbox\UI +2 dirs · SandboxEngineStats area — 217 symbols across 15 files (82% cohesion)"
---

# Sandbox\UI +2 dirs · SandboxEngineStats

217 symbols | 15 files | 82% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\NyxGuiSettings.cs`
- `Sandbox\SandboxUIDefinitions.cs`
- `Sandbox\SandboxUIManager.cs`
- `Sandbox\UI\BestiaryCatalog.cs`
- `Sandbox\UI\QuestLogCatalog.cs`
- `Sandbox\UI\SandboxBestiary.cs`
- `Sandbox\UI\SandboxEngineStats.cs`
- `Sandbox\UI\SandboxExpAnalyzer.cs`
- `Sandbox\UI\SandboxMainMenu.cs`
- `Sandbox\UI\SandboxMiniWindowBehavior.cs`
- `Sandbox\UI\SandboxObjectFit.cs`
- `Sandbox\UI\SandboxPlayerStats.cs`
- `Sandbox\UI\SandboxQuestLog.cs`
- `Sandbox\UI\SandboxShell.cs`
- `Sandbox\UI\SandboxUIKeyBinding.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\NyxGuiSettings.cs` | NyxGuiSettings, PanelDragOpacity, Default |
| `Sandbox\SandboxUIDefinitions.cs` | options, windowHeight, windowWidth, baseName, TryLoad, ... |
| `Sandbox\SandboxUIManager.cs` | _questLog, _playerStats, _lastVpH, _shell, _inventory, ... |
| `Sandbox\UI\BestiaryCatalog.cs` | Entries, BestiaryCatalog, Default, BestiaryEntry |
| `Sandbox\UI\QuestLogCatalog.cs` | QuestLogEntry, Entries, QuestLogCatalog, Default |
| `Sandbox\UI\SandboxBestiary.cs` | settings, Bind, entry, _detail, SandboxBestiary, ... |
| `Sandbox\UI\SandboxEngineStats.cs` | _renderer, _toggleWasDown, RegisterOtherScreens, _document, _root, ... |
| `Sandbox\UI\SandboxExpAnalyzer.cs` | settings, tracker, SandboxExpAnalyzer.<init>, guiRoots, renderer |
| `Sandbox\UI\SandboxMainMenu.cs` | SandboxMainMenu.<init>, settings, renderer, guiRoots |
| `Sandbox\UI\SandboxMiniWindowBehavior.cs` | document, window, SandboxMiniWindowBehavior, window, TryAppendChrome, ... |
| `Sandbox\UI\SandboxObjectFit.cs` | UpdateViewport, settings, WidgetCount, _layoutApplied, SandboxObjectFit.<init>, ... |
| `Sandbox\UI\SandboxPlayerStats.cs` | settings, renderer, settings, TryLoad, loaded, ... |
| `Sandbox\UI\SandboxQuestLog.cs` | _lastVpH, width, _shell, _qWasDown, _detailDesc, ... |
| `Sandbox\UI\SandboxShell.cs` | _root, SandboxShell, child, _lastVpH, _theme, ... |
| `Sandbox\UI\SandboxUIKeyBinding.cs` | SandboxUIKeyBinding, name, moduleId, TryGetToggleKey, ParseKey, ... |

## Entry Points

- `Sandbox\UI\SandboxQuestLog.cs::SandboxQuestLog.<init>`

## Connected Communities

- **Sandbox\UI +1 dirs** (2 cross-edges)
- **Sandbox +2 dirs · SandboxGameWorld** (1 cross-edges)
- **Sandbox\UI · SandboxPlayerStats** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-171"
smart_context with task: "understand Sandbox\UI +2 dirs · SandboxEngineStats", format: "gcx"
find_usages with id: "Sandbox\UI\SandboxQuestLog.cs::SandboxQuestLog.<init>", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
