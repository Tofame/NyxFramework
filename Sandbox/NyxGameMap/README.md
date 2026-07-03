# NyxGameMap

NyxGameMap is a subproject of the NyxFramework. It is responsible for loading and saving different map formats, sectors, and handling conversion between formats.

## Key Features
- **Map Loader & Converters**: Support for loading NBM files and converting them into Minecraft-like chunk sector (`.sec`) files.
- **Dynamic Sector Chunks**: Map represents a grid of 32x32 sectors stored as standalone coordinate-indexed `.sec` chunk files.
- **Metadata Serialization**: Saves ground tiles, stacked items, and tile metadata (such as zone IDs) using a storage-efficient dynamic bitmask schema.
- **Zones Support**: Ability to resolve external zone positions XML and TOML files associated with a map.

## Folder Structure
- `Formats/`: Contains parsers, writers, and converter utilities:
  - `OtbmFormat.cs`: Base parser and serializer for Open Nyx Binary Map (NBM) node trees.
  - `SecFormat.cs`: Reader and writer for the custom binary `.sec` sector chunk format.
  - `OtbmToSecConverter.cs`: Command-line utility to convert an NBM file + separate zone descriptions into `.sec` chunk files.
- `docs/`: Guides on API usage and integration.

## Documentation
Please refer to [docs/usage.md](docs/usage.md) for detailed instructions on API usage and how to integrate NyxGameMap into a game engine.
