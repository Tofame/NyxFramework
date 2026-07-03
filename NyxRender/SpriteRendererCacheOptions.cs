namespace NyxRender;

/// <summary>
/// Bounds GPU-resident sprites (atlas cache). The full <c>.spr</c> catalog stays on disk;
/// only a working set lives in VRAM and is evicted by LRU + optional idle time.
/// </summary>
public sealed class SpriteRendererCacheOptions
{
    /// <summary>Default: 8192 sprites (~2 full 2048² atlases), 8 atlases max, ~60s idle at 60 FPS.</summary>
    public static SpriteRendererCacheOptions Default { get; } = new();

    /// <summary>Same as <see cref="Default"/> — use for 60 FPS targets.</summary>
    public static SpriteRendererCacheOptions For60Fps => Default;

    /// <summary>~60 s idle retention when <see cref="SpriteRenderer.BeginFrame"/> is called 120 times per second.</summary>
    public static SpriteRendererCacheOptions For120Fps { get; } = new(maxIdleFrames: 7200);

    /// <summary>~60 s idle retention when <see cref="SpriteRenderer.BeginFrame"/> is called 180 times per second.</summary>
    public static SpriteRendererCacheOptions For180Fps { get; } = new(maxIdleFrames: 10_800);

    /// <summary>~192 MB atlas cap, 16k ids — reasonable “high VRAM” profile at 60 FPS (~60 s idle).</summary>
    public static SpriteRendererCacheOptions HighVram60Fps { get; } = new(
        maxResidentSprites: 16_384,
        maxAtlasCount: 12,
        maxIdleFrames: 3600);

    /// <summary><see cref="HighVram60Fps"/> with idle scaled for 120 FPS.</summary>
    public static SpriteRendererCacheOptions HighVram120Fps { get; } = new(
        maxResidentSprites: 16_384,
        maxAtlasCount: 12,
        maxIdleFrames: 7200);

    public SpriteRendererCacheOptions(
        int maxResidentSprites = 8192,
        int maxAtlasCount = 8,
        int maxIdleFrames = 3600)
    {
        if (maxResidentSprites < 64)
            throw new ArgumentOutOfRangeException(nameof(maxResidentSprites));
        if (maxAtlasCount < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAtlasCount));
        if (maxIdleFrames < 0)
            throw new ArgumentOutOfRangeException(nameof(maxIdleFrames));

        MaxResidentSprites = maxResidentSprites;
        MaxAtlasCount = maxAtlasCount;
        MaxIdleFrames = maxIdleFrames;
    }

    /// <summary>Maximum distinct sprite ids with GPU atlas slots (LRU evicts beyond this).</summary>
    public int MaxResidentSprites { get; }

    /// <summary>Cap on 2048×2048 atlas textures (~16 MB RGBA each).</summary>
    public int MaxAtlasCount { get; }

    /// <summary>
    /// Evict if not drawn for this many <see cref="SpriteRenderer.BeginFrame"/> calls.
    /// <c>0</c> disables time-based eviction (count cap still applies).
    /// </summary>
    public int MaxIdleFrames { get; }
}
