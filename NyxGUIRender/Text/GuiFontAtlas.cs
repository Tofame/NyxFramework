using System;
using NyxGuiRender.Gl;
using Silk.NET.OpenGL;

namespace NyxGuiRender.Text;

internal sealed class GuiFontAtlas : IDisposable
{
    private readonly GL _gl;
    private GuiTexture? _texture;
    private readonly int _width;
    private readonly int _height;
    private int _curX;
    private int _curY;
    private int _curRowHeight;

    public GuiFontAtlas(GL gl, int width = 2048, int height = 2048)
    {
        _gl = gl;
        _width = width;
        _height = height;

        // Allocate GPU texture without uploading pixel data (GPU zero-inits).
        _texture = new GuiTexture(gl, width, height, linearFilter: false);
    }

    public GuiTexture Texture => _texture ?? throw new ObjectDisposedException(nameof(GuiFontAtlas));
    public int Width => _width;
    public int Height => _height;

    public bool TryAddGlyph(int width, int height, ReadOnlySpan<byte> rgba, out int uvX, out int uvY)
    {
        uvX = 0;
        uvY = 0;

        if (width == 0 || height == 0) return true;

        // 1px padding to avoid bleed
        var pWidth = width + 1;
        var pHeight = height + 1;

        if (_curX + pWidth > _width)
        {
            _curX = 0;
            _curY += _curRowHeight;
            _curRowHeight = 0;
        }

        if (_curY + pHeight > _height)
        {
            // Atlas full (could trigger a resize or clearing, but 2048x2048 is huge for UI fonts)
            return false;
        }

        uvX = _curX;
        uvY = _curY;

        _curX += pWidth;
        if (pHeight > _curRowHeight)
            _curRowHeight = pHeight;

        UploadSubImage(uvX, uvY, width, height, rgba);

        return true;
    }

    private unsafe void UploadSubImage(int x, int y, int w, int h, ReadOnlySpan<byte> rgba)
    {
        _texture!.Bind();
        fixed (byte* ptr = rgba)
        {
            _gl.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                x,
                y,
                (uint)w,
                (uint)h,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ptr);
        }
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
    }
}
