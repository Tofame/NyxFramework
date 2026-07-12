using System.Numerics;
using NyxGui;
using SkiaSharp;
using NyxGuiRender.Gl;
using Silk.NET.OpenGL;

namespace NyxGuiRender.Text;

internal readonly record struct GlyphInfo(
    float U0, float V0, float U1, float V1,
    int Width, int Height,
    float OffsetX, float OffsetY,
    float AdvanceX
);

internal sealed class GuiFontRasterizer : IDisposable
{
    private readonly GL? _gl;
    private SKFont? _font;
    private SKTypeface? _typeface;
    private bool _hardenPixels;
    
    private GuiFontAtlas? _atlas;
    private readonly Dictionary<char, GlyphInfo> _glyphs = new();
    private readonly Dictionary<char, GlyphInfo> _outlinedGlyphs = new();
    private readonly Dictionary<string, (int Width, int Height)> _measureCache = new();

    public bool HasFont => _font is not null;
    public int CacheCount => _glyphs.Count + _outlinedGlyphs.Count;

    public GuiFontRasterizer(GL? gl = null)
    {
        _gl = gl;
        if (gl is not null)
            _atlas = new GuiFontAtlas(gl);
    }

    public GuiTexture? AtlasTexture => _atlas?.Texture;

    public bool TryLoad(string? path, float sizePt, FontStyle style = FontStyle.Regular, bool hardenPixels = false)
    {
        if (path is null || !File.Exists(path))
            return false;
        try
        {
            var skStyle = style switch
            {
                FontStyle.Bold => SKFontStyle.Bold,
                FontStyle.Italic => SKFontStyle.Italic,
                FontStyle.BoldItalic => SKFontStyle.BoldItalic,
                _ => SKFontStyle.Normal
            };
            _typeface = SKTypeface.FromFile(path);
            if (_typeface is null)
                return false;
            _font = new SKFont(_typeface, sizePt);
            _hardenPixels = hardenPixels;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool GetGlyph(char c, bool outlined, out GlyphInfo info)
    {
        var cache = outlined ? _outlinedGlyphs : _glyphs;
        if (cache.TryGetValue(c, out info))
            return true;

        var s = c.ToString();
        ReadOnlySpan<char> charSpan = stackalloc char[] { c };
        Span<ushort> glyphs = stackalloc ushort[1];
        _font!.GetGlyphs(charSpan, glyphs);
        float advanceX = _font.MeasureText(glyphs, out var bounds);
        _font.GetFontMetrics(out var metrics);
        var ascent = metrics.Ascent;

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            info = new GlyphInfo(0, 0, 0, 0, 0, 0, 0, 0, advanceX);
            cache[c] = info;
            return true;
        }

        const int pad = 8;
        float originX = -bounds.Left + pad;
        float originY = -bounds.Top + pad;

        if (outlined)
        {
            int innerW = (int)Math.Ceiling(bounds.Width) + pad * 2;
            int innerH = (int)Math.Ceiling(bounds.Height) + pad * 2;
            var ow = innerW + 2;
            var oh = innerH + 2;
            var pixels = new byte[ow * oh * 4];
            
            var innerPixels = RasterCharWhite(s, innerW, innerH, originX, originY, harden: true);
            ComposeOutlinedDilate(innerPixels, innerW, innerH, pixels, ow, oh);

            float outOffsetX = bounds.Left - pad - 1f;
            float outOffsetY = -ascent + bounds.Top - pad - 1f;

            if (_atlas!.TryAddGlyph(ow, oh, pixels, out var uvX, out var uvY))
            {
                info = new GlyphInfo(
                    uvX / (float)_atlas.Width, uvY / (float)_atlas.Height,
                    (uvX + ow) / (float)_atlas.Width, (uvY + oh) / (float)_atlas.Height,
                    ow, oh,
                    outOffsetX, outOffsetY, 
                    advanceX
                );
                cache[c] = info;
                return true;
            }
        }
        else
        {
            int w = (int)Math.Ceiling(bounds.Width) + pad * 2;
            int h = (int)Math.Ceiling(bounds.Height) + pad * 2;
            var pixels = RasterCharWhite(s, w, h, originX, originY, harden: _hardenPixels);

            float offsetX = bounds.Left - pad;
            float offsetY = -ascent + bounds.Top - pad;

            if (_atlas!.TryAddGlyph(w, h, pixels, out var uvX, out var uvY))
            {
                info = new GlyphInfo(
                    uvX / (float)_atlas.Width, uvY / (float)_atlas.Height,
                    (uvX + w) / (float)_atlas.Width, (uvY + h) / (float)_atlas.Height,
                    w, h,
                    offsetX, offsetY,
                    advanceX
                );
                cache[c] = info;
                return true;
            }
        }

        return false;
    }

    private byte[] RasterCharWhite(string s, int w, int h, float originX, float originY, bool harden)
    {
        var info = new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface is null)
            return new byte[w * h * 4];

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint();
        paint.Color = SKColors.White;
        paint.IsAntialias = !harden;
        
        canvas.DrawText(s, originX, originY, SKTextAlign.Left, _font!, paint);
        canvas.Flush();

        var pixels = new byte[w * h * 4];
        using var image = surface.Snapshot();
        unsafe
        {
            fixed (byte* p = pixels)
            {
                image.ReadPixels(info, (IntPtr)p, w * 4, 0, 0);
            }
        }

        if (harden)
            HardenGlyphWhite(pixels);
        return pixels;
    }

    public void MeasureOutlinedLine(ReadOnlySpan<char> text, out int width, out int height)
    {
        var inner = MeasureLine(text);
        width = inner.Width + 2;
        height = inner.Height + 2;
    }

    public (int Width, int Height) MeasureLine(ReadOnlySpan<char> text)
    {
        var key = text.ToString();
        if (_measureCache.TryGetValue(key, out var cached))
            return cached;

        float totalAdvance = 0f;
        float maxHeight = 0f;
        foreach (var c in text)
        {
            if (GetGlyph(c, false, out var info))
            {
                totalAdvance += info.AdvanceX;
                float rawH = info.Height > 0 ? info.Height - 16 : 0;
                if (rawH > maxHeight)
                    maxHeight = rawH;
            }
        }

        var result = ((int)Math.Ceiling(totalAdvance) + 2, (int)Math.Ceiling(maxHeight) + 2);
        _measureCache[key] = result;
        return result;
    }

    public void Dispose()
    {
        _font?.Dispose();
        _typeface?.Dispose();
        _atlas?.Dispose();
        _measureCache.Clear();
        _glyphs.Clear();
        _outlinedGlyphs.Clear();
    }

    private static void HardenGlyphWhite(byte[] rgba)
    {
        const byte threshold = 96;
        for (var i = 0; i < rgba.Length; i += 4)
        {
            if (rgba[i + 3] < threshold)
            {
                rgba[i] = 0;
                rgba[i + 1] = 0;
                rgba[i + 2] = 0;
                rgba[i + 3] = 0;
                continue;
            }
            rgba[i] = 255;
            rgba[i + 1] = 255;
            rgba[i + 2] = 255;
            rgba[i + 3] = 255;
        }
    }

    private static void ComposeOutlinedDilate(
        ReadOnlySpan<byte> inner, int iw, int ih,
        Span<byte> dst, int ow, int oh)
    {
        const byte fillThreshold = 96;

        for (var y = 0; y < ih; y++)
        {
            for (var x = 0; x < iw; x++)
            {
                var si = (y * iw + x) * 4;
                if (inner[si + 3] < fillThreshold)
                    continue;

                var di = ((y + 1) * ow + (x + 1)) * 4;
                dst[di] = 255;
                dst[di + 1] = 255;
                dst[di + 2] = 255;
                dst[di + 3] = 255;
            }
        }

        for (var oy = 0; oy < oh; oy++)
        {
            for (var ox = 0; ox < ow; ox++)
            {
                var di = (oy * ow + ox) * 4;
                if (dst[di + 3] != 0)
                    continue;

                var ix = ox - 1;
                var iy = oy - 1;
                if (InnerFilled(inner, iw, ih, ix, iy, fillThreshold))
                    continue;

                if (InnerNeighborFilled(inner, iw, ih, ix, iy, fillThreshold))
                {
                    dst[di] = 0;
                    dst[di + 1] = 0;
                    dst[di + 2] = 0;
                    dst[di + 3] = 255;
                }
            }
        }
    }

    private static bool InnerFilled(ReadOnlySpan<byte> inner, int iw, int ih, int x, int y, byte threshold)
    {
        if (x < 0 || y < 0 || x >= iw || y >= ih) return false;
        return inner[(y * iw + x) * 4 + 3] >= threshold;
    }

    private static bool InnerNeighborFilled(ReadOnlySpan<byte> inner, int iw, int ih, int x, int y, byte threshold)
    {
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                if (InnerFilled(inner, iw, ih, x + dx, y + dy, threshold))
                    return true;
            }
        }
        return false;
    }
}
