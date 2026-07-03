# Rendering External Viewports in NyxGUI

NyxGUI is primarily a 2D layout and widget system. When building a game, you often need to render a 3D scene, a tile-based map, or any dynamic OpenGL viewport surrounded by sidebars, action bars, and overlays.

There are two primary patterns supported by the NyxFramework to achieve this:

1. **Offscreen Framebuffer Rendering (`NyxRenderSurface`)**
2. **Direct Screen Rendering (Hybrid Viewport)**

---

## 1. Offscreen Framebuffer Rendering (`NyxRenderSurface`)

The `NyxRenderSurface` class (in `NyxGUIRender`) is a GUI widget that owns an OpenGL Framebuffer Object (FBO) and a color texture attachment.

### How it Works
1. You bind the FBO using `gameSurface.BeginRender()`.
2. You draw your game world directly using standard OpenGL calls. The output is captured in the FBO texture.
3. You unbind the FBO using `gameSurface.EndRender()`.
4. When NyxGUI draws the widget tree, `NyxRenderSurface` paints its captured texture as a textured quad inside its layout bounds on the screen.

### Code Example
```csharp
// Initialization
var gameSurface = new NyxRenderSurface(gl, 800, 600, "gameSurface");
shell.AdoptIntoGamePanel(gameSurface);

// Main loop
window.Render += deltaTime =>
{
    // Make sure the internal FBO texture matches layout dimensions
    gameSurface.SurfaceWidth = gameSurface.Bounds.Width;
    gameSurface.SurfaceHeight = gameSurface.Bounds.Height;

    gameSurface.BeginRender();
    gl.Clear(ClearBufferMask.ColorBufferBit);
    
    // Draw game world here
    gameRenderer.Draw();
    
    gameSurface.EndRender();

    // Paint UI
    guiRenderer.BeginFrame();
    shell.Draw(guiRenderer);
    guiRenderer.EndFrame();
};
```

### When to Use
* When the game view needs UI styling effects, transparency/alpha blending behind overlays, scaling, or rotatable viewports.
* When applying screen-space post-processing shaders (bloom, blur, color grading) to the entire game viewport as a post-pass.

---

## 2. Direct Screen Rendering (Hybrid Viewport)

In the Hybrid Viewport approach, you do not use a framebuffer or texture. Instead, you use a standard, phantom layout container (e.g. `<Container gamePanel>` in a `.nyxui` markup file) to act as a positioning placeholder, and render the game world directly to the backbuffer.

### How it Works
1. Let the GUI layout stack resolve the widget tree bounds.
2. Query the resolved layout bounds of the game panel placeholder.
3. Calculate the OpenGL viewport coordinates. Since OpenGL's origin starts at the bottom-left of the window and UI bounds start at the top-left, you must map the vertical coordinate:
   $$\text{Viewport Y} = \text{Window Height} - (\text{Bounds Y} + \text{Bounds Height})$$
4. Set `gl.Viewport` and `gl.Scissor` to this region.
5. Render the game world directly onto the screen's backbuffer.
6. Render the GUI overlays on top of the game screen.

### Code Example
```csharp
// Query layout panel bounds
var gamePanel = shell.GamePanel;
if (gamePanel is not null)
{
    var bounds = gamePanel.Bounds;
    int vpX = bounds.X;
    int vpY = windowHeight - (bounds.Y + bounds.Height);
    int vpW = bounds.Width;
    int vpH = bounds.Height;

    // Direct scissor clip & viewport configuration
    gl.Viewport(vpX, vpY, (uint)vpW, (uint)vpH);
    gl.Enable(EnableCap.ScissorTest);
    gl.Scissor(vpX, vpY, (uint)vpW, (uint)vpH);

    // Clear and draw game world directly to screen
    gl.ClearColor(0.1f, 0.15f, 0.2f, 1f);
    gl.Clear(ClearBufferMask.ColorBufferBit);
    gameWorld.Draw(vpW, vpH);

    gl.Disable(EnableCap.ScissorTest);
}

// Draw GUI overlay directly on top
guiRenderer.BeginFrame();
shell.Draw(guiRenderer);
guiRenderer.EndFrame();
```

### When to Use
* **For maximum performance**: Ideal for action, real-time, or tile-based games (like Nyx-style clients) where low latency and high framerates are critical.
* When you want to avoid GPU state/binding overhead and memory reallocation stutters during window resizing.

---

## Trade-offs Comparison

| Feature | `NyxRenderSurface` (FBO) | Hybrid Viewport (Direct backbuffer) |
|---|---|---|
| **GPU Context Switches** | High (binds FBO texture, clears FBO, resets state) | Low (only configures viewport/scissor bounds) |
| **Performance Cost** | Moderate-to-high (extra color/depth copies + blending) | Zero overhead (direct-to-screen) |
| **Resizing Behavior** | Stutters (re-allocates GPU texture memory) | Perfectly fluid (dynamic viewport bounds adjustment) |
| **Nesting & Layout** | Simple (acts like a normal widget, handles cropping) | Slightly complex (requires manual coordinate mapping) |
| **Post-Processing** | Very simple (apply shader on widget quad texture) | Harder (requires global post-processing shader setup) |
