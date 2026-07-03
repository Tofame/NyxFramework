using Silk.NET.OpenGL;
using NyxRender.Shaders;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace NyxRender;

/// <summary>
/// Batches 32×32 sprites by atlas. Draw paths take <see cref="Sprite"/> (id + optional RGBA) or
/// <see cref="TryDraw(int,ReadOnlySpan{byte},float,float,Color)"/> so callers (e.g. NyxAssets) supply pixels; ids only
/// identify atlas slots for caching.
///
/// <b>Batching model:</b> between <see cref="BeginFrame"/> and <see cref="EndFrame"/>, draw calls
/// are collected into a run list.  Sprite draws and effect draws form separate contiguous runs
/// within the list, preserving submission order.  <see cref="EndFrame"/> flushes all runs:
/// sprite runs first (via <see cref="SpriteBatch"/>), then effect runs (via <see cref="EffectDrawBatch"/>).
/// Effect draws therefore render on top of sprite draws submitted before them in the same frame.
/// </summary>
public sealed class SpriteRenderer : IDisposable
{
    private const int AtlasSize = 2048;
    private static readonly float InvAtlasSize = 1f / AtlasSize;

    private readonly GL _gl;
    private readonly SpriteBatch _spriteBatch;
    private readonly EffectDrawBatch _effectBatch;
    private readonly SpriteRendererCacheOptions _cache;
    private readonly SpriteResidentLru _lru = new();
    private readonly List<SpriteAtlas> _atlases = new();
    private readonly Dictionary<int, SpriteLocation> _sprites = new();
    private readonly List<SpriteDrawOp> _spriteOps = new();
    private readonly List<EffectDrawOp> _effectOps = new();
    private readonly List<DrawRun> _runs = new();
	private bool[] _isSpriteResident = Array.Empty<bool>();
    private int _nonFullAtlasIndex = -1;
    private Matrix4x4 _projectionMatrix;
    private bool _disposed;
    private bool _inFrame;
    private float _frameTimeSeconds;
    private uint _frameIndex;
    private long _evictedTotal;

    /// <summary>Host registers outfit/sprite effect programs on <see cref="ShaderRegistry"/> before using shader names in draws.</summary>
    public ShaderRegistry Shaders { get; }

    /// <summary>Resident sprite cache uses <see cref="SpriteRendererCacheOptions.Default"/>.</summary>
    public SpriteRenderer(GL gl, int viewportWidth, int viewportHeight)
        : this(gl, viewportWidth, viewportHeight, null)
    {
    }

    public SpriteRenderer(GL gl, int viewportWidth, int viewportHeight, SpriteRendererCacheOptions? cache)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _cache = cache ?? SpriteRendererCacheOptions.Default;
        _spriteBatch = new SpriteBatch(gl);
        _effectBatch = new EffectDrawBatch(gl);
        Shaders = new ShaderRegistry(gl);
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, viewportWidth, viewportHeight, 0, -1, 1);
		_spriteOps.EnsureCapacity(16384);
		_effectOps.EnsureCapacity(16384);
		_runs.EnsureCapacity(4096);
    }

    /// <summary>LRU / idle eviction policy for the GPU atlas cache.</summary>
    public SpriteRendererCacheOptions CacheOptions => _cache;

    /// <summary>Load sprite from RGB (opaque alpha 255).</summary>
    public void LoadSprite(int spriteId, byte[] rgbData)
    {
        ArgumentNullException.ThrowIfNull(rgbData);
        var n = Sprite.Size * Sprite.Size * 3;
        if (rgbData.Length != n)
            throw new ArgumentException($"RGB data must be {n} bytes.", nameof(rgbData));

        Span<byte> rgba = stackalloc byte[Sprite.Size * Sprite.Size * 4];
        CopyRgbToRgba(rgbData, rgba);
        RegisterSpriteRgba(spriteId, rgba);
    }

    /// <summary>32×32 RGBA, <c>R,G,B,A</c> per pixel (OpenGL upload order).</summary>
    public void LoadSpriteRgba(int spriteId, ReadOnlySpan<byte> rgbaData)
    {
        if (rgbaData.Length != Sprite.Size * Sprite.Size * 4)
            throw new ArgumentException($"RGBA data must be {Sprite.Size * Sprite.Size * 4} bytes.", nameof(rgbaData));
        RegisterSpriteRgba(spriteId, rgbaData, preferredAtlasId: null);
    }

    /// <summary>
    /// Loads two sprites (e.g. outfit base + mask) into the same atlas so
    /// <see cref="TryDrawOutfitLayers"/> can use the GPU path.
    /// If one sprite is already resident, the other is loaded into that atlas.
    /// </summary>
    public void LoadSpritePair(int spriteIdA, ReadOnlySpan<byte> rgbaA, int spriteIdB, ReadOnlySpan<byte> rgbaB)
    {
        var aResident = _sprites.TryGetValue(spriteIdA, out var locA);
        var bResident = _sprites.TryGetValue(spriteIdB, out var locB);
        int? preferredAtlas = null;

        if (aResident && bResident && locA.AtlasId == locB.AtlasId)
            return;

        if (aResident)
            preferredAtlas = locA.AtlasId;
        else if (bResident)
            preferredAtlas = locB.AtlasId;

        if (aResident && bResident && locA.AtlasId != locB.AtlasId)
        {
            EvictSprite(spriteIdB);
            bResident = false;
        }

        if (!aResident && rgbaA.Length == Sprite.Size * Sprite.Size * 4)
        {
            RegisterSpriteRgba(spriteIdA, rgbaA, preferredAtlas);
            bResident = spriteIdA == spriteIdB;
        }
        if (!bResident && rgbaB.Length == Sprite.Size * Sprite.Size * 4)
            RegisterSpriteRgba(spriteIdB, rgbaB, preferredAtlas);
    }

    /// <summary>Consecutive <paramref name="spriteCount"/> RGB sprites packed in <paramref name="binaryData"/>.</summary>
    public void LoadSprites(int startSpriteId, byte[] binaryData, int spriteCount)
    {
        ArgumentNullException.ThrowIfNull(binaryData);
        var bpp = Sprite.Size * Sprite.Size * 3;
        if (binaryData.Length < bpp * spriteCount)
            throw new ArgumentException("Binary data too small for spriteCount.", nameof(binaryData));

        var rgba = new byte[Sprite.Size * Sprite.Size * 4];
        for (var i = 0; i < spriteCount; i++)
        {
            CopyRgbToRgba(binaryData.AsSpan(i * bpp, bpp), rgba);
            RegisterSpriteRgba(startSpriteId + i, rgba);
        }
    }

    /// <summary>Begins a frame with zero elapsed time (no animated shader effects).</summary>
    public void BeginFrame()
    {
        BeginFrame(0f);
    }

    /// <summary>
    /// Begins a new frame.  All draws between this and <see cref="EndFrame"/> are batched.
    /// </summary>
    /// <param name="timeSeconds">Elapsed time for animated shaders (rainbow scroll, etc.).</param>
    public void BeginFrame(float timeSeconds)
    {
        if (_inFrame)
            throw new InvalidOperationException("Already in frame.");
        _inFrame = true;
        _frameTimeSeconds = timeSeconds;
        _frameIndex++;
        _spriteOps.Clear();
        _effectOps.Clear();
        _runs.Clear();
        _effectBatch.Clear();
    }

    /// <summary>
    /// Draws a sprite at screen position (x, y).  If the sprite has pixels but is not
    /// yet resident, it is uploaded to a GPU atlas first.  Throws if the sprite cannot
    /// be made resident (no pixels and not preloaded).
    /// </summary>
    public void Draw(in Sprite sprite, float x, float y) =>
        Draw(in sprite, x, y, Color.White);

    /// <inheritdoc cref="Draw(in Sprite, float, float)"/>
    public void Draw(in Sprite sprite, float x, float y, Color tint)
    {
        if (!_inFrame)
            throw new InvalidOperationException("Must call BeginFrame first.");
        EnsureSprite(in sprite);
        ref var loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, sprite.Id);
        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
            throw new ArgumentException($"Sprite {sprite.Id} could not be resolved (atlas full or invalid).");
        TouchAndEnqueue(sprite.Id, ref loc, x, y, tint);
    }

    /// <summary>
    /// Draws a sprite, returning false instead of throwing if the sprite is not resident
    /// and has no pixel payload to upload.
    /// </summary>
    public bool TryDraw(in Sprite sprite, float x, float y) =>
        TryDraw(in sprite, x, y, Color.White);

    /// <inheritdoc cref="TryDraw(in Sprite, float, float)"/>
    public bool TryDraw(in Sprite sprite, float x, float y, Color tint)
    {
        if (!_inFrame)
            throw new InvalidOperationException("Must call BeginFrame first.");
        if (!TryEnsureSprite(in sprite))
            return false;
        ref var loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, sprite.Id);
        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
            return false;
        TouchAndEnqueue(sprite.Id, ref loc, x, y, tint);
        return true;
    }

    /// <summary>
    /// Uploads from <paramref name="rgba4096"/> the first time <paramref name="spriteId"/> is seen; afterwards
    /// ignores the span (same as <see cref="Sprite.Resident"/> draws).
    /// </summary>
    public bool TryDraw(int spriteId, ReadOnlySpan<byte> rgba4096, float x, float y) =>
        TryDraw(spriteId, rgba4096, x, y, Color.White);

    /// <inheritdoc cref="TryDraw(int, ReadOnlySpan{byte}, float, float)"/>
    public bool TryDraw(int spriteId, ReadOnlySpan<byte> rgba4096, float x, float y, Color tint)
    {
        if (!_inFrame)
            throw new InvalidOperationException("Must call BeginFrame first.");
        ref var loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, spriteId);
        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
        {
            if (rgba4096.Length != Sprite.Rgba32Length || !TryRegisterSpriteRgba(spriteId, rgba4096))
                return false;
            loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, spriteId);
            if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
                return false;
        }

        TouchAndEnqueue(spriteId, ref loc, x, y, tint);
        return true;
    }

    /// <summary>Each id must already be resident (e.g. after <see cref="LoadSprites"/>).</summary>
    public void DrawComposite(ReadOnlySpan<int> preloadedSpriteIds, float x, float y)
    {
        foreach (var id in preloadedSpriteIds)
        {
            var s = Sprite.Resident(id);
            Draw(in s, x, y);
        }
    }

    public void DrawComposite(IEnumerable<int> preloadedSpriteIds, float x, float y)
    {
        foreach (var id in preloadedSpriteIds)
        {
            var s = Sprite.Resident(id);
            Draw(in s, x, y);
        }
    }

    /// <summary>
    /// GPU outfit layers (base + mask template). Requires both sprites resident in the same atlas.
    /// <paramref name="shaderName"/> — e.g. <c>outfit_default</c>, <c>outfit_gold</c>, <c>outline_outfit_yellow</c>.
    /// </summary>
    public bool TryDrawOutfitLayers(
        int baseSpriteId,
        int maskSpriteId,
        float x,
        float y,
        Color head,
        Color body,
        Color legs,
        Color feet,
        string? shaderName = null,
        bool paletteFromMask = true)
    {
        if (!_inFrame)
            throw new InvalidOperationException("Must call BeginFrame first.");
        ref var baseLoc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, baseSpriteId);
        ref var maskLoc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, maskSpriteId);
        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref baseLoc) || System.Runtime.CompilerServices.Unsafe.IsNullRef(ref maskLoc))
            return false;
        var shader = string.IsNullOrEmpty(shaderName) ? "outfit_default" : shaderName;
        if (!Shaders.TryGet(shader, out _))
            return false;

        if (baseLoc.AtlasId != maskLoc.AtlasId)
            return false;

        EnqueueOutfitEffect(baseLoc.AtlasId, x, y, baseLoc.UVRect, maskLoc.UVRect, head, body, legs, feet, shader, paletteFromMask);
        Touch(baseSpriteId, ref baseLoc);
        Touch(maskSpriteId, ref maskLoc);
        return true;
    }

    /// <summary>Single-texture draw with a registered sprite shader (e.g. <c>outline_yellow</c>).</summary>
    public bool TryDrawSpriteEffect(int spriteId, ReadOnlySpan<byte> rgba4096, float x, float y, string shaderName)
    {
        if (!_inFrame)
            throw new InvalidOperationException("Must call BeginFrame first.");
        ref var loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, spriteId);
        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
        {
            if (rgba4096.Length != Sprite.Rgba32Length || !TryRegisterSpriteRgba(spriteId, rgba4096))
                return false;
            loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, spriteId);
            if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
                return false;
        }

        return TryDrawSpriteEffect(spriteId, ref loc, x, y, shaderName, Color.White);
    }

    public bool TryDrawSpriteEffect(in Sprite sprite, float x, float y, string shaderName) =>
        TryDrawSpriteEffect(in sprite, x, y, shaderName, Color.White);

    public bool TryDrawSpriteEffect(in Sprite sprite, float x, float y, string shaderName, Color tint)
    {
        if (!_inFrame)
            throw new InvalidOperationException("Must call BeginFrame first.");
        if (!TryEnsureSprite(in sprite))
            return false;
        ref var loc = ref CollectionsMarshal.GetValueRefOrNullRef(_sprites, sprite.Id);
        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref loc))
            return false;

        return TryDrawSpriteEffect(sprite.Id, ref loc, x, y, shaderName, tint);
    }

    private bool TryDrawSpriteEffect(int spriteId, ref SpriteLocation loc, float x, float y, string shaderName, Color tint)
    {
        EnqueueSpriteEffect(loc.AtlasId, x, y, loc.UVRect, tint, shaderName);
        Touch(spriteId, ref loc);
        return true;
    }

    /// <summary>
    /// Flushes all pending draw runs to the GPU and runs scheduled eviction.
    /// Runs are flushed in order: sprite runs first, then effect runs.
    /// Effect draws submitted after a sprite draw in the same frame will render on top.
    /// </summary>
    public void EndFrame()
    {
        if (!_inFrame)
            throw new InvalidOperationException("Not in frame.");

        var si = 0;
        var ei = 0;
        foreach (var run in CollectionsMarshal.AsSpan(_runs))
        {
            if (run.IsEffect)
                FlushEffectRun(ei, run.Count, ref ei);
            else
                FlushSpriteRun(si, run.Count, ref si);
        }

        RunScheduledEviction();
        _inFrame = false;
    }

    /// <summary>Updates the orthographic projection for viewport size changes (e.g. window resize).</summary>
    public void UpdateViewport(int width, int height) =>
        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1);

    /// <summary>True if this id already has atlas UVs (after a successful draw or <c>Load*</c>).</summary>
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	public bool IsSpriteResident(int spriteId)
	{
		var arr = _isSpriteResident;
		return (uint)spriteId < (uint)arr.Length && arr[spriteId];
	}

    /// <summary>Returns resident-sprite and atlas statistics for diagnostics.</summary>
    public SpriteRendererStats GetStats()
    {
        var usedSlots = 0;
        for (var i = 0; i < _atlases.Count; i++)
            usedSlots += _atlases[i].UsedSlots;

        var vramBytes = (long)_atlases.Count * AtlasSize * AtlasSize * 4;
        return new SpriteRendererStats
        {
            LoadedSprites = _sprites.Count,
            AtlasCount = _atlases.Count,
            AtlasSlotsUsed = usedSlots,
            AtlasSlotCapacity = _atlases.Count > 0 ? _atlases[0].Capacity * _atlases.Count : 0,
            MemoryUsageMB = vramBytes / (1024f * 1024f),
            EvictedTotal = _evictedTotal,
            FrameIndex = _frameIndex,
        };
    }

    /// <summary>Force-evict one LRU sprite (e.g. before loading a large known set).</summary>
    public bool TryEvictOne() => TryEvictSpriteId(out _);

    /// <summary>Evict until resident count is at or below <paramref name="maxResident"/>.</summary>
    public int EvictDownTo(int maxResident)
    {
        var evicted = 0;
        while (_sprites.Count > maxResident && TryEvictSpriteId(out _))
            evicted++;
        return evicted;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _spriteBatch.Dispose();
        _effectBatch.Dispose();
        Shaders.Dispose();
        foreach (var atlas in _atlases)
            atlas.Dispose();
        _disposed = true;
    }

    private void EnsureSprite(in Sprite sprite)
    {
        if (IsSpriteResident(sprite.Id))
            return;
        if (!sprite.HasPixels)
        {
            throw new ArgumentException(
                $"Sprite {sprite.Id} is not resident. Pass {Sprite.Rgba32Length} RGBA bytes in {nameof(Sprite)}, use {nameof(TryDraw)}(id, span, …), or call {nameof(LoadSpriteRgba)} first.");
        }

        RegisterSpriteRgba(sprite.Id, sprite.Rgba.Span);
    }

    private bool TryEnsureSprite(in Sprite sprite)
    {
        if (IsSpriteResident(sprite.Id))
            return true;
        if (!sprite.HasPixels)
            return false;
        return TryRegisterSpriteRgba(sprite.Id, sprite.Rgba.Span);
    }

    private void RegisterSpriteRgba(int spriteId, ReadOnlySpan<byte> rgba, int? preferredAtlasId = null)
    {
        if (IsSpriteResident(spriteId))
            throw new ArgumentException($"Sprite {spriteId} already loaded.");

        if (!TryRegisterSpriteRgba(spriteId, rgba, preferredAtlasId))
            throw new InvalidOperationException(
                $"Could not upload sprite {spriteId}: atlas cache full ({_sprites.Count} resident, {_atlases.Count} atlases).");
    }

    /// <summary>
    /// Tries up to 64 attempts to place a sprite into a GPU atlas slot.
    ///
    /// On each attempt:
    /// 1. <see cref="MakeRoomForNewSprite"/> evicts LRU sprites if the resident count exceeds the cache cap.
    /// 2. A target atlas is chosen: the caller's preferred atlas (if it has room), or <see cref="FindAtlasWithSpace"/>.
    /// 3. If no atlas has room and we haven't hit <see cref="SpriteRendererCacheOptions.MaxAtlasCount"/>, a new atlas is created.
    /// 4. If atlas count is at max, an LRU sprite is evicted to free a slot.
    /// 5. The sprite is uploaded to the atlas slot.  If the upload throws (atlas was full despite checks),
    ///    another LRU eviction occurs and the loop retries.
    ///
    /// This handles a race between `IsFull` checks and actual uploads when multiple
    /// sprites are being loaded in rapid succession.
    /// </summary>
    private bool TryRegisterSpriteRgba(int spriteId, ReadOnlySpan<byte> rgba, int? preferredAtlasId = null)
    {
        if (IsSpriteResident(spriteId))
            return true;

        const int maxAttempts = 64;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            MakeRoomForNewSprite();

            var atlas = preferredAtlasId.HasValue && preferredAtlasId.Value < _atlases.Count && !_atlases[preferredAtlasId.Value].IsFull
                ? _atlases[preferredAtlasId.Value]
                : FindAtlasWithSpace();
            if (atlas is null)
            {
                if (_atlases.Count < _cache.MaxAtlasCount)
                    atlas = CreateNewAtlas();
                else if (!TryEvictSpriteId(out _))
                    return false;
                else
                    continue;
            }

            try
            {
                var gridPos = atlas.AddSprite(rgba);
                var loc = new SpriteLocation
                {
                    AtlasId = atlas.Id,
                    GridPosition = gridPos,
                    UVRect = UvForGridCell(gridPos),
                    LastUsedFrame = _frameIndex,
                };
                _sprites[spriteId] = loc;
                _lru.Add(spriteId);
				if (spriteId >= _isSpriteResident.Length)
					Array.Resize(ref _isSpriteResident, Math.Max(spriteId + 1, _isSpriteResident.Length * 2));
				_isSpriteResident[spriteId] = true;
                return true;
            }
            catch (InvalidOperationException)
            {
                if (!TryEvictSpriteId(out _))
                    return false;
            }
        }

        return false;
    }

    private void MakeRoomForNewSprite()
    {
        while (_sprites.Count >= _cache.MaxResidentSprites && TryEvictSpriteId(out _))
        {
        }
    }

    /// <summary>Evicts sprites exceeding the count cap or idle for longer than MaxIdleFrames.</summary>
    private void RunScheduledEviction()
    {
        if (_cache.MaxIdleFrames > 0)
            EvictIdleSprites();

        while (_sprites.Count > _cache.MaxResidentSprites && TryEvictSpriteId(out _))
        {
        }
    }

    private void EvictIdleSprites()
    {
        var threshold = _frameIndex > (uint)_cache.MaxIdleFrames
            ? _frameIndex - (uint)_cache.MaxIdleFrames
            : 0u;

        while (_lru.TryPeekOldest(out var oldestId) &&
               _sprites.TryGetValue(oldestId, out var loc) &&
               loc.LastUsedFrame < threshold)
        {
            if (!TryEvictSpriteId(out _))
                break;
        }
    }

    private bool TryEvictSpriteId(out int evictedId)
    {
        if (!_lru.TryEvictOldest(out evictedId))
            return false;

        if (!_sprites.Remove(evictedId, out var loc))
            return false;
		if ((uint)evictedId < (uint)_isSpriteResident.Length)
			_isSpriteResident[evictedId] = false;

        if ((uint)loc.AtlasId < (uint)_atlases.Count)
            _atlases[loc.AtlasId].FreeSlot(loc.GridPosition);

        _evictedTotal++;
        return true;
    }

    private void EvictSprite(int spriteId)
    {
        if (!_sprites.Remove(spriteId, out var loc))
            return;
        if ((uint)loc.AtlasId < (uint)_atlases.Count)
            _atlases[loc.AtlasId].FreeSlot(loc.GridPosition);
        _lru.Remove(spriteId);
    }

    /// <summary>
    /// Updates the sprite's <c>LastUsedFrame</c> directly via the reference.
    /// </summary>
    private void Touch(int spriteId, ref SpriteLocation sprite)
    {
        if (sprite.LastUsedFrame == _frameIndex)
            return;

        sprite.LastUsedFrame = _frameIndex;
        _lru.Touch(spriteId);
    }

    /// <summary>
    /// Records a sprite draw op and appends/extends the last sprite run.
    /// If the last run was an effect run, a new sprite run is started so sprite/effect
    /// ops are never interleaved within a single run.
    /// </summary>
    private void TouchAndEnqueue(int spriteId, ref SpriteLocation sprite, float x, float y, Color tint)
    {
        Touch(spriteId, ref sprite);
        _spriteOps.Add(new SpriteDrawOp(sprite.AtlasId, x, y, sprite.UVRect, tint));
        var runs = CollectionsMarshal.AsSpan(_runs);
        if (runs.Length == 0 || runs[^1].IsEffect)
            _runs.Add(new DrawRun { StartOffset = _spriteOps.Count - 1, Count = 1, IsEffect = false });
        else
            runs[^1].Count++;
    }

    private void EnqueueOutfitEffect(
        int atlasId,
        float x,
        float y,
        UVRect baseUv,
        UVRect maskUv,
        Color head,
        Color body,
        Color legs,
        Color feet,
        string shaderName,
        bool paletteFromMask)
    {
        _effectOps.Add(new EffectDrawOp(atlasId, x, y, baseUv, maskUv, Color.White, head, body, legs, feet, shaderName, paletteFromMask, true));
        TrackEffectOp();
    }

    private void EnqueueSpriteEffect(int atlasId, float x, float y, UVRect uv, Color tint, string shaderName)
    {
        _effectOps.Add(new EffectDrawOp(atlasId, x, y, uv, default, tint, default, default, default, default, shaderName, false, false));
        TrackEffectOp();
    }

    /// <summary>
    /// Appends/extends the last effect run, or starts a new one if the last run was a sprite run.
    /// </summary>
    private void TrackEffectOp()
    {
        var runs = CollectionsMarshal.AsSpan(_runs);
        if (runs.Length == 0 || !runs[^1].IsEffect)
            _runs.Add(new DrawRun { StartOffset = _effectOps.Count - 1, Count = 1, IsEffect = true });
        else
            runs[^1].Count++;
    }

    /// <summary>
    /// Flushes a contiguous run of sprite draw ops.  Binds the atlas and calls
    /// <see cref="SpriteBatch.Begin"/> once; if the atlas changes mid-run (edge case
    /// from a merged run), the batch is ended and re-begun with the new atlas.
    /// </summary>
    private void FlushSpriteRun(int start, int count, ref int index)
    {
        if (count <= 0) return;
        var end = start + count;
        var ops = CollectionsMarshal.AsSpan(_spriteOps);
        var atlasId = ops[start].AtlasId;
        _atlases[atlasId].Bind();
        _spriteBatch.Begin(_projectionMatrix);

        for (var i = start; i < end; i++)
        {
            ref var op = ref ops[i];
            if (op.AtlasId != atlasId)
            {
                _spriteBatch.End();
                atlasId = op.AtlasId;
                _atlases[atlasId].Bind();
                _spriteBatch.Begin(_projectionMatrix);
            }

            _spriteBatch.DrawSprite(op.X, op.Y, op.UvRect, op.Color);
        }

        _spriteBatch.End();
        index = end;
    }

    /// <summary>
    /// Flushes a contiguous run of effect draw ops through <see cref="EffectDrawBatch"/>.
    /// All ops are first collected into the batch, then flushed once via
    /// <see cref="EffectDrawBatch.Flush"/> which handles per-group draw calls.
    /// </summary>
    private void FlushEffectRun(int start, int count, ref int index)
    {
        if (count <= 0) return;
        var end = start + count;
        _effectBatch.Clear();
        for (var i = start; i < end; i++)
        {
            var op = _effectOps[i];
            if (op.HasMaskUv)
            {
                _effectBatch.AddOutfit(
                    op.AtlasId, op.X, op.Y, op.UvRect, op.MaskUvRect,
                    op.Head, op.Body, op.Legs, op.Feet, op.ShaderName, op.PaletteFromMask);
            }
            else
            {
                _effectBatch.AddSprite(op.AtlasId, op.X, op.Y, op.UvRect, op.Color, op.ShaderName);
            }
        }

        _effectBatch.Flush(_projectionMatrix, _frameTimeSeconds, Shaders, _atlases);
        index = end;
    }

    /// <summary>
    /// Scans atlases for one with free slots, using a cached index to avoid O(n) on every call.
    /// The cache is invalidated when the preferred atlas fills up, triggering a full scan.
    /// </summary>
    private SpriteAtlas? FindAtlasWithSpace()
    {
        if (_nonFullAtlasIndex >= 0 && _nonFullAtlasIndex < _atlases.Count && !_atlases[_nonFullAtlasIndex].IsFull)
            return _atlases[_nonFullAtlasIndex];

        for (var i = 0; i < _atlases.Count; i++)
        {
            if (!_atlases[i].IsFull)
            {
                _nonFullAtlasIndex = i;
                return _atlases[i];
            }
        }

        _nonFullAtlasIndex = -1;
        return null;
    }

    private SpriteAtlas CreateNewAtlas()
    {
        var atlas = new SpriteAtlas(_gl, _atlases.Count, AtlasSize);
        _nonFullAtlasIndex = _atlases.Count;
        _atlases.Add(atlas);
        return atlas;
    }

    private static void CopyRgbToRgba(ReadOnlySpan<byte> rgb, Span<byte> rgba)
    {
        var px = Sprite.Size * Sprite.Size;
        var i = 0;

        if (Sse2.IsSupported)
        {
            // Process 4 pixels per iteration: 12 RGB bytes → 16 RGBA bytes
            var alphaMask = Vector128.Create((byte)255);
            for (; i <= px - 4; i += 4)
            {
                var k = i * 3;
                var o = i * 4;
                // Load 12 bytes (3 pixels × 4 bytes, but we only use 12)
                var r0 = rgb[k];     var g0 = rgb[k + 1];  var b0 = rgb[k + 2];
                var r1 = rgb[k + 3]; var g1 = rgb[k + 4];  var b1 = rgb[k + 5];
                var r2 = rgb[k + 6]; var g2 = rgb[k + 7];  var b2 = rgb[k + 8];
                var r3 = rgb[k + 9]; var g3 = rgb[k + 10]; var b3 = rgb[k + 11];
                rgba[o]      = r0; rgba[o + 1]  = g0; rgba[o + 2]  = b0; rgba[o + 3]  = 255;
                rgba[o + 4]  = r1; rgba[o + 5]  = g1; rgba[o + 6]  = b1; rgba[o + 7]  = 255;
                rgba[o + 8]  = r2; rgba[o + 9]  = g2; rgba[o + 10] = b2; rgba[o + 11] = 255;
                rgba[o + 12] = r3; rgba[o + 13] = g3; rgba[o + 14] = b3; rgba[o + 15] = 255;
            }
        }

        // Scalar tail
        for (; i < px; i++)
        {
            var o = i * 4;
            var k = i * 3;
            rgba[o] = rgb[k];
            rgba[o + 1] = rgb[k + 1];
            rgba[o + 2] = rgb[k + 2];
            rgba[o + 3] = 255;
        }
    }

    /// <summary>
    /// Computes UV coordinates for a grid cell in the atlas.
    /// Each cell is inset by 0.05 pixels on all sides to prevent texture bleeding
    /// from adjacent sprites under camera scaling or sub-pixel positioning.
    /// The epsilon is 0.05/2048 ≈ 0.0024% of the texture — small enough to be invisible,
    /// large enough to avoid sampling neighbouring sprite pixels at typical zoom levels.
    /// </summary>
    private static UVRect UvForGridCell(Point gridPos)
    {
        const float eps = 0.05f;
        var x0 = (gridPos.X * Sprite.Size + eps) * InvAtlasSize;
        var y0 = (gridPos.Y * Sprite.Size + eps) * InvAtlasSize;
        var x1 = ((gridPos.X + 1) * Sprite.Size - eps) * InvAtlasSize;
        var y1 = ((gridPos.Y + 1) * Sprite.Size - eps) * InvAtlasSize;
        return new UVRect(x0, y0, x1, y1);
    }

    internal struct SpriteLocation
    {
        public int AtlasId;
        public Point GridPosition;
        public UVRect UVRect;
        public uint LastUsedFrame;
    }

    private struct SpriteDrawOp(int atlasId, float x, float y, UVRect uvRect, Color color)
    {
        public int AtlasId = atlasId;
        public float X = x;
        public float Y = y;
        public UVRect UvRect = uvRect;
        public Color Color = color;
    }

    private struct EffectDrawOp
    {
        public int AtlasId;
        public float X, Y;
        public UVRect UvRect;
        public UVRect MaskUvRect;
        public Color Color;
        public Color Head, Body, Legs, Feet;
        public string ShaderName;
        public bool PaletteFromMask;
        public bool HasMaskUv;

        public EffectDrawOp(int atlasId, float x, float y, UVRect uvRect, UVRect maskUvRect,
            Color color, Color head, Color body, Color legs, Color feet, string shaderName, bool paletteFromMask, bool hasMaskUv)
        {
            AtlasId = atlasId;
            X = x; Y = y;
            UvRect = uvRect; MaskUvRect = maskUvRect;
            Color = color;
            Head = head; Body = body; Legs = legs; Feet = feet;
            ShaderName = shaderName;
            PaletteFromMask = paletteFromMask;
            HasMaskUv = hasMaskUv;
        }
    }

    /// <summary>Describes a contiguous range of ops (sprite or effect) in the run list.</summary>
    private struct DrawRun
    {
        public int StartOffset;
        public int Count;
        public bool IsEffect;
    }
}

public readonly struct UVRect(float u1, float v1, float u2, float v2)
{
    public float U1 { get; } = u1;
    public float V1 { get; } = v1;
    public float U2 { get; } = u2;
    public float V2 { get; } = v2;
}

public readonly record struct Point(int X, int Y);

public readonly struct SpriteRendererStats
{
    public int LoadedSprites { get; init; }
    public int AtlasCount { get; init; }
    public int AtlasSlotsUsed { get; init; }
    public int AtlasSlotCapacity { get; init; }
    public float MemoryUsageMB { get; init; }
    public long EvictedTotal { get; init; }
    public uint FrameIndex { get; init; }
}
