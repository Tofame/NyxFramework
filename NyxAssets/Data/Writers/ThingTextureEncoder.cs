using NyxAssets.Sprites;
using NyxAssets.Things;

namespace NyxAssets.Data.Writers;

/// <summary>Serializes the sprite-index block after property flags (inverse of <see cref="NyxAssets.Data.Readers.ThingTextureDecoder"/>).</summary>
internal static class ThingTextureEncoder
{
    /// <param name="includePatternZ"><see langword="true"/> for modern <c>.dat</c>; <see langword="false"/> for legacy ≤ 7.50.</param>
    public static void Write(
        LittleEndianStreamWriter w,
        ThingType thing,
        bool extendedSpriteIds,
        bool improvedAnimations,
        bool outfitFrameGroups,
        bool includePatternZ)
    {
        if (thing.FrameGroups.Count == 0)
            throw new InvalidOperationException($"Thing {thing.Id} ({thing.Kind}) has no frame groups.");

        var useOutfitGroups = outfitFrameGroups && thing.Kind == ThingKind.Outfit;
        var groupCount = useOutfitGroups ? thing.FrameGroups.Count : 1;

        if (!useOutfitGroups && thing.FrameGroups.Count != 1)
            throw new InvalidOperationException("Exactly one frame group is required unless outfit frame groups are enabled.");

        if (useOutfitGroups)
        {
            if (groupCount > 255)
                throw new InvalidOperationException("Too many outfit frame groups.");
            w.WriteU8((byte)groupCount);
        }

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
        {
            if (useOutfitGroups)
            {
                var groupId = (uint)groupIndex;
                if (groupCount < 2)
                    groupId = 1;
                w.WriteU8((byte)groupId);
            }

            var fg = thing.FrameGroups[groupIndex];
            w.WriteU8((byte)fg.Width);
            w.WriteU8((byte)fg.Height);
            if (fg.Width > 1 || fg.Height > 1)
                w.WriteU8((byte)fg.ExactSize);
            w.WriteU8((byte)fg.Layers);
            w.WriteU8((byte)fg.PatternX);
            w.WriteU8((byte)fg.PatternY);
            if (includePatternZ)
                w.WriteU8((byte)fg.PatternZ);
            w.WriteU8((byte)fg.Frames);

            if (improvedAnimations && fg.IsAnimation && fg.Frames > 1)
            {
                w.WriteU8((byte)fg.AnimationMode);
                w.WriteI32(fg.LoopCount);
                w.WriteU8(unchecked((byte)fg.StartFrame));
                if (fg.FrameTimings is null || fg.FrameTimings.Length != fg.Frames)
                    throw new InvalidOperationException("FrameTimings must match frame count when writing improved animations.");
                for (var i = 0u; i < fg.Frames; i++)
                {
                    w.WriteU32(fg.FrameTimings[i].MinimumMilliseconds);
                    w.WriteU32(fg.FrameTimings[i].MaximumMilliseconds);
                }
            }

            var total = fg.GetTotalSpriteSlots();
            if (total != fg.SpriteIds.Length)
                throw new InvalidOperationException("SpriteIds length must match layout dimensions.");

            foreach (var sid in fg.SpriteIds)
            {
                if (extendedSpriteIds)
                    w.WriteU32(sid);
                else
                {
                    if (sid > ushort.MaxValue)
                        throw new InvalidOperationException("Sprite id exceeds UInt16; use extended sprite ids.");
                    w.WriteU16((ushort)sid);
                }
            }
        }
    }
}
