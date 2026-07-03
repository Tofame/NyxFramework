using NyxAssets.Sprites;
using NyxAssets.Things;

namespace NyxAssets.Data.Readers;

/// <summary>Reads the sprite-index block after property flags for one <see cref="ThingType"/>.</summary>
internal static class ThingTextureDecoder
{
    private const uint MaxSpriteIndicesPerThing = 4096;

    /// <param name="includePatternZ"><see langword="true"/> for modern <c>.dat</c> (reads PatternZ); <see langword="false"/> for legacy ≤ 7.50.</param>
    public static void Read(
        ref LittleEndianSpanReader reader,
        ThingType thing,
        bool extendedSpriteIds,
        bool improvedAnimations,
        bool outfitFrameGroups,
        uint defaultFrameDurationMs,
        bool includePatternZ)
    {
        var groupCount = 1u;
        if (outfitFrameGroups && thing.Kind == ThingKind.Outfit)
            groupCount = reader.ReadU8();

        for (var groupIndex = 0u; groupIndex < groupCount; groupIndex++)
        {
            if (outfitFrameGroups && thing.Kind == ThingKind.Outfit)
                reader.ReadU8();

            var fg = new ThingFrameGroup { GroupTypeId = groupIndex };
            fg.Width = reader.ReadU8();
            fg.Height = reader.ReadU8();
            fg.ExactSize = fg.Width > 1 || fg.Height > 1 ? reader.ReadU8() : (uint)SpritePixelCodec.SpriteEdgeLength;
            fg.Layers = reader.ReadU8();
            fg.PatternX = reader.ReadU8();
            fg.PatternY = reader.ReadU8();
            fg.PatternZ = includePatternZ ? reader.ReadU8() : 1u;
            fg.Frames = reader.ReadU8();

            if (fg.Frames > 1)
            {
                fg.IsAnimation = true;
                fg.FrameTimings = new AnimationFrameTiming[fg.Frames];
                if (improvedAnimations)
                {
                    fg.AnimationMode = reader.ReadU8();
                    fg.LoopCount = reader.ReadI32();
                    fg.StartFrame = reader.ReadS8();
                    for (var i = 0u; i < fg.Frames; i++)
                    {
                        var min = reader.ReadU32();
                        var max = reader.ReadU32();
                        fg.FrameTimings[i] = new AnimationFrameTiming(min, max);
                    }
                }
                else
                {
                    for (var i = 0u; i < fg.Frames; i++)
                        fg.FrameTimings[i] = new AnimationFrameTiming(defaultFrameDurationMs, defaultFrameDurationMs);
                }
            }

            var totalSprites = fg.GetTotalSpriteSlots();
            if (totalSprites > MaxSpriteIndicesPerThing)
                throw new InvalidDataException("Thing type exceeds maximum sprite index count.");

            fg.SpriteIds = new uint[totalSprites];
            for (var i = 0u; i < totalSprites; i++)
                fg.SpriteIds[i] = extendedSpriteIds ? reader.ReadU32() : reader.ReadU16();

            thing.FrameGroups.Add(fg);
        }
    }
}
