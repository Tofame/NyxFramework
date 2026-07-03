---
name: gortex-nyxgui-definitions-1-dirs-trygetint
description: "Work in the NyxGUI\Definitions +1 dirs · TryGetInt area — 330 symbols across 9 files (92% cohesion)"
---

# NyxGUI\Definitions +1 dirs · TryGetInt

330 symbols | 9 files | 92% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUI\Core\NyxIconAlign.cs`
- `NyxGUI\Definitions\NyxGuiBuiltDocument.cs`
- `NyxGUI\Definitions\NyxGuiDefinitionBuilder.cs`
- `NyxGUI\Definitions\NyxGuiDefinitionProperties.cs`
- `NyxGUI\Definitions\NyxGuiLoadOptions.cs`
- `NyxGUI\Definitions\NyxGuiPropertyBag.cs`
- `NyxGUI\Definitions\NyxGuiStateTemplateLoader.cs`
- `NyxGUI\Definitions\NyxGuiWidgetStateApplicator.cs`
- `NyxGUI\Definitions\NyxGuiWidgetStateNames.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUI\Core\NyxIconAlign.cs` | Center, Left, NyxIconAlign, Right |
| `NyxGUI\Definitions\NyxGuiBuiltDocument.cs` | actionName, NyxGuiEventLink, ActionName, EventName, bindKeys, ... |
| `NyxGUI\Definitions\NyxGuiDefinitionBuilder.cs` | table, CreateWidget, bounds, table, table, ... |
| `NyxGUI\Definitions\NyxGuiDefinitionProperties.cs` | TryGetPaddingSide, anchorsTable, element, options, key, ... |
| `NyxGUI\Definitions\NyxGuiLoadOptions.cs` | source, source, ResolveImagePath, ResolveFontSource, ResolveFontFile, ... |
| `NyxGUI\Definitions\NyxGuiPropertyBag.cs` | TryGetValue, TryWrap, From, TryGetNested, value, ... |
| `NyxGUI\Definitions\NyxGuiStateTemplateLoader.cs` | stateGroup, LoadFromStateGroup, options, NyxGuiStateTemplateLoader |
| `NyxGUI\Definitions\NyxGuiWidgetStateApplicator.cs` | byId, ApplyStateGroupToElement, stateTable, options, state, ... |
| `NyxGUI\Definitions\NyxGuiWidgetStateNames.cs` | DottedSuffixes, NyxGuiWidgetStateNames, All, InteractionOnly |

## Entry Points

- `NyxGUI\Definitions\NyxGuiDefinitionBuilder.cs::NyxGuiDefinitionBuilder.Build`

## Connected Communities

- **NyxGUI\Core +9 dirs** (6 cross-edges)
- **NyxGUI\Core +3 dirs** (2 cross-edges)
- **NyxGUI\Core +4 dirs** (2 cross-edges)
- **NyxGUI\Core +6 dirs** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-55"
smart_context with task: "understand NyxGUI\Definitions +1 dirs · TryGetInt", format: "gcx"
find_usages with id: "NyxGUI\Definitions\NyxGuiDefinitionBuilder.cs::NyxGuiDefinitionBuilder.Build", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
