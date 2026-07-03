# NBM (Open Nyx Binary Map) Format Info

The Open Nyx Binary Map (NBM) format is a hierarchical node-based binary format used to store Nyx game maps.

## Magic Header
The format starts with a 4-byte signature:
- Signature: `NBM` (ASCII bytes: `4F 54 42 4D` or `0x4D42544F` as a little-endian `uint32`).

## Node-Based Structure
An NBM file consists of nested serialized nodes using markers:
- `NodeStart` (`0xFE`): Signals the beginning of a node.
- `NodeEnd` (`0xFF`): Signals the end of the current node.
- `EscapeChar` (`0xFD`): Used to escape any special characters (`0xFE`, `0xFF`, or `0xFD`) within the data blocks.

Each node is composed of:
1. `Type` (1 byte): The first byte of the node data signifies the node type.
2. `Data`: Followed by optional data payload.
3. `Children`: Nested nodes enclosed within node boundary markers.

### Node Types
`NyxGameMap` defines the following node types:
- `RootV1` (`1`): The root node of the NBM file containing map parameters.
- `MapData` (`2`): The structural map parent containing all tile areas, towns, and waypoints.
- `TileArea` (`4`): A bounding area holding multiple tiles around a base X/Y coordinate.
- `Tile` (`5`): A standard ground tile containing items.
- `Item` (`6`): A serialized item stacked on a tile.
- `HouseTile` (`14`): A special tile belonging to a house with a specific `HouseId`.

---

## Serialization & Escaping
When writing an NBM node tree:
- Nodes are serialized sequentially.
- If data bytes contain `0xFE`, `0xFF`, or `0xFD`, they are preceded by `0xFD` (the escape character).
- Children are recursively serialized inside their parent node boundaries.
