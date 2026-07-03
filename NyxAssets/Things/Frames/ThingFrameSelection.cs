namespace NyxAssets.Things.Frames;

/// <summary>
/// Resolved slice into a <see cref="ThingFrameGroup"/> — pattern axes + animation frame ready for
/// <see cref="ThingFrameGroup.TryGetSpriteId"/> or export/decode.
/// </summary>
public readonly struct ThingFrameSelection
{
    public required ThingFrameGroup FrameGroup { get; init; }
    public required int FrameGroupIndex { get; init; }
    public uint PatternX { get; init; }
    public uint PatternY { get; init; }
    public uint PatternZ { get; init; }
    public uint Frame { get; init; }

    /// <summary>One drawable sprite cell within the resolved selection.</summary>
    public readonly struct SpriteSlot
    {
        public uint InnerWidth { get; init; }
        public uint InnerHeight { get; init; }
        public uint Layer { get; init; }
        public uint SpriteId { get; init; }
    }

    public IEnumerable<SpriteSlot> EnumerateSpriteSlots()
    {
        var fg = FrameGroup;
        for (var cellY = 0u; cellY < fg.Height; cellY++)
        {
            for (var cellX = 0u; cellX < fg.Width; cellX++)
            {
                for (var layer = 0u; layer < fg.Layers; layer++)
                {
                    if (!fg.TryGetSpriteId(cellX, cellY, layer, PatternX, PatternY, PatternZ, Frame, out var spriteId) || spriteId == 0)
                        continue;

                    yield return new SpriteSlot
                    {
                        InnerWidth = cellX,
                        InnerHeight = cellY,
                        Layer = layer,
                        SpriteId = spriteId,
                    };
                }
            }
        }
    }

    public uint[] GetSpriteIds() =>
        EnumerateSpriteSlots().Select(s => s.SpriteId).ToArray();
}
