using Silk.NET.OpenGL;
using StbImageSharp;

namespace NyxGuiRender.Gl;

/// <summary>
/// 2D GPU texture for the NyxGUIRender batch.
///
/// Three constructors:
/// <list type="bullet">
///   <item><b>File path:</b> loads PNG/JPG via StbImage.</item>
///   <item><b>Raw RGBA:</b> uploads a <c>ReadOnlySpan&lt;byte&gt;</c> of RGBA pixels.</item>
///   <item><b>Empty:</b> allocates GPU storage without uploading data (for scratch/mutable textures).</item>
/// </list>
///
/// <see cref="UploadRgba"/> updates a mutable texture in-place (used for scratch text
/// and 32×32 sprite blits).  Throws <c>ArgumentException</c> if the buffer is too small.
/// </summary>
internal sealed class GuiTexture : IDisposable
{
    private readonly GL _gl;
    private uint _handle;

    public GuiTexture(GL gl, string imagePath, bool linearFilter)
    {
        _gl = gl;
        using var stream = File.OpenRead(imagePath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        Width = image.Width;
        Height = image.Height;
        CreateFromRgba(image.Width, image.Height, image.Data, linearFilter);
    }

    public GuiTexture(GL gl, int width, int height, ReadOnlySpan<byte> rgba, bool linearFilter)
    {
        _gl = gl;
        Width = width;
        Height = height;
        CreateFromRgba(width, height, rgba, linearFilter);
    }

    public GuiTexture(GL gl, int width, int height, bool linearFilter)
    {
        _gl = gl;
        Width = width;
        Height = height;
        CreateEmpty(width, height, linearFilter);
    }

    public int Width  { get; }
    public int Height { get; }
    public uint Handle => _handle;

    /// <summary>Binds this texture to texture unit 0.</summary>
    public void Bind()
    {
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _handle);
    }

    /// <summary>
    /// Uploads RGBA pixel data to the entire texture.  Buffer must be at least
    /// <c>Width * Height * 4</c> bytes.
    /// </summary>
    public unsafe void UploadRgba(ReadOnlySpan<byte> rgba)
    {
        if (rgba.Length < Width * Height * 4)
            throw new ArgumentException("RGBA buffer too small.", nameof(rgba));
        Bind();
        fixed (byte* ptr = rgba)
        {
            _gl.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                0,
                0,
                (uint)Width,
                (uint)Height,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ptr);
        }
    }

    public void Dispose()
    {
        if (_handle != 0)
        {
            _gl.DeleteTexture(_handle);
            _handle = 0;
        }
    }

    private unsafe void CreateFromRgba(int width, int height, ReadOnlySpan<byte> rgba, bool linearFilter)
    {
        _handle = _gl.GenTexture();
        Bind();
        var filter = linearFilter ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        fixed (byte* ptr = rgba)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                (int)InternalFormat.Rgba,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ptr);
        }
    }

    private unsafe void CreateEmpty(int width, int height, bool linearFilter)
    {
        _handle = _gl.GenTexture();
        Bind();
        var filter = linearFilter ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexImage2D(
            TextureTarget.Texture2D,
            0,
            (int)InternalFormat.Rgba,
            (uint)width,
            (uint)height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            null);
    }
}
