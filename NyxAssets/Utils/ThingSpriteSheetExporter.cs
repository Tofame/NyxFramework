using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using NyxAssets.Sprites;
using NyxAssets.Things;

namespace NyxAssets.Utils;

/// <summary>
/// Builds Asset Editor–style sprite sheets: one bitmap per <see cref="ThingFrameGroup"/> or all groups of a <see cref="ThingType"/> stacked vertically
/// (same layout as <c>ThingData.getSpriteSheet</c> / <c>getTotalSpriteSheet</c> in Asset Editor).
/// </summary>
public static class ThingSpriteSheetExporter
{
    /// <summary>One frame group → PNG (32 px per inner cell, matching Asset Editor <c>SpriteExtent.DEFAULT_SIZE</c>).</summary>
    public static bool TryWriteFrameGroupSpriteSheetPng(ISpriteSource archive, ThingFrameGroup group, Stream destination) =>
        TryWriteFrameGroupSpriteSheet(archive, group, SpriteImageExporter.SavePng, destination);

    public static bool TryWriteFrameGroupSpriteSheetJpeg(ISpriteSource archive, ThingFrameGroup group, Stream destination, int quality = SpriteImageExporter.DefaultJpegQuality) =>
        TryWriteFrameGroupSpriteSheet(archive, group, (img, s) => SpriteImageExporter.SaveJpeg(img, s, quality), destination);

    public static bool TryWriteFrameGroupSpriteSheetBmp(ISpriteSource archive, ThingFrameGroup group, Stream destination) =>
        TryWriteFrameGroupSpriteSheet(archive, group, SpriteImageExporter.SaveBmp, destination);

    public static bool TryWriteFrameGroupSpriteSheetPng(ISpriteSource archive, ThingFrameGroup group, string filePath)
    {
        using var fs = File.Create(filePath);
        return TryWriteFrameGroupSpriteSheetPng(archive, group, fs);
    }

    public static bool TryWriteFrameGroupSpriteSheetJpeg(ISpriteSource archive, ThingFrameGroup group, string filePath, int quality = SpriteImageExporter.DefaultJpegQuality)
    {
        using var fs = File.Create(filePath);
        return TryWriteFrameGroupSpriteSheetJpeg(archive, group, fs, quality);
    }

    public static bool TryWriteFrameGroupSpriteSheetBmp(ISpriteSource archive, ThingFrameGroup group, string filePath)
    {
        using var fs = File.Create(filePath);
        return TryWriteFrameGroupSpriteSheetBmp(archive, group, fs);
    }

    /// <summary>All <see cref="ThingType.FrameGroups"/> in order, stacked vertically (Asset Editor <c>getTotalSpriteSheet</c> rules).</summary>
    public static bool TryWriteThingSpriteSheetPng(ISpriteSource archive, ThingType thing, Stream destination) =>
        TryWriteThingSpriteSheet(archive, thing, SpriteImageExporter.SavePng, destination);

    public static bool TryWriteThingSpriteSheetJpeg(ISpriteSource archive, ThingType thing, Stream destination, int quality = SpriteImageExporter.DefaultJpegQuality) =>
        TryWriteThingSpriteSheet(archive, thing, (img, s) => SpriteImageExporter.SaveJpeg(img, s, quality), destination);

    public static bool TryWriteThingSpriteSheetBmp(ISpriteSource archive, ThingType thing, Stream destination) =>
        TryWriteThingSpriteSheet(archive, thing, SpriteImageExporter.SaveBmp, destination);

    public static bool TryWriteThingSpriteSheetPng(ISpriteSource archive, ThingType thing, string filePath)
    {
        using var fs = File.Create(filePath);
        return TryWriteThingSpriteSheetPng(archive, thing, fs);
    }

    public static bool TryWriteThingSpriteSheetJpeg(ISpriteSource archive, ThingType thing, string filePath, int quality = SpriteImageExporter.DefaultJpegQuality)
    {
        using var fs = File.Create(filePath);
        return TryWriteThingSpriteSheetJpeg(archive, thing, fs, quality);
    }

    public static bool TryWriteThingSpriteSheetBmp(ISpriteSource archive, ThingType thing, string filePath)
    {
        using var fs = File.Create(filePath);
        return TryWriteThingSpriteSheetBmp(archive, thing, fs);
    }

    private delegate void SaveRaster(Image<Rgba32> image, Stream stream);

    private static bool TryWriteFrameGroupSpriteSheet(ISpriteSource archive, ThingFrameGroup group, SaveRaster save, Stream destination)
    {
        if (!TryGetFrameGroupBitmapDimensions(group, out var totalX, out var bitmapW, out var bitmapH))
            return false;

        using var sheet = new Image<Rgba32>(bitmapW, bitmapH, default);
        Span<byte> scratch = stackalloc byte[SpritePixelCodec.RgbaBufferLength];
        CompositeFrameGroupOnto(sheet, archive, group, totalX, group.Width, group.Height, scratch, destYOffsetPixels: 0);
        save(sheet, destination);
        return true;
    }

    private static bool TryWriteThingSpriteSheet(ISpriteSource archive, ThingType thing, SaveRaster save, Stream destination)
    {
        if (thing.FrameGroups.Count == 0)
            return false;

        if (!TryGetThingSpriteSheetDimensions(thing, out var totalX, out var maxTileW, out var maxTileH, out var bitmapW, out var bitmapH))
            return false;

        using var sheet = new Image<Rgba32>(bitmapW, bitmapH, default);
        Span<byte> scratch = stackalloc byte[SpritePixelCodec.RgbaBufferLength];
        var cell = SpritePixelCodec.SpriteEdgeLength;
        var pixelsH = (int)(maxTileH * (uint)cell);
        var yOffsetRows = 0;
        foreach (var group in thing.FrameGroups)
        {
            var yPixels = yOffsetRows * pixelsH;
            CompositeFrameGroupOnto(sheet, archive, group, totalX, maxTileW, maxTileH, scratch, yPixels);
            yOffsetRows += (int)group.GetSpriteSheetTextureRows();
        }

        save(sheet, destination);
        return true;
    }

    private static bool TryGetFrameGroupBitmapDimensions(ThingFrameGroup group, out int totalX, out int bitmapW, out int bitmapH)
    {
        totalX = (int)group.GetSpriteSheetTextureColumns();
        if (totalX <= 0)
            totalX = 1;
        var tw = Math.Max(1u, group.Width);
        var th = Math.Max(1u, group.Height);
        var cell = SpritePixelCodec.SpriteEdgeLength;
        var pixelsW = (long)tw * cell;
        var pixelsH = (long)th * cell;
        var bw = pixelsW * totalX;
        var bh = pixelsH * (int)group.GetSpriteSheetTextureRows();
        if (bw > int.MaxValue || bh > int.MaxValue || bw <= 0 || bh <= 0)
        {
            bitmapW = bitmapH = 0;
            return false;
        }

        bitmapW = (int)bw;
        bitmapH = (int)bh;
        return true;
    }

    private static bool TryGetThingSpriteSheetDimensions(ThingType thing, out int totalX, out uint maxTileW, out uint maxTileH, out int bitmapW, out int bitmapH)
    {
        totalX = 0;
        maxTileW = 0;
        maxTileH = 0;
        long totalYRows = 0;
        foreach (var g in thing.FrameGroups)
        {
            var tx = (int)g.GetSpriteSheetTextureColumns();
            if (totalX < tx)
                totalX = tx;
            if (maxTileW < g.Width)
                maxTileW = g.Width;
            if (maxTileH < g.Height)
                maxTileH = g.Height;
            totalYRows += g.GetSpriteSheetTextureRows();
        }

        if (totalX <= 0)
            totalX = 1;
        maxTileW = Math.Max(1u, maxTileW);
        maxTileH = Math.Max(1u, maxTileH);
        var cell = SpritePixelCodec.SpriteEdgeLength;
        var pixelsW = (long)maxTileW * cell;
        var pixelsH = (long)maxTileH * cell;
        var bw = pixelsW * totalX;
        var bh = pixelsH * totalYRows;
        if (bw > int.MaxValue || bh > int.MaxValue || bw <= 0 || bh <= 0)
        {
            bitmapW = bitmapH = 0;
            return false;
        }

        bitmapW = (int)bw;
        bitmapH = (int)bh;
        return true;
    }

    /// <summary>Places sprites for one group into <paramref name="sheet"/> (Asset Editor <c>ThingData.getSpriteSheet</c> loop).</summary>
    private static void CompositeFrameGroupOnto(
        Image<Rgba32> sheet,
        ISpriteSource archive,
        ThingFrameGroup group,
        int totalXColumns,
        uint sheetTileWidth,
        uint sheetTileHeight,
        Span<byte> decodeScratch,
        int destYOffsetPixels)
    {
        var cell = SpritePixelCodec.SpriteEdgeLength;
        var pixelsWidth = (int)(sheetTileWidth * (uint)cell);
        var pixelsHeight = (int)(sheetTileHeight * (uint)cell);
        var totalX = Math.Max(1, totalXColumns);

        for (var f = 0u; f < group.Frames; f++)
        {
            for (var z = 0u; z < group.PatternZ; z++)
            {
                for (var py = 0u; py < group.PatternY; py++)
                {
                    for (var px = 0u; px < group.PatternX; px++)
                    {
                        for (var l = 0u; l < group.Layers; l++)
                        {
                            var texIndex = group.GetTextureIndex(l, px, py, z, f);
                            var fx = (int)(texIndex % (uint)totalX) * pixelsWidth;
                            var fy = (int)(texIndex / (uint)totalX) * pixelsHeight + destYOffsetPixels;

                            for (var w = 0u; w < group.Width; w++)
                            {
                                for (var h = 0u; h < group.Height; h++)
                                {
                                    if (!group.TryGetSpriteId(w, h, l, px, py, z, f, out var spriteId) || spriteId == 0)
                                        continue;
                                    if (!archive.TryDecodeSpriteById(spriteId, decodeScratch))
                                        continue;
                                    var innerX = (int)((group.Width - w - 1) * (uint)cell);
                                    var innerY = (int)((group.Height - h - 1) * (uint)cell);
                                    SpriteImageExporter.BlitSpriteBufferOnto(sheet, fx + innerX, fy + innerY, decodeScratch);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
