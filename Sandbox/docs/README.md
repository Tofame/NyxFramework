# Sandbox Architecture Documentation

Welcome to the **Sandbox** architectural overview. The Sandbox project is the sample game application of the NyxFramework. It features a Nyx-style tile map, pathfinding, player and NPC movement, spells, missile effects, an inventory overlay, sidebars, minimaps, engine stats, and a bestiary.

To maintain modularity and readability, the application is divided into a high-level game loop coordinator, a game-state logic system, a dedicated UI screens manager, and nested game logic libraries.

---

## Architecture Diagram

```
       Program.cs (Entry Point)
                 │
                 ▼
            SandboxApp (App lifecycle, window, graphics device, input)
                 │
                 ▼
           ISandboxScreen (active screen state manager)
            /        \
           /          \
          ▼            ▼
   MainMenuScreen    GameplayScreen (Gameplay session state)
                     ├── SandboxGameWorld (Decoupled entities, player, NPCs, spells, assets)
                     ├── SandboxUIManager (GUI stack & sub-screen widgets)
                     │      ├── SandboxShell
                     │      ├── UIInventory
                     │      ├── UIMinimap
                     │      └── Stats Panels (Quest log, Bestiary, etc.)
                     ▼
             [Nested Projects]
             ├── NyxGameCore (Position, Tile, base Item)
             └── NyxGameMap  (GameMap, Sector loading & pathfinding)
```

---

## Component Responsibilities

### 1. [Program.cs](../Program.cs)
The bootstrapper of the Sandbox. It contains only a minimal entry point that instantiates the main application wrapper and runs its window loop:
```csharp
using Sandbox;

using var app = new SandboxApp();
app.Run();
```

### 2. [SandboxApp.cs](../SandboxApp.cs)
Acts as the central orchestrator of the Silk.NET window lifecycle:
* **OpenGL Context**: Initializes the graphics device and global state.
* **State Management**: Manages transitions between active screen states (`ISandboxScreen`) via `TransitionTo`.
* **Input & Keyboard**: Configures inputs and instantiates the `SandboxNyxGUIKeyboard` mapper to track focus and block movement.
* **Loop Coordination**: Coordinates window events (`Load`, `Resize`, `Render`, `Closing`), delegating updates to the active screen.

### 3. [SandboxGameWorld.cs](../SandboxGameWorld.cs)
Owns the game world simulation:
* **Decoupled Architecture**: Fully decoupled from global singletons, accepting state and configurations via constructor/method dependency injection.
* **Assets**: Loads Nyx `.dat` and `.spr` archives, configuration files, items catalogs, and spell databases using modularized load helpers (`TryResolveSpriteSource`, `TryLoadThingCatalog`, `InitializeEntities`, `LoadSpells`).
* **Entities**: Manages player properties, equipment data, paths, and NPC instances.
* **Simulation**: Ticks coordinates, path-walking routines, active missile coordinates, and spelling cast visual effects.

### 4. [SandboxUIManager.cs](../SandboxUIManager.cs)
Encapsulates all NyxGUI widgets, styles, and HUD wrappers:
* **Renderer**: Initializes the `NyxGuiRenderer` and registers font files.
* **Widget Tree**: Creates the root stack, loads screen layout files (`shell.nyxui`, `action_bar.nyxui`, `player_stats.nyxui`, etc.), and docks sidebar modules.
* **Shared Input Routing**: Exposes `ProcessMouse` to dispatch pointer clicks, dragging coordinates, and scroll wheel ticks to the NyxGUI stack from any active screen without duplicating logic.
* **Data Binding**: Directly wires game-world entities (e.g., mapping player backpack slots to `UIInventory` items or spelling scripts to the `ActionBar` callback).

---

## Game-Specific Project Nesting

To prevent game-specific logic from bloating the core NyxFramework library namespaces, two modules are nested inside `Sandbox/` as separate projects:

1. **NyxGameCore** (`Sandbox/NyxGameCore/`): Defines underlying structures such as `Position` coordinate math, the base `Item` class, and the grid `Tile` stack.
2. **NyxGameMap** (`Sandbox/NyxGameMap/`): Handles sector-loading formats (NBM/SEC file structures) and provides A* / BFS pathfinding implementations on the tile map grid.

To avoid duplicate compilation of nested source files by the main `Sandbox` executable project, `Sandbox.csproj` explicitly ignores these directories via MSBuild `<Compile Remove>` tags, referencing them strictly as standard `<ProjectReference>` links.

---

## NyxGUI Widget Extensions (`NyxGUI_Extend`)

UI elements designed to display Nyx/sprite assets (such as `UIItem` and `UIItemStackOverlay`) live in the `Sandbox/NyxGUI_Extend/` folder. These classes are compiled directly inside the `Sandbox` project assembly to provide low-overhead rendering widgets that extend base `NyxElement` behaviors.

---

## Rendering Model (Hybrid Viewport)

To achieve maximum performance and fluid rendering, the Sandbox uses a **Hybrid Viewport** pattern:

1. **Layout Placeholder**: The main UI layout file `shell.nyxui` defines a central `<Container gamePanel>` widget positioned relative to the left and right sidebar panels.
2. **Bounds Retrieval**: During rendering, `SandboxApp` reads the resolved screen-space bounds (`X, Y, Width, Height`) of this placeholder.
3. **Coordinate Flip**: OpenGL defines its viewport origin `(0, 0)` at the bottom-left of the window, whereas UI systems define coordinates from the top-left. `SandboxApp` converts this using:
   $$\text{Viewport Y} = \text{Window Height} - (\text{Bounds Y} + \text{Bounds Height})$$
4. **Scissor Draw**: The backbuffer is scissored to this region (`gl.Enable(EnableCap.ScissorTest)`), the game world draws natively inside it, and finally, the scissor test is disabled before overlays are painted on top.
