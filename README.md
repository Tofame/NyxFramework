# NyxFramework

Single repository for **Nyx**-oriented client building blocks: rendering, client asset I/O,
declarative UI, and small sample/test executables.

## Contents

| Folder | Description |
|--------|-------------|
| **[NyxGameCore](NyxGameCore/)** | Low-level domain library (Position, Tile, base Item, ICreature). |
| **[NyxGameMap](NyxGameMap/README.md)** | Map parsing/writing (NBM support, custom space-saving `.sec` sectors). |
| **[NyxRender](NyxRender/README.md)** | GPU sprite rendering (Silk.NET OpenGL), atlases, batching, effect shaders. |
| **[NyxAssets](NyxAssets/README.md)** | Read/write **`.dat` / `.spr`** in Asset Editor–compatible layouts. |
| **[NyxDrawer](NyxDrawer/README.md)** | Nyx-style drawing (creatures, items, effects, missiles) on NyxAssets + NyxRender. |
| **[NyxGUI](NyxGUI/README.md)** | Widget tree (panels, scroll areas, buttons, windows) with `.nyxui` declarative markup. |
| **[NyxGUIRender](NyxGUIRender/README.md)** | OpenGL UI renderer for NyxGUI (batched quads, cached TTF, 9-slice). Separate from NyxRender sprites. |
| **[NyxNetwork](NyxNetwork/README.md)** | Transport-agnostic, packet-oriented networking (TCP, WebSocket, UDP LAN discovery). |
| **[Sandbox](Sandbox/)** | Sample game/sandbox executable (map, spells, Nyx assets, UI overlays). |

### API Documentation

- [NyxRender docs](NyxRender/docs/) — high-level API guide and sprite cache tuning.
- [NyxGUI docs](NyxGUI/docs/) — layout system, widget authoring, declarative `.nyxui` reference.
- [NyxNetwork docs](NyxNetwork/docs/) — transport abstractions, packet registry, and LAN discovery beacon API.
- [NyxGameMap docs](NyxGameMap/docs/) — NBM converter, sector chunks and binary specifications guide.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build

From the repository root:

```bash
dotnet build NyxFramework.slnx
```

Run the sample app:

```bash
dotnet run --project Sandbox/Sandbox.csproj
```

## Solution layout

The file **`NyxFramework.slnx`** lists the library and app projects. Open it in
**Visual Studio 2022** (17.10+) or use the .NET CLI as above.

Each subproject keeps its own **README** and, where relevant, a **`docs/`** folder
(for example NyxRender's API guide under `NyxRender/docs/`, NyxGUI's layout
system guide under `NyxGUI/docs/`, and NyxNetwork's API guide under `NyxNetwork/docs/`,
and NyxGameMap's map serialization guide under `NyxGameMap/docs/`).
