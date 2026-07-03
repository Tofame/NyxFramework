using NyxAssets.Sprites;
using NyxAssets.Utils;
using NyxAssets.Things;

namespace NyxAssets.Client;

/// <summary>High-level entry: one loaded thing catalog plus one sprite source for the same client build.</summary>
public sealed class ClientAssetBundle : IDisposable
{
    private readonly bool _disposeSprites;

    public ClientAssetBundle(ThingCatalog things, ISpriteSource sprites, bool disposeSprites = false)
    {
        Things = things;
        Sprites = sprites;
        _disposeSprites = disposeSprites;
    }

    public ThingCatalog Things { get; }
    public ISpriteSource Sprites { get; }

    /// <summary>Loads both blobs into managed memory (full <c>byte[]</c> for each file).</summary>
    public static ClientAssetBundle Load(ReadOnlyMemory<byte> dat, ReadOnlyMemory<byte> spr, ClientDataReadOptions options) =>
        new(ThingCatalog.Load(dat, options), SpriteArchive.Load(spr, options), disposeSprites: false);

    /// <inheritdoc cref="Load"/>
    public static ClientAssetBundle LoadFromFiles(string datPath, string sprPath, ClientDataReadOptions options)
    {
        var dat = File.ReadAllBytes(datPath).AsMemory();
        var spr = File.ReadAllBytes(sprPath).AsMemory();
        return Load(dat, spr, options);
    }

    /// <summary>
    /// Reads the whole <c>.dat</c> into memory, but opens <c>.spr</c> with a read-only memory map (no giant <c>byte[]</c> for the sheet).
    /// Dispose the bundle to release the map.
    /// </summary>
    public static ClientAssetBundle OpenFromFiles(string datPath, string sprPath, ClientDataReadOptions options)
    {
        var dat = File.ReadAllBytes(datPath).AsMemory();
        var spr = SpriteArchive.OpenReadOnlyFile(sprPath, options);
        return new(ThingCatalog.Load(dat, options), spr, disposeSprites: true);
    }

    /// <summary>
    /// Opens a ZSTD page-based <c>.assets</c> sprite archive alongside a loaded <c>.dat</c> catalog.
    /// </summary>
    public static ClientAssetBundle OpenAssetsFromFiles(string datPath, string assetsPath, ClientDataReadOptions options, bool preloadPages = false)
    {
        var dat = File.ReadAllBytes(datPath).AsMemory();
        var sprites = AssetArchive.OpenReadOnlyFile(assetsPath, preloadPages);
        return new(ThingCatalog.Load(dat, options), sprites, disposeSprites: true);
    }

    /// <summary>
    /// Loads catalog and sprites from paths, choosing <see cref="SpriteArchive"/> or <see cref="AssetArchive"/> by file extension.
    /// </summary>
    public static ClientAssetBundle OpenFromFilesAuto(string datPath, string spritePath, ClientDataReadOptions options, bool preloadSprites = false)
    {
        if (spritePath.EndsWith(".assets", StringComparison.OrdinalIgnoreCase))
            return OpenAssetsFromFiles(datPath, spritePath, options, preloadSprites);

        return OpenFromFiles(datPath, spritePath, options);
    }

    /// <summary>Uses the sprite source to decode exactly one sprite (see <see cref="ISpriteSource.TryDecodeSpriteById"/>).</summary>
    public bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination) =>
        Sprites.TryDecodeSpriteById(spriteId, rgbaDestination);

    /// <summary>1-based sprite id → 32×32 RGBA buffer (new allocation).</summary>
    public byte[] DecodeSpriteById(uint spriteId) => Sprites.DecodeSpriteById(spriteId);

    /// <summary>Same as <see cref="DecodeSpriteById"/>.</summary>
    public byte[] GetSpriteRgba(uint spriteId) => DecodeSpriteById(spriteId);

    /// <summary>Decodes one sprite and writes a PNG file.</summary>
    public bool TryExportSpritePng(uint spriteId, string filePath) =>
        SpriteImageExporter.TryDecodeAndWritePng(Sprites, spriteId, filePath);

    /// <summary>Decodes one sprite and writes a JPEG file (<paramref name="quality"/> 1–100).</summary>
    public bool TryExportSpriteJpeg(uint spriteId, string filePath, int quality = SpriteImageExporter.DefaultJpegQuality) =>
        SpriteImageExporter.TryDecodeAndWriteJpeg(Sprites, spriteId, filePath, quality);

    /// <summary>Decodes one sprite and writes a BMP file.</summary>
    public bool TryExportSpriteBmp(uint spriteId, string filePath) =>
        SpriteImageExporter.TryDecodeAndWriteBmp(Sprites, spriteId, filePath);

    /// <summary>One frame group as a single spritesheet PNG (Asset Editor layout).</summary>
    public bool TryExportFrameGroupSpriteSheetPng(ThingFrameGroup group, string filePath) =>
        ThingSpriteSheetExporter.TryWriteFrameGroupSpriteSheetPng(Sprites, group, filePath);

    public bool TryExportFrameGroupSpriteSheetJpeg(ThingFrameGroup group, string filePath, int quality = SpriteImageExporter.DefaultJpegQuality) =>
        ThingSpriteSheetExporter.TryWriteFrameGroupSpriteSheetJpeg(Sprites, group, filePath, quality);

    public bool TryExportFrameGroupSpriteSheetBmp(ThingFrameGroup group, string filePath) =>
        ThingSpriteSheetExporter.TryWriteFrameGroupSpriteSheetBmp(Sprites, group, filePath);

    /// <summary>All frame groups of a thing stacked into one PNG (standing + walking for outfits when both exist).</summary>
    public bool TryExportThingSpriteSheetPng(ThingType thing, string filePath) =>
        ThingSpriteSheetExporter.TryWriteThingSpriteSheetPng(Sprites, thing, filePath);

    public bool TryExportThingSpriteSheetJpeg(ThingType thing, string filePath, int quality = SpriteImageExporter.DefaultJpegQuality) =>
        ThingSpriteSheetExporter.TryWriteThingSpriteSheetJpeg(Sprites, thing, filePath, quality);

    public bool TryExportThingSpriteSheetBmp(ThingType thing, string filePath) =>
        ThingSpriteSheetExporter.TryWriteThingSpriteSheetBmp(Sprites, thing, filePath);

    public ThingType GetItem(uint id) => Things.GetItem(id);
    public ThingType GetOutfit(uint id) => Things.GetOutfit(id);
    public ThingType GetEffect(uint id) => Things.GetEffect(id);
    public ThingType GetMissile(uint id) => Things.GetMissile(id);

    public void Dispose()
    {
        if (_disposeSprites)
            Sprites.Dispose();
    }
}
