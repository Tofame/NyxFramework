# AGENTS.md — NyxRender

This project is the GPU sprite rendering engine based on Silk.NET OpenGL.

## Architecture & Responsibilities

- **SpriteRenderer**: Batches drawing commands and compiles them into a unified rendering pipeline. Manages active OpenGL textures and shaders.
- **LRU Sprite Cache**: Keeps frequently used sprites in VRAM using a least-recently-used paging and caching strategy, avoiding reloading textures every frame.
- **Batched Atlases**: Sprites are batched into textures to minimize draw calls and bind states.

## Key APIs & Models

- `SpriteRenderer.Begin()`: Prepares the state and shaders for drawing.
- `SpriteRenderer.TryDraw(int spriteId, ReadOnlySpan<byte> rgbaPixels, ...)`: Draw/cache sprite pixels.
- `SpriteRenderer.End()`: Flushes batches and renders to the screen.

## Development Guidelines

- **AllowUnsafeBlocks**: Enabled for high-performance direct buffer management.
- **Decoupling**: Keep OpenGL logic separated from game state and UI layouts. Callers supply raw sprite pixels; `NyxRender` only manages GPU resources and coordinates.
