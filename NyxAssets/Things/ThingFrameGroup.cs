using System.Collections.Generic;

namespace NyxAssets.Things;

/// <summary>One frame-group: dimensions, patterns, animation, and sprite id list (Asset Editor <c>FrameGroup</c>).</summary>
public sealed class ThingFrameGroup
{
    public uint GroupTypeId { get; set; }
    public uint Width { get; set; } = 1;
    public uint Height { get; set; } = 1;
    public uint ExactSize { get; set; } = 32;
    public uint Layers { get; set; } = 1;
    public uint PatternX { get; set; } = 1;
    public uint PatternY { get; set; } = 1;
    public uint PatternZ { get; set; } = 1;
    public uint Frames { get; set; } = 1;
    public bool IsAnimation { get; set; }
    public uint AnimationMode { get; set; }
    public int LoopCount { get; set; }
    public int StartFrame { get; set; }
    public AnimationFrameTiming[]? FrameTimings { get; set; }
    public uint[] SpriteIds { get; set; } = Array.Empty<uint>();

    public uint GetTotalSpriteSlots() =>
        Width * Height * PatternX * PatternY * PatternZ * Frames * Layers;

    /// <summary>Asset Editor <c>FrameGroup.getTotalX</c>: horizontal texture slots in the editor sprite sheet.</summary>
    public uint GetSpriteSheetTextureColumns() => PatternZ * PatternX * Layers;

    /// <summary>Asset Editor <c>FrameGroup.getTotalY</c>: vertical texture slot rows in the editor sprite sheet.</summary>
    public uint GetSpriteSheetTextureRows() => Frames * PatternY;

    /// <summary>Asset Editor <c>FrameGroup.getTotalTextures</c> — count of pattern cells (one per texture slot).</summary>
    public uint GetSpriteSheetTextureCount() => PatternX * PatternY * PatternZ * Frames * Layers;

    /// <summary>
    /// Flat index into <see cref="SpriteIds"/> for one inner tile cell, layer, pattern axes, and animation frame.
    /// Same formula as Asset Editor <c>FrameGroup.getSpriteIndex</c> (width/height here are <b>indices</b> inside the thing tile, not pixel sizes).
    /// </summary>
    /// <param name="innerWidth">0 … <see cref="Width"/> − 1</param>
    /// <param name="innerHeight">0 … <see cref="Height"/> − 1</param>
    /// <param name="layer">0 … <see cref="Layers"/> − 1</param>
    /// <param name="patternX">First pattern axis (outfits often use this for facing / direction when <see cref="PatternX"/> is 4).</param>
    /// <param name="patternY">Second pattern axis.</param>
    /// <param name="patternZ">Third pattern axis (addons / mounts when used).</param>
    /// <param name="frame">Animation frame; combined with <see cref="Frames"/> as <c>frame % Frames</c> when <see cref="Frames"/> is not 0.</param>
    public uint GetSpriteIndex(uint innerWidth, uint innerHeight, uint layer, uint patternX, uint patternY, uint patternZ, uint frame)
    {
        var f = Frames != 0 ? frame % Frames : 0u;
        var i = f * PatternZ + patternZ;
        i = i * PatternY + patternY;
        i = i * PatternX + patternX;
        i = i * Layers + layer;
        i = i * Height + innerHeight;
        i = i * Width + innerWidth;
        return i;
    }

    /// <summary>Same as <see cref="GetSpriteIndex(uint, uint, uint, uint, uint, uint, uint)"/> for a single inner cell (<c>Width</c>×<c>Height</c> = 1×1).</summary>
    public uint GetSpriteIndex(uint layer, uint patternX, uint patternY, uint patternZ, uint frame) =>
        GetSpriteIndex(0, 0, layer, patternX, patternY, patternZ, frame);

    /// <summary>
    /// Texture-slot index (no inner width/height walk). Matches Asset Editor <c>FrameGroup.getTextureIndex</c> — used when compositing one pattern cell.
    /// </summary>
    public uint GetTextureIndex(uint layer, uint patternX, uint patternY, uint patternZ, uint frame)
    {
        var f = Frames != 0 ? frame % Frames : 0u;
        var i = f * PatternZ + patternZ;
        i = i * PatternY + patternY;
        i = i * PatternX + patternX;
        i = i * Layers + layer;
        return i;
    }

    /// <summary>Resolves <see cref="SpriteIds"/>[<see cref="GetSpriteIndex(uint, uint, uint, uint, uint, uint, uint)"/>].</summary>
    public uint GetSpriteId(uint innerWidth, uint innerHeight, uint layer, uint patternX, uint patternY, uint patternZ, uint frame)
    {
        var i = GetSpriteIndex(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame);
        if (i >= SpriteIds.Length)
            throw new ArgumentOutOfRangeException(nameof(frame), $"Computed sprite slot {i} is outside SpriteIds.Length ({SpriteIds.Length}).");
        return SpriteIds[i];
    }

    /// <inheritdoc cref="GetSpriteId(uint, uint, uint, uint, uint, uint, uint)"/>
    public uint GetSpriteId(uint layer, uint patternX, uint patternY, uint patternZ, uint frame) =>
        GetSpriteId(0, 0, layer, patternX, patternY, patternZ, frame);

    /// <summary>Non-throwing variant of <see cref="GetSpriteId(uint, uint, uint, uint, uint, uint, uint)"/>.</summary>
    public bool TryGetSpriteId(uint innerWidth, uint innerHeight, uint layer, uint patternX, uint patternY, uint patternZ, uint frame, out uint spriteId)
    {
        var i = GetSpriteIndex(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame);
        if (i >= SpriteIds.Length)
        {
            spriteId = 0;
            return false;
        }

        spriteId = SpriteIds[i];
        return true;
    }

    /// <inheritdoc cref="TryGetSpriteId(uint, uint, uint, uint, uint, uint, uint, out uint)"/>
    public bool TryGetSpriteId(uint layer, uint patternX, uint patternY, uint patternZ, uint frame, out uint spriteId) =>
        TryGetSpriteId(0, 0, layer, patternX, patternY, patternZ, frame, out spriteId);

    /// <summary>
    /// Enumerates <see cref="SpriteIds"/> for every combination of dimensions you leave open.
    /// Pass <see langword="null"/> for a dimension to iterate all valid indices (<c>0 … count−1</c>); pass a value to fix that axis (same rule as Asset Editor UI: <c>value % count</c> when <c>count &gt; 0</c>).
    /// </summary>
    /// <remarks>
    /// Matches the nested loops in Asset Editor <c>ThingTypeEditor</c> (e.g. <c>setTextureForAllFramesAndDirectionsHandler</c>): vary every axis you do not pin down.
    /// </remarks>
    public IEnumerable<uint> EnumerateSpriteIds(
        uint? innerWidth = null,
        uint? innerHeight = null,
        uint? layer = null,
        uint? patternX = null,
        uint? patternY = null,
        uint? patternZ = null,
        uint? frame = null)
    {
        foreach (var l in Each(Layers, layer))
        {
            foreach (var px in Each(PatternX, patternX))
            {
                foreach (var py in Each(PatternY, patternY))
                {
                    foreach (var pz in Each(PatternZ, patternZ))
                    {
                        foreach (var fr in Each(Frames, frame))
                        {
                            foreach (var h in Each(Height, innerHeight))
                            {
                                foreach (var w in Each(Width, innerWidth))
                                {
                                    var idx = GetSpriteIndex(w, h, l, px, py, pz, fr);
                                    if (idx < SpriteIds.Length)
                                        yield return SpriteIds[idx];
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>Materializes <see cref="EnumerateSpriteIds"/> without intermediate list allocations.</summary>
    public uint[] GetSpriteIds(
        uint? innerWidth = null,
        uint? innerHeight = null,
        uint? layer = null,
        uint? patternX = null,
        uint? patternY = null,
        uint? patternZ = null,
        uint? frame = null)
    {
        var count = CountSpriteIds(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame);
        if (count == 0)
            return Array.Empty<uint>();

        var ids = new uint[count];
        var index = 0;
        foreach (var id in EnumerateSpriteIds(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame))
            ids[index++] = id;
        return ids;
    }

    private int CountSpriteIds(
        uint? innerWidth,
        uint? innerHeight,
        uint? layer,
        uint? patternX,
        uint? patternY,
        uint? patternZ,
        uint? frame)
    {
        var count = 0;
        foreach (var _ in EnumerateSpriteIds(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame))
            count++;
        return count;
    }

    private static IEnumerable<uint> Each(uint count, uint? only)
    {
        if (count == 0)
            yield break;
        if (only.HasValue)
        {
            yield return only.Value % count;
            yield break;
        }

        for (var i = 0u; i < count; i++)
            yield return i;
    }
}
