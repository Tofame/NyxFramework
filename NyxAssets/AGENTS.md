# AGENTS.md — NyxAssets

This project is responsible for reading and writing client data files (specifically `Nyx.dat` for object definitions and `Nyx.spr` for sprite assets) or custom compiled asset packages.

## Architecture & Responsibilities

- **Thing Catalog**: `ThingCatalog` stores definitions for items, outfits, effects, and missiles. Supports importing/exporting from legacy binary `.dat` formats and modern JSON catalog representations.
- **Thing Exchange**: `ThingDocument`, `ThingDocumentJsonCodec`, and `ObdThingCodec` import/export **single** things as `nyx-thing` JSON or Object Builder `.obd` (with optional embedded sprite pixels).
- **Sprite Archive**: `SpriteArchive` manages sprite lookups and reading RLE-compressed 32×32 pixel buffers from `.spr` files.
- **Client Asset Bundle**: `ClientAssetBundle` wraps both `ThingCatalog` and an `ISpriteSource` to serve as the unified resource container for rendering systems.

## Key APIs & Models

- `ThingCatalog.Load(byte[] datBytes, ClientDataReadOptions options)`: Parse binary definitions.
- `ClientAssetBundle.Load(string datPath, string sprPath, ClientDataReadOptions options)`: Load metadata and sprite lookups.
- `SpritePixelCodec.UncompressToRgba(ReadOnlySpan<byte> src, Span<byte> dest)`: Decompress 32×32 sprite pixels.

## Development Guidelines

- **Decoupling**: Keep file parsing separate from rendering pipelines. `NyxAssets` has zero references to Silk.NET.OpenGL.
- **Performance**: Use `ReadOnlySpan<byte>` and avoid copying raw sprite buffers unnecessarily. Keep decompression fast and low-overhead.
- **JSON fields**: Add or change JSON-serialized `ThingType` properties in `ThingTypeJsonMapper` only — not in `JsonThingCatalogReader`/`Writer`. See [docs/development/json-mapper.md](docs/development/json-mapper.md).

## Documentation

| Area | Path |
|------|------|
| Index | [docs/README.md](docs/README.md) |
| Usage | [docs/guides/usage.md](docs/guides/usage.md) |
| Extend NyxAssets | [docs/development/overview.md](docs/development/overview.md) |
| JSON mapper | [docs/development/json-mapper.md](docs/development/json-mapper.md) |
| Frame resolver | [docs/development/frame-resolver.md](docs/development/frame-resolver.md) |
| Thing exchange | [docs/development/thing-exchange.md](docs/development/thing-exchange.md) |
