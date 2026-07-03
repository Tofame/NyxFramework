---
name: gortex-sandbox-2-dirs-sandboxgameworld
description: "Work in the Sandbox +2 dirs · SandboxGameWorld area — 116 symbols across 9 files (74% cohesion)"
---

# Sandbox +2 dirs · SandboxGameWorld

116 symbols | 9 files | 74% cohesion

## When to Use

Use this skill when working on files in:
- `Sandbox\SandboxApp.cs`
- `Sandbox\SandboxDefaults.cs`
- `Sandbox\SandboxGameWorld.cs`
- `Sandbox\SandboxNyxGUISettingsLoader.cs`
- `Sandbox\SandboxResources.cs`
- `Sandbox\SandboxUIDefinitions.cs`
- `Sandbox\UI\Inventory\UIInventory.cs`
- `Sandbox\UI\Screens\GameplayScreen.cs`
- `Sandbox\UI\Screens\ISandboxScreen.cs`

## Key Files

| File | Symbols |
|------|---------|
| `Sandbox\SandboxApp.cs` | Exit, OnLoad |
| `Sandbox\SandboxDefaults.cs` | SandboxDefaults, WindowHeight, WindowWidth |
| `Sandbox\SandboxGameWorld.cs` | usingAssetsFormat, serverName, sprSource, onProgress, _creatureDrawState, ... |
| `Sandbox\SandboxNyxGUISettingsLoader.cs` | LoadOrDefault, DefaultFileName, path, SandboxNyxGUISettingsLoader |
| `Sandbox\SandboxResources.cs` | fileName, FontsDirectory, ImagesDirectory, DefaultUiFontFile, TryGetAssetsPath, ... |
| `Sandbox\SandboxUIDefinitions.cs` | windowHeight, settings, CreateLoadOptions, windowWidth |
| `Sandbox\UI\Inventory\UIInventory.cs` | UIInventory.<init>, shell, guiRoots, settings, renderer, ... |
| `Sandbox\UI\Screens\GameplayScreen.cs` | _uiManager, _creatureInfoDrawer, _lanDialog, graphicsDevice, _renderer, ... |
| `Sandbox\UI\Screens\ISandboxScreen.cs` | ISandboxScreen |

## Entry Points

- `Sandbox\SandboxGameWorld.cs::SandboxGameWorld.LoadOffThread`

## Connected Communities

- **Sandbox\UI\Inventory · UIInventory** (3 cross-edges)
- **Sandbox +2 dirs · Initialize** (2 cross-edges)
- **NyxRender +4 dirs** (1 cross-edges)
- **NyxGUI\Core +9 dirs** (1 cross-edges)
- **Sandbox · SandboxApp** (1 cross-edges)
- **NyxRender +1 dirs · RegisterAll** (1 cross-edges)
- **Sandbox\UI +1 dirs** (1 cross-edges)
- **Sandbox\Spells +1 dirs** (1 cross-edges)
- **Sandbox\Items +2 dirs** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-140"
smart_context with task: "understand Sandbox +2 dirs · SandboxGameWorld", format: "gcx"
find_usages with id: "Sandbox\SandboxGameWorld.cs::SandboxGameWorld.LoadOffThread", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
