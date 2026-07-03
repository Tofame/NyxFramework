# C# API Reference

This document provides a comprehensive C# class and method API reference for the NyxGUI layout framework.

---

## 1. `NyxElement` (Base Class)
The abstract base class for all GUI components. Manages identity, visibility, focus, layout bounds, rendering triggers, styling properties, and input routing.

### Constructors
- `protected NyxElement(uint internalId = 0)`

### Properties
- **Tree**:
  - `Parent` (`NyxElement?`): The immediate parent element.
  - `IsAttached` (`bool`): True if attached to an active UI tree.
- **Bounds**:
  - `Bounds` (`NyxRect`): Bounding box in absolute screen coordinates.
  - `DesiredSize` (`NyxSize`): Size computed during the layout measure pass.
- **States**:
  - `Visible` (`bool`): Dictates rendering and input hit testing.
  - `Enabled` (`bool`): Toggles cursor mouse input and disabled styles.
  - `Phantom` (`bool`): Disables click hit testing while rendering.
  - `Focusable` (`bool`): Enables widget selection and keyboard focus.
  - `IsFocused` (`bool`): True if the widget holds keyboard focus.
  - `IsOn` (`bool`): True if the widget's toggle/selection state is active.
- **Styling**:
  - `States` (`NyxWidgetStates`): Collection of visual overrides per state.
  - `ThemeClass` (`string?`): Selector tag used for style resolution.
  - `Opacity` (`float`): Transparency scalar `0f` (hidden) to `1f` (visible).
  - `Image` (`NyxImageStyle?`): Background image parameters.
  - `Icon` (`NyxIconStyle?`): Overlay icon parameters.
  - `Font` (`NyxFontStyle?`): Typography overrides.
  - `TextOffsetX` / `TextOffsetY` (`int`): Text positioning offset.
  - `BoxSizing` (`NyxBoxSizing`): Sizing calculation behavior (`ContentBox` or `BorderBox`).
  - `Tooltip` (`string?`): Hover hint text.
  - `TooltipDelayMs` (`int`): Miliseconds before displaying tooltip.
- **Input tracking**:
  - `PointerInside` (`bool`): True if pointer is within bounds.
  - `PointerPressed` (`bool`): True if left-mouse button is held down.

### Key Methods
- `public virtual void SetBounds(NyxRect bounds)`: Sets absolute screen space coordinates.
- `public virtual void Measure(NyxSize availableSize)`: Calculates `DesiredSize`.
- `public virtual void Arrange(NyxRect finalRect)`: Sizes and positions children.
- `public void InvalidateLayout()`: Flags layout for recalculation.
- `public void InvalidateStyle()`: Triggers styling property resolution pass.
- `public void InvalidateRender()`: Flags visual repaint.
- `public virtual bool HitTest(int x, int y)`: Returns true if point is inside bounds.
- `public abstract void Paint(INyxGuiPainter painter, NyxGuiTheme theme)`: Draws the component.

### Callbacks
- `public Action<int, int>? RightClicked`: Fires on right click release.

---

## 2. `NyxContainer` (inherits from `NyxElement`)
Main layout container that coordinates child widget positioning and tree updates.

### Constructors
- `public NyxContainer(NyxRect bounds, uint internalId = 0)`

### Properties
- `Children` (`IReadOnlyList<NyxElement>`): Child elements.
- `ChildCount` (`int`): Count of children.
- `ActiveChild` (`NyxElement?`): Focused child widget.

### Key Methods
- `public virtual void AddChild(NyxElement child)`: Adds a child widget.
- `public virtual bool RemoveChild(NyxElement child)`: Removes a child widget.
- `public void ClearChildren()`: Removes all children.
- `public void BringChildToFront(NyxElement child)`: Shifts child to the top of the draw/input hierarchy.
- `public void SetActiveChild(NyxElement? element)`: Focuses specific child widget.
- `public void LayoutTree(NyxSize availableSize)`: Runs measure/arrange steps on the container subtree.
- `public override void SetBounds(NyxRect newBounds)`: Overridden to calculate translation delta (`dx`, `dy`) and shift children's bounds.

---

## 3. Fluent Layout Extension Methods (`NyxElementLayoutExtensions`)
Provides utility extension methods on any `NyxElement` to enable fluent in-code layout, sizing, margin, and anchoring specification:

- `AnchorTop(string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Top)`: Anchors the top edge of the widget to the target's edge.
- `AnchorBottom(string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Bottom)`: Anchors the bottom edge of the widget to the target's edge.
- `AnchorLeft(string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Left)`: Anchors the left edge of the widget to the target's edge.
- `AnchorRight(string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.Right)`: Anchors the right edge of the widget to the target's edge.
- `AnchorHorizontalCenter(string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.CenterX)`: Anchors the horizontal center of the widget to the target's horizontal center line.
- `AnchorVerticalCenter(string target = "parent", NyxAnchorEdge edge = NyxAnchorEdge.CenterY)`: Anchors the vertical center of the widget to the target's vertical center line.
- `AnchorFill(string target = "parent")`: Anchors all four edges of the widget to the target's corresponding edges.
- `Margin(int uniform)`: Applies a uniform margin.
- `Margin(int left, int top, int right, int bottom)`: Applies specific margins to each side.
- `Margin(int? left = null, int? top = null, int? right = null, int? bottom = null)`: Applies partial margins, preserving existing values for unspecified edges.
- `Padding(int uniform)`: Applies a uniform padding.
- `Padding(int left, int top, int right, int bottom)`: Applies specific paddings to each side.
- `Padding(int? left = null, int? top = null, int? right = null, int? bottom = null)`: Applies partial paddings, preserving existing values for unspecified edges.
- `FixedWidth(int width)`: Specifies a fixed width.
- `FixedHeight(int height)`: Specifies a fixed height.
- `FixedSize(int width, int height)`: Specifies a fixed width and height.
- `BoxSizing(NyxBoxSizing boxSizing)`: Configures the box-sizing behavior for the element.
- `StackLayout(Orientation orientation = Orientation.Vertical, int spacing = 0, NyxThickness? padding = null, Alignment alignment = Alignment.Start)`: Attaches a stack layout strategy to the container.
- `GridLayout(int columns = 1, int rows = 0, int spacing = 0, NyxThickness? padding = null, int cellWidth = 0, int cellHeight = 0, bool? fitChildren = null)`: Attaches a grid layout strategy to the container.
- `DockLayout(NyxThickness? padding = null)`: Attaches a dock layout strategy to the container.
- `WrapLayout(Orientation orientation = Orientation.Horizontal, int spacing = 0, NyxThickness? padding = null)`: Attaches a wrap layout strategy to the container.
- `DockChild(Dock dock)`: Configures the docking edge for a child element inside a Dock layout.

---

## 4. Visual States & Styling (`NyxWidgetStates`, `NyxWidgetStateOverrides`)

Visual properties like background color, borders, background images, and opacity are defined on a per-state basis for each widget using the widget's `States` property (`NyxElement.States`).

### `NyxWidgetStates`
A container holding style overrides for all supported interaction states of a widget.
- **Properties**:
  - `Normal` (`NyxWidgetStateOverrides`): Style overrides applied by default when idle.
  - `Hover` (`NyxWidgetStateOverrides`): Style overrides applied when the mouse cursor is over the widget.
  - `Pressed` (`NyxWidgetStateOverrides`): Style overrides applied when the widget is clicked or held.
  - `Focused` (`NyxWidgetStateOverrides`): Style overrides applied when the widget has keyboard focus.
  - `On` (`NyxWidgetStateOverrides`): Style overrides applied when the widget is toggled/turned on (e.g., checkbox is checked).
  - `OnHover` (`NyxWidgetStateOverrides`): Style overrides applied when toggled on and hovered.
  - `OnPressed` (`NyxWidgetStateOverrides`): Style overrides applied when toggled on and pressed.
  - `Disabled` (`NyxWidgetStateOverrides`): Style overrides applied when `Enabled = false`.
- **Methods**:
  - `public NyxWidgetStateOverrides GetStateTable(string stateName)`: Retrieves the styling override instance by its string state name (e.g., `"normal"`, `"hover"`, `"pressed"`, `"disabled"`, `"on"`, `"on.hover"`, `"on.pressed"`).

### `NyxWidgetStateOverrides`
Holds the actual visual style override parameters. If a property is `null`, it falls back to the `Normal` state or widget base style values.
- **Properties**:
  - `BackgroundColor` (`NyxColor?`): Solid background fill color.
  - `BorderWidth` (`int?`): Outline border thickness in pixels.
  - `BorderColor` (`NyxColor?`): Outline border color.
  - `Opacity` (`float?`): State-specific opacity level override (from `0f` to `1f`).
  - `Image` (`NyxImageStyle?`): State-specific background image styling.

### `NyxImageStyle`
Holds styling parameters for textured backgrounds.
- **Properties**:
  - `ImageSource` (`string?`): Path or logical ID of the image file.
  - `ImageFixedRatio` (`bool`): When true, retains the texture's original aspect ratio (legacy property, maps to `NyxObjectFit.Contain`).
  - `ImageRect` (`NyxRect?`): Sub-rectangle in source texture coordinates.
  - `ImageSmooth` (`bool`): Toggles linear filtering (true) or nearest-neighbor (false).
  - `ImageColor` (`NyxColor?`): Tint color applied to the texture.
  - `ImageClip` (`NyxRect?`): Sub-rectangle crop applied after `ImageRect`.
  - `ImageBorders` (`NyxImageBorders`): 9-slice insets for scaling borders.
  - `ImageObjectFit` (`NyxObjectFit`): How the image is resized to fit its container (`Fill`, `Contain`, `Cover`, `None`, `ScaleDown`).

### `NyxObjectFit` (Enum)
Controls how an image fits within the widget's bounds:
- `Fill`: Stretches the image to fill the container bounds completely (ignores aspect ratio, default).
- `Contain`: Scales the image to be as large as possible without cropping or distortion.
- `Cover`: Scales the image to fill the container completely while maintaining its aspect ratio (crops overflowing parts).
- `None`: Keeps the image at its original size, centered within the bounds.
- `ScaleDown`: Behaves as `None` or `Contain`, whichever results in a smaller rendered size.

---

## 5. Core Widget Classes

### `NyxLabel`
Renders a read-only text segment.
- **Properties**:
  - `Text` (`string`): The text string.
  - `Align` (`NyxTextAlign`): Text alignment flags.
  - `Wrap` (`bool`): Wraps text.
  - `LineHeight` (`int`): Distance between wrapped lines.
- **Events**:
  - `event Action<NyxLabel, string, string>? TextChanged`: Fires when text value changes.

---

### `NyxButton`
Clickable push button control.
- **Properties**:
  - `Label` (`string`): Text displayed on the button.
  - `Description` (`string`): Secondary description metadata.
  - `IsSelected` (`bool`): Maps to `IsOn` styling toggle.
- **Events**:
  - `event EventHandler<NyxClickEventArgs>? Click`: Fires on button click.
  - `event EventHandler<NyxClickEventArgs>? RightClick`: Fires on button right click.

---

### `NyxCheckBox`
Binary toggle selection checkbox.
- **Properties**:
  - `Label` (`string`): Checkbox label string.
  - `IsChecked` (`bool`): Checked/unchecked state.
- **Events**:
  - `event EventHandler<EventArgs>? CheckedChanged`: Fires on state toggle.

---

### `NyxRadioButton`
Choice button that functions in mutually exclusive groups.
- **Properties**:
  - `Label` (`string`): Button text.
  - `Group` (`string`): Radio group name.
  - `IsChecked` (`bool`): True if active option.
- **Events**:
  - `event EventHandler<EventArgs>? CheckedChanged`: Fires on selection changes.

### `NyxScrollablePanel`
A container panel that wraps a vertical and/or horizontal scrollbar to show content larger than the panel bounds.
- **Properties**:
  - `InvertedScroll` (`bool`): Inverts scroll directions.
  - `VerticalScroll` (`string`): Target ID of linked vertical scrollbar.
  - `HorizontalScroll` (`string`): Target ID of linked horizontal scrollbar.

---

### `NyxVScrollBar` / `NyxHScrollBar`
Visual scroll bar controls.
- **Properties**:
  - `ScrollStep` (`int`): Scrolling speed/increment in pixels.
  - `Value` (`int`): Current scroll value.
  - `Minimum` (`int`): Minimum range boundary.
  - `Maximum` (`int`): Maximum range boundary.

---

### `NyxSlider`
Horizontal slider control.
- **Properties**:
  - `Minimum` (`float`): Minimum range value limit.
  - `Maximum` (`float`): Maximum range value limit.
  - `Value` (`float`): Current slider value.
- **Events**:
  - `event EventHandler<NyxSliderValueChangedEventArgs>? ValueChanged`: Fires when slider thumb moves.

---

### `NyxTextBox`
Single-line editable text input box.
- **Properties**:
  - `Text` (`string`): Input text value.
  - `MaxLength` (`int`): Maximum allowed input length.
  - `ReadOnly` (`bool`): If true, blocks typing edits.
  - `Align` (`NyxTextAlign`): Horizontal text alignment.
- **Events**:
  - `event Action<NyxTextBox, string, string>? TextChanged`: Fires when text changes.

---

### `NyxMiniWindow`
A window container featuring a title bar, draggable panel frame, and a bottom border resize grip.
- **Properties**:
  - `Title` (`string`): Text rendered on the title bar.
  - `TitleBarHeight` (`int`): Title bar height in pixels.
  - `Resizable` (`bool`): Enables resizing handles.
  - `AutoSize` (`bool`): Automatically expands heights to contain children.
  - `MinExpandedHeight` / `MaxExpandedHeight` (`int`): Resize boundaries.
  - `Body` (`NyxContainer`): Child container where content is drawn.
- **Events**:
  - `event Action<NyxMiniWindow>? DragEnded`: Fires when dragging completes.
  - `event Action<NyxMiniWindow>? ResizeEnded`: Fires when resizing completes.
  - `event Action<NyxMiniWindow>? BoundsChanged`: Fires when bounds change.

---

### `NyxFileDialogButton`
A compound widget that renders a button face alongside a read-only filename label.
Clicking the button face opens an OS-native file dialog on a background thread — the render loop is never blocked.
Cross-platform: PowerShell on Windows, `osascript` on macOS, `zenity`/`kdialog` on Linux.
- **Properties**:
  - `ButtonLabel` (`string`): Text on the button face. Default: `"Select file…"`.
  - `ButtonWidth` (`int`): Width of the button face in pixels. Default: `120`.
  - `Gap` (`int`): Pixel gap between button face and filename label. Default: `6`.
  - `ShowSelectedPath` (`bool`): When `false` hides the filename label. Default: `true`.
  - `PlaceholderText` (`string`): Label text when nothing is selected. Default: `"No file selected"`.
  - `Mode` (`NyxFileDialogMode`): `Open` (default) or `Save`.
  - `DialogOptions` (`NyxFileDialogOptions`): Title, extension filter, initial directory, default extension.
  - `SelectedPath` (`string?`): Full path of the last confirmed selection, or `null`.
- **Events**:
  - `event EventHandler<NyxFileSelectedEventArgs>? FileSelected`: Raised (possibly on a thread-pool thread) after the user confirms a selection. `e.Path` is the absolute path.

### `NyxFileDialogOptions`
Options forwarded to the OS dialog.
- `Title` (`string`): Dialog window title.
- `Extensions` (`string[]?`): Allowed extensions without leading dot, e.g. `new[]{"png","jpg"}`. `null` = all files.
- `FilterLabel` (`string?`): Display name for the extension group in the filter dropdown.
- `InitialDirectory` (`string?`): Starting folder. `null` = OS default.
- `DefaultExtension` (`string?`): Extension appended in Save mode when the user omits one.

### `NyxFileDialog` (static helper)
Low-level async API for launching OS dialogs without the compound widget.
- `static Task<string?> OpenFileAsync(NyxFileDialogOptions? options = null)`
- `static Task<string?> SaveFileAsync(NyxFileDialogOptions? options = null)`
- `static Task<string?> ShowAsync(NyxFileDialogMode mode, NyxFileDialogOptions? options = null)`

All three methods return the chosen absolute path, or `null` if the user cancelled.

## 6. Layout Strategy Classes (`NyxLayout`)

Base abstract class for all layout strategies, delegating the measurement and arrangement steps of containers.

### `NyxLayout` (Abstract Class)
- `public abstract void Measure(NyxContainer container, NyxSize availableSize)`: Computes and sets the container's `DesiredSize`.
- `public abstract void Arrange(NyxContainer container, NyxRect finalRect)`: Positions and sizes all visible children within the final bounding box.

### `NyxStackLayout` (inherits from `NyxLayout`)
Arranges child elements sequentially in a single column or row.
- **Properties**:
  - `Orientation` (`Orientation`): Stacking direction (`Vertical` or `Horizontal`).
  - `Spacing` (`int`): Pixel spacing between children.
  - `Padding` (`NyxThickness`): Inner padding around the stacked area.
  - `Alignment` (`Alignment`): Alignment of children along the cross-axis (`Start`, `Center`, `End`, or `Stretch`).

### `NyxGridLayout` (inherits from `NyxLayout`)
Arranges child elements in columns and rows.
- **Properties**:
  - `Columns` (`int`): The number of grid columns.
  - `Rows` (`int`): The number of grid rows (flows vertically if Columns is 0 and Rows > 0).
  - `Spacing` (`int`): The spacing between cells.
  - `Padding` (`NyxThickness`): Inner padding around the grid.
  - `CellWidth` (`int`): Explicit width for each cell.
  - `CellHeight` (`int`): Explicit height for each cell.
  - `FitChildren` (`bool`): Stretches cells to fill the available container width (default is true when CellWidth is 0).

### `NyxDockLayout` (inherits from `NyxLayout`)
Arranges child elements by docking them to the left, right, top, bottom, or filling the remaining center area.
- **Properties**:
  - `Padding` (`NyxThickness`): Inner padding around the docking bounds.

### `NyxWrapLayout` (inherits from `NyxLayout`)
Arranges child elements sequentially, wrapping them to the next line or row when they exceed the available space.
- **Properties**:
  - `Orientation` (`Orientation`): Layout flow direction (`Horizontal` or `Vertical`).
  - `Spacing` (`int`): Spacing between items.
  - `Padding` (`NyxThickness`): Inner padding around the wrapping area.

---

## 7. Routed Events & Handlers

The NyxGUI programmatic UI has a fully realized routed event system that supports bubbling events up the UI tree hierarchy.

### `NyxEventType` (Enum)
Specifies the type of UI event being routed.
- **Mouse**: `MouseDown`, `MouseUp`, `MouseMove`, `MouseWheel`, `MouseEnter`, `MouseLeave`
- **Keyboard**: `KeyDown`, `KeyUp`, `TextInput`
- **Focus**: `FocusGained`, `FocusLost`
- **Drag & Drop**: `DragStart`, `DragEnter`, `DragLeave`, `DragOver`, `Drop`, `DragEnd`
- **Widgets**: `Click`, `RightClick`, `TextChanged`, `SelectionChanged`, `ValueChanged`, `Checked`, `Unchecked`

### `NyxEventArgs` (Base Class)
The base arguments payload for all routed events.
- **Properties**:
  - `EventType` (`NyxEventType`): The type of this event.
  - `Source` (`NyxElement`): The original widget that raised the event.
  - `CurrentTarget` (`NyxElement?`): The widget currently evaluating the event handler during routing.
  - `Handled` (`bool`): Set to `true` to stop the event from bubbling further up the tree.

### Specialized Event Arguments (inherit from `NyxEventArgs`)
- `NyxMouseEventArgs`: Adds `X`, `Y` (cursor screen coordinates), and `Button` (`NyxMouseButton`).
- `NyxMouseWheelEventArgs`: Adds `Delta` (wheel movement).
- `NyxKeyEventArgs`: Adds `Key` (`NyxGuiKey`) and `Character` (`char?`).
- `NyxTextInputEventArgs`: Adds `Character` (`char`).
- `NyxFocusEventArgs`: Adds `RelatedTarget` (`NyxElement?` element that is losing/gaining focus).
- `NyxDragEventArgs`: Adds `Data` (`NyxDragData?`) and `Target` (`NyxElement?` widget under cursor).
- `NyxDragEndEventArgs`: Adds `Dropped` (`bool` true if drop succeeded).
- `NyxClickEventArgs`: Inherits from `NyxMouseEventArgs` (triggered when clicked).
- `NyxChangedEventArgs`: General value changed event args.

### `NyxEventHandlerExtensions` (Static Extensions)
Utility extension methods on any `NyxElement` to register event listeners programmatically:
- `AddHandler(NyxEventType eventType, NyxEventHandler handler)`: Attaches a handler callback (e.g. `widget.AddHandler(NyxEventType.Click, (sender, args) => { ... });`).
- `RemoveHandler(NyxEventType eventType, NyxEventHandler handler)`: Detaches a previously added handler callback (e.g. `widget.RemoveHandler(NyxEventType.Click, handler);`).

