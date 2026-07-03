# Declarative Layout Properties (.nyxui)

This document provides a comprehensive reference of all layout, styling, and configuration properties that can be declared within `.nyxui` files.

---

## 1. Common Properties

These properties apply to all UI widgets:

| Key | Type | Description |
|-----|------|-------------|
| `id` | string | Unique identifier for C# referencing and anchor targeting. |
| `visible` | bool | Controls whether the widget is rendered and hit-tested (`true` by default). |
| `enablement` / `events` | bool | Controls pointer interaction capability (`true` by default). |
| `phantom` | bool | If `true`, the widget is rendered but click events pass through to elements underneath. |
| `focusable` | bool | If `true`, the widget can receive keyboard/input focus. |
| `tooltip` | string | Text shown when hovering over the widget. Support `\n` linebreaks. |
| `tooltip_delay_ms` | int | Delay in milliseconds before showing the tooltip (default 400ms). |
| `opacity` | float | Opacity value from `0.0` (fully transparent) to `1.0` (fully opaque). |
| `internal_id` | int | Internal identifier used by the engine. |

---

## 2. Layout & Size Properties

Layout properties dictate how the widget is sized and positioned.

### Absolute Sizing
- `x`, `y`, `width`, `height` (int): Sets the absolute boundary box coordinates and size.
- `fixed_width` / `fixed_height` (int): Shorthands to define static dimensions.
- `min_width` / `min_height` / `max_width` / `max_height` (int): Binds the scale boundaries of the widget.

### Compound Sizing
You can declare flexible compound sizing specifications:
```
# Sizing with range limits
width = { value = 300, min = 150, max = 500 }

# Making a size non-resizable / rigid
height = { fixed = 200 }
```

### Box Sizing
Controls how the width and height of an element are calculated.
- `box_sizing` (string):
  - `"content_box"` (default): Width and height apply to the content area only. Padding and border are added outside. The actual space the element takes up on screen is `width + padding + border`.
  - `"border_box"`: Include padding and border inside the defined width/height. The element stays exactly the defined size.

### Margins
Margins create space around the widget relative to its anchors.
- `margin` (thickness): Sets margins on all sides:
  - `"8"` (uniform 8px margin)
  - `"12 8 12 8"` (`"left top right bottom"` format)
- `margin_left` / `margin_right` / `margin_top` / `margin_bottom` (int): Sets specific edge margins.

### Padding
Padding creates space inside the widget's bounds, affecting where its anchored children are positioned.
- `padding` (thickness): Sets paddings on all sides:
  - `"8"` (uniform 8px padding)
  - `"12 8 12 8"` (`"left top right bottom"` format)
- `padding_left` / `padding_right` / `padding_top` / `padding_bottom` (int): Sets specific edge paddings.

---

## 3. Edge Anchors (`anchors.*`)

Anchors align a widget's edges with other widgets or the root viewport boundaries.

Available anchor keys:
- `anchors.top`
- `anchors.bottom`
- `anchors.left`
- `anchors.right`
- `anchors.horizontalCenter` / `anchors.verticalCenter`
- `anchors.fill`

Anchor value targets:
- `parent.[edge]`: Anchor to the widget's parent container (e.g. `parent.top`).
- `rootWindow.[edge]`: Anchor to the game window viewport (e.g. `rootWindow.left`).
- `siblingId.[edge]`: Anchor to a sibling widget inside the same document (e.g. `titleLabel.bottom`).

*Example*:
```
anchors.top = "titleLabel.bottom"
anchors.left = "parent.left"
anchors.right = "parent.right"
```

---

## 4. Solid Styling & Borders

These styling properties control the solid background fill and the border outline of a widget. They can be defined at the top-level of a widget declaration (which applies to the `normal` state) or nested inside state-specific blocks.

| Key | Type | Description |
|-----|------|-------------|
| `background_color` | color | Hex background color code (e.g., `#14181f` or `#ff0000`). |
| `border` | table | Defines the border as a table specifying both `width` and `color` (e.g., `{ width = 2, color = "#ff0000" }`). |
| `border_width` | int | Standalone outline border thickness in pixels. |
| `border_color` | color | Standalone hex border outline color code (e.g., `#4a5568` or `#00ff00`). |

### State-Specific Styling

Widgets support visual overrides depending on their current interaction state. You can nest visual styling properties (including `opacity` and `image_*` properties) inside the following state block keys:

- `normal` / `default`: The default visual state when idle.
- `hover` / `hovered`: Applied when the pointer is hovering over the widget.
- `pressed` / `clicked`: Applied when the widget is clicked or held down.
- `focused`: Applied when the widget has input focus.
- `disabled`: Applied automatically when `enablement = false` or `events = false`.
- `on`: Applied when the widget is toggled/turned on (e.g., active tab, checked checkbox, selected button).
- `on.hover` / `on.hovered`: Applied when the widget is toggled on and hovered.
- `on.pressed` / `on.clicked`: Applied when the widget is toggled on and pressed.

*Example*:
```toml
# Styling applied directly to the widget (affects the normal state)
background_color = "#2d2d30"

# Using a compound border table:
border = { width = 1, color = "#64646e" }

# Alternatively, using standalone properties:
# border_width = 1
# border_color = "#64646e"

# State overrides block
hover = { background_color = "#3d3d40", border = { width = 1, color = "#00ff00" } }
pressed = { background_color = "#1e1e1f", border_color = "#00bb00" }
disabled = { opacity = 0.5 }
```

---

## 5. Background Image Styling (`image_*`)

Applies background textures with support for 9-slice tiling, tints, and cropping.

| Key | Type | Description |
|-----|------|-------------|
| `image_source` | string | Image file path relative to the UI directory (e.g., `panel.png`). |
| `image_rect` | rect | Sub-rectangle within the source image to draw (`"x y width height"`). |
| `image_clip` | rect | Inner clip rectangle of source pixels. |
| `image_border` | int | Uniform border size in pixels for 9-slice scaling. |
| `image_border_top` / `_bottom` / `_left` / `_right` | int | Edge overrides for the 9-slice border grid. |
| `image_fixed_ratio`| bool | Retains the texture's original aspect ratio while rendering. |
| `image_smooth` | bool | Enables linear texture filtering (`true`) or nearest-neighbor (`false`). |
| `image_color` | color | Hex tint color applied to the texture (e.g. `#ffffff`). |
| `image_object_fit` / `object_fit` | string | Resizing behavior: `fill`, `contain`, `cover`, `none`, `scale-down` (defaults to `fill`). |

---

## 6. Icon Styling (`icon_*`)

Applies an optional overlay icon rendered on top of the widget's background.

- `icon` (string): Path to the icon image file.
- `icon_size` (size): Destination dimensions (`"width height"`).
- `icon_offset` (pair): Relative rendering offset (`"x y"`).
- `icon_rect` (rect): Relative destination bounding box.
- `icon_align` (string): Horizontal alignment (`left`, `center`, `right`).
- `icon_clip` (rect): Sub-rectangle source crop within the icon texture.
- `icon_smooth` (bool): True for linear texture filtering.
- `icon_color` (color): Tint color.

---

## 7. Font Properties (`font_*`)

Unset font properties will inherit down the widget tree from parents to children.

- `font` (string): Font family file name (e.g., `ARIAL.TTF`).
- `font_size` (float): Font height point size (e.g., `13.5`).
- `font_bold` (bool): If `true`, applies bold weight.
- `text_outline` (bool): Draws a 1-pixel dark outline around characters for readability.
- `text_offset` (pair): Offsets the text drawing coordinates slightly (`"x y"`).

---

## 8. Event & Reactive Bindings

All event property names use **underscores only** — hyphenated variants (e.g. `on-click`) are not supported.

- `bind_key` (string): Binds a widget's text or state to a data key (e.g., `bind_key = "playerHealth"`).
- `on_click` (string): Binds a button click to a C# action name (e.g., `on_click = "submitAction"`).
- `on_right_click` (string): Binds a right-click release to a C# action name.
- `on_value_change` (string): Binds slider value updates (`NyxSlider`) to a C# action name.
- `on_change` (string): Binds value/state/text edits (`NyxCheckBox`, `NyxRadioButton`, `NyxTextBox`, `NyxTextArea`) to a C# action name.
- `on_selection_change` (string): Binds selection changes (`NyxComboBox`) to a C# action name.

> **Note:** Enter-key handling for text boxes (`NyxTextBox.EnterPressed`) is not available as a declarative event.
> Wire it in C# instead: `doc.TryGetTextBox("myBox").EnterPressed += ...`

---

## 9. Layout Engine & Docking (`layout`, `dock`)

You can specify a layout strategy on containers (`Container`, `ScrollablePanel`, `MiniWindow`, etc.) to automatically arrange their children.

### The `layout` Property

The layout property can be defined as a simple string type name or a compound table mapping configuration options.

#### String Format
Sets the layout type with default settings:
```toml
layout = "grid"
```
Supported layout types:
- `stack` / `vertical` / `horizontal`
- `grid`
- `dock`
- `wrap`

#### Compound Format
Configures specific layout options:
```toml
layout = { type = "grid", columns = 2, spacing = 8, cell_size = "130 64" }
```

#### Layout Configuration Keys:
- **Common (all layouts)**:
  - `type` (string): The layout strategy (`stack`, `grid`, `dock`, `wrap`).
  - `padding` (thickness): Inner padding bounds applied to the layout area (`"8"` or `"8 10 8 10"`).
- **`stack` layout**:
  - `orientation` (string): `"vertical"` (default) or `"horizontal"`.
  - `spacing` (int): Pixel spacing between stacked elements.
  - `alignment` (string): `"Start"` (default), `"Center"`, `"End"`, or `"Stretch"`.
- **`grid` layout**:
  - `columns` (int): Number of columns (default `1`).
  - `rows` (int): Number of rows (default `0`). If set and `columns` is 0, flows column-by-column (vertically).
  - `spacing` (int): Spacing between cells.
  - `fit_children` (bool): When `true` (default), any axis not constrained by an explicit `cell_width` / `cell_height` is auto-stretched: width fills the container divided across columns, height fills the container divided across rows. Disabled automatically if both dimensions are set explicitly.
  - `cell_size` (pair): Fixed dimensions for every cell (`"width height"`, e.g. `"130 64"`). Sets both `cell_width` and `cell_height`.
  - `cell_width` (int): Fixed width per cell. Disables width auto-fitting.
  - `cell_height` (int): Fixed height per cell. Disables height auto-fitting.
- **`wrap` layout**:
  - `orientation` (string): `"horizontal"` (default) or `"vertical"`.
  - `spacing` (int): Spacing between items.

### The `dock` Property
For children of a `dock` layout, sets their docking edge alignment:
```toml
dock = "left"
```
Supported values: `left`, `right`, `top`, `bottom`, `fill`.


