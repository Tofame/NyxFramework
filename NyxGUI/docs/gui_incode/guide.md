# Programmatic UI Guide (C# Code-Only)

This guide walks you through building a user interface entirely in C# without using declarative `.nyxui` files.

---

## 1. Structural Workflow

Building programmatic UI follows a strict step-by-step structure to ensure correct layout coordinate calculation:

```
[1] Instantiate Root (at 0,0) 
  ──→ [2] Instantiate Children (at 0,0-based coords)
  ──→ [3] Add Children to Parent via AddChild()
  ──→ [4] Position Root via SetBounds() (propagates absolute screen coords)
  ──→ [5] Register Event Handlers
  ──→ [6] Register Root on NyxGuiRootStack
```

---

## 2. Fluent Anchoring & Sizing (C# Layout Extensions)

Instead of manually instantiating and configuring a `NyxLayoutBox`, NyxGUI provides extension methods in the `NyxGui` namespace so you can configure widget anchoring, sizes, and margins fluently in C#:

```csharp
var title = new NyxLabel()
	.AnchorTop("parent", NyxAnchorEdge.Top)
	.AnchorLeft("parent", NyxAnchorEdge.Left)
	.AnchorRight("parent", NyxAnchorEdge.Right)
	.FixedHeight(18)
	.Margin(12, 10, 12, 0)
	.Text("Title Text");
```

### Supported Layout Helpers
- **Anchoring**: `.AnchorTop(...)`, `.AnchorBottom(...)`, `.AnchorLeft(...)`, `.AnchorRight(...)`, `.AnchorHorizontalCenter(...)`, `.AnchorVerticalCenter(...)`, `.AnchorFill(...)`.
- **Sizing**: `.FixedWidth(w)`, `.FixedHeight(h)`, `.FixedSize(w, h)`, `.BoxSizing(boxSizing)`.
- **Margins**: `.Margin(uniform)`, `.Margin(left, top, right, bottom)`, or `.Margin(left: null, top: null, right: null, bottom: null)` (partial updates).
- **Padding**: `.Padding(uniform)`, `.Padding(left, top, right, bottom)`, or `.Padding(left: null, top: null, right: null, bottom: null)` (partial updates).
- **Layout Engines**:
  - `.StackLayout(orientation, spacing, padding, alignment)`: Attaches a stack layout strategy to the container.
  - `.GridLayout(columns, rows, spacing, padding, cellWidth, cellHeight, fitChildren)`: Attaches a grid layout strategy to the container. Allows row-based or column-based flow. `fitChildren` (default `true`) auto-stretches any dimension not constrained by an explicit `cellWidth`/`cellHeight` to fill the parent's available space.
  - `.DockLayout(padding)`: Attaches a dock layout strategy to the container.
  - `.WrapLayout(orientation, spacing, padding)`: Attaches a wrap layout strategy to the container.
  - `.DockChild(dockEdge)`: Sets the docking edge (`Dock.Left`, `Dock.Right`, `Dock.Top`, `Dock.Bottom`, `Dock.Fill`) for a child inside a Dock layout.

*Example (Dynamic Grid)*:
```csharp
var gridContainer = new NyxContainer(NyxRect.Empty)
	.GridLayout(columns: 3, spacing: 6, padding: NyxThickness.Uniform(8), cellWidth: 60, cellHeight: 60);

for (int i = 0; i < 9; i++)
{
	gridContainer.AddChild(new NyxButton { Label = $"Btn {i}" });
}
```

---

## 3. Walkthrough Example

Here is how to create a popup dialogue that prompts the user:

```csharp
using System;
using NyxGui;

namespace Sandbox.UI
{
    public sealed class ConfirmPrompt
    {
        private const int Width = 300;
        private const int Height = 120;

        private readonly NyxContainer _root;
        private readonly NyxContainer _panel;
        private readonly NyxLabel _messageLabel;
        private readonly NyxButton _yesButton;
        private readonly NyxButton _noButton;

        public bool Visible
        {
            get => _root.Visible;
            set => _root.Visible = value;
        }

        public ConfirmPrompt(int viewportWidth, int viewportHeight, NyxGuiRootStack guiRoots)
        {
            // 1. Create a viewport-sized root container to block or capture interactions
            _root = new NyxContainer(new NyxRect(0, 0, viewportWidth, viewportHeight))
            {
                Visible = false
            };

            // 2. Create the dialogue panel container at (0, 0)
            _panel = new NyxContainer(new NyxRect(0, 0, Width, Height));
            _panel.States.Normal.BackgroundColor = NyxColor.FromRgb(45, 45, 48);
            _panel.States.Normal.BorderWidth = 1;
            _panel.States.Normal.BorderColor = NyxColor.FromRgb(100, 100, 110);

            // 3. Create children widgets relative to the parent panel's (0, 0) coordinate
            _messageLabel = new NyxLabel
            {
                Text = "Are you sure you want to proceed?",
                Align = NyxTextAlign.Center
            };
            _messageLabel.SetBounds(new NyxRect(12, 16, Width - 24, 20));

            _yesButton = new NyxButton { Label = "Yes" };
            _yesButton.SetBounds(new NyxRect(40, Height - 42, 100, 26));

            _noButton = new NyxButton { Label = "No" };
            _noButton.SetBounds(new NyxRect(Width - 140, Height - 42, 100, 26));

            // 4. Add the children to the parent panel, and the panel to the root
            _panel.AddChild(_messageLabel);
            _panel.AddChild(_yesButton);
            _panel.AddChild(_noButton);
            _root.AddChild(_panel);

            // 5. Wire up the event listeners
            _yesButton.Click += (s, e) => HandleChoice(true);
            _noButton.Click += (s, e) => HandleChoice(false);

            // 6. Register on the input stack
            guiRoots.Add(_root, () => Visible);

            // 7. Place the panel on screen (translates the children correctly)
            UpdateViewport(viewportWidth, viewportHeight);
        }

        public void Show()
        {
            Visible = true;
        }

        private void HandleChoice(bool confirm)
        {
            Visible = false;
            Console.WriteLine($"User selected: {(confirm ? "YES" : "NO")}");
        }

        public void UpdateViewport(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            // Fit the full-screen interceptor
            _root.SetBounds(new NyxRect(0, 0, width, height));

            // Center the dialogue panel inside the viewport bounds
            var x = Math.Max(0, (width - Width) / 2);
            var y = Math.Max(0, (height - Height) / 2);
            _panel.SetBounds(new NyxRect(x, y, Width, Height));
        }
    }
}
```

---

## 4. Dynamic Reparenting

At runtime, you can move elements from one container to another:
```csharp
// Removes child from oldParent, updates parenting variables, and invalidates layouts
newParent.AddChild(childWidget);
```

---

## 5. Event Handling in C#

NyxGUI supports two paradigms for handling UI events in C# code: **Direct C# Events** (for quick, widget-specific event subscriptions) and the **Routed Event System** (for generic, bubbling event routing and delegation).

### A. Direct C# Events
Most interactive widgets expose standard C# events that you can subscribe to using the `+=` operator.

```csharp
var button = new NyxButton { Label = "Click Me" };

// Subscribe to click events
button.Click += (sender, args) => 
{
	Console.WriteLine($"Button clicked at coordinates ({args.X}, {args.Y})");
};

var checkBox = new NyxCheckBox { Label = "Enable Feature" };
checkBox.CheckedChanged += (sender, args) =>
{
	Console.WriteLine($"Feature enabled: {checkBox.IsChecked}");
};
```

Common widget-specific events include:
- `NyxButton`: `Click` and `RightClick` (type `EventHandler<NyxClickEventArgs>`)
- `NyxCheckBox` / `NyxRadioButton`: `CheckedChanged` (type `EventHandler<EventArgs>`)
- `NyxSlider`: `ValueChanged` (type `EventHandler<NyxSliderValueChangedEventArgs>`)
- `NyxVScrollBar` / `NyxHScrollBar`: `ValueChanged` (type `EventHandler`)
- `NyxTextBox` / `NyxLabel`: `TextChanged` (type `Action<TWidget, string, string>`)
- `NyxMiniWindow`: `DragEnded`, `ResizeEnded` (type `Action<NyxMiniWindow>`), and `BoundsChanged` (type `Action<NyxMiniWindow>`)
- `NyxFileDialogButton`: `FileSelected` (type `EventHandler<NyxFileSelectedEventArgs>`)


### B. Routed Event System (`AddHandler` / `RemoveHandler`)
NyxGUI provides a bubbling routed event system. When a user interacts with a widget, a routed event is raised. If the event type is configured to bubble, the event travels up the parent hierarchy (child &rarr; parent &rarr; root) until a handler marks it as handled by setting `args.Handled = true`.

To register or unregister a routed event handler programmatically, import the `NyxGui` namespace to access the extension methods:

```csharp
using NyxGui;

var panel = new NyxContainer(bounds);

// Example 1: Add a handler to a specific widget
panel.AddHandler(NyxEventType.MouseEnter, (sender, args) =>
{
	Console.WriteLine("Mouse cursor entered the panel.");
});

// Example 2: Event Delegation (handle child click events on their parent container)
panel.AddHandler(NyxEventType.Click, (sender, args) =>
{
	if (args.Source is NyxButton clickedButton)
	{
		Console.WriteLine($"Clicked button labeled: {clickedButton.Label}");
		args.Handled = true; // Stop event bubbling to parent containers
	}
});
```

To remove a handler later, call `RemoveHandler` with the same delegate:
```csharp
NyxEventHandler onMouseUp = (sender, args) => { /* ... */ };

widget.AddHandler(NyxEventType.MouseUp, onMouseUp);
// ...
widget.RemoveHandler(NyxEventType.MouseUp, onMouseUp);
```

---

## 6. Programmatic File Dialogs

NyxGUI features a cross-platform, zero-dependency file dialog system that can be fully integrated and customized via C# code. You can use either the static `NyxFileDialog` helper to prompt for a file path asynchronously, or the `NyxFileDialogButton` compound widget to render a browse button and path label directly in your layout hierarchy.

### A. Using the Static Helper (`NyxFileDialog`)
The static helper is ideal when you want to trigger a dialog in response to custom input or actions (e.g., clicking a custom button or hitting a keyboard shortcut).

```csharp
// Open Dialog
string? openPath = await NyxFileDialog.OpenFileAsync(new NyxFileDialogOptions
{
    Title = "Open Config",
    Extensions = new[] { "toml", "json" }
});

// Save Dialog
string? savePath = await NyxFileDialog.SaveFileAsync(new NyxFileDialogOptions
{
    Title = "Save Config",
    Extensions = new[] { "toml", "json" },
    DefaultExtension = "toml"
});
```

### B. Using the Button Widget (`NyxFileDialogButton`)
The button widget can be added directly to any parent container. It automatically manages opening the dialog and updating its own label with the chosen file.

```csharp
var fileBtn = new NyxFileDialogButton("configBtn")
{
    ButtonLabel = "Browse...",
    ButtonWidth = 80,
    Mode = NyxFileDialogMode.Open,
    DialogOptions = new NyxFileDialogOptions
    {
        Title = "Select Configuration",
        Extensions = new[] { "toml", "json" }
    }
};

fileBtn.FileSelected += (sender, args) =>
{
    Console.WriteLine($"Selected path: {args.Path}");
};

parent.AddChild(fileBtn);
```

### C. Open vs. Save Mode Differences
- **Open Mode** (`NyxFileDialogMode.Open`):
  - Used for selecting an **existing file**. The user cannot select a file path that does not exist.
  - Useful for importing, opening, or loading assets, configuration files, or documents.
- **Save Mode** (`NyxFileDialogMode.Save`):
  - Used for specifying a target location to **write/create a file**. The target file does not need to exist yet.
  - If the user selects an existing file, the OS dialog automatically prompts them with a confirmation warning to overwrite it.
  - Supports `DefaultExtension`, which is automatically appended if the user omits the file extension in the input box.
  - **Important**: The dialog itself does not perform any writing or file-creation operations. It only returns the selected absolute path string. Your C# code must handle writing the data to that path (e.g., using standard C# I/O APIs such as `System.IO.File.WriteAllText`).

---

## 7. Stacking & Snapping Docks (`NyxDockPanel`)

`NyxDockPanel` is a specialized layout container that manages the stacking, dragging, and automatic docking of `NyxMiniWindow` controls. It is ideal for sidebar columns (like standard MMO/Nyx panels).

### A. How it Works (Under the Hood)
1. **Vertical Stacking**: It stacks all child `NyxMiniWindow` widgets vertically inside its bounds, separated by a configurable `Gap` and offset by a `Margin`.
2. **Snapping & Overlap Detection**: When any `NyxMiniWindow` is dragged, its `DragProgress` event is caught by all active docks. The docks measure the horizontal overlap between the dragged window and the dock panel. If the overlap exceeds a size-adjusted threshold (e.g. `Math.Min(60, window.Width / 2)`), the mini-window is instantly adopted into the dock panel, and its width is automatically constrained to the dock's inner width.
3. **Undocking (Floating)**: If a docked mini-window is dragged horizontally out of the dock panel such that the overlap drops below the threshold, it is automatically removed from the dock and added to the root container (`SandboxShell`), turning it into a free-floating screen-space element.
4. **Dynamic Event Hooking**: A `NyxDockPanel` recursively scans the root UI tree to locate all `NyxMiniWindow` widgets and automatically hooks their drag-progress, drag-ended, and closing handlers.

### B. Declarative vs Programmatic Setup

#### Declarative (TOML / `.nyxui`)
```toml
DockPanel leftPanel
  anchors.top = "rootWindow.top"
  anchors.left = "rootWindow.left"
  anchors.bottom = "rootWindow.bottom"
  fixed_width = 300
  margin = 8
  gap = 6
```

#### Programmatic C# Instantiation
```csharp
var dock = new NyxDockPanel(new NyxRect(0, 0, 300, 768))
{
    Margin = 8,
    Gap = 6
};
parent.AddChild(dock);
```

### C. Sandbox Project Integration Pattern
In the `Sandbox` application, the side dock panels are declared in the unified layout shell (`shell.nyxui`) as `leftPanel` and `rightPanel`. Mini-windows from other modules (like `PlayerStats`, `ExpAnalyzer`, and `Backpack`) are loaded from their own document files and adopted into these docks at startup:

```csharp
// Load the player stats document
playerStatsNyx = new SandboxPlayerStatsNyx(nyxGuiRenderer, nyxGuiSettings, null);

// Adopt the mini-window into the shell's side docks
if (playerStatsNyx.MiniWindow is { } statsMw)
{
    var side = config.Docks.TryGetValue("player_stats", out var d) ? d.Side : "left";
    if (string.Equals(side, "right", StringComparison.OrdinalIgnoreCase))
        shellNyx.AdoptIntoRightDock(playerStatsNyx.Document!);
    else
        shellNyx.AdoptIntoLeftDock(playerStatsNyx.Document!);
}
```

When `Adopt` is called, the child elements are merged into the main shell's widget tree, and the mini-windows are appended as children of the `NyxDockPanel`. The dock panel automatically handles stacking them and managing subsequent dragging gestures.

---

## 8. Key Rules for In-Code UI

1. **Avoid Hardcoding Screen Positions**: Make sure your panel positions are derived from the viewport sizing (e.g. centered or docked relative to edge dimensions).
2. **Coalesced Layout Updates**: If you manually move widgets, adjust sizes, or add elements at runtime, call `InvalidateLayout()` on their parent to make sure layout resolvers realign them during the next system update sweep.
3. **Dynamic Root Resolution**: When moving elements between parents (e.g. docking/undocking), avoid caching root variables. `FindRoot()` evaluates parent paths dynamically, preventing stale caches if a widget's ancestor root changes at runtime.


