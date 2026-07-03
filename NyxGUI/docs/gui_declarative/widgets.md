# Declarative Widget Reference (.nyxui)

This document describes all built-in widgets available for use in `.nyxui` files, along with their markup syntax and widget-specific property keys.

---

## 1. Panels & Containers

### `Container`
A basic layout container for grouping controls.
* **Markup Type**: `Container`
* **Properties**:
  * `phantom` (bool): When `true`, input events ignore the container and fall through to elements below.

---

### `MiniWindow`
A window container that features a draggable title-bar frame and a resize handle at the bottom edge.
* **Markup Type**: `MiniWindow`
* **Properties**:
  * `title` (string): Title text rendered on the frame title bar.
  * `title_bar_height` / `image_border_top` (int): Height of the title bar in pixels.
  * `resizable` (bool): Enables resizing by dragging the bottom border.
  * `auto_size` (bool): Automatically adjusts the window height to fit child elements.
  * `min_height` / `max_height` (int): Window height limits during resize.
  * `resize_grip_height` (int): Hitbox thickness of the resize grip.

---

### `ScrollablePanel`
A panel with optional vertical and/or horizontal scrollbars for presenting content larger than the container bounds.
* **Markup Type**: `ScrollablePanel`
* **Properties**:
  * `vertical_scrollbar` (string): ID of a separate vertical scrollbar (`VScrollBar`) widget to link.
  * `horizontal_scrollbar` (string): ID of a separate horizontal scrollbar (`HScrollBar`) widget to link.
  * `inverted_scroll` (bool): If `true`, inverts the direction of mouse wheel scrolling.

---

### `ImageContainer`
A pannable image crop view.
* **Markup Type**: `ImageContainer`
* **Properties**:
  * `content_width` / `content_height` (int): Scale dimensions of the interior image content.

---

### `DockPanel`
A layout panel designed for vertically stacking child `MiniWindow` widgets. Floating mini-windows dragged near the panel bounds automatically snap and dock into the panel column, matching its width.
* **Markup Type**: `DockPanel`
* **Properties**:
  * `margin` (int): Padding around the inside edges of the panel. Default: `8`.
  * `gap` (int): Vertical gap between stacked mini-windows. Default: `6`.

---

## 2. Text & Input Fields

### `Label`
Displays static read-only text.
* **Markup Type**: `Label`
* **Properties**:
  * `text` (string): The text string to show.
  * `text_wrap` / `wrap` (bool): Wraps words when they overflow the width bounds.
  * `text_align` (string): Text alignment (`left`, `center`, `right`).
  * `line_height` (int): Pixel spacing between lines of wrapped text.

---

### `ExtendedLabel`
A label supporting inline colored text using formatting tags: `{#RRGGBB}colored{/}`.
* **Markup Type**: `ExtendedLabel`
* **Properties**:
  * `text` (string): The formatted text markup.
  * `color` (color): Default fallback color for un-tagged text.
  * `text_wrap` (bool): Wraps words at limits.
  * `text_align` (string): Alignment selector.

---

### `TextBox`
A single-line input text field.
* **Markup Type**: `TextBox` or `TextEdit`
* **Properties**:
  * `text` (string): Prefilled field text.
  * `max_length` (int): Maximum length of input characters allowed.
  * `read_only` (bool): Disables keyboard typing input.
  * `text_align` (string): Horizontal alignment of input characters.
  * `on_change` (string): Binds text input edits to a C# action.

---

### `TextArea`
A multi-line text block with keyboard and mouse-wheel scrolling.
* **Markup Type**: `TextArea`
* **Properties**:
  * `text` (string): Prefilled text (supports `\n`).
  * `max_length` (int): Character limits.
  * `read_only` (bool): Disables text editing.
  * `line_height` (int): Space between lines.
  * `scroll_bar_width` (int): Width of the vertical scrollbar.
  * `text_wrap` / `wrap` (bool): Wraps overflow lines.
  * `text_align` (string): Alignment.
  * `on_change` (string): Binds text edits to a C# action.


---

## 3. Interactive Widgets

### `Button`
A clickable button control.
* **Markup Type**: `Button`
* **Properties**:
  * `label` / `text` (string): The button text label.
  * `description` (string): Internal metadata description string.
  * `on_click` (string): Event action triggers routed to the C# controller.

---

### `FileDialogButton`
A compound button that opens an OS-native file chooser dialog and displays the selected filename next to the button face. Cross-platform: uses PowerShell on Windows, `osascript` on macOS, `zenity`/`kdialog` on Linux. No external NuGet dependencies.
* **Markup Type**: `FileDialogButton`
* **Properties**:
  * `button_label` (string): Text shown on the clickable button face. Default: `"Select file…"`.
  * `button_width` (int): Pixel width of the button face. Default: `120`.
  * `placeholder` (string): Text shown in the path zone when no file is selected. Default: `"No file selected"`.
  * `show_path` (bool): When `false`, hides the filename label next to the button. Default: `true`.
  * `mode` (string): `"open"` (default) or `"save"`.
  * `dialog_title` (string): Title bar text of the OS dialog. Default: `"Select File"`.
  * `extensions` (array): Allowed file extensions without leading dot, e.g. `["png", "jpg"]`. Empty = all files.
  * `filter_label` (string): Human-readable name for the extension group, e.g. `"Image files"`.
  * `default_extension` (string): Extension appended automatically in Save mode when the user omits one.
  * `initial_directory` (string): Starting directory shown when the dialog opens.
  * `on_file_selected` (string): Action name called after the user confirms a selection.

### `CheckBox`
A binary toggle button.
* **Markup Type**: `CheckBox`
* **Properties**:
  * `label` (string): Text displayed next to the checkbox.
  * `checked` (bool): Prefilled checked state.
  * `on_change` (string): Binds state toggle changes to a C# action.


---

### `RadioButton`
Mutually exclusive choice buttons that cooperate in a named selection group.
* **Markup Type**: `RadioButton`
* **Properties**:
  * `label` (string): Text descriptor.
  * `group` (string): ID of the radio group they belong to.
  * `checked` (bool): Marks initial selection state.
  * `on_change` (string): Binds group selection changes to a C# action.


---

### `Slider`
A value adjustment slider.
* **Markup Type**: `Slider`
* **Properties**:
  * `value` (float): Initial value.
  * `minimum` (float): Minimum value limit.
  * `maximum` (float): Maximum value limit.
  * `on_value_change` (string): Binds slider thumb value updates to a C# action.


---

### `ProgressBar`
Shows completion progress.
* **Markup Type**: `ProgressBar`
* **Properties**:
  * `value` (float): Current filled amount.
  * `minimum` / `maximum` (float): Bounds.
  * `show_label` (bool): Renders a percentage label overlay.

---

### `ComboBox`
A dropdown selection box.
* **Markup Type**: `ComboBox`
* **Properties**:
  * `items` (array): List of option string values (e.g. `items = ["Option A", "Option B"]`).
  * `selected_index` (int): Index of the default chosen item.
  * `row_height` (int): Height of row items in the dropdown list.
  * `on_selection_change` (string): Binds item selection changes to a C# action.


---

## 4. Visuals & Utilities

### `Image`
Renders a styled image.
* **Markup Type**: `Image`
* **Properties**:
  * Uses standard image styling parameters (`image_source`, etc.).

---

### `Tooltip`
Creates a dedicated hover tooltip boundary zone.
* **Markup Type**: `Tooltip`
* **Properties**:
  * `text` (string): Tooltip hover text.
  * `delay_ms` (int): Delay in milliseconds before opening.

---

### `Table`
Presents columnar data grids.
* **Markup Type**: `Table`
* **Properties**:
  * `show_header` (bool): Displays table column headers.
  * `row_height` (int): Individual grid row height in pixels.

---

### `Graph`
Plots data series lines.
* **Markup Type**: `Graph`
* **Properties**:
  * `minimum_y` / `maximum_y` (float): Range boundary limits of the Y axis.
  * `auto_scale_y` (bool): Dynamically fits bounds to values.
  * `show_series_b` / `show_series_c` (bool): Renders additional data lines.
  * `square_plot` (bool): Renders uniform aspect graph.
  * `show_scale_labels` (bool): Show numbers along the graph border.

---

### `Separator`
Renders a visual horizontal or vertical divider line.
* **Markup Type**: `Separator`

---

### `ScrollBar`
Visual scroll bar control.
* **Markup Type**: `ScrollBar`, `VScrollBar`, or `HScrollBar`
* **Properties**:
  * `orientation` (string): Sets the scrolling axis direction (`vertical` or `horizontal`). Only needed if using type `ScrollBar`.
  * `step` (int): Scroll speed/step increment in pixels.
