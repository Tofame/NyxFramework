using Silk.NET.OpenGL;
using StbImageSharp;

namespace NyxRender
{
    /// <summary>
    /// 2D GPU texture.
    ///
    /// Default sampling parameters are <b>nearest-neighbour</b> filtering and
    /// <b>clamp-to-edge</b> wrapping — chosen for pixel-art sprites where
    /// bilinear sampling and repeating edges would cause visible artefacts.
    ///
    /// <see cref="SetWrapRepeat"/> overrides the wrap mode to repeat (useful for
    /// scrolling shader effects like the rainbow outline).
    ///
    /// <see cref="BindFast"/> binds directly without setting the active texture unit;
    /// callers must ensure texture unit 0 is already active (used by atlas binding
    /// in the sprite renderer where the unit is constant).
    /// </summary>
    public sealed class Texture : IDisposable
    {
        private GL _gl;
        private uint _handle;
        private bool _disposed = false;

        /// <summary>The OpenGL API instance.</summary>
        public GL GL => _gl;

        /// <summary>The OpenGL texture object handle.</summary>
        public uint Handle => _handle;
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>Creates a texture from an image file (PNG, JPG, etc.) loaded via StbImage.</summary>
        public Texture(GL gl, string imagePath)
        {
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));

            ImageResult image;
            using (var stream = File.OpenRead(imagePath))
            {
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            }

            Width = image.Width;
            Height = image.Height;

            _handle = _gl.GenTexture();
            Bind();
            SetParameters();
            UploadFull(image.Data);
        }

        /// <summary>Creates a texture of the given dimensions from raw RGBA byte data.</summary>
        public Texture(GL gl, int width, int height, ReadOnlySpan<byte> data)
        {
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));
            Width = width;
            Height = height;

            _handle = _gl.GenTexture();
            Bind();
            SetParameters();
            UploadFull(data);
        }

        private void SetParameters()
        {
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        }

        private unsafe void UploadFull(ReadOnlySpan<byte> data)
        {
            fixed (byte* ptr = data)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    (int)InternalFormat.Rgba,
                    (uint)Width,
                    (uint)Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }

        /// <summary>Repeat wraps for scrolling shaders.</summary>
        public void SetWrapRepeat()
        {
            Bind();
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        /// <summary>
        /// Binds this texture to the given texture unit, activating that unit first.
        /// </summary>
        public void Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(unit);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        /// <summary>
        /// Binds this texture without changing the active texture unit.
        /// Caller must ensure the correct unit was already set (via <see cref="Bind"/>
        /// or external <c>gl.ActiveTexture</c>).
        /// </summary>
        internal void BindFast()
        {
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        /// <summary>Uploads a tight RGBA rectangle into this texture.</summary>
        public unsafe void UploadSubImage(int x, int y, int width, int height, ReadOnlySpan<byte> rgba)
        {
            if (rgba.Length < width * height * 4)
                throw new ArgumentException("RGBA span too small for the given dimensions.", nameof(rgba));
            BindFast();
            fixed (byte* ptr = rgba)
            {
                _gl.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    x,
                    y,
                    (uint)width,
                    (uint)height,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    ptr);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_handle != 0)
            {
                _gl.DeleteTexture(_handle);
                _handle = 0;
            }

            _disposed = true;
        }
    }
}
