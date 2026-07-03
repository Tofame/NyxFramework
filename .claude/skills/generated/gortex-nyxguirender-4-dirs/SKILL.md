---
name: gortex-nyxguirender-4-dirs
description: "Work in the NyxGUIRender +4 dirs area — 278 symbols across 14 files (75% cohesion)"
---

# NyxGUIRender +4 dirs

278 symbols | 14 files | 75% cohesion

## When to Use

Use this skill when working on files in:
- `NyxGUIRender\Gl\GuiColor.cs`
- `NyxGUIRender\Gl\GuiTexture.cs`
- `NyxGUIRender\NineSlice.cs`
- `NyxGUIRender\NyxGuiRenderer.cs`
- `NyxGUIRender\NyxGuiRendererStats.cs`
- `NyxGUIRender\NyxRenderSurface.cs`
- `NyxGUI\Core\NyxIconStyle.cs`
- `NyxGUI\Core\NyxImageStyle.cs`
- `NyxGUI\Core\NyxLayoutEnums.cs`
- `NyxGUI\Core\NyxRect.cs`
- `NyxGUI\Elements\NyxFileDialogButton.cs`
- `NyxGUI\Elements\NyxTable.cs`
- `NyxGUI\Elements\ZoomCanvas.cs`
- `NyxGUI\Windows\NyxTooltip.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxGUIRender\Gl\GuiColor.cs` | c, b, G, R, A, ... |
| `NyxGUIRender\Gl\GuiTexture.cs` | Width, height, height, width, gl, ... |
| `NyxGUIRender\NineSlice.cs` | src, dest, tint, clip, borders, ... |
| `NyxGUIRender\NyxGuiRenderer.cs` | Uv1, UpdateViewport, _resolveFontPath, _gl, font, ... |
| `NyxGUIRender\NyxGuiRendererStats.cs` | NyxGuiRendererStats, CachedTextGlyphs, CachedTextures, LastFrameGlDraws, LastFrameQuads |
| `NyxGUIRender\NyxRenderSurface.cs` | painter, theme, Paint |
| `NyxGUI\Core\NyxIconStyle.cs` | HasExplicitSize, IconSource, Height, ResolveSize, OffsetY, ... |
| `NyxGUI\Core\NyxImageStyle.cs` | objectFit, destination, imageSource, NyxImagePaintCommand.<init>, tint, ... |
| `NyxGUI\Core\NyxLayoutEnums.cs` | None, Cover, ScaleDown, Contain, Fill, ... |
| `NyxGUI\Core\NyxRect.cs` | GetHashCode, height, X, dy, Intersection, ... |
| `NyxGUI\Elements\NyxFileDialogButton.cs` | GetPathRect |
| `NyxGUI\Elements\NyxTable.cs` | Paint, theme, painter |
| `NyxGUI\Elements\ZoomCanvas.cs` | CanvasToWorld, canvasRect |
| `NyxGUI\Windows\NyxTooltip.cs` | anchor, hoverSinceMs, painter, text, font, ... |

## Entry Points

- `NyxGUIRender\NyxGuiRenderer.cs::NyxGuiRenderer.DrawSprite32`

## Connected Communities

- **NyxGUI\Core · WithOpacity** (1 cross-edges)
- **NyxGUI\Core +6 dirs** (1 cross-edges)

## How to Explore

```
get_communities with id: "community-31"
smart_context with task: "understand NyxGUIRender +4 dirs", format: "gcx"
find_usages with id: "NyxGUIRender\NyxGuiRenderer.cs::NyxGuiRenderer.DrawSprite32", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
