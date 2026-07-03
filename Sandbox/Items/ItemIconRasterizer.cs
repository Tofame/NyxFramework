using NyxAssets.Things.Frames;
using NyxAssets.Client;
using NyxAssets.Things;

namespace Sandbox.Items;

/// <summary>Decodes Nyx item <c>.dat</c> + <c>.spr</c> layers into a 32×32 RGBA buffer for <see cref="NyxGUI_Extend.UIItem"/>.</summary>
public sealed class ItemIconRasterizer
{
    private readonly ClientAssetBundle _assets;
    private readonly Dictionary<IconCacheKey, byte[]> _cache = new();

    public ItemIconRasterizer(ClientAssetBundle assets) => _assets = assets;

    public bool TryGetIconRgba(uint itemTypeId, ushort count, Span<byte> rgba4096)
    {
        if (itemTypeId == 0 || rgba4096.Length != UIItemSpriteBytes.Length)
            return false;

        var def = ItemsManager.Instance.Get(itemTypeId);
        if (!def.IsNone && def.PrimarySpriteIds is { Length: > 0 })
        {
            var key = IconCacheKey.From(def, count);
            if (_cache.TryGetValue(key, out var cached))
            {
                cached.CopyTo(rgba4096);
                return true;
            }

            if (!TryRasterizeFromItemType(def, count, rgba4096))
                return false;

            _cache[key] = rgba4096.ToArray();
            return true;
        }

        var thing = _assets.Things.TryGetItem(itemTypeId);
        if (thing is null || thing.FrameGroups.Count == 0)
            return false;

        var keyThing = IconCacheKey.From(thing, itemTypeId, count);
        if (_cache.TryGetValue(keyThing, out var cachedThing))
        {
            cachedThing.CopyTo(rgba4096);
            return true;
        }

        if (!TryRasterize(thing, thing.Stackable, count, rgba4096))
            return false;

        _cache[keyThing] = rgba4096.ToArray();
        return true;
    }

    public byte[]? GetOrCreateCached(uint itemTypeId, ushort count = 1)
    {
        if (itemTypeId == 0)
            return null;

        var def = ItemsManager.Instance.Get(itemTypeId);
        if (!def.IsNone && def.PrimarySpriteIds is { Length: > 0 })
        {
            var key = IconCacheKey.From(def, count);
            if (_cache.TryGetValue(key, out var cachedDef))
                return cachedDef;

            var buf = new byte[UIItemSpriteBytes.Length];
            return TryGetIconRgba(itemTypeId, count, buf) ? buf : null;
        }

        var thing = _assets.Things.TryGetItem(itemTypeId);
        if (thing is null || thing.FrameGroups.Count == 0)
            return null;

        var keyThing = IconCacheKey.From(thing, itemTypeId, count);
        if (_cache.TryGetValue(keyThing, out var cachedThing))
            return cachedThing;

        var buf2 = new byte[UIItemSpriteBytes.Length];
        return TryGetIconRgba(itemTypeId, count, buf2) ? buf2 : null;
    }

    private bool TryRasterizeFromItemType(ItemType t, ushort count, Span<byte> dest)
    {
        dest.Clear();
        var w = t.PrimaryWidth == 0 ? 1u : t.PrimaryWidth;
        var h = t.PrimaryHeight == 0 ? 1u : t.PrimaryHeight;
        ItemStackPatterns.Resolve(t.PrimaryPatternX, t.PrimaryPatternY, t.Stackable, count, out var patternX, out var patternY);

        if (w == 1 && h == 1)
            return TryRasterizeDirect32FromItemType(t, patternX, patternY, dest);

        const int cellPx = 32;
        const int canvasSize = 64;
        Span<byte> canvas = stackalloc byte[canvasSize * canvasSize * 4];
        canvas.Clear();

        var originX = (canvasSize - w * cellPx) * 0.5f;
        var originY = (canvasSize - h * cellPx) * 0.5f;
        var frame = 0u;
        Span<byte> cell = stackalloc byte[UIItemSpriteBytes.Length];

        for (var iy = 0u; iy < h; iy++)
        {
            for (var ix = 0u; ix < w; ix++)
            {
                for (var layer = 0u; layer < t.PrimaryLayers; layer++)
                {
                    if (!t.TryGetPrimarySpriteId(ix, iy, layer, patternX, patternY, 0, frame, out var sid) || sid == 0)
                        continue;

                    if (!_assets.TryDecodeSpriteById(sid, cell))
                        continue;

                    var sx = (int)MathF.Round(originX + (w - 1 - ix) * cellPx);
                    var sy = (int)MathF.Round(originY + (h - 1 - iy) * cellPx);
                    BlitOnto(canvas, canvasSize, canvasSize, cell, sx, sy);
                }
            }
        }

        return NormalizeOpaqueToIcon32(canvas, canvasSize, canvasSize, dest);
    }

    private bool TryRasterizeDirect32FromItemType(ItemType t, uint patternX, uint patternY, Span<byte> dest)
    {
        dest.Clear();
        Span<byte> cell = stackalloc byte[UIItemSpriteBytes.Length];
        var drew = false;

        for (var layer = 0u; layer < t.PrimaryLayers; layer++)
        {
            if (!t.TryGetPrimarySpriteId(0, 0, layer, patternX, patternY, 0, 0, out var sid) || sid == 0)
                continue;

            if (!_assets.TryDecodeSpriteById(sid, cell))
                continue;

            BlitOnto(dest, 32, 32, cell, 0, 0);
            drew = true;
        }

        return drew;
    }

    private bool TryRasterize(ThingType thing, bool stackable, ushort count, Span<byte> dest)
    {
        dest.Clear();
        if (thing.FrameGroups.Count == 0)
            return false;

        var fg = thing.FrameGroups[0];
        var w = fg.Width == 0 ? 1u : fg.Width;
        var h = fg.Height == 0 ? 1u : fg.Height;
        ItemStackPatterns.Resolve(fg, stackable, count, out var patternX, out var patternY);

        if (w == 1 && h == 1)
            return TryRasterizeDirect32(fg, patternX, patternY, dest);

        const int cellPx = 32;
        const int canvasSize = 64;
        Span<byte> canvas = stackalloc byte[canvasSize * canvasSize * 4];
        canvas.Clear();

        var originX = (canvasSize - w * cellPx) * 0.5f;
        var originY = (canvasSize - h * cellPx) * 0.5f;
        var frame = 0u;
        Span<byte> cell = stackalloc byte[UIItemSpriteBytes.Length];

        for (var iy = 0u; iy < h; iy++)
        {
            for (var ix = 0u; ix < w; ix++)
            {
                for (var layer = 0u; layer < fg.Layers; layer++)
                {
                    if (!fg.TryGetSpriteId(ix, iy, layer, patternX, patternY, 0, frame, out var sid) || sid == 0)
                        continue;

                    if (!_assets.TryDecodeSpriteById(sid, cell))
                        continue;

                    var sx = (int)MathF.Round(originX + (w - 1 - ix) * cellPx);
                    var sy = (int)MathF.Round(originY + (h - 1 - iy) * cellPx);
                    BlitOnto(canvas, canvasSize, canvasSize, cell, sx, sy);
                }
            }
        }

        return NormalizeOpaqueToIcon32(canvas, canvasSize, canvasSize, dest);
    }

    /// <summary>One dat cell, one pattern — copy spr pixels as-is (OT inventory icon).</summary>
    private bool TryRasterizeDirect32(ThingFrameGroup fg, uint patternX, uint patternY, Span<byte> dest)
    {
        dest.Clear();
        Span<byte> cell = stackalloc byte[UIItemSpriteBytes.Length];
        var drew = false;

        for (var layer = 0u; layer < fg.Layers; layer++)
        {
            if (!fg.TryGetSpriteId(0, 0, layer, patternX, patternY, 0, 0, out var sid) || sid == 0)
                continue;

            if (!_assets.TryDecodeSpriteById(sid, cell))
                continue;

            BlitOnto(dest, 32, 32, cell, 0, 0);
            drew = true;
        }

        return drew;
    }

    /// <summary>Scales visible pixels to fit and centers them in a 32×32 icon (ignores world-map displacement).</summary>
    private static bool NormalizeOpaqueToIcon32(ReadOnlySpan<byte> canvas, int canvasW, int canvasH, Span<byte> dest)
    {
        dest.Clear();
        var minX = canvasW;
        var minY = canvasH;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < canvasH; y++)
        {
            var row = y * canvasW * 4;
            for (var x = 0; x < canvasW; x++)
            {
                if (canvas[row + x * 4 + 3] == 0)
                    continue;

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX)
            return false;

        var bw = maxX - minX + 1;
        var bh = maxY - minY + 1;
        const int outSize = 32;
        const int margin = 1;
        var target = outSize - margin * 2;
        var scale = Math.Min(target / (float)bw, target / (float)bh);
        if (scale > 1f)
            scale = 1f;

        var drawW = Math.Clamp((int)MathF.Round(bw * scale), 1, outSize);
        var drawH = Math.Clamp((int)MathF.Round(bh * scale), 1, outSize);
        var startX = (outSize - drawW) / 2;
        var startY = (outSize - drawH) / 2;

        for (var dy = 0; dy < drawH; dy++)
        {
            var sy = minY + SampleSourceCoord(dy, drawH, bh);
            sy = Math.Clamp(sy, 0, canvasH - 1);
            var row = sy * canvasW * 4;
            var oy = startY + dy;
            if (oy < 0 || oy >= outSize)
                continue;

            var diRow = oy * outSize * 4;

            for (var dx = 0; dx < drawW; dx++)
            {
                var sx = minX + SampleSourceCoord(dx, drawW, bw);
                sx = Math.Clamp(sx, 0, canvasW - 1);
                var ox = startX + dx;
                if (ox < 0 || ox >= outSize)
                    continue;

                var si = row + sx * 4;
                var di = diRow + ox * 4;
                dest[di] = canvas[si];
                dest[di + 1] = canvas[si + 1];
                dest[di + 2] = canvas[si + 2];
                dest[di + 3] = canvas[si + 3];
            }
        }

        return true;
    }

    private static int SampleSourceCoord(int destCoord, int destSize, int sourceSize) =>
        destSize <= 1 ? 0 : (int)((long)destCoord * (sourceSize - 1) / (destSize - 1));

    private static void BlitOnto(Span<byte> dest, int destW, int destH, ReadOnlySpan<byte> src32, int destX, int destY)
    {
        for (var y = 0; y < 32; y++)
        {
            var dy = destY + y;
            if (dy < 0 || dy >= destH)
                continue;

            for (var x = 0; x < 32; x++)
            {
                var dx = destX + x;
                if (dx < 0 || dx >= destW)
                    continue;

                var si = (y * 32 + x) * 4;
                var a = src32[si + 3];
                if (a == 0)
                    continue;

                var di = (dy * destW + dx) * 4;
                if (a == 255)
                {
                    dest[di] = src32[si];
                    dest[di + 1] = src32[si + 1];
                    dest[di + 2] = src32[si + 2];
                    dest[di + 3] = 255;
                }
                else
                {
                    var inv = 255 - a;
                    dest[di] = (byte)((dest[di] * inv + src32[si] * a) / 255);
                    dest[di + 1] = (byte)((dest[di + 1] * inv + src32[si + 1] * a) / 255);
                    dest[di + 2] = (byte)((dest[di + 2] * inv + src32[si + 2] * a) / 255);
                    dest[di + 3] = (byte)Math.Min(255, dest[di + 3] + a);
                }
            }
        }
    }

    private readonly record struct IconCacheKey(uint TypeId, uint PatternX, uint PatternY)
    {
        public static IconCacheKey From(ItemType t, ushort count)
        {
            ItemStackPatterns.Resolve(t.PrimaryPatternX, t.PrimaryPatternY, t.Stackable, count, out var patternX, out var patternY);
            return new(t.DatId, patternX, patternY);
        }

        public static IconCacheKey From(ThingType thing, uint itemTypeId, ushort count)
        {
            var fg = thing.FrameGroups[0];
            ItemStackPatterns.Resolve(fg, thing.Stackable, count, out var patternX, out var patternY);
            return new(itemTypeId, patternX, patternY);
        }
    }
}

/// <summary>Shared size constant for UI item icons (avoids referencing NyxGUI_Extend from Items when not needed).</summary>
public static class UIItemSpriteBytes
{
    public const int Length = 32 * 32 * 4;
}
