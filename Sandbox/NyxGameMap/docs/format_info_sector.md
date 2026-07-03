# SEC (Sector Chunks) Format Info

The SEC format is a custom, coordinate-indexed chunk binary format designed for streaming map sectors on-demand.

## Magic Header
The format starts with an 11-byte header:
- Signature (6 bytes): `NYXSEC` (ASCII bytes)
- Version (1 byte): `2` (Current format version)
- SizeX (1 byte): `32` (Width of a sector grid)
- SizeY (1 byte): `32` (Height of a sector grid)
- Sector Header/Flags (2 bytes): Reserved for future flags. Currently set to `0`.

## File Naming Convention
Sector files are saved with the following coordinate naming schema:
`cx_cy_cz.sec`
- `cx`: Sector X coordinate (4-digit integer, e.g. `0000` to `9999`).
- `cy`: Sector Y coordinate (4-digit integer).
- `cz`: Z plane floor layer (4-digit integer).

---

## Sector Data Structure
A sector file contains a continuous stream of `32 * 32 = 1024` tiles stored in row-major order:

### Per Tile Structure:
1. **Ground Type ID** (`uint16` / `2 bytes`):
   - `0` means the tile has no ground.
   - Non-zero values correspond to the ground item ID.
2. **Stacked Items**:
   - `ItemCount` (`uint8` / `1 byte`): The number of items stacked on this tile.
   - For each item:
     - `ItemTypeId` (`uint16` / `2 bytes`): The type index of the item.
     - `Count` (`uint16` / `2 bytes`): The quantity of this item.
