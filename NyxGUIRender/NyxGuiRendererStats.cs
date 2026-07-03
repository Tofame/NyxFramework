namespace NyxGuiRender;

/// <summary>Last-frame metrics from <see cref="NyxGuiRenderer"/>.</summary>
public readonly struct NyxGuiRendererStats
{
    /// <summary>Total quads submitted in the last frame.</summary>
    public int LastFrameQuads { get; init; }

    /// <summary>Number of GPU draw-call flushes in the last frame.</summary>
    public int LastFrameGlDraws { get; init; }

    /// <summary>Images currently cached (loaded from disk and kept in memory).</summary>
    public int CachedTextures { get; init; }

    /// <summary>Total glyphs rasterized across all font caches (process-wide).</summary>
    public int CachedTextGlyphs { get; init; }
}
