# NyxGUI Documentation

Welcome to the **NyxGUI** documentation. NyxGUI is a lightweight, engine-agnostic, retained-mode widget tree designed for in-game user interfaces. It decouples UI layout, styling, and logic from concrete rendering layers, painting entirely through the `INyxGuiPainter` interface.

This documentation is structured to help you understand how to design, build, style, and wire GUI components in NyxGUI.

---

## Documentation Directory

### 🚀 Getting Started & Fundamentals
- 📖 **[Getting Started](getting_started.md)**: Introduction to NyxGUI's architecture, lifecycle, and how to create your first UI module.
- 📐 **[Layout & Coordinate System](layout_system.md)**: Deep dive into screen-space positioning, `SetBounds` propagation, and parent-child coordinate alignment.
- 💡 **[Best Practices & Pitfalls](best_practices.md)**: Coding conventions, layout alignment safety, Z-ordering, and common developer pitfalls.
- 🖼️ **[Rendering External Viewports](render_surface.md)**: Offscreen FBO rendering via NyxRenderSurface vs. Direct Backbuffer Hybrid rendering.

### ✍️ Declarative Layouts (`.nyxui`)
- 📝 **[Declarative Layout Properties](gui_declarative/properties.md)**: Reference sheet of all layout, sizing, image, icon, font, and binding properties available in `.nyxui` files.
- 🧩 **[Declarative Widget Reference](gui_declarative/widgets.md)**: Details on each markup widget type (such as `MiniWindow`, `Label`, `Button`, etc.) and their specific property tags.

### 💻 Programmatic UI (C# Code-Only)
- ⚙️ **[Programmatic UI Guide](gui_incode/guide.md)**: How-to guide for constructing, layering, and wiring widgets dynamically directly in C# code.
- 🗃️ **[C# API Reference](gui_incode/api_reference.md)**: Class API specifications, properties, methods, callbacks, and inheritance layouts.

---

## Core System Overview

```
    .nyxui Markup File  ──┐
                          ├──→ NyxGuiDefinitionBuilder ──→ NyxGuiBuiltDocument
    C# Programmatic API ──┘             │
                                        ├──→ Invalidation & Layout Pass (Measure/Arrange)
                                        └──→ Paint Pass (INyxGuiPainter) ──→ GPU Render
```

- **Retained-mode**: Widgets persist in a tree, maintaining state, input routing, and styling cached across frames.
- **Renderer-agnostic**: Widgets describe *what* to draw via abstract paint commands (`FillRect`, `DrawText`, `DrawImage`), which the game engine's specific painter implementation translates into draw calls.
- **Topological Invalidation**: Changes to properties mark specific nodes dirty, prompting localized style resolution, layout recalculation, and rendering updates without rebuilding the entire UI.
