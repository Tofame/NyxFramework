# AI Agent Guide — Sandbox

This guide helps AI agents quickly understand, navigate, and refactor the `Sandbox` application within the `NyxFramework` repository.

---

## 🎯 Architecture & Execution Flow

```
Program.cs (Entry Point)
      │
      ▼
 SandboxApp (Instantiates input, window lifecycle, and the keyboard manager)
      │
      ▼
ISandboxScreen (Active screen state manager)
 ├── MainMenuScreen (Asset pre-loading, LAN host discovery, client connections)
 └── GameplayScreen (Ticks and draws the gameplay session)
       ├── SandboxGameWorld (Game simulation, decoupled from singletons)
       └── SandboxUIManager (Main UI stack, widgets, HUD elements)
```

---

## 🚀 Key Entry Points & Files

- **[SandboxApp.cs](file:///d:/ReposCSharp/NyxFramework/Sandbox/SandboxApp.cs)**: Orchestrates the window and graphics device lifecycles. Manages transitions between `ISandboxScreen` screens.
- **[SandboxGameWorld.cs](file:///d:/ReposCSharp/NyxFramework/Sandbox/SandboxGameWorld.cs)**: Handles player, NPC, and item states. Runs entirely off-thread during loading and is fully decoupled (no static singletons).
- **[SandboxUIManager.cs](file:///d:/ReposCSharp/NyxFramework/Sandbox/SandboxUIManager.cs)**: Root of the NyxGUI stack. Hooks and updates stats panels, inventory, action bar, quest log, and minimap.
- **[SandboxNyxGUIKeyboard.cs](file:///d:/ReposCSharp/NyxFramework/Sandbox/UI/SandboxNyxGUIKeyboard.cs)**: Instance-based keyboard state manager. Tracks keyboard focus and determines if game movement keys should be blocked.

---

## 🛠 Asset Loading Lifecycle

Asset loading in `SandboxGameWorld.LoadOffThread` is divided into single-responsibility private helper methods:
1. `TryExportJson`: Runs only if `config.Client.ExportJson` is set. Conversions of raw files happen here, followed by an application exit.
2. `TryResolveSpriteSource`: Probes path configuration and returns an `ISpriteSource` (resolves between `Nyx.spr` or custom ZSTD `Nyx.assets`).
3. `TryLoadThingCatalog`: Parses binary `.dat` client data or `things.json` and bundles it inside a `ClientAssetBundle`.
4. `InitializeEntities`: Validates and instantiates player and NPC outfit appearance types.
5. `LoadSpells`: Loads spells catalog and sets up the active keyboard shortcuts.

---

## ⚠️ Important Conventions & Pitfalls

### 1. No Global Singletons
- Do **not** restore the static `SandboxGameWorld.Instance`.
- Inject the `SandboxGameWorld` instance directly or pass delegate actions/callbacks for event notifications (e.g. `MapItemSurface` sync callback).

### 2. UI Elements Visibility
- Keep Sandbox-specific elements (`UISlot`, `ItemDragService`, `MapItemSurface`, `UIContainer`, etc.) `internal`. There is no cross-assembly consumer for these classes.

### 3. Coordinate Systems
- **World Coordinates**: 3D tiles (`Position(X, Y, Z)`). Z=7 is the main ground floor.
- **Screen/UI Coordinates**: (0,0) is top-left, matching typical desktop layouts.
- **OpenGL Viewport Coordinates**: (0,0) is bottom-left. Be sure to flip the Y-axis when rendering the scissored game panel viewport inside `GameplayScreen.Draw`.

### 4. Input Routing
- Do **not** duplicate mouse handling. Call `SandboxUIManager.ProcessMouse(input)` from screen updates.
- Check the `blocksMovement` flag passed to updates before processing movement controls.
