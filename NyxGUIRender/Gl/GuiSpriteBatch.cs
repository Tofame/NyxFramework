using System.Numerics;
using Silk.NET.OpenGL;

namespace NyxGuiRender.Gl;

/// <summary>
/// Accumulates textured quads and flushes per texture (one GPU draw per texture).
///
/// <b>Invariant:</b> <see cref="Begin"/> must be called before <see cref="Draw"/>,
/// and <see cref="End"/> must be called to flush the final batch.  <c>Draw</c> throws
/// <c>InvalidOperationException</c> if <c>Begin</c> was not called.
///
/// <b>Auto-flush:</b> <c>Draw</c> automatically flushes when the bound texture changes
/// or when the 4096-quad capacity is reached.
/// </summary>
internal sealed class GuiSpriteBatch : IDisposable
{
    // Vertex layout: vec2 position + vec2 UV + vec4 color = 8 floats (Z dropped).
    private const int VertexFloats = 8;
    private const int MaxQuads = 4096;
    private const int MaxVertices = MaxQuads * 4;
    private const int MaxIndices = MaxQuads * 6;

    private readonly GL _gl;
    private readonly GuiShader _shader;
    private readonly float[] _vertices = new float[MaxVertices * VertexFloats];
    private readonly uint[] _indices = new uint[MaxIndices];
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;

    private Matrix4x4 _projection;
    private bool _active;
    private int _vertexCount;
    private int _indexCount;
    private GuiTexture? _boundTexture;
    private uint _boundTextureHandle;

    public GuiSpriteBatch(GL gl, GuiShader shader)
    {
        _gl = gl;
        _shader = shader;
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        InitIndices();
        InitBuffers();
    }

    /// <summary>Number of GPU draw calls issued in the current batch session.</summary>
    public int ActualDrawCount { get; private set; }

    /// <summary>
    /// Begins a new batching session.  Sets the projection matrix once (not repeated per flush).
    /// </summary>
    public void Begin(Matrix4x4 projection)
    {
        _projection = projection;
        _active = true;
        _vertexCount = 0;
        _indexCount = 0;
        _boundTexture = null;
        _boundTextureHandle = 0;
        ActualDrawCount = 0;

        // Set projection once per frame — not repeated in every Flush.
        _shader.Use();
        _shader.SetProjection(projection);
        _shader.SetTextureUnit(0);
    }

    /// <summary>
    /// Draws a textured quad.  Auto-flushes if the texture changes or capacity is reached.
    /// </summary>
    public void Draw(
        GuiTexture texture,
        Vector2 position,
        Vector2 size,
        GuiColor color,
        Vector2 uvStart,
        Vector2 uvEnd)
    {
        if (!_active)
            throw new InvalidOperationException("GuiSpriteBatch.Begin was not called.");

        if (_boundTexture is not null && !ReferenceEquals(_boundTexture, texture))
            Flush();

        if (_vertexCount > MaxVertices - 4)
            Flush();

        _boundTexture = texture;

        // Bind only if the texture actually changed.
        if (texture.Handle != _boundTextureHandle)
        {
            texture.Bind();
            _boundTextureHandle = texture.Handle;
        }

        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var a = color.A / 255f;
        var x = position.X;
        var y = position.Y;
        var w = size.X;
        var h = size.Y;

        AddVertex(x,     y,     uvStart.X, uvStart.Y, r, g, b, a);
        AddVertex(x + w, y,     uvEnd.X,   uvStart.Y, r, g, b, a);
        AddVertex(x + w, y + h, uvEnd.X,   uvEnd.Y,   r, g, b, a);
        AddVertex(x,     y + h, uvStart.X, uvEnd.Y,   r, g, b, a);
        _indexCount += 6;
    }

    /// <summary>Flushes the final batch and ends the batching session.</summary>
    public void End()
    {
        if (!_active)
            return;
        Flush();
        _active = false;
        _boundTexture = null;
        _boundTextureHandle = 0;
    }

    /// <summary>Uploads pending vertices when texture, clip region, or scratch content is about to change.</summary>
    public void FlushPending() => Flush();

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }

    private unsafe void Flush()
    {
        if (_vertexCount == 0 || _boundTexture is null)
        {
            _vertexCount = 0;
            _indexCount = 0;
            return;
        }

        // Shader.Use + projection already set in Begin; skip redundant calls here.
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* ptr = _vertices)
        {
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_vertexCount * VertexFloats * sizeof(float)), ptr);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, null);
        _gl.BindVertexArray(0);

        ActualDrawCount++;
        _vertexCount = 0;
        _indexCount = 0;
    }

    private void AddVertex(float x, float y, float u, float v, float r, float g, float b, float a)
    {
        var i = _vertexCount * VertexFloats;
        ref var ptr = ref _vertices[i];
        ptr = x;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 1) = y;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 2) = u;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 3) = v;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 4) = r;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 5) = g;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 6) = b;
        System.Runtime.CompilerServices.Unsafe.Add(ref ptr, 7) = a;
        _vertexCount++;
    }

    private unsafe void InitBuffers()
    {
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(
            BufferTargetARB.ArrayBuffer,
            (nuint)(_vertices.Length * sizeof(float)),
            null,
            BufferUsageARB.DynamicDraw);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData(
            BufferTargetARB.ElementArrayBuffer,
            (nuint)(_indices.Length * sizeof(uint)),
            _indices,
            BufferUsageARB.StaticDraw);

        var stride = VertexFloats * sizeof(float);
        // location 0: vec2 position
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
        _gl.EnableVertexAttribArray(0);
        // location 1: vec2 UV
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)stride, (void*)(2 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        // location 2: vec4 color
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)stride, (void*)(4 * sizeof(float)));
        _gl.EnableVertexAttribArray(2);
        _gl.BindVertexArray(0);
    }

    private void InitIndices()
    {
        for (var i = 0; i < MaxQuads; i++)
        {
            var v = (uint)(i * 4);
            var idx = i * 6;
            _indices[idx]     = v;
            _indices[idx + 1] = v + 1;
            _indices[idx + 2] = v + 2;
            _indices[idx + 3] = v;
            _indices[idx + 4] = v + 2;
            _indices[idx + 5] = v + 3;
        }
    }
}
