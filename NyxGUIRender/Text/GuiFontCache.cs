using Silk.NET.OpenGL;
using SixLabors.Fonts;

namespace NyxGuiRender.Text;

/// <summary>Process-wide font rasterizer cache (Phase 3 — avoids duplicate LRU caches per renderer).</summary>
internal static class GuiFontCache
{
    private static readonly object Gate = new();
    private static readonly Dictionary<FontCacheKey, GuiFontRasterizer> Cache = new();

    public static bool TryGet(FontCacheKey key, out GuiFontRasterizer rasterizer)
    {
        lock (Gate)
            return Cache.TryGetValue(key, out rasterizer!);
    }

    public static void Store(FontCacheKey key, GuiFontRasterizer rasterizer)
    {
        lock (Gate)
            Cache[key] = rasterizer;
    }

    public static int TotalGlyphCount
    {
        get
        {
            lock (Gate)
            {
                var total = 0;
                foreach (var rasterizer in Cache.Values)
                    total += rasterizer.CacheCount;
                return total;
            }
        }
    }

    public static void Clear()
    {
        lock (Gate)
        {
            foreach (var rasterizer in Cache.Values)
                rasterizer.Dispose();
            Cache.Clear();
        }
    }

    internal readonly record struct FontCacheKey(string Path, float SizePt, FontStyle Style, bool Harden);
}
