using System.Numerics;
using Silk.NET.OpenGL;
using NyxRender.Shaders;

namespace NyxRender;

/// <summary>
/// Batched draw-call submission for effect shaders (outfits with palette remapping,
/// outline overlays, and custom sprite effects).
///
/// Draws are grouped by <see cref="EffectGroupKey"/> so quads sharing the same atlas,
/// shader, and outfit colours are issued in a single GPU draw call.
///
/// <b>Outline shaders are special-cased:</b> outline fragment shaders need per-quad UV
/// bounds for texel stepping, so each outline quad is flushed one-at-a-time via
/// <see cref="SetOutlineUniforms"/>.  Non-outline shaders batch all quads in their group.
/// </summary>
internal sealed class EffectDrawBatch : IDisposable
{
    private const int VertexFloats = 10;
    private const int VertexSize = VertexFloats * sizeof(float);
    private const int MaxQuads = 4096;
    private const int MaxVertices = MaxQuads * 4;
    private const int MaxIndices = MaxQuads * 6;

    private static readonly string[] PartColorNames =
        ["u_PartColors[0]", "u_PartColors[1]", "u_PartColors[2]", "u_PartColors[3]"];

    private readonly GL _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;

    // Vertex layout: pos(2) + baseUV(2) + maskUV(2) + color(4) = 10 floats.
    // maskUV is always sent even for non-outfit sprites (it defaults to baseUV).
    // The effect vertex shader outputs v_TexCoord3 = fract(aTexCoord + u_Time*offset)
    // for scrolling secondary texture sampling.
    private readonly float[] _vertices = new float[MaxVertices * VertexFloats];
    private readonly uint[] _indices = new uint[MaxIndices];

    /// <summary>Maps from group key to group for deduplication.</summary>
    private readonly Dictionary<EffectGroupKey, EffectDrawGroup> _groupMap = new();

    /// <summary>Ordered list of groups added this frame (preserves draw order).</summary>
    private readonly List<EffectDrawGroup> _activeGroups = new();

    private int _vertexCount;
    private int _indexCount;
    private bool _disposed;

    public EffectDrawBatch(GL gl)
    {
        _gl = gl;
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();
        InitLayout();
        GenerateIndices();
    }

    /// <summary>Discards all pending quads without flushing.</summary>
    public void Clear()
    {
        _activeGroups.Clear();
        _groupMap.Clear();
    }

    public void AddOutfit(
        int atlasId, float x, float y,
        UVRect baseUv, UVRect maskUv,
        Color head, Color body, Color legs, Color feet,
        string shaderName, bool paletteFromMask = true)
    {
        // Outline shaders include the baseUv in the group key so per-quad UV bounds
        // can be set individually.  Non-outline shaders omit it so all quads batch.
        var key = new EffectGroupKey(atlasId, shaderName, true, head, body, legs, feet, paletteFromMask,
            UsesOutlineShader(shaderName) ? baseUv : default);
        if (!_groupMap.TryGetValue(key, out var group))
        {
            group = new EffectDrawGroup(atlasId, shaderName, true, head, body, legs, feet, key.SpriteUv, paletteFromMask);
            _groupMap.Add(key, group);
            _activeGroups.Add(group);
        }
        else if (group.Quads.Count == 0)
            _activeGroups.Add(group);

        group.Quads.Add(new EffectQuad(x, y, baseUv, maskUv, Color.White));
    }

    public void AddSprite(int atlasId, float x, float y, UVRect uv, Color tint, string shaderName)
    {
        var key = new EffectGroupKey(atlasId, shaderName, false, Color.White, default, default, default, false,
            UsesOutlineShader(shaderName) ? uv : default);
        if (!_groupMap.TryGetValue(key, out var group))
        {
            group = new EffectDrawGroup(atlasId, shaderName, false, Color.White, Color.White, Color.White, Color.White, key.SpriteUv, false);
            _groupMap.Add(key, group);
            _activeGroups.Add(group);
        }
        else if (group.Quads.Count == 0)
            _activeGroups.Add(group);

        group.Quads.Add(new EffectQuad(x, y, uv, uv, tint));
    }

    /// <summary>Determines whether outline-specific per-quad flush is needed.</summary>
    private static bool UsesOutlineShader(string shaderName) =>
        shaderName.Contains("outline", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Submits all pending groups to the GPU.
    /// For outline shaders each quad is flushed individually (so per-quad UV bounds
    /// can be set via <see cref="SetOutlineUniforms"/>).  Non-outline groups are batched
    /// into a single draw call.
    /// </summary>
    public unsafe void Flush(Matrix4x4 projection, float timeSeconds, ShaderRegistry registry, IReadOnlyList<SpriteAtlas> atlases)
    {
        foreach (var group in _activeGroups)
        {
            if (group.Quads.Count == 0) continue;
            if (!registry.TryGet(group.ShaderName, out var shaderEntry)) continue;
            if ((uint)group.AtlasId >= (uint)atlases.Count) continue;

            atlases[group.AtlasId].Bind();
            shaderEntry.Program.Use();
            shaderEntry.Program.SetMatrix4("uProjection", projection);
            shaderEntry.Program.SetUniform("uTexture", 0);
            shaderEntry.Program.SetUniform("u_Time", timeSeconds);

            if (shaderEntry.SecondaryTexture is not null)
            {
                shaderEntry.SecondaryTexture.Bind(TextureUnit.Texture1);
                shaderEntry.Program.SetUniform("uEffectTexture", 1);
            }

            if (group.IsOutfit)
            {
                SetOutfitColors(shaderEntry.Program, group.Head, group.Body, group.Legs, group.Feet);
                shaderEntry.Program.SetUniform("u_PaletteFromMask", group.PaletteFromMask ? 1f : 0f);
            }

            if (UsesOutlineShader(group.ShaderName))
            {
                // Outline shaders need per-quad UV bounds for texel stepping.
                // Flush one quad at a time so SetOutlineUniforms can target each quad's UV rect.
                foreach (var q in group.Quads)
                {
                    _vertexCount = 0;
                    _indexCount = 0;
                    AddQuadInternal(q);
                    if (_vertexCount == 0) continue;
                    SetOutlineUniforms(shaderEntry.Program, q.BaseUv);
                    DrawBuffered();
                }
            }
            else
            {
                _vertexCount = 0;
                _indexCount = 0;
                foreach (var q in group.Quads)
                    AddQuadInternal(q);

                if (_vertexCount > 0)
                    DrawBuffered();
            }

            group.Quads.Clear();
        }

        _activeGroups.Clear();
    }

    /// <summary>Adds one quad to the vertex buffer.  Silently drops if the buffer is full.</summary>
    private void AddQuadInternal(EffectQuad q)
    {
        if (_vertexCount >= MaxVertices - 4)
            return;

        float r = q.Tint.R / 255f;
        float g = q.Tint.G / 255f;
        float b = q.Tint.B / 255f;
        float a = q.Tint.A / 255f;

        var v = _vertices;
        var i = _vertexCount * VertexFloats;
        float x2 = q.X + Sprite.Size;
        float y2 = q.Y + Sprite.Size;

        v[i]      = q.X; v[i + 1]  = q.Y; v[i + 2]  = q.BaseUv.U1; v[i + 3]  = q.BaseUv.V1;
        v[i + 4]  = q.MaskUv.U1; v[i + 5]  = q.MaskUv.V1; v[i + 6]  = r; v[i + 7]  = g; v[i + 8]  = b; v[i + 9]  = a;

        i += VertexFloats;
        v[i]      = x2;  v[i + 1]  = q.Y; v[i + 2]  = q.BaseUv.U2; v[i + 3]  = q.BaseUv.V1;
        v[i + 4]  = q.MaskUv.U2; v[i + 5]  = q.MaskUv.V1; v[i + 6]  = r; v[i + 7]  = g; v[i + 8]  = b; v[i + 9]  = a;

        i += VertexFloats;
        v[i]      = x2;  v[i + 1]  = y2;  v[i + 2]  = q.BaseUv.U2; v[i + 3]  = q.BaseUv.V2;
        v[i + 4]  = q.MaskUv.U2; v[i + 5]  = q.MaskUv.V2; v[i + 6]  = r; v[i + 7]  = g; v[i + 8]  = b; v[i + 9]  = a;

        i += VertexFloats;
        v[i]      = q.X; v[i + 1]  = y2;  v[i + 2]  = q.BaseUv.U1; v[i + 3]  = q.BaseUv.V2;
        v[i + 4]  = q.MaskUv.U1; v[i + 5]  = q.MaskUv.V2; v[i + 6]  = r; v[i + 7]  = g; v[i + 8]  = b; v[i + 9]  = a;

        _vertexCount += 4;
        _indexCount += 6;
    }

    private void AddVert(float x, float y, float u, float v, float mu, float mv, float r, float g, float b, float a)
    {
        var i = _vertexCount * VertexFloats;
        _vertices[i]      = x;
        _vertices[i + 1]  = y;
        _vertices[i + 2]  = u;
        _vertices[i + 3]  = v;
        _vertices[i + 4]  = mu;
        _vertices[i + 5]  = mv;
        _vertices[i + 6]  = r;
        _vertices[i + 7]  = g;
        _vertices[i + 8]  = b;
        _vertices[i + 9]  = a;
        _vertexCount++;
    }

    private static void SetOutfitColors(Shader program, Color head, Color body, Color legs, Color feet)
    {
        SetPart(program, 0, head);
        SetPart(program, 1, body);
        SetPart(program, 2, legs);
        SetPart(program, 3, feet);
    }

    private static void SetPart(Shader program, int index, Color c) =>
        program.SetUniform(PartColorNames[index], new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f));

    private unsafe void DrawBuffered()
    {
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* ptr = _vertices)
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(_vertexCount * VertexSize), ptr);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, null);
    }

    /// <summary>
    /// Sets per-quad outline shader uniforms: texel step (du, dv) derived from the
    /// quad's UV rectangle, plus UV min/max so the outline shader can sample a
    /// neighbourhood of pixels around each fragment without leaking past sprite edges.
    /// </summary>
    private static void SetOutlineUniforms(Shader program, UVRect qb)
    {
        var du = (qb.U2 - qb.U1) / Sprite.Size;
        var dv = (qb.V2 - qb.V1) / Sprite.Size;
        program.SetUniform("u_TexStep", new Vector2(du, dv));
        program.SetUniform("u_UvMin",   new Vector2(qb.U1, qb.V1));
        program.SetUniform("u_UvMax",   new Vector2(qb.U2, qb.V2));
    }

    private unsafe void InitLayout()
    {
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(MaxVertices * VertexSize), null, BufferUsageARB.DynamicDraw);

        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, VertexSize, (void*)0);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexSize, (void*)(2 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, VertexSize, (void*)(4 * sizeof(float)));
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, VertexSize, (void*)(6 * sizeof(float)));
        _gl.EnableVertexAttribArray(3);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(MaxIndices * sizeof(uint)), null, BufferUsageARB.StaticDraw);
        _gl.BindVertexArray(0);
    }

    private unsafe void GenerateIndices()
    {
        for (var i = 0; i < MaxIndices; i += 6)
        {
            var baseIdx = (uint)(i / 6 * 4);
            _indices[i]     = baseIdx;
            _indices[i + 1] = baseIdx + 1;
            _indices[i + 2] = baseIdx + 2;
            _indices[i + 3] = baseIdx;
            _indices[i + 4] = baseIdx + 2;
            _indices[i + 5] = baseIdx + 3;
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        fixed (uint* ptr = _indices)
            _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, 0, (nuint)(MaxIndices * sizeof(uint)), ptr);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _disposed = true;
    }

    /// <summary>
    /// Identifies a unique draw group.  Outfit groups differ by head/body/legs/feet colour;
    /// sprite groups by tint colour.  Outline shaders additionally include the sprite UV
    /// so per-quad bounds can be issued.
    /// </summary>
    private readonly struct EffectGroupKey : IEquatable<EffectGroupKey>
    {
        public readonly int AtlasId;
        public readonly string ShaderName;
        public readonly bool IsOutfit;
        public readonly Color Head, Body, Legs, Feet;
        public readonly bool PaletteFromMask;
        public readonly UVRect SpriteUv;

        public EffectGroupKey(int atlasId, string shaderName, bool isOutfit,
            Color head, Color body, Color legs, Color feet, bool paletteFromMask, UVRect spriteUv)
        {
            AtlasId = atlasId;
            ShaderName = shaderName;
            IsOutfit = isOutfit;
            Head = head;
            Body = body;
            Legs = legs;
            Feet = feet;
            PaletteFromMask = paletteFromMask;
            SpriteUv = spriteUv;
        }

        public bool Equals(EffectGroupKey other) =>
            AtlasId == other.AtlasId &&
            ShaderName == other.ShaderName &&
            IsOutfit == other.IsOutfit &&
            Head.R == other.Head.R && Head.G == other.Head.G && Head.B == other.Head.B && Head.A == other.Head.A &&
            Body.R == other.Body.R && Body.G == other.Body.G && Body.B == other.Body.B && Body.A == other.Body.A &&
            Legs.R == other.Legs.R && Legs.G == other.Legs.G && Legs.B == other.Legs.B && Legs.A == other.Legs.A &&
            Feet.R == other.Feet.R && Feet.G == other.Feet.G && Feet.B == other.Feet.B && Feet.A == other.Feet.A &&
            PaletteFromMask == other.PaletteFromMask &&
            SpriteUv.U1 == other.SpriteUv.U1 && SpriteUv.V1 == other.SpriteUv.V1 &&
            SpriteUv.U2 == other.SpriteUv.U2 && SpriteUv.V2 == other.SpriteUv.V2;

        public override bool Equals(object? obj) => obj is EffectGroupKey key && Equals(key);

        public override int GetHashCode()
        {
            var headPacked = (Head.R << 24) | (Head.G << 16) | (Head.B << 8) | Head.A;
            var bodyPacked = (Body.R << 24) | (Body.G << 16) | (Body.B << 8) | Body.A;
            var legsPacked = (Legs.R << 24) | (Legs.G << 16) | (Legs.B << 8) | Legs.A;
            var feetPacked = (Feet.R << 24) | (Feet.G << 16) | (Feet.B << 8) | Feet.A;
            var h = HashCode.Combine(AtlasId, ShaderName, IsOutfit, PaletteFromMask);
            h = HashCode.Combine(h, headPacked, bodyPacked, legsPacked, feetPacked);
            return h;
        }
    }

    private sealed class EffectDrawGroup
    {
        public int AtlasId;
        public string ShaderName;
        public bool IsOutfit;
        public bool PaletteFromMask;
        public Color Head, Body, Legs, Feet;
        public UVRect BaseUvBounds;
        public List<EffectQuad> Quads = new();

        public EffectDrawGroup(int atlasId, string shaderName, bool isOutfit,
            Color head, Color body, Color legs, Color feet, UVRect baseUvBounds, bool paletteFromMask)
        {
            AtlasId = atlasId;
            ShaderName = shaderName;
            IsOutfit = isOutfit;
            Head = head;
            Body = body;
            Legs = legs;
            Feet = feet;
            BaseUvBounds = baseUvBounds;
            PaletteFromMask = paletteFromMask;
        }
    }

    private struct EffectQuad(float x, float y, UVRect baseUv, UVRect maskUv, Color tint)
    {
        public float X = x;
        public float Y = y;
        public UVRect BaseUv = baseUv;
        public UVRect MaskUv = maskUv;
        public Color Tint = tint;
    }
}
