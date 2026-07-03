# NyxGameMap Usage Guide

This guide explains how to use the `NyxGameMap` API to load, convert, and manage maps in your game.

## Formats Overview

### NBM (Open Nyx Binary Map)
NBM is a node-based binary tree format. `OtbmParser` parses the raw bytes of an `.nbm` file into an `OtbmNode` tree structure.

### SEC (Sector Chunks)
SEC is a custom, lightweight binary format optimized for streaming map sectors on-demand.
- Each sector is a `32x32` grid of `Tile` objects on a single Z-axis plane.
- The filenames follow the coordinate pattern: `cx_cy_cz.sec` (e.g., `0007_0001_0007.sec` representing sector coordinates $x=7, y=1, z=7$).

---

## 1. Converting NBM to SEC
To convert an NBM map and its optional zone attributes folder to sector files:

```csharp
using NyxGameMap.Formats;

string nbmPath = "resources/world.nbm";
string outputDir = "resources/map/sectors/";

// Converts world.nbm into sectors and outputs them into outputDir.
// If "resources/world-zones/" folder exists, it also parses XML/TOML zone maps.
OtbmToSecConverter.Convert(nbmPath, outputDir);
```

---

## 2. Reading Sector Chunks
To read a sector file back into memory:

```csharp
using NyxGameMap.Formats;

string sectorPath = "resources/map/sectors/0000_0000_0007.sec";
SecSector sector = SecSector.Read(sectorPath);

// Access sector dimensions and tiles
int cx = sector.ChunkX;
int cy = sector.ChunkY;
int cz = sector.ChunkZ;

Tile tile = sector.Tiles[0, 0]; // Access tile at local index x=0, y=0 inside the chunk
uint groundId = tile.GroundId;
```

---

## 3. Writing Sector Chunks
To dynamically construct or edit a sector in memory and save it back to disk:

```csharp
using NyxGameMap.Formats;
using NyxGameCore;

// Create a new sector chunk at sector coordinates (X=1, Y=2, Z=7)
var sector = new SecSector(1, 2, 7);

// Edit a tile
Tile tile = sector.Tiles[10, 15];
tile.SetGround(new NyxGameCore.Item(100)); // Set ground type

// Save the sector to a file
string filename = SecSector.GetFileName(1, 2, 7); // returns "0001_0002_0007.sec"
sector.Write(Path.Combine("resources/map/sectors/", filename));
```
