using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using NyxGui;
using NyxGuiRender.Gl;
using NyxGuiRender.Text;
using Silk.NET.OpenGL;

namespace NyxGuiRender;

/// <summary>
/// OpenGL backend for <see cref="INyxGuiPainter"/>: one batched flush per frame, cached text bitmaps.
///
/// <b>Frame lifecycle:</b> call <see cref="BeginFrame"/> → draw calls → <see cref="EndFrame"/>.
/// All draws are collected into a <c>_textured</c> list and flushed in a single pass at end-of-frame.
/// Scissor clips are applied per-draw via <see cref="ApplyScissor"/>.
///
/// <b>Sprite32 cache:</b> <see cref="DrawSprite32"/> maintains an LRU cache of up to 512
/// dedicated 32×32 textures keyed by a caller-chosen uint (e.g. sprite ID).  Evicted
/// textures are disposed.
///
/// <b>Text rendering:</b> glyphs are rasterized via <see cref="GuiFontRasterizer"/> into a
/// shared atlas, then drawn as textured quads.  Optional outlined text uses a hardened+outlined
/// variant font cached separately in <see cref="GuiFontCache"/>.
///
/// <b>Image caching:</b> loaded images are cached in <c>_images</c>; a <c>_notFound</c> set
/// avoids repeated disk I/O for missing files.
/// </summary>
public sealed class NyxGuiRenderer : INyxGuiPainter, IDisposable
{
    private readonly GL _gl;
    private readonly GuiShader _shader;
    private readonly GuiSpriteBatch _batch;
    private readonly GuiFontRasterizer _fonts;
    private readonly string? _defaultFontPath;
    private readonly float _defaultSizePt;
    private readonly Func<string, string?>? _resolveFontPath;
    private readonly Dictionary<string, GuiTexture> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<uint, (GuiTexture Texture, LinkedListNode<uint> Node)> _spriteCache = new();
    private readonly LinkedList<uint> _spriteCacheLru = new();
    private const int MaxCachedSprites = 512;
    private readonly HashSet<string> _notFound = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<TexturedDraw> _textured = new();
    private GuiTexture? _scratchText;
    private readonly Stack<NyxRect> _clipStack = new();
    private NyxRect? _activeClip;

    private GuiTexture? _white;
    private Matrix4x4 _projection;
    private int _viewportW = 1;
    private int _viewportH = 1;
    private bool _frameOpen;
    private NyxRect? _lastEmittedClip;
    private int _lastFrameQuadCount;
    private int _lastFrameGlDraws;

    public NyxGuiRenderer(GL gl, NyxGuiFontOptions? fontOptions = null)
    {
        _gl = gl;
        _shader = new GuiShader(gl);
        _batch = new GuiSpriteBatch(gl, _shader);
        _fonts = new GuiFontRasterizer(gl);
        _resolveFontPath = fontOptions?.ResolveFontPath;

        fontOptions ??= new NyxGuiFontOptions();
        _defaultSizePt = fontOptions.SizePt;
        _defaultFontPath = fontOptions.ResolveFontPath?.Invoke(fontOptions.FontFileName ?? string.Empty)
            ?? fontOptions.FontFileName;

        if (_defaultFontPath is not null && File.Exists(_defaultFontPath))
        {
            if (!_fonts.TryLoad(_defaultFontPath, _defaultSizePt))
                Console.WriteLine($"NyxGUIRender: failed to load font \"{_defaultFontPath}\".");
        }
        else
            Console.WriteLine($"NyxGUIRender: UI font not found (tried \"{_defaultFontPath ?? fontOptions.FontFileName}\").");
    }

    /// <summary>True if at least one font has been successfully loaded.</summary>
    public bool HasFont => _fonts.HasFont;

    /// <summary>Returns diagnostic counters for the last completed frame.</summary>
    public NyxGuiRendererStats GetStats() =>
        new()
        {
            LastFrameQuads = _lastFrameQuadCount,
            LastFrameGlDraws = _lastFrameGlDraws,
            CachedTextures = _images.Count,
            CachedTextGlyphs = _fonts.CacheCount + GuiFontCache.TotalGlyphCount,
        };

    /// <summary>Updates the orthographic projection for the new viewport size.</summary>
    public void UpdateViewport(int width, int height)
    {
        _viewportW = Math.Max(1, width);
        _viewportH = Math.Max(1, height);
        _projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);
    }

    /// <summary>Begins a new frame.  Must be paired with <see cref="EndFrame"/>.</summary>
    public void BeginFrame()
    {
        _frameOpen = true;
        _textured.Clear();
        _clipStack.Clear();
        _activeClip = null;

        _gl.Viewport(0, 0, (uint)_viewportW, (uint)_viewportH);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.ScissorTest);
    }

    /// <summary>Flushes all accumulated draws to the GPU and ends the frame.</summary>
    public void EndFrame()
    {
        if (!_frameOpen)
            return;

        while (_clipStack.Count > 0)
            PopClip();
        _gl.Disable(EnableCap.ScissorTest);

        _lastEmittedClip = null;
        _lastFrameQuadCount = _textured.Count;
        _batch.Begin(_projection);
        foreach (var draw in _textured)
            EmitTextured(draw);
        _batch.End();
        _lastFrameGlDraws = _batch.ActualDrawCount;
        _gl.Disable(EnableCap.ScissorTest);

        _textured.Clear();
        _frameOpen = false;
    }

    /// <summary>Pushes a scissor clip rectangle.  Subsequent draws are clipped to the intersection of all active clips.</summary>
    public void PushClip(NyxRect rect)
    {
        _clipStack.Push(rect);
        _activeClip = _activeClip is null ? rect : _activeClip.Value.Intersection(rect);
        if (_activeClip is { Width: <= 0 } or { Height: <= 0 })
            _activeClip = null;
    }

    /// <summary>Pops the most recent scissor clip, recomputing the accumulated intersection of remaining clips.</summary>
    public void PopClip()
    {
        if (_clipStack.Count == 0)
            return;
        _clipStack.Pop();
        // Recompute accumulated clip from remaining stack (pop is rare and stack is shallow).
        _activeClip = RecomputeClip();
    }

    public void FillRect(NyxRect rect, NyxColor color) =>
        QueueSolid(rect, GuiColor.FromNyx(color));

    public void DrawRect(NyxRect rect, NyxColor color, int thickness = 1)
    {
        thickness = Math.Max(1, thickness);
        var c = GuiColor.FromNyx(color);
        QueueSolid(new NyxRect(rect.X, rect.Y, rect.Width, thickness), c);
        QueueSolid(new NyxRect(rect.X, rect.Bottom - thickness, rect.Width, thickness), c);
        QueueSolid(new NyxRect(rect.X, rect.Y + thickness, thickness, rect.Height - 2 * thickness), c);
        QueueSolid(new NyxRect(rect.Right - thickness, rect.Y + thickness, thickness, rect.Height - 2 * thickness), c);
    }

    public void DrawText(
        NyxRect bounds,
        ReadOnlySpan<char> text,
        NyxTextAlign align,
        NyxColor color,
        NyxFontStyle? font = null)
    {
        if (text.Length == 0)
            return;

        var rasterizer = GetRasterizer(font, out var outlined);
        if (!rasterizer.HasFont)
            return;

        var atlasTex = rasterizer.AtlasTexture;
        if (atlasTex is null) return;

        var inner = rasterizer.MeasureLine(text);
        var drawW = inner.Width;
        var drawH = inner.Height;

        var px = align switch
        {
            NyxTextAlign.TopCenter => bounds.X + (bounds.Width - drawW) * 0.5f,
            NyxTextAlign.TopRight => bounds.X + bounds.Width - drawW,
            NyxTextAlign.Center => bounds.X + (bounds.Width - drawW) * 0.5f,
            _ => bounds.X,
        };
        var py = align == NyxTextAlign.Center
            ? bounds.Y + (bounds.Height - drawH) * 0.5f
            : bounds.Y;

        var curX = px;
        var tint = GuiColor.FromNyx(color);

        foreach (var c in text)
        {
            if (rasterizer.GetGlyph(c, outlined, out var glyph))
            {
                if (glyph.Width > 0 && glyph.Height > 0)
                {
                    var drawX = MathF.Round(curX) + glyph.OffsetX;
                    var drawY = MathF.Round(py) + glyph.OffsetY;

                    var w = glyph.Width;
                    var h = glyph.Height;

                    Enqueue(new TexturedDraw(
                        atlasTex, 
                        drawX, 
                        drawY, 
                        w, 
                        h, 
                        tint, 
                        new Vector2(glyph.U0, glyph.V0), 
                        new Vector2(glyph.U1, glyph.V1)));
                }
                curX += glyph.AdvanceX;
            }
        }
    }

    public void MeasureText(ReadOnlySpan<char> text, NyxFontStyle? font, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (text.Length == 0)
            return;

        var rasterizer = GetRasterizer(font, out var outlined);
        if (!rasterizer.HasFont)
            return;

        if (outlined)
            rasterizer.MeasureOutlinedLine(text, out width, out height);
        else
        {
            var inner = rasterizer.MeasureLine(text);
            width = inner.Width;
            height = inner.Height;
        }
    }

    public void DrawImage(in NyxImagePaintCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.ImageSource))
            return;


        if (!_images.TryGetValue(cmd.ImageSource, out var tex))
        {
            if (_notFound.Contains(cmd.ImageSource))
                return;
            try
            {
                tex = new GuiTexture(_gl, cmd.ImageSource, cmd.Smooth);
                _images[cmd.ImageSource] = tex;
            }
            catch (System.Exception)
            {
                _notFound.Add(cmd.ImageSource);
                return;
            }
        }

        var sx0 = cmd.SourceRect?.X ?? 0;
        var sy0 = cmd.SourceRect?.Y ?? 0;
        var sw0 = cmd.SourceRect?.Width ?? tex.Width;
        var sh0 = cmd.SourceRect?.Height ?? tex.Height;
        var baseRect = new NyxRect(sx0, sy0, sw0, sh0);
        var src = baseRect;
        if (cmd.SourceClip is { } clip)
            src = baseRect.Intersection(clip);
        if (src.Width <= 0 || src.Height <= 0)
            return;

        var dest = cmd.Destination;
        var objectFit = cmd.ObjectFit;
        if (objectFit == NyxObjectFit.Fill && cmd.FixedRatio)
            objectFit = NyxObjectFit.Contain;

        switch (objectFit)
        {
            case NyxObjectFit.Contain:
                {
                    var scale = Math.Min(dest.Width / (float)src.Width, dest.Height / (float)src.Height);
                    var w = (int)(src.Width * scale);
                    var h = (int)(src.Height * scale);
                    dest = new NyxRect(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
                }
                break;

            case NyxObjectFit.Cover:
                {
                    var aspectContainer = dest.Width / (float)dest.Height;
                    var aspectSource = src.Width / (float)src.Height;
                    if (aspectSource > aspectContainer)
                    {
                        var newSrcW = src.Height * aspectContainer;
                        var dx = (src.Width - newSrcW) / 2f;
                        src = new NyxRect((int)MathF.Round(src.X + dx), src.Y, (int)MathF.Round(newSrcW), src.Height);
                    }
                    else if (aspectSource < aspectContainer)
                    {
                        var newSrcH = src.Width / aspectContainer;
                        var dy = (src.Height - newSrcH) / 2f;
                        src = new NyxRect(src.X, (int)MathF.Round(src.Y + dy), src.Width, (int)MathF.Round(newSrcH));
                    }
                }
                break;

            case NyxObjectFit.None:
                {
                    var drawW = src.Width;
                    var drawH = src.Height;
                    if (drawW <= dest.Width)
                    {
                        dest = new NyxRect(dest.X + (dest.Width - drawW) / 2, dest.Y, drawW, dest.Height);
                    }
                    else
                    {
                        var dx = (drawW - dest.Width) / 2;
                        src = new NyxRect(src.X + dx, src.Y, dest.Width, src.Height);
                    }

                    if (drawH <= dest.Height)
                    {
                        dest = new NyxRect(dest.X, dest.Y + (dest.Height - drawH) / 2, dest.Width, drawH);
                    }
                    else
                    {
                        var dy = (drawH - dest.Height) / 2;
                        src = new NyxRect(src.X, src.Y + dy, src.Width, dest.Height);
                    }
                }
                break;

            case NyxObjectFit.ScaleDown:
                {
                    if (src.Width <= dest.Width && src.Height <= dest.Height)
                    {
                        var drawW = src.Width;
                        var drawH = src.Height;
                        dest = new NyxRect(dest.X + (dest.Width - drawW) / 2, dest.Y + (dest.Height - drawH) / 2, drawW, drawH);
                    }
                    else
                    {
                        var scale = Math.Min(dest.Width / (float)src.Width, dest.Height / (float)src.Height);
                        var w = (int)(src.Width * scale);
                        var h = (int)(src.Height * scale);
                        dest = new NyxRect(dest.X + (dest.Width - w) / 2, dest.Y + (dest.Height - h) / 2, w, h);
                    }
                }
                break;
        }

        var tint = GuiColor.FromNyx(cmd.Tint);
        if (cmd.ImageBorders.HasAny)
            Enqueue(new TexturedDraw(tex, dest, src, cmd.ImageBorders, tint));
        else
        {
            var u0 = src.X / (float)tex.Width;
            var v0 = src.Y / (float)tex.Height;
            var u1 = src.Right / (float)tex.Width;
            var v1 = src.Bottom / (float)tex.Height;
            Enqueue(new TexturedDraw(tex, dest.X, dest.Y, dest.Width, dest.Height, tint, new Vector2(u0, v0), new Vector2(u1, v1)));
        }
    }

	internal void DrawRawTexture(GuiTexture tex, NyxRect dest, NyxColor tint, bool flipY = true)
	{
		var u0 = 0f;
		var v0 = flipY ? 1f : 0f;
		var u1 = 1f;
		var v1 = flipY ? 0f : 1f;
		Enqueue(new TexturedDraw(tex, dest.X, dest.Y, dest.Width, dest.Height, GuiColor.FromNyx(tint), new Vector2(u0, v0), new Vector2(u1, v1)));
	}

    /// <summary>
    /// Draws a 32×32 RGBA sprite to the destination rectangle.
    /// If <paramref name="cacheKey"/> is non-zero, the sprite is cached in an LRU pool
    /// of up to 512 textures for reuse across frames.  Uncached sprites use a pooled
    /// scratch texture and <c>ArrayPool</c> for the pixel buffer.
    /// </summary>
    public void DrawSprite32(NyxRect dest, ReadOnlySpan<byte> rgba4096, uint cacheKey = 0, bool smooth = false)
    {
        if (rgba4096.Length != 32 * 32 * 4 || dest.Width <= 0 || dest.Height <= 0)
            return;

        var x = (int)MathF.Round(dest.X);
        var y = (int)MathF.Round(dest.Y);
        var w = Math.Max(1, (int)MathF.Round(dest.Width));
        var h = Math.Max(1, (int)MathF.Round(dest.Height));

        if (cacheKey > 0)
        {
            if (!_spriteCache.TryGetValue(cacheKey, out var entry))
            {
                if (_spriteCache.Count >= MaxCachedSprites)
                {
                    var oldestNode = _spriteCacheLru.Last;
                    if (oldestNode != null)
                    {
                        var oldestKey = oldestNode.Value;
                        if (_spriteCache.Remove(oldestKey, out var oldEntry))
                        {
                            oldEntry.Texture.Dispose();
                        }
                        _spriteCacheLru.RemoveLast();
                    }
                }

                var tex = new GuiTexture(_gl, 32, 32, rgba4096, linearFilter: smooth);
                var node = _spriteCacheLru.AddFirst(cacheKey);
                _spriteCache[cacheKey] = (tex, node);
                entry = (tex, node);
            }
            else
            {
                _spriteCacheLru.Remove(entry.Node);
                _spriteCacheLru.AddFirst(entry.Node);
            }

            Enqueue(new TexturedDraw(
                entry.Texture,
                x,
                y,
                w,
                h,
                new GuiColor(255, 255, 255, 255),
                Vector2.Zero,
                Vector2.One));
            return;
        }

        var pixels = ArrayPool<byte>.Shared.Rent(4096);
        rgba4096.CopyTo(pixels);
        Enqueue(new TexturedDraw(
            pixels,
            x,
            y,
            w,
            h,
            new GuiColor(255, 255, 255, 255),
            linearFilter: false));
    }

    public void Dispose()
    {
        foreach (var t in _images.Values)
            t.Dispose();
        _images.Clear();
        foreach (var entry in _spriteCache.Values)
            entry.Texture.Dispose();
        _spriteCache.Clear();
        _spriteCacheLru.Clear();
        _scratchText?.Dispose();
        _white?.Dispose();
        _batch.Dispose();
        _shader.Dispose();
        _fonts.Dispose();
    }

    private GuiFontRasterizer GetRasterizer(NyxFontStyle? font, out bool outlined)
    {
        outlined = font?.Outlined ?? false;
        if (font is null || font.IsDefault)
            return _fonts;

        var path = font.File ?? _defaultFontPath;
        if (!string.IsNullOrEmpty(font.File) && _resolveFontPath is not null)
        {
            var resolved = _resolveFontPath(font.File);
            if (!string.IsNullOrEmpty(resolved))
                path = resolved;
        }

        if (path is null || !File.Exists(path))
            return _fonts;

        var sizePt = font.SizePt ?? _defaultSizePt;
        var style = font.Bold ? FontStyle.Bold : FontStyle.Regular;
        var harden = outlined;

        if (path.Equals(_defaultFontPath, StringComparison.OrdinalIgnoreCase) &&
            Math.Abs(sizePt - _defaultSizePt) < 0.01f &&
            style == FontStyle.Regular &&
            !harden)
            return _fonts;

        var key = new GuiFontCache.FontCacheKey(path, sizePt, style, harden);
        if (GuiFontCache.TryGet(key, out var cached))
            return cached;

        var rasterizer = new GuiFontRasterizer(_gl);
        if (!rasterizer.TryLoad(path, sizePt, style, hardenPixels: harden))
        {
            rasterizer.Dispose();
            return _fonts;
        }

        GuiFontCache.Store(key, rasterizer);
        return rasterizer;
    }

    private void QueueSolid(NyxRect rect, GuiColor color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        Enqueue(new TexturedDraw(White, rect.X, rect.Y, rect.Width, rect.Height, color, Vector2.Zero, Vector2.One));
    }

    private GuiTexture White
    {
        get
        {
            if (_white is null)
                _white = new GuiTexture(_gl, 1, 1, [255, 255, 255, 255], linearFilter: false);
            return _white;
        }
    }

    private void Enqueue(TexturedDraw draw)
    {
        draw.Clip = _activeClip;
        _textured.Add(draw);
    }

    private NyxRect? RecomputeClip()
    {
        NyxRect? acc = null;
        foreach (var r in _clipStack)
            acc = acc is null ? r : acc.Value.Intersection(r);
        return acc is { Width: > 0, Height: > 0 } ? acc : null;
    }

    private void EmitTextured(in TexturedDraw draw)
    {
        if (!ClipsEqual(_lastEmittedClip, draw.Clip))
        {
            _batch.FlushPending();
            _lastEmittedClip = draw.Clip;
        }

        ApplyScissor(draw.Clip);

        if (draw.Sprite32Rgba is { } sprite32)
        {
            _batch.FlushPending();
            var tex = EnsureScratch(32, 32);
            tex.UploadRgba(sprite32);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            var w = draw.W;
            var h = draw.H;
            if (w == 32 && h == 32)
            {
                _batch.Draw(tex, new Vector2(draw.X, draw.Y), new Vector2(32, 32), draw.Tint, Vector2.Zero, Vector2.One);
            }
            else
            {
                var scale = Math.Min(w / 32f, h / 32f);
                var iw = Math.Max(1, (int)(32 * scale));
                var ih = Math.Max(1, (int)(32 * scale));
                var ox = draw.X + (w - iw) / 2f;
                var oy = draw.Y + (h - ih) / 2f;
                _batch.Draw(tex, new Vector2(ox, oy), new Vector2(iw, ih), draw.Tint, Vector2.Zero, Vector2.One);
            }

            ArrayPool<byte>.Shared.Return(sprite32);
            return;
        }

        if (draw.ImageBorders.HasAny && draw.Src is { } src)
        {
            NineSlice.Draw(_batch, draw.Texture!, draw.Dest, src, draw.ImageBorders, draw.Tint);
            return;
        }

        _batch.Draw(
            draw.Texture!,
            new Vector2(draw.X, draw.Y),
            new Vector2(draw.W, draw.H),
            draw.Tint,
            draw.Uv0,
            draw.Uv1);
    }

    private GuiTexture EnsureScratch(int width, int height)
    {
        if (_scratchText is null || _scratchText.Width != width || _scratchText.Height != height)
        {
            _scratchText?.Dispose();
            _scratchText = new GuiTexture(_gl, width, height, new byte[width * height * 4], linearFilter: false);
        }

        return _scratchText;
    }

    private void ApplyScissor(NyxRect? clip)
    {
        if (clip is not { Width: > 0, Height: > 0 } c)
        {
            _gl.Disable(EnableCap.ScissorTest);
            return;
        }

        _gl.Enable(EnableCap.ScissorTest);
        var glY = _viewportH - c.Y - c.Height;
        _gl.Scissor(c.X, glY, (uint)c.Width, (uint)c.Height);
    }

    private static bool ClipsEqual(NyxRect? a, NyxRect? b) =>
        a is null && b is null || a is { } ca && b is { } cb && ca.Equals(cb);

    private struct TexturedDraw
    {
        public TexturedDraw(GuiTexture texture, float x, float y, int w, int h, GuiColor tint, Vector2 uv0, Vector2 uv1)
        {
            Texture = texture;
            X = x;
            Y = y;
            W = w;
            H = h;
            Tint = tint;
            Uv0 = uv0;
            Uv1 = uv1;
        }

        public TexturedDraw(byte[] sprite32Rgba, float x, float y, int w, int h, GuiColor tint, bool linearFilter)
        {
            Sprite32Rgba = sprite32Rgba;
            X = x;
            Y = y;
            W = w;
            H = h;
            Tint = tint;
            LinearFilter = linearFilter;
        }

        public TexturedDraw(GuiTexture texture, NyxRect dest, NyxRect src, NyxImageBorders borders, GuiColor tint)
        {
            Texture = texture;
            Dest = dest;
            Src = src;
            ImageBorders = borders;
            Tint = tint;
        }

        public byte[]? Sprite32Rgba { get; }
        public bool LinearFilter { get; }
        public GuiTexture? Texture { get; }
        public float X { get; }
        public float Y { get; }
        public int W { get; }
        public int H { get; }
        public GuiColor Tint { get; }
        public Vector2 Uv0 { get; }
        public Vector2 Uv1 { get; }
        public NyxImageBorders ImageBorders { get; }
        public NyxRect Dest { get; }
        public NyxRect? Src { get; }
        public NyxRect? Clip { get; set; }
    }
}
