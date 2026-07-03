using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

namespace NyxRender
{
    /// <summary>
    /// Owns the OpenGL context and a Silk.NET window.  Configures global GL state
    /// for 2D sprite rendering: premultiplied alpha blending, no depth test (draw order
    /// is submission order), and cull-face disabled (sprites are 2D quads with no
    /// winding guarantee).
    /// </summary>
    public sealed class GraphicsDevice : IDisposable
    {
        private GL _gl = null!;
        private IWindow _window;
        private bool _disposed = false;

        /// <summary>The OpenGL API instance for this context.</summary>
        public GL GL => _gl;

        /// <summary>The Silk.NET window associated with this device.</summary>
        public IWindow Window => _window;

        public GraphicsDevice(IWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _window.Load += () =>
            {
                _gl = _window.CreateOpenGL();
                InitializeOpenGL();
            };
        }

        /// <summary>
        /// Sets global GL state for 2D rendering.
        /// Blending uses <c>SrcAlpha</c> / <c>OneMinusSrcAlpha</c> (standard premultiplied alpha).
        /// Depth test is disabled — z-order is determined by submission order.
        /// Cull face is disabled — sprites are 2D geometry with no consistent winding.
        /// </summary>
        private void InitializeOpenGL()
        {
            // Set the clear color
            _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            // Enable blending for transparency
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Enable 2D textures
            _gl.Enable(EnableCap.Texture2D);

            // Disable depth testing for 2D rendering
            _gl.Disable(EnableCap.DepthTest);

            // Disable back-face culling for 2D sprites
            _gl.Disable(EnableCap.CullFace);

            // Set viewport
            var size = _window.Size;
            _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        }

        /// <summary>Clears the framebuffer to the given colour.</summary>
        public void Clear(Color color)
        {
            _gl.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
        }

        /// <summary>Swaps the back buffer to the front (double-buffered present).</summary>
        public void Present()
        {
            _window.SwapBuffers();
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed)
                return;

            _gl?.Dispose();
            _disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// 8-bit RGBA colour with standard named presets.
    /// </summary>
    public readonly struct Color : IEquatable<Color>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public bool Equals(Color other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object? obj) => obj is Color c && Equals(c);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
        public static bool operator ==(Color a, Color b) => a.Equals(b);
        public static bool operator !=(Color a, Color b) => !a.Equals(b);

        public static readonly Color White = new Color(255, 255, 255, 255);
        public static readonly Color Black = new Color(0, 0, 0, 255);
        public static readonly Color Red = new Color(255, 0, 0, 255);
        public static readonly Color Green = new Color(0, 255, 0, 255);
        public static readonly Color Blue = new Color(0, 0, 255, 255);
        public static readonly Color Transparent = new Color(0, 0, 0, 0);
    }
}
