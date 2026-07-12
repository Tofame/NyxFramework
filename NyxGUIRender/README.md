# NyxGUIRender

OpenGL implementation of [NyxGUI](../NyxGUI/) `INyxGuiPainter`. **Not** part of NyxRender (game sprites stay there).

## Responsibilities

- Batched textured quads (one GPU flush per frame where possible)
- TTF via SkiaSharp (CPU raster + GPU upload)
- CPU cache for repeated label strings
- 9-slice panel chrome, scissor, solid fills

## Usage

```csharp
var gui = new NyxGuiRenderer(gl, new NyxGuiFontOptions { /* font path */ });
gui.UpdateViewport(width, height);

gui.BeginFrame();
root.Paint(gui, theme);
gui.EndFrame();
```

**Sandbox** uses TOML screens (`engine_stats.toml`, `player_stats.toml`, `bestiary.toml`) and `Sandbox*Nyx` wrappers that call `root.Paint` inside `Draw()`.

## NyxRenderSurface

`NyxRenderSurface` is a custom widget inside `NyxGUIRender` designed to render external content (such as a 2D/3D game scene or map viewport) into an offscreen OpenGL Framebuffer Object (FBO) and paint it as a standard UI widget in the NyxGUI widget tree.

### Key Features
- **Independent Resolution**: Define custom width/height for the internal color attachment texture, decoupled from the screen-space widget layout size.
- **State Preservation**: Automatically saves and restores the active framebuffer binding and OpenGL viewport during the offscreen draw pass.
- **Vertical Flip Support**: The offscreen texture is automatically mapped and flipped to align properly with the standard UI coordinate system (origin at top-left).

### API Overview
- `NyxRenderSurface(GL gl, int surfaceWidth, int surfaceHeight, string? id = null)`: Constructor.
- `int SurfaceWidth` & `int SurfaceHeight`: Control the offscreen resolution (triggers FBO texture reallocation when changed).
- `uint Fbo`: Returns the raw OpenGL Framebuffer handle.
- `uint TextureHandle`: Returns the color attachment texture handle.
- `void BeginRender()`: Binds the framebuffer and configures the OpenGL viewport to the surface dimensions.
- `void EndRender()`: Unbinds the framebuffer and restores the prior framebuffer/viewport state.

### Usage Example
```csharp
// 1. Initialization
var gameSurface = new NyxRenderSurface(gl, 800, 600, "gameSurface");
shell.AdoptIntoGamePanel(gameSurface);

// 2. Rendering Loop
window.Render += deltaTime =>
{
    // Update resolution if widget bounds changed
    gameSurface.SurfaceWidth = gameSurface.Bounds.Width;
    gameSurface.SurfaceHeight = gameSurface.Bounds.Height;

    // Draw offscreen content
    gameSurface.BeginRender();
    gl.Clear(ClearBufferMask.ColorBufferBit);
    gameWorld.Draw(gameSurface.SurfaceWidth, gameSurface.SurfaceHeight);
    gameSurface.EndRender();

    // Draw UI (which includes the gameSurface texture widget)
    guiRenderer.BeginFrame();
    shell.Draw(guiRenderer);
    guiRenderer.EndFrame();
};

// 3. Disposal
window.Closing += () =>
{
    gameSurface.Dispose();
};
```
