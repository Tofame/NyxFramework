using Silk.NET.OpenGL;
using System.Numerics;

namespace NyxRender
{
    /// <summary>
    /// Batches textured quads for sprite rendering.  Uses an orthographic projection
    /// matrix (set via <see cref="Begin(Matrix4x4)"/>) and submits all pending quads
    /// to the GPU on <see cref="End"/> or when the vertex buffer is full.
    ///
    /// <see cref="Draw"/> is a general-purpose textured-quad path (any texture, any size).
    /// <see cref="DrawSprite"/> is the 32×32 sprite fast path that assumes a 32×32 quad
    /// with precomputed UV coordinates.  The atlas texture must be bound externally
    /// before calling either path.
    ///
    /// Max batch size is 10,000 sprites (40,000 vertices) — chosen to stay well within
    /// typical 16-bit index limits while accommodating a full-screen of 32×32 sprites
    /// at 1920×1080 plus headroom.
    /// </summary>
    public sealed class SpriteBatch : IDisposable
    {
        private GL _gl;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private Shader _shader = null!;
        private int _projectionLoc = -1;
        private int _textureLoc = -1;
        private bool _disposed = false;
        private bool _beginCalled = false;
        private uint _boundTextureHandle;

        // Vertex layout: vec2 position + vec2 UV + vec4 color = 8 floats (Z dropped).
        private const int VertexSize = 8 * sizeof(float);
        private const int MaxSprites = 10000;
        private const int MaxVertices = MaxSprites * 4;
        private const int MaxIndices = MaxSprites * 6;

        private float[] _vertices;
        private uint[] _indices;
        private int _vertexCount;
        private int _indexCount;

        public SpriteBatch(GL gl)
        {
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));
            _vertices = new float[MaxVertices * 8];
            _indices = new uint[MaxIndices];

            InitializeBuffers();
            InitializeShader();
            GenerateIndices();
        }

        private unsafe void InitializeBuffers()
        {
            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            _vbo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MaxVertices * VertexSize), null, BufferUsageARB.DynamicDraw);

            _ebo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(MaxIndices * sizeof(uint)), null, BufferUsageARB.StaticDraw);

            // location 0: vec2 position
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VertexSize, (void*)0);
            _gl.EnableVertexAttribArray(0);

            // location 1: vec2 UV
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexSize, (void*)(2 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);

            // location 2: vec4 color
            _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, VertexSize, (void*)(4 * sizeof(float)));
            _gl.EnableVertexAttribArray(2);

            _gl.BindVertexArray(0);
        }

        private void InitializeShader()
        {
            string vertexShaderSource = @"
                #version 330 core
                layout (location = 0) in vec2 aPosition;
                layout (location = 1) in vec2 aTexCoord;
                layout (location = 2) in vec4 aColor;

                uniform mat4 uProjection;

                out vec2 TexCoord;
                out vec4 Color;

                void main()
                {
                    gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
                    TexCoord = aTexCoord;
                    Color = aColor;
                }";

            string fragmentShaderSource = @"
                #version 330 core
                in vec2 TexCoord;
                in vec4 Color;

                out vec4 FragColor;

                uniform sampler2D uTexture;

                void main()
                {
                    FragColor = texture(uTexture, TexCoord) * Color;
                }";

            _shader = new Shader(_gl, vertexShaderSource, fragmentShaderSource);
            _projectionLoc = _gl.GetUniformLocation(_shader.Handle, "uProjection");
            _textureLoc = _gl.GetUniformLocation(_shader.Handle, "uTexture");
        }

        private unsafe void GenerateIndices()
        {
            for (int i = 0; i < MaxIndices; i += 6)
            {
                uint baseIdx = (uint)(i / 6 * 4);
                _indices[i]     = baseIdx;
                _indices[i + 1] = baseIdx + 1;
                _indices[i + 2] = baseIdx + 2;
                _indices[i + 3] = baseIdx;
                _indices[i + 4] = baseIdx + 2;
                _indices[i + 5] = baseIdx + 3;
            }

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            fixed (uint* indicesPtr = _indices)
            {
                _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, 0, (nuint)(MaxIndices * sizeof(uint)), indicesPtr);
            }
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        /// <summary>Begins batching with an identity projection (raw NDC coordinates).</summary>
        public void Begin()
        {
            Begin(Matrix4x4.Identity);
        }

        /// <summary>
        /// Begins batching with the given orthographic projection matrix.
        /// Sets uniform uTexture=0 (the atlas is bound to texture unit 0 externally).
        /// </summary>
        public void Begin(Matrix4x4 projectionMatrix)
        {
            if (_beginCalled)
                throw new InvalidOperationException("End must be called before Begin can be called again.");

            _beginCalled = true;
            _vertexCount = 0;
            _indexCount = 0;
            _boundTextureHandle = 0;

            _shader.Use();
            unsafe
            {
                _gl.UniformMatrix4(_projectionLoc, 1, false, (float*)&projectionMatrix);
            }
            _gl.Uniform1(_textureLoc, 0);
        }

        /// <summary>
        /// Draws a texture at its native size at the given position (white tint).
        /// </summary>
        public void Draw(Texture texture, Vector2 position, Color color)
        {
            Draw(texture, position, new Vector2(texture.Width, texture.Height), color, new Vector2(0, 0), new Vector2(1, 1));
        }

        /// <summary>
        /// Draws a texture at the given position with the given size (white tint, full UV range).
        /// </summary>
        public void Draw(Texture texture, Vector2 position, Vector2 size, Color color)
        {
            Draw(texture, position, size, color, new Vector2(0, 0), new Vector2(1, 1));
        }

        /// <summary>
        /// Draws a texture region at the given screen position and size.
        /// If the texture handle changes mid-batch, pending quads are auto-flushed
        /// before the new texture is bound.
        /// </summary>
        public unsafe void Draw(Texture texture, Vector2 position, Vector2 size, Color color, Vector2 uvStart, Vector2 uvEnd)
        {
            if (!_beginCalled)
                throw new InvalidOperationException("Begin must be called before Draw can be called.");

            if (_vertexCount >= MaxVertices - 4 || _indexCount >= MaxIndices - 6)
                Flush();

            // Flush pending quads before switching texture.
            if (texture.Handle != _boundTextureHandle)
            {
                if (_vertexCount > 0)
                    Flush();
                texture.Bind();
                _boundTextureHandle = texture.Handle;
            }

            float x = position.X;
            float y = position.Y;
            float width = size.X;
            float height = size.Y;

            float r = color.R / 255.0f;
            float g = color.G / 255.0f;
            float b = color.B / 255.0f;
            float a = color.A / 255.0f;

            AddVertex(x,         y,          uvStart.X, uvStart.Y, r, g, b, a);
            AddVertex(x + width, y,          uvEnd.X,   uvStart.Y, r, g, b, a);
            AddVertex(x + width, y + height, uvEnd.X,   uvEnd.Y,   r, g, b, a);
            AddVertex(x,         y + height, uvStart.X, uvEnd.Y,   r, g, b, a);

            _indexCount += 6;
        }

        /// <summary>Simple sprite draw using UV coordinates. Atlas must be bound externally.</summary>
        public void DrawSprite(float x, float y, UVRect uvRect)
        {
            DrawSprite(x, y, uvRect, Color.White);
        }

        /// <summary>Simple sprite draw with color tinting. Atlas must be bound externally.</summary>
        public void DrawSprite(float x, float y, UVRect uvRect, Color color)
        {
            if (!_beginCalled)
                throw new InvalidOperationException("Begin must be called first");

            if (_vertexCount >= MaxVertices - 4)
                Flush();

            float r, g, b, a;
            if (color == Color.White)
            {
                r = 1f; g = 1f; b = 1f; a = 1f;
            }
            else
            {
                r = color.R / 255.0f;
                g = color.G / 255.0f;
                b = color.B / 255.0f;
                a = color.A / 255.0f;
            }

            var v = _vertices;
            var idx = _vertexCount * 8;
            float x2 = x + Sprite.Size;
            float y2 = y + Sprite.Size;

            v[idx]      = x;  v[idx + 1]  = y;  v[idx + 2]  = uvRect.U1; v[idx + 3]  = uvRect.V1;
            v[idx + 4]  = r;  v[idx + 5]  = g;  v[idx + 6]  = b;        v[idx + 7]  = a;

            v[idx + 8]  = x2; v[idx + 9]  = y;  v[idx + 10] = uvRect.U2; v[idx + 11] = uvRect.V1;
            v[idx + 12] = r;  v[idx + 13] = g;  v[idx + 14] = b;        v[idx + 15] = a;

            v[idx + 16] = x2; v[idx + 17] = y2; v[idx + 18] = uvRect.U2; v[idx + 19] = uvRect.V2;
            v[idx + 20] = r;  v[idx + 21] = g;  v[idx + 22] = b;        v[idx + 23] = a;

            v[idx + 24] = x;  v[idx + 25] = y2; v[idx + 26] = uvRect.U1; v[idx + 27] = uvRect.V2;
            v[idx + 28] = r;  v[idx + 29] = g;  v[idx + 30] = b;        v[idx + 31] = a;

            _vertexCount += 4;
            _indexCount += 6;
        }

        private void AddVertex(float x, float y, float u, float v, float r, float g, float b, float a)
        {
            int idx = _vertexCount * 8;
            _vertices[idx]     = x;
            _vertices[idx + 1] = y;
            _vertices[idx + 2] = u;
            _vertices[idx + 3] = v;
            _vertices[idx + 4] = r;
            _vertices[idx + 5] = g;
            _vertices[idx + 6] = b;
            _vertices[idx + 7] = a;
            _vertexCount++;
        }

        /// <summary>Flushes all pending quads to the GPU and ends the batch.</summary>
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
            if (_vertexCount == 0) return;

            _gl.BindVertexArray(_vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            fixed (float* verticesPtr = _vertices)
            {
                _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_vertexCount * VertexSize), verticesPtr);
            }

            _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, null);

            _vertexCount = 0;
            _indexCount = 0;
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

            if (_ebo != 0)
            {
                _gl.DeleteBuffer(_ebo);
                _ebo = 0;
            }

            _disposed = true;
        }

        #endregion
    }
}
