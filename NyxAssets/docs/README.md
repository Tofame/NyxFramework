# NyxAssets documentation

Documentation for using, extending, and maintaining **NyxAssets** — the .NET library for Nyx-style `.dat`, `.spr`, `.assets`, and JSON thing catalogs.

## Guides (using the library)

| Document | Description |
|----------|-------------|
| [API.md](API.md) | **Full public API reference** — every type, property, and method. |
| [guides/usage.md](guides/usage.md) | Project reference, loading bundles, decoding sprites, export utilities, scratch buffers. |
| [guides/supported-clients.md](guides/supported-clients.md) | Which `.dat` tiers are supported, compile APIs, client-version switches. |

## Format specifications (on-disk layout)

| Document | Description |
|----------|-------------|
| [formats/dat-binary.md](formats/dat-binary.md) | Legacy `.dat` header, sections, flag stream, texture block. |
| [formats/spr-binary.md](formats/spr-binary.md) | Legacy `.spr` lookup table, sprite blobs, RLE pixel encoding. |
| [formats/assets-binary.md](formats/assets-binary.md) | Modern `.assets` pages, ZSTD compression, sprite index. |
| [formats/things-json.md](formats/things-json.md) | JSON catalog schema (`things.json`) — fields, frame groups, `properties`. |

## Development (extending NyxAssets)

| Document | Description |
|----------|-------------|
| [development/overview.md](development/overview.md) | Project layout, extension points, conventions, where to change what. |
| [development/json-mapper.md](development/json-mapper.md) | How `ThingTypeJsonMapper` works and how to add JSON fields safely. |
| [development/frame-resolver.md](development/frame-resolver.md) | `ThingFrameResolver` — direction, addons, stack piles, missile aim → sprite ids. |
| [development/custom-formats.md](development/custom-formats.md) | Implementing `ISpriteSource`, `IThingCatalogReader`, `IThingCatalogWriter`. |

## Quick links from the repo root

- [../README.md](../README.md) — library overview and quick start
- [../AGENTS.md](../AGENTS.md) — agent-oriented architecture summary
