# Getting Started with NyxGUI

This guide will walk you through the core architecture of NyxGUI and show you how to build a basic user interface module and integrate it with your game host.

---

## High-Level Architecture

NyxGUI is a retained-mode widget framework that decouples layout structure and styling from concrete rendering.

1. **Widget Tree (`NyxElement`)**: Every UI component inherits from `NyxElement`. Containers (like `NyxContainer`, `NyxMiniWindow`, etc.) manage a list of children.
2. **Document (`NyxGuiBuiltDocument`)**: Loaded from a `.nyxui` markup file or built programmatically. Holds references to all instanced widgets and handles by-ID lookups.
3. **Root Stack (`NyxGuiRootStack`)**: Manages the multiple active UI documents/overlays, handles z-ordering, and routes input events (mouse clicks, movement, keypresses) to the correct widgets.
4. **Painter (`INyxGuiPainter`)**: The drawing abstraction. Widgets call painter methods (e.g. `painter.FillRect`, `painter.DrawImage`) during their `Paint()` pass.

---

## Step-by-Step: Creating a GUI Module

A typical module consists of a **layout definition** (usually a `.nyxui` file) and a **C# controller/host class** that manages input, state updates, and drawing.

### Step 1: Write the `.nyxui` Layout
Create a file at `Sandbox/resources/ui/hello_world.nyxui`:

```
# resources/ui/hello_world.nyxui
[document]
  root = "HelloRoot"
  root-window = "rootWindow"

MiniWindow HelloRoot
  title = "Greeting Window"
  fixed-size = 300 150
  image_source = "miniwindow.png"
  image_border = 4
  image_border_top = 23
  visible = false

  Label welcomeLabel
    text = "Hello, adventurer!"
    text-align = "center"
    anchors.left = "parent.left"
    anchors.right = "parent.right"
    anchors.top = "parent.top"
    margin = "12 10"

  Button okButton
    label = "OK"
    anchors.horizontalCenter = "parent.horizontalCenter"
    anchors.bottom = "parent.bottom"
    margin_bottom = 12
    fixed-width = 80
    fixed-height = 26
```

### Step 2: Implement the C# Controller Class
Create a host class in your game project (e.g., `Sandbox/UI/HelloDialog.cs`):

```csharp
using System;
using NyxGui;
using NyxGui.Definitions;

namespace Sandbox.UI
{
    public sealed class HelloDialog
    {
        private readonly NyxGuiBuiltDocument _document;
        private readonly NyxContainer _root;
        private readonly NyxMiniWindow _window;
        private readonly NyxButton _okButton;

        public bool IsOpen
        {
            get => _root.Visible;
            set => _root.Visible = value;
        }

        public NyxContainer Root => _root;

        public HelloDialog(int viewportWidth, int viewportHeight, NyxGuiRootStack guiRoots)
        {
            // 1. Load the UI document layout definition
            var options = new NyxGuiLoadOptions
            {
                UiImagesDirectory = "resources/images/ui",
            };
            _document = NyxGuiDefinitionLoader.Load("resources/ui/hello_world.nyxui", options);
            _root = _document.Root;

            // 2. Resolve references to widgets defined in .nyxui
            _window = _document.TryGet<NyxMiniWindow>("HelloRoot")!;
            _okButton = _document.TryGetButton("okButton")!;

            // 3. Register input/action handlers
            _okButton.Click += (sender, args) => Close();

            // 4. Register the root container on the global GUI stack
            guiRoots.Add(_root, () => IsOpen);

            // 5. Center the window initially
            UpdateViewport(viewportWidth, viewportHeight);
        }

        public void Show()
        {
            IsOpen = true;
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void UpdateViewport(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            // Update document's view dimensions
            _document.SetWindowSize(width, height);
            
            // Adjust coordinates and center the mini window
            var panelW = _window.Bounds.Width;
            var panelH = _window.Bounds.Height;
            var x = Math.Max(0, (width - panelW) / 2);
            var y = Math.Max(0, (height - panelH) / 2);
            
            _window.SetBounds(new NyxRect(x, y, panelW, panelH));
        }
    }
}
```

### Step 3: Integrate with App Loop

1. **Instantiate**: Create the controller inside your game initialization phase:
   ```csharp
   var helloDialog = new HelloDialog(viewportWidth, viewportHeight, guiRoots);
   ```

2. **Resize**: Route window resize events:
   ```csharp
   helloDialog.UpdateViewport(newWidth, newHeight);
   ```

3. **Input**: Ensure key and mouse events are routed to your `NyxGuiRootStack`:
   ```csharp
   guiRoots.ProcessMouse(mouseEventX, mouseEventY, buttonState);
   ```

4. **Paint**: Call `Paint` on the root stack within your main render loop:
   ```csharp
   guiRoots.Paint(painter, activeTheme);
   ```
