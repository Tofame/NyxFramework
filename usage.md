# NyxFramework Integration & Usage Guide

This guide explains how to integrate the building blocks of **NyxFramework** into your own projects and tools. The framework is designed modularly, allowing you to include only what you need (e.g., a CLI tool does not need to bring in OpenGL libraries or UI systems).

---

## Integration Methods

You can integrate NyxFramework into your C#/.NET 10 project in two primary ways:

### 1. Source-Level Integration (Project References)
This method is recommended if you plan to modify or debug NyxFramework classes alongside your application code.

1. Add the NyxFramework repository as a Git submodule in your project repository:
   ```bash
   git submodule add https://github.com/Tofame/SilkFramework.git lib/NyxFramework
   ```
2. Reference the specific project files (`.csproj`) directly from your application's `.csproj`.
   For example, to use only the GUI library:
   ```xml
   <ItemGroup>
       <ProjectReference Include="..\lib\NyxFramework\NyxGUI\NyxGUI.csproj" />
   </ItemGroup>
   ```
3. If using Microsoft Visual Studio or Rider, you can add existing projects directly to your solution (`.sln` or `.slnx`).

---

### 2. Assembly Integration (DLL References)
This method is recommended for stable integrations where you want to keep your project build decoupled from the framework's source compile times.

1. Build the NyxFramework solution in `Release` configuration:
   ```bash
   dotnet build NyxFramework.slnx -c Release
   ```
2. Retrieve the compiled assembly DLLs from the respective project build output directories. They are located under:
   `[ProjectName]/bin/Release/net10.0/[ProjectName].dll`
3. Reference the DLLs in your target project's `.csproj` using `<Reference>` elements:
   ```xml
   <ItemGroup>
       <Reference Include="NyxGUI">
           <HintPath>..\lib\bin\NyxGUI.dll</HintPath>
       </Reference>
   </ItemGroup>
   ```
4. Copy any native dependencies (e.g., Silk.NET native library binaries if using `NyxRender`/`NyxGUIRender`) to your output directory if they are not automatically resolved by the package manager.

---

## Architectural Modularity & Dependency Mapping

Because NyxFramework is decoupled, you should only import the projects/DLLs that match your tool or game's context:

```
[Full Game Client]
  ├── NyxDrawer       (Nyx Sprite/Outfit Rendering)
  ├── NyxRender       (OpenGL Batching & Shaders)
  ├── NyxGUIRender    (OpenGL UI Painter)
  ├── NyxGUI          (UI Widgets & Layout)
  ├── NyxAssets          (Asset Catalog & Sprite Extractors)
  ├── NyxNetwork      (Client & Host Protocol Networking)
  ├── NyxGameMap      (Map Parsing & SEC/NBM IO)
  └── NyxGameCore     (Domain Coordinates & Tile Stacks)
```

### Dependency Reference Matrix

Depending on the type of application you are building, here are the projects you need to import:

| Target Application Type | Required Projects / DLLs | Purpose |
| :--- | :--- | :--- |
| **Full Game Client** | *All projects* | Requires drawing, network sync, game maps, and the UI system. |
| **Graphical Map Editor / Sprite Viewer** | `NyxGUI`, `NyxGUIRender`, `NyxAssets`, `NyxGameMap`, `NyxGameCore`, `NyxRender` | Visual tool requiring map visualization, catalog lookup, and declarative window UI. |
| **Headless Game Server** | `NyxNetwork`, `NyxGameMap`, `NyxGameCore` | Handles connection protocols, validates movement pathfinding, and reads NBM/SEC map files on tick. No rendering dependencies. |
| **CLI Map Converter / Generator** | `NyxGameMap`, `NyxGameCore` | Reads and writes NBM or custom `.sec` sectors. Runs completely headless. |
| **Asset Packer / Sprite Extractor** | `NyxAssets` | standalone extractor/compiler for `.spr` and `.dat` client files. Depends only on `SkiaSharp`. |
