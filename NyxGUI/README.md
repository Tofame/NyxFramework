# NyxGUI

**NyxGUI** is a small, engine-agnostic **widget tree** for in-game UI: containers, side panels, scrollable regions, buttons, labels, scroll bars, **fractional anchors with margins**, optional **image chrome** (9-slice, tint, clip, fixed ratio), and **dragging**. Layout and input routing follow the same ideas as **The Forgotten Client** `src/GUI_Elements/`.

Rendering is **not** tied to OpenGL: widgets call `INyxGuiPainter` so your host (Silk + `SpriteBatch`, CPU raster, etc.) implements geometry, text, and images.

## Definition pipeline (v1)

```text
Lua flat table (canonical)  ──┐
                               ├──→ NyxGuiDefinitionBuilder → NyxGuiBuiltDocument
Lua tables (canonical) ─────────┘
```

- **Lua authoring:** flat map `widget_id = { type = "Label", parent = "…", bind_key = "…", … }` via `NyxGuiLuaBuildSpec`.
- **Lua (canonical):** `NyxGuiModuleHost` / `ctx:LoadTree` → `NyxGuiLuaBuildSpec` → `NyxGuiDefinitionBuilder`.
- **MoonSharp:** `NyxScript` project — `ctx:IncludeStyles`, `ctx:LoadTree`, module `on_init(ctx)`.
- **Live text:** optional `bind_key` + `NyxUiState`; fixed-size labels skip full relayout on text-only changes (§6.4).
- **Hidden modules:** `NyxGuiBuiltDocument.Suspend()` / `Resume()` stops binding refresh and layout work.
- **Phase 3:** `NyxGuiRowList` for C# scroll lists; `MarkLayoutDirty` / `FlushLayout` for subtree relayout; `set_children` relayouts parent only.

## Folder layout

| Folder | Contents |
|--------|----------|
| **Core/** | `NyxRect`, `NyxColor`, `NyxIconStyle`, `NyxImageStyle`, `NyxLayoutResolver`, `INyxGuiPainter`, `NyxElement`, `NyxContent`, … |
| **Elements/** | `NyxButton`, `NyxLabel`, …, `NyxMiniWindow`, `NyxScrollablePanel` |
| **ScrollBars/** | `NyxVScrollBar`, `NyxHScrollBar` (also `[[ScrollBar]]` / `[[VScrollBar]]` / `[[HScrollBar]]` in TOML) |
| **Definitions/** | `NyxGuiDefinitionBuilder`, `NyxGuiLuaBuildSpec`, `NyxGuiBuiltDocument` |
| **Binding/** | `bind_key` via `NyxUiState` / `INyxUiValueSource` |
| **Lists/** | `NyxGuiRowList<T>` — scroll list row pool + diff sync (Phase 3) |
| **Hosting/** | `INyxGuiScriptContext`, `INyxGuiInputRouter`, `INyxGuiPaintSession` |
| **docs/** | [Creating modules](docs/creating_a_gui_module.md), [Lua GUI](docs/lua_gui/README.md) |

## Behaviour highlights

### Interaction flags

| Property | Default | Role |
|----------|---------|------|
| `Enablement` | `true` | When `false`, no pointer input; paint uses the **`disabled`** state (TOML `enablement = false`). |
| `Phantom` | `false` | Drawn but **not** hit-tested (clicks pass through to widgets below). |
| `Focusable` | `false` | When `true`, click gives keyboard focus via <see cref="NyxGuiRootStack"/> (e.g. `TextBox` / `TextArea` default to focusable). Clicking elsewhere clears focus. |
| `Tooltip` | — | Hover hint text (TOML `tooltip = "…"`). Supports `\n` lines. Shown after `tooltip-delay-ms` (default 400). |
| `Opacity` | `1` | `0`–`1`; multiplies alpha on this widget’s own painting. |
| `Id` | — | String id for code / TOML `parent` links (separate from numeric `InternalId`). |

### Anchors & margins (`NyxLayoutBox`)

**Edge anchors** (TOML `anchors.top = "parent.top"` or `[anchors]` table):

- **`parent`** = the widget’s **parent** container (the one named in TOML `parent = "…"`).
- **`rootWindow`** (or `root`, `window`, or the name from `[document] root-window`) = the **game window / viewport** (full screen rect). Host sets size with `NyxGuiBuiltDocument.SetWindowSize(w, h)` on resize.
- **Any other id** = another widget in the same UI document, e.g. `anchors.top = "lblHealth.bottom"`, `anchors.left = "accountNameTextEdit.right"`.
- Sides: `left`, `right`, `top`, `bottom`, `center` / `centerx` / `centery`.

**Fractional anchors** (legacy `anchor-left = 0` or `anchor = "0 0 1 1"`):

- **AnchorLeft / AnchorRight / AnchorTop / AnchorBottom** are in **0–1** range relative to the **parent’s width or height**.
- If **Left ≠ Right**, the element is stretched horizontally; **Top ≠ Bottom** stretches vertically.
- If anchors are **equal** on an axis, that axis uses **`FixedWidth`** / **`FixedHeight`** instead of stretching.
- **`Margin`** (`NyxThickness`) is applied **after** computing the anchor span, shrinking the final rectangle. Use `margin = "left top right bottom"` (e.g. `"0 5 5 15"`), or override sides with **`margin-top`**, **`margin-right`**, **`margin-bottom`**, **`margin-left`** (they win over `margin`).
- **`Padding`** (`NyxThickness`) is applied to the container's **internal content area**, shrinking the parent boundary area against which anchored children are laid out. Use `padding = "left top right bottom"` (e.g. `"10"` or `"5 10 5 10"`), or override sides with **`padding-top`**, **`padding-right`**, **`padding-bottom`**, **`padding-left`** (they win over `padding`).
- **`anchors.fill = "parent"`** (or `fill = "parent"` inside `[anchors]`) sets left/right/top/bottom to the parent edges — same as four edge anchors, shorthand for “fill parent”.
- **`anchors.horizontalCenter = "parent.horizontalCenter"`** / **`anchors.verticalCenter = "widgetId.verticalCenter"`** — pins left+right (or top+bottom) to the target’s center line; use with **`fixed-width`** / **`fixed-height`**. Boolean `true` is shorthand for the parent’s center on that axis.
- **`element.SetParent(container, env, byId)`** — reparent at runtime; `parent.*` anchors then resolve against the new parent’s bounds.
- Margins and anchors take precedence over any initial `NyxRect` passed to the constructor for laid-out children.

Set `NyxElement.LayoutBox` on a child, then `NyxContent.AddChild` or call `RelayoutChildren()` after edits.

### Image styling (`NyxImageStyle` on `NyxElement.Image`)

Maps to NyxClient-style properties:

| Property | Role |
|----------|------|
| `ImageSource` | Host-resolved file path or logical id |
| `ImageRect` | Source rectangle in texture pixels (null = full texture) |
| `ImageClip` | Extra crop in texture pixels (intersected with `ImageRect`) |
| `ImageBorders` | Per-edge 9-slice insets in **source pixels** (`image-border`, `image-border-top`, …). Screen edge sizes match 1:1; center and edges are **tiled** (NyxClient `addRepeatedRects`), not stretched. |
| `ImageFixedRatio` | Letterbox inside destination |
| `ImageSmooth` | Hint for linear vs nearest filtering (host) |
| `ImageColor` | Tint (`NyxColor`; use `NyxColor.TryParseHex` for `#rrggbb` / `#rrggbbaa`) |

`PaintBackground` draws `Image` when set; several widgets call it before drawing text or theme fills.

### Icons (`NyxIconStyle` on `NyxElement.Icon`)

Optional small texture drawn inside the widget bounds (mini window title bars, etc.):

| Key | Role |
|-----|------|
| `icon` | Resolved like `image-source` (relative to `UiImagesDirectory`) |
| `icon-size` | Destination size in pixels, e.g. `"26 26"`. Omitted → PNG file size (or `icon-clip` size if set) |
| `icon-offset` | Pixel offset after alignment, e.g. `"-26 0"` |
| `icon-align` | `left` (default), `center`, `right` — horizontal in the area |
| `icon-rect` | Destination rect relative to the widget (`4 4 16 16` = NyxClient title-bar icon) |
| `icon-clip` | Source crop in texture pixels, e.g. `"0 0 18 18"` |
| `icon-smooth` | Linear vs nearest (host) |
| `icon-color` | Tint `#rrggbb` |

Call `PaintIcon` from custom widgets, or use `[[MiniWindow]]` which paints the icon in the title bar and nudges the title text right.

### Fonts (`font`, `font-size`)

Per-widget font overrides (any widget row). Unset fields inherit from ancestors; the host renderer supplies the default file (e.g. `ARIAL.TTF`).

| Key | Role |
|-----|------|
| `font` | Font file name or path (resolved via `NyxGuiLoadOptions.ResolveFontSource` / `UiFontsDirectory`) |
| `font-size` | Point size (e.g. `15` for stack counts) |
| `font-bold` | Bold style |
| `text-outline` | 1px black outline (labels); stack counts always use an outline |

```toml
[[Container]]
id = "InventorySlots"
font-size = 15
font-bold = true
```

Children (including code-created `UIItem` slots under that container) inherit these settings for `DrawText` / stack overlays.

### Text offset (`text-offset`)

Pixel shift applied when drawing text (layout bounds unchanged), e.g. `text-offset = "4 0"`. Works on `[[Label]]`, `[[Button]]`, `[[TextBox]]`, `[[TextArea]]`, and `[[MiniWindow]]` (title text).

### Dragging (`NyxElement.Draggable`) — NyxClient-style

Set `draggable = true` on the root `[[Container]]`. Click **anywhere** on that container that is not an interactive child (button, checkbox, scroll area) to drag — labels, nested containers, and empty chrome all move the window. Nested anchored containers are not draggable themselves; they drag the outer container.

Use **`phantom = true`** only for overlays that must not receive hits themselves (NyxClient `miniwindowTopBar`, invisible shell layers). **Do not** mark content layout containers phantom when they contain buttons — use a normal `[[Container]]` so children hit-test normally (or rely on phantom child pass-through in `NyxPointerInput` if you really need it).

While dragging, the whole container subtree is drawn at **`NyxGuiSettings.PanelDragOpacity`** (default `0.7`, overridable in `nyxguisettings.toml`). Drag state (`IsDragging`) lives on each **`NyxContainer`**, so multiple loaded UIs do not share one drag target. **`LayoutBox` is cleared** on the dragged container so it stays where you release it.

When several NyxGUI roots are on screen, register them on a **`NyxGuiRootStack`** (topmost hit, capture until mouse up). Each module calls `stack.Add(root, () => visible)` once in its constructor — the host only calls `stack.ProcessMouse(...)` per frame.

### `NyxMiniWindow` (`[[MiniWindow]]`)

NyxClient-style container: **`Body`** holds module content; chrome (frame image, title, icon, close / minimize / lock buttons) is declared in **project TOML**, not hardcoded in NyxGUI.

- Root row: `image-source`, `image-border`, `image-border-top` (title bar height), `title`, `icon`, `text-offset`.
- Content children: `parent = "<miniWindowId>"` → parented into **`Body`**.
- Chrome widgets: `chrome = true` + `parent = "<miniWindowId>"` → parented on the mini window itself (title-bar layer).
- Toggle visuals: `[id.on]`, `[id.on.hover]`, `[id.on.pressed]` (NyxClient `$on`). `Button.IsSelected` drives **`$on`** clips.

`anchors.fill = "parent"` on a **Body** child resolves against the **content area below the title bar** (use small margins, e.g. `margin = "4 4 4 4"`). Chrome widgets on the mini window root still use the full frame bounds.

Dragging works from **anywhere** except interactive children. Wire button behaviour in the host (Sandbox `SandboxMiniWindowBehavior` + `miniwindow_chrome.toml`). **`DragEnded`** fires when a drag completes (see `SandboxSideDock`).

**Resize:** expanded mini windows include a **bottom edge grip** (6px hit area; 1px white line only while hovered or dragging). Drag downward to grow height; body content is **clipped** to the content rect when shorter. Disable with `resizable = false` on `[[MiniWindow]]`; optional `resize-grip-height`, `min-height`. **`ResizeEnded`** / **`BoundsChanged`** fire while resizing.

```toml
[[MiniWindow]]
id = "StatsMw"
title = "Player Stats"
image-source = "miniwindow.png"
image-border = 4
image-border-top = 23
fixed-width = 284
fixed-height = 202
```

### Widget states (`NyxElement.States`) — visual only

Interaction states (**normal**, **hover**, **pressed** / **clicked**, **focused**, **disabled**, **on**, **on.hover**, **on.pressed**) change **paint chrome only**. **`disabled`** is applied automatically when **`enablement = false`** (or set explicitly in TOML): borders, images, opacity, `background-color`. They do **not** touch content (`Label.Text`, `Button.Label`, click handlers, or layout).

**Widget row** sets content and layout (`text`, `label`, `text-align`, anchors, …) on the widget itself. **State tables** (`[id.hover]`, `[bestiary-entry.focused]`, …) only list visual deltas.

Supported state keys: `opacity`, `border`, `border-color`, `background-color`, `image-source`, `image-border`, `image-border-top`, `image-border-bottom`, `image-border-left`, `image-border-right`, `image-color`, `image-smooth`, `image-rect`, `image-clip`, `image-fixed-ratio`.

```toml
[[Label]]
id = "detailName"
text = "Rat"          # widget content — updated from code at runtime
text-align = "center"   # left (default), center, right — per line when text-wrap is on
text-wrap = true        # labels, TextArea, ExtendedLabel
border = 0            # normal-state chrome

[detailName.hover]
border = 1
border-color = "#FFFF00"
```

Paint: one `TryBeginPaintVisual` → `NyxWidgetStates.Resolve` per widget per frame. List selection on buttons uses **focused** for the white border.

### Settings (`NyxGuiSettings`)

Pass one instance to every overlay / `NyxGuiLoadOptions.Settings`. Optional TOML: `resources/ui/nyxguisettings.toml`:

```toml
[drag]
panel-opacity = 0.7
```

```csharp
var settings = NyxGuiSettingsLoader.LoadOrDefault("resources/ui/nyxguisettings.toml");
var doc = NyxGuiDefinitionLoader.Load("ui/panel.toml", new NyxGuiLoadOptions { Settings = settings, ... });
```

## Forgotten Client mapping

| C++ | C# |
|-----|-----|
| `GUI_Element` | `NyxElement` |
| `GUI_Content` | `NyxContainer` |
| `GUI_Panel` / `GUI_PanelWindow` | `NyxContainer` |
| `GUI_VScrollBar` / `GUI_HScrollBar` | `NyxVScrollBar` / `NyxHScrollBar` |
| `GUI_TextEdit` | `NyxTextBox` |
| `GUI_ComboBox` | `NyxComboBox` |
| `GUI_ProgressBar` | `NyxProgressBar` |
| `GUI_ScrollBar` (standalone) | `NyxVScrollBar` / `NyxHScrollBar` |

## Build

```bash
dotnet build NyxGUI/NyxGUI.csproj
```

## TOML definitions

Declarative UI is a good fit: layout and chrome stay in data files, code only binds behaviour (stats text, callbacks). Load with:

```csharp
var doc = NyxGuiDefinitionLoader.Load("resources/ui/player_stats.toml", new NyxGuiLoadOptions
{
    UiImagesDirectory = ".../images/ui",
    ResolveImageSource = path => TryResolve(path),
});
NyxContainer root = (NyxContainer)doc.Root;
NyxLabel? hp = doc.TryGetLabel("lblHealth");
```

Each widget is a **table array** entry (`[[Container]]`, `[[MiniWindow]]`, `[[Label]]`, …). Use **`id`** and **`parent`** to build the tree. Optional **`[document] root = "MyPanel"`** picks the screen root.

Supported keys (kebab-case): `image-source`, …, `anchor` / `anchor-left` … (fractional), **`anchors.top`** / **`anchors.fill`** / **`[anchors]`** (edge), `margin`, `margin-top` …, `padding`, `padding-top` …, `fixed-width`, `fixed-height`, `parent`, `phantom`, `focusable`, `tooltip`, `tooltip-delay-ms`, `opacity`, …

```toml
[document]
root = "HudRoot"
root-window = "rootWindow"

[[Container]]
id = "HudRoot"
anchors.top = "rootWindow.top"
anchors.right = "rootWindow.right"
anchors.left = "rootWindow.right"
fixed-width = 260
fixed-height = 210
margin = "16 16 16 16"

[[Label]]
id = "submit"
parent = "HudRoot"
anchors.top = "accountNameTextEdit.bottom"
anchors.left = "parent.left"
anchors.right = "parent.right"
fixed-height = 28
```

```csharp
// After load or on resize:
document.SetWindowSize(viewportWidth, viewportHeight);
```

Example: `Sandbox/resources/ui/player_stats.toml`.

### Widget types (TOML `[[Type]]`)

| TOML | C# | Notes |
|------|-----|--------|
| `[[TextBox]]` / `[[TextEdit]]` | `NyxTextBox` | Single-line; `text`, `max-length`, `read-only`, `text-align`; keys via `NyxGuiRootStack.ProcessKeyboard` |
| `[[TextArea]]` | `NyxTextArea` | Multi-line; `text` (use `\\n` in TOML), `read-only`, `line-height`, `text-wrap`, `text-align`, `scroll-bar-width`, Enter = newline, wheel + scrollbar when content overflows |
| `[[CheckBox]]` | `NyxCheckBox` | `label`, `checked` |
| `[[RadioButton]]` | `NyxRadioButton` | `label`, `group`, `checked` |
| `[[ComboBox]]` | `NyxComboBox` | `items = ["a","b"]`, `selected-index` |
| `[[ExtendedLabel]]` | `NyxExtendedLabel` | `text` with `{#rrggbb}…{/}` markup |
| `[[Image]]` | `NyxImage` | `image-source`, 9-slice, etc. |
| `[[ImageContainer]]` | `NyxImageContainer` | clipped pan; `content-width` / `content-height` |
| `[[VScrollBar]]` / `[[HScrollBar]]` / `[[ScrollBar]]` | scroll bars | `orientation = "horizontal"` on `ScrollBar`; configure extent in code |
| `[[Slider]]` | `NyxSlider` | `value`, `minimum`, `maximum` |
| `[[ProgressBar]]` | `NyxProgressBar` | `value`, `minimum`, `maximum`, `show-label` |
| `[[Tooltip]]` | `NyxTooltip` | Phantom hover region; prefer `tooltip = "…"` on any widget |
| (context menu) | `NyxContextMenu` | Built in code: `SetItems` / `Open` / `Close` |
| `[[Button]]` | `NyxButton` | `label`, `description` (metadata only), `tooltip`, `RightClick` event |
| `[[Table]]` | `NyxTable` | rows from code (`SetRows`); `show-header`, `row-height` |
| `[[Graph]]` | `NyxGraph` | series from code (`SetSeriesA` / `SetSeriesB`); `auto-scale-y` |

```toml
[[TextBox]]
id = "nameInput"
parent = "HudRoot"
text = ""
max-length = 32

[[TextArea]]
id = "notes"
parent = "HudRoot"
text = "Editable notes."
read-only = false
max-length = 32

[[ExtendedLabel]]
id = "lblRich"
parent = "HudRoot"
text = "HP: {#55cc55}100{/} / 100"
```

Text entry from the host (Silk keyboard):

```csharp
nyxGuiRoots.ProcessKeyboard(NyxGuiKey.Backspace);
nyxGuiRoots.ProcessKeyboard(NyxGuiKey.Enter);
nyxGuiRoots.ProcessKeyboard(NyxGuiKey.None, 'x'); // typed character
```

## Sandbox example

See **Sandbox** `SandboxPlayerStatsNyx` (toggle with **G**): loads `player_stats.toml`, draggable panel, anchored labels, optional `resources/images/ui/panel_side.png` chrome. OpenGL painter: **[NyxGUIRender](../NyxGUIRender/)** (`NyxGuiRenderer`).

To add another overlay (bestiary, engine stats, etc.), see **[docs/creating_a_gui_module.md](docs/creating_a_gui_module.md)** and **[docs/lua_gui/](docs/lua_gui/README.md)**.
