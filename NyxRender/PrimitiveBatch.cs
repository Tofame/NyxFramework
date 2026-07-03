using Silk.NET.OpenGL;
using System.Numerics;

namespace NyxRender
{
    /// <summary>
    /// Batched line and rectangle drawing.
    ///
    /// Unlike <see cref="SpriteBatch"/> which uses a projection matrix, this class
    /// manually converts screen-space coordinates to NDC [-1, 1] via viewport
    /// half-width/height division.  Set the viewport with <see cref="SetViewport"/>
    /// before calling <see cref="Begin"/>.
    ///
    /// Primitive vertex shader has no projection uniform; positions are passed as-is
    /// to the GPU in NDC space.  This keeps the shader trivial (passthrough) and
    /// avoids matrix uniform overhead for simple debug overlays.
    ///
    /// Lines are always 1-pixel wide (the <c>thickness</c> parameter on
    /// <see cref="DrawLine"/> is accepted but currently ignored).
    /// </summary>
    public sealed class PrimitiveBatch : IDisposable
    {
        private GL _gl;
        private uint _vao;
        private uint _vbo;
        private Shader _shader = null!;
        private bool _disposed = false;
        private bool _beginCalled = false;
        private int _viewportW;
        private int _viewportH;

        // Vertex structure: Position (2 floats) + Color (4 floats)
        private const int VertexSize = 6 * sizeof(float);
        private const int MaxVertices = 10000;

        private float[] _vertices;
        private int _vertexCount;

        public PrimitiveBatch(GL gl)
        {
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));
            _vertices = new float[MaxVertices * 6];

            InitializeBuffers();
            InitializeShader();
        }

        /// <summary>Sets the viewport size for NDC coordinate conversion.</summary>
        public void SetViewport(int width, int height)
        {
            _viewportW = width;
            _viewportH = height;
        }

        private unsafe void InitializeBuffers()
        {
            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            _vbo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MaxVertices * VertexSize), null, BufferUsageARB.DynamicDraw);

            // Position: 2 floats
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VertexSize, (void*)0);
            _gl.EnableVertexAttribArray(0);

            // Color: 4 floats
            _gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, VertexSize, (void*)(2 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);

            _gl.BindVertexArray(0);
        }

        /// <summary>
        /// Passthrough vertex shader — positions are expected in NDC space [-1, 1].
        /// No projection matrix is applied.
        /// </summary>
        private void InitializeShader()
        {
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec2 aPosition;
                layout (location = 1) in vec4 aColor;

                out vec4 Color;

                void main()
                {
                    gl_Position = vec4(aPosition, 0.0, 1.0);
                    Color = aColor;
                }";

            string fragmentShaderSource = @"
                #version 330 core
                in vec4 Color;

                out vec4 FragColor;

                void main()
                {
                    FragColor = Color;
                }";

            _shader = new Shader(_gl, vertexShaderSource, fragmentShaderSource);
        }

        /// <summary>Begins batching.  Viewport must be set first via <see cref="SetViewport"/>.</summary>
        public void Begin()
        {
            if (_beginCalled)
                throw new InvalidOperationException("End must be called before Begin can be called again.");

            _beginCalled = true;
            _vertexCount = 0;
            _shader.Use();
        }

        /// <summary>
        /// Draws a 1-pixel-wide line between two screen-space points.
        /// The <paramref name="thickness"/> parameter is accepted for API symmetry but
        /// currently ignored — all lines are 1 pixel wide.
        /// </summary>
        public unsafe void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1.0f)
        {
            if (!_beginCalled)
                throw new InvalidOperationException("Begin must be called before DrawLine can be called.");

            if (_vertexCount >= MaxVertices - 2)
                Flush();

            float r = color.R / 255.0f;
            float g = color.G / 255.0f;
            float b = color.B / 255.0f;
            float a = color.A / 255.0f;

            // Convert screen-space → NDC: map [0, viewport] to [-1, 1] with Y flipped.
            var hw = _viewportW > 0 ? _viewportW / 2.0f : 1f;
            var hh = _viewportH > 0 ? _viewportH / 2.0f : 1f;

            float startX = start.X / hw - 1.0f;
            float startY = 1.0f - start.Y / hh;
            float endX   = end.X   / hw - 1.0f;
            float endY   = 1.0f - end.Y   / hh;

            AddVertex(startX, startY, r, g, b, a);
            AddVertex(endX,   endY,   r, g, b, a);
        }

        /// <summary>Draws a hollow rectangle outline.</summary>
        public void DrawRectangle(Vector2 position, Vector2 size, Color color)
        {
            if (!_beginCalled)
                throw new InvalidOperationException("Begin must be called before DrawRectangle can be called.");

            var tl = position;
            var tr = new Vector2(position.X + size.X, position.Y);
            var br = new Vector2(position.X + size.X, position.Y + size.Y);
            var bl = new Vector2(position.X, position.Y + size.Y);

            DrawLine(tl, tr, color);
            DrawLine(tr, br, color);
            DrawLine(br, bl, color);
            DrawLine(bl, tl, color);
        }

        /// <summary>Draws a filled rectangle using two CCW triangles.</summary>
        public unsafe void FillRectangle(Vector2 position, Vector2 size, Color color)
        {
            if (!_beginCalled)
                throw new InvalidOperationException("Begin must be called before FillRectangle can be called.");

            if (_vertexCount >= MaxVertices - 6)
                Flush();

            float r = color.R / 255.0f;
            float g = color.G / 255.0f;
            float b = color.B / 255.0f;
            float a = color.A / 255.0f;

            var hw = _viewportW > 0 ? _viewportW / 2.0f : 1f;
            var hh = _viewportH > 0 ? _viewportH / 2.0f : 1f;

            float x = position.X / hw - 1.0f;
            float y = 1.0f - position.Y / hh;
            float w = size.X / hw;
            float h = size.Y / hh;

            // Two CCW triangles
            AddVertex(x,     y,     r, g, b, a);
            AddVertex(x + w, y,     r, g, b, a);
            AddVertex(x,     y - h, r, g, b, a);

            AddVertex(x + w, y,     r, g, b, a);
            AddVertex(x + w, y - h, r, g, b, a);
            AddVertex(x,     y - h, r, g, b, a);
        }

        private void AddVertex(float x, float y, float r, float g, float b, float a)
        {
            int i = _vertexCount * 6;
            _vertices[i]     = x;
            _vertices[i + 1] = y;
            _vertices[i + 2] = r;
            _vertices[i + 3] = g;
            _vertices[i + 4] = b;
            _vertices[i + 5] = a;
            _vertexCount++;
        }

        /// <summary>Flushes all pending primitives to the GPU and ends the batch.</summary>
        public void End()
        {
            if (!_beginCalled)
                throw new InvalidOperationException("Begin must be called before End can be called.");

            if (_vertexCount > 0)
                Flush();

            _beginCalled = false;
        }

        private unsafe void Flush()
        {
            _gl.BindVertexArray(_vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            fixed (float* verticesPtr = _vertices)
            {
                _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_vertexCount * VertexSize), verticesPtr);
            }

            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)_vertexCount);

            _vertexCount = 0;
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed)
                return;

            _shader?.Dispose();

            if (_vao != 0)
            {
                _gl.DeleteVertexArray(_vao);
                _vao = 0;
            }

            if (_vbo != 0)
            {
                _gl.DeleteBuffer(_vbo);
                _vbo = 0;
            }

            _disposed = true;
        }

        #endregion
    }
}
