# AGENTS.md — NyxGUIRender

This project provides the OpenGL painting implementation for the `NyxGUI` framework.

## Architecture & Responsibilities

- **INyxGuiPainter**: Implements the widget rendering contract via OpenGL commands.
- **Batched Quads**: Gathers UI geometry and flushes quad batches to minimize draw state switches.
- **TTF Rendering**: Renders TrueType font glyphs dynamically via `SixLabors.ImageSharp`.
- **9-Slice Styling**: Draws stretchable UI panels using 9-slice borders.

## Development Guidelines

- **AllowUnsafeBlocks**: Enabled for high-performance direct memory vertex buffer writing.
- **Decoupling**: Keep UI rendering pipeline separate from game-world entity shaders to prevent state leakage.
