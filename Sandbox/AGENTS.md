# AGENTS.md — Sandbox

This project is the main executable for the sample game client, orchestrating input, screen management, map drawing, and network messaging.

## Subproject Dependencies

- **NyxGameMap**: Map loading and saving (supporting modern `.nbm` and `.sec` binary layouts), grid pathfinding (A*/BFS).
- **NyxGameCore**: Shared coordinates, base items, tiles stack structures, creature definitions.

## Key Screens & UI Extensions

- **Screens**: Handles main menu, character lobby, and main gameplay screens (e.g. `GameplayScreen`).
- **UI Customizations**: Programs custom widget behaviors for inventories (`UIItem`, `UIItemStackOverlay`) and overlay panels, located in `NyxGUI_Extend/`.

## Conventions

- **Assets Resolution**: Looks up assets (e.g., config files, fonts, layout definitions) under `Sandbox/resources/`.
- **Game Thread Loop**: Avoid running long-running parsing/calculations synchronously on the main thread. Delegate map loading to background loaders where possible.
- **Decoupling**: Ensure game events propagate through callbacks rather than tight couplings with the UI controls.
