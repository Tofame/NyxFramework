# Developing and extending NyxAssets

This guide is for contributors and integrators who need to **change** NyxAssets — not just load files from game code.

## Design principles

1. **Parsing stays separate from rendering.** NyxAssets has no OpenGL/GPU dependencies. It produces `ThingCatalog` metadata and decoded RGBA bytes; renderers consume those.
2. **Format seams are interfaces.** New storage backends implement `ISpriteSource` or catalog reader/writer interfaces instead of forking core types.
3. **Span-first I/O.** Hot paths use `ReadOnlySpan<byte>`, `ArrayPool`, and memory-mapped files. Avoid copying whole archives unless the API explicitly targets in-memory use (`Load` vs `OpenReadOnlyFile`).
4. **Asset Editor parity.** Binary layouts match [Asset Editor](https://github.com/ottools/ObjectBuilder) (`MetadataReader*`, `SpriteReader`). When in doubt, compare against that tool’s behavior.

## Source layout

```
NyxAssets/
├── Things/           Domain model + catalog API
│   ThingCatalog, ThingType, ThingFrameGroup
│   DatThingCatalogReader/Writer, JsonThingCatalogReader/Writer
│   ThingTypeJsonMapper          ← JSON field mapping (single source of truth)
│   ItemsXmlMerger               ← items.xml → ExtraProperties
├── Data/
│   Readers/          Zero-copy .dat parsing (LittleEndianSpanReader, decoders)
│   Writers/          .dat serialization
├── Sprites/
│   Readers/          SpriteArchive (.spr), AssetArchive (.assets)
│   Writers/          SpriteSheetCompiler, AssetArchiveWriter
│   SpritePixelCodec  Shared RLE compress/decompress
├── Client/           ClientAssetBundle façade
├── Utils/            PNG/JPEG/BMP export (ImageSharp)
└── Tests/            xUnit round-trip and edge-case tests
```

## Extension points

| Goal | Implement / use | Notes |
|------|-----------------|-------|
| New sprite storage | `ISpriteSource` | Must support `TryDecodeSpriteById`, `DecodeSpriteById`, `IsEmptySprite`, `Dispose`. |
| New thing catalog format | `IThingCatalogReader`, `IThingCatalogWriter` | Populate `ThingCatalog` / read from `ThingCatalog`. JSON is the built-in example. |
| Merge server item metadata | `ItemsXmlMerger` or `ThingCatalog.LoadItemsXml` | Writes into `ThingType.ExtraProperties` only. |
| Export images | `SpriteImageExporter`, `ThingSpriteSheetExporter` | Tooling; not used in-game hot paths. |

See [custom-formats.md](custom-formats.md) for worked examples (PNG atlas, custom binary, etc.).

## Common change scenarios

### Add a JSON-serialized `ThingType` field

Use the centralized mapper — **do not** edit `JsonThingCatalogReader` / `JsonThingCatalogWriter` directly.

→ [json-mapper.md](json-mapper.md)

### Preview or export a thing slice (asset editor)

Use `ThingFrameResolver` to map direction / walk phase / stack count / missile aim to sprite ids.

→ [frame-resolver.md](frame-resolver.md)

### Add a `.dat` flag for a new client version

Edit `ThingPropertyDecoder` and `DatThingPropertySerializer` for the relevant `DatThingFormat` tier. Update [formats/dat-binary.md](../formats/dat-binary.md) if the on-disk layout changed. Add or extend a round-trip test in `Tests/ThingCatalogTests.cs`.

### Add a sprite backend

Implement `ISpriteSource`, wire it through `ClientAssetBundle` constructors/factories if it should be a first-class entry point. Document the binary layout under `docs/formats/`.

### Improve catalog mutation performance

`ThingCatalog.Put*` accepts `rebuildArrays: false` for batch imports; call `InitializeFastArrays()` once after bulk load (see `JsonThingCatalogReader`).

## Conventions

- **Indentation:** 4-space tabs in C# (project-wide).
- **IDs:** Items start at `100`; outfits/effects/missiles at `1`. Catalog section counts are **inclusive max ids**.
- **Sprite ids:** 1-based in public APIs; `0` or missing lookup entry means empty slot.
- **JSON names:** camelCase, matching existing keys in [formats/things-json.md](../formats/things-json.md).
- **Tests:** Run with `dotnet test NyxAssets/Tests/NyxAssets.Tests.csproj`. Prefer round-trip tests when changing serializers.

## Related docs

- [json-mapper.md](json-mapper.md) — JSON read/write implementation
- [frame-resolver.md](frame-resolver.md) — outfit/item/effect/missile frame queries
- [custom-formats.md](custom-formats.md) — custom reader/writer guide
- [formats/things-json.md](../formats/things-json.md) — JSON schema for consumers
- [guides/usage.md](../guides/usage.md) — end-user API examples
