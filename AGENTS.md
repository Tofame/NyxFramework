# AGENTS.md — NyxFramework (Root)

Welcome to NyxFramework. This file defines the global solution layout, build rules, dependency graph, and high-level architectural conventions.

## Build & Run

- **.NET 10 SDK** required. Solution file is `NyxFramework.slnx` (XML format, VS 2022 17.10+).
- `dotnet build NyxFramework.slnx` — builds all projects.
- `dotnet run --project Sandbox/Sandbox.csproj` — runs the sample game. Needs client asset files (`*.dat` / `*.spr`) present at runtime.
- No linter, formatter, or `.editorconfig` configured. No CI workflows.

## Tests

- `Tests/` is a **console app** (`OutputType=Exe`), not a test framework. It runs arithmetic checks on NyxRender atlas math.
- Run with `dotnet run --project Tests/Tests.csproj`.
- No unit/integration test framework is in use.

## Project Dependency Graph

```
Sandbox (Exe)
├── NyxDrawer      → NyxRender, NyxAssets
├── NyxGUI         (standalone, no OpenGL dep)
├── NyxGUIRender   → NyxGUI, Silk.NET.OpenGL, SixLabors
├── NyxRender      (Silk.NET OpenGL, StbImageSharp)
├── NyxAssets      (SixLabors.ImageSharp)
├── NyxNetwork     (Sockets, packets & serialization)
├── NyxGameMap     (Nested - Map parsing & pathfinding)
└── NyxGameCore    (Nested - Position math, Item, Tile)
```

- **NyxGUI** is intentionally decoupled from OpenGL — widgets paint through `INyxGuiPainter`.
- **NyxGUIRender** is the OpenGL painter for NyxGUI; separate from NyxRender (game sprites).
- `AllowUnsafeBlocks=true` on Sandbox, NyxRender, NyxGUIRender.

## Hierarchical Agent Contexts

For project-specific rules, APIs, and folder layouts, refer to their localized `AGENTS.md` files:

1. **[NyxAssets/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/NyxAssets/AGENTS.md)** — Binary client assets, catalogs, sprite archive parsing.
2. **[NyxRender/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/NyxRender/AGENTS.md)** — GPU sprite renderer, LRU cache, batched texture atlases.
3. **[NyxDrawer/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/NyxDrawer/AGENTS.md)** — Sprite compositing and high-level drawing wrapper.
4. **[NyxGUI/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/NyxGUI/AGENTS.md)** — Widget trees, programmatic layouters, and anchoring systems.
5. **[NyxGUIRender/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/NyxGUIRender/AGENTS.md)** — OpenGL backend for NyxGUI.
6. **[NyxNetwork/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/NyxNetwork/AGENTS.md)** — Socket management, packets queue, serialization.
7. **[Sandbox/AGENTS.md](file:///d:/ReposCSharp/NyxFramework/Sandbox/AGENTS.md)** — Game shell, configuration, gameplay modules (Map, Core, Spells, UI).

## Global Conventions

- **Decoupling Rule**: Avoid global singletons. Use constructor injection or event callbacks to pass state between game systems and UI.
- **Keyboard & Input**: Keyboard blocking/focus tracking is instanced and passed down. Screen interactions route mouse inputs through `ProcessMouse`.
- **Formatting Rule**: Use 4-width tab indentation for C# files. Pointer notation is typed as `Texture* texture` (no space before asterisk).

<!-- gortex:communities:start -->
<!-- gortex:skills:start -->
## Community Skills

| Area | Description | Skill |
|------|-------------|-------|
| Nyxgui Core 9 Dirs | 408 symbols | `/gortex-nyxgui-core-9-dirs` |
| Nyxgui Core 6 Dirs | 345 symbols | `/gortex-nyxgui-core-6-dirs` |
| Nyxgui Definitions 1 Dirs Trygetint | 330 symbols | `/gortex-nyxgui-definitions-1-dirs-trygetint` |
| Nyxrender 4 Dirs | 280 symbols | `/gortex-nyxrender-4-dirs` |
| Nyxguirender 4 Dirs | 278 symbols | `/gortex-nyxguirender-4-dirs` |
| Nyxassets Things 3 Dirs | 267 symbols | `/gortex-nyxassets-things-3-dirs` |
| Sandbox Ui Inventory 1 Dirs | 237 symbols | `/gortex-sandbox-ui-inventory-1-dirs` |
| Sandbox Items 2 Dirs | 229 symbols | `/gortex-sandbox-items-2-dirs` |
| Nyxgui Core 3 Dirs | 225 symbols | `/gortex-nyxgui-core-3-dirs` |
| Sandbox Ui 2 Dirs Sandboxenginestats | 217 symbols | `/gortex-sandbox-ui-2-dirs-sandboxenginestats` |
| Sandbox Networking Packets 4 Dirs | 180 symbols | `/gortex-sandbox-networking-packets-4-dirs` |
| Sandbox Rendering 4 Dirs | 173 symbols | `/gortex-sandbox-rendering-4-dirs` |
| Nyxgui Core 1 Dirs Invalidatelayout | 164 symbols | `/gortex-nyxgui-core-1-dirs-invalidatelayout` |
| Sandbox Creatures 4 Dirs | 139 symbols | `/gortex-sandbox-creatures-4-dirs` |
| Sandbox Ui Actionbar 2 Dirs | 136 symbols | `/gortex-sandbox-ui-actionbar-2-dirs` |
| Nyxgui Elements 3 Dirs | 132 symbols | `/gortex-nyxgui-elements-3-dirs` |
| Nyxgui Core 4 Dirs | 123 symbols | `/gortex-nyxgui-core-4-dirs` |
| Sandbox 2 Dirs Sandboxgameworld | 116 symbols | `/gortex-sandbox-2-dirs-sandboxgameworld` |
| Sandbox Nyxgamecore 3 Dirs | 110 symbols | `/gortex-sandbox-nyxgamecore-3-dirs` |
| Sandbox Bin Debug Net10 0 Spells Trycastslot | 108 symbols | `/gortex-sandbox-bin-debug-net10-0-spells-trycastslot` |
<!-- gortex:skills:end -->

<!-- gortex:communities:end -->
