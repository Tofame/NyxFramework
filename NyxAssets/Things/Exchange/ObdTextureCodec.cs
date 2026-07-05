using NyxAssets.Data.Readers;
using NyxAssets.Data.Writers;
using NyxAssets.Sprites;
using NyxAssets.Things;

namespace NyxAssets.Things.Exchange;

internal static class ObdTextureCodec
{
    private const uint MaxSpriteIndicesPerThing = 4096;

    public static void ReadV2V3(
        ref LittleEndianSpanReader reader,
        ThingType thing,
        ushort obdVersion,
        bool transparentSprites,
        uint defaultFrameDurationMs,
        Dictionary<uint, byte[]> spritesRgba)
    {
        var groupCount = 1u;
        if (thing.Kind == ThingKind.Outfit && obdVersion >= ObdVersions.Version3)
            groupCount = reader.ReadU8();

        for (var groupIndex = 0u; groupIndex < groupCount; groupIndex++)
        {
            if (thing.Kind == ThingKind.Outfit && obdVersion >= ObdVersions.Version3)
                reader.ReadU8();

            var fg = ReadFrameGroupHeader(ref reader, defaultFrameDurationMs);
            ReadEmbeddedSprites(ref reader, fg, obdVersion, transparentSprites, spritesRgba);
            thing.FrameGroups.Add(fg);
        }
    }

    public static void WriteV2V3(
        LittleEndianStreamWriter writer,
        ThingType thing,
        ushort obdVersion,
        bool transparentSprites,
        IReadOnlyDictionary<uint, byte[]> spritesRgba)
    {
        var useOutfitGroups = thing.Kind == ThingKind.Outfit && obdVersion >= ObdVersions.Version3;
        var groupCount = useOutfitGroups ? thing.FrameGroups.Count : 1;
        if (groupCount == 0)
            throw new InvalidOperationException("Thing has no frame groups.");

        if (useOutfitGroups)
        {
            if (groupCount > 255)
                throw new InvalidOperationException("Too many outfit frame groups.");
            writer.WriteU8((byte)groupCount);
        }

        for (var groupIndex = 0; groupIndex < groupCount; groupIndex++)
        {
            if (useOutfitGroups)
            {
                var groupId = (uint)groupIndex;
                if (groupCount < 2)
                    groupId = 1;
                writer.WriteU8((byte)groupId);
            }

            var fg = thing.FrameGroups[groupIndex];
            WriteFrameGroupHeader(writer, fg);
            WriteEmbeddedSprites(writer, fg, obdVersion, transparentSprites, spritesRgba);
        }
    }

    public static void ReadV1(
        ref LittleEndianSpanReader reader,
        ThingType thing,
        bool transparentSprites,
        uint defaultFrameDurationMs,
        Dictionary<uint, byte[]> spritesRgba)
    {
        var fg = ReadFrameGroupHeader(ref reader, defaultFrameDurationMs, legacyDefaultTimings: true);
        ReadEmbeddedSpritesV1(ref reader, fg, transparentSprites, spritesRgba);
        thing.FrameGroups.Add(fg);
    }

    public static void WriteV1(
        LittleEndianStreamWriter writer,
        ThingType thing,
        bool transparentSprites,
        IReadOnlyDictionary<uint, byte[]> spritesRgba)
    {
        if (thing.FrameGroups.Count != 1)
            throw new InvalidOperationException("OBD v1 supports exactly one frame group.");

        var fg = thing.FrameGroups[0];
        WriteFrameGroupHeader(writer, fg);
        WriteEmbeddedSpritesV1(writer, fg, transparentSprites, spritesRgba);
    }

    private static ThingFrameGroup ReadFrameGroupHeader(
        ref LittleEndianSpanReader reader,
        uint defaultFrameDurationMs,
        bool legacyDefaultTimings = false)
    {
        var fg = new ThingFrameGroup();
        fg.Width = reader.ReadU8();
        fg.Height = reader.ReadU8();
        fg.ExactSize = fg.Width > 1 || fg.Height > 1
            ? reader.ReadU8()
            : (uint)SpritePixelCodec.SpriteEdgeLength;
        fg.Layers = reader.ReadU8();
        fg.PatternX = reader.ReadU8();
        fg.PatternY = reader.ReadU8();
        fg.PatternZ = reader.ReadU8();
        fg.Frames = reader.ReadU8();

        if (fg.Frames > 1)
        {
            fg.IsAnimation = true;
            if (legacyDefaultTimings)
            {
                fg.FrameTimings = new AnimationFrameTiming[fg.Frames];
                for (var i = 0u; i < fg.Frames; i++)
                    fg.FrameTimings[i] = new AnimationFrameTiming(defaultFrameDurationMs, defaultFrameDurationMs);
            }
            else
            {
                fg.AnimationMode = reader.ReadU8();
                fg.LoopCount = reader.ReadI32();
                fg.StartFrame = reader.ReadS8();
                fg.FrameTimings = new AnimationFrameTiming[fg.Frames];
                for (var i = 0u; i < fg.Frames; i++)
                {
                    var min = reader.ReadU32();
                    var max = reader.ReadU32();
                    fg.FrameTimings[i] = new AnimationFrameTiming(min, max);
                }
            }
        }

        return fg;
    }

    private static void WriteFrameGroupHeader(
        LittleEndianStreamWriter writer,
        ThingFrameGroup fg,
        bool includePatternZ = true)
    {
        writer.WriteU8((byte)fg.Width);
        writer.WriteU8((byte)fg.Height);
        if (fg.Width > 1 || fg.Height > 1)
            writer.WriteU8((byte)fg.ExactSize);
        writer.WriteU8((byte)fg.Layers);
        writer.WriteU8((byte)fg.PatternX);
        writer.WriteU8((byte)fg.PatternY);
        if (includePatternZ)
            writer.WriteU8((byte)(fg.PatternZ == 0 ? 1 : fg.PatternZ));
        writer.WriteU8((byte)fg.Frames);

        if (fg.Frames > 1)
        {
            fg.IsAnimation = true;
            writer.WriteU8((byte)fg.AnimationMode);
            writer.WriteI32(fg.LoopCount);
            writer.WriteU8(unchecked((byte)fg.StartFrame));

            if (fg.FrameTimings is null || fg.FrameTimings.Length != fg.Frames)
                throw new InvalidOperationException("FrameTimings must match frame count when frames > 1.");

            foreach (var timing in fg.FrameTimings)
            {
                writer.WriteU32(timing.MinimumMilliseconds);
                writer.WriteU32(timing.MaximumMilliseconds);
            }
        }
    }

    private static void ReadEmbeddedSprites(
        ref LittleEndianSpanReader reader,
        ThingFrameGroup fg,
        ushort obdVersion,
        bool transparentSprites,
        Dictionary<uint, byte[]> spritesRgba)
    {
        var totalSprites = fg.GetTotalSpriteSlots();
        if (totalSprites > MaxSpriteIndicesPerThing)
            throw new InvalidDataException("Thing exceeds maximum sprite count.");

        fg.SpriteIds = new uint[totalSprites];
        var rgba = new byte[SpritePixelCodec.RgbaBufferLength];

        for (var i = 0u; i < totalSprites; i++)
        {
            var spriteId = reader.ReadU32();
            fg.SpriteIds[i] = spriteId;

            if (obdVersion == ObdVersions.Version2)
            {
                var argb = reader.ReadBytes(SpritePixelCodec.RgbaBufferLength);
                ObdSpritePixels.ObjectBuilderArgbToRgba(argb, rgba);
                spritesRgba[spriteId] = rgba.ToArray();
            }
            else
            {
                var dataSize = reader.ReadU32();
                if (dataSize > SpritePixelCodec.RgbaBufferLength)
                    throw new InvalidDataException("Invalid OBD sprite data size.");

                if (dataSize == SpritePixelCodec.RgbaBufferLength)
                {
                    var argb = reader.ReadBytes(SpritePixelCodec.RgbaBufferLength);
                    ObdSpritePixels.ObjectBuilderArgbToRgba(argb, rgba);
                    spritesRgba[spriteId] = rgba.ToArray();
                }
                else
                {
                    var compressed = reader.ReadBytes((int)dataSize);
                    ObdSpritePixels.DecompressFromObd(compressed, transparentSprites, rgba);
                    spritesRgba[spriteId] = rgba.ToArray();
                }
            }
        }
    }

    private static void WriteEmbeddedSprites(
        LittleEndianStreamWriter writer,
        ThingFrameGroup fg,
        ushort obdVersion,
        bool transparentSprites,
        IReadOnlyDictionary<uint, byte[]> spritesRgba)
    {
        foreach (var spriteId in fg.SpriteIds)
        {
            if (!spritesRgba.TryGetValue(spriteId, out var rgba))
                throw new InvalidDataException($"Sprite {spriteId} is missing from embedded sprite data.");

            writer.WriteU32(spriteId);

            if (obdVersion == ObdVersions.Version2)
            {
                var argb = ObdSpritePixels.RgbaToObjectBuilderArgb(rgba);
                writer.WriteBytes(argb);
            }
            else
            {
                // OBD v3 (Object Builder): uint32 length + 4096-byte Flash ARGB — not .spr RLE.
                var argb = ObdSpritePixels.RgbaToObjectBuilderArgb(rgba);
                writer.WriteU32((uint)argb.Length);
                writer.WriteBytes(argb);
            }
        }
    }

    private static void ReadEmbeddedSpritesV1(
        ref LittleEndianSpanReader reader,
        ThingFrameGroup fg,
        bool transparentSprites,
        Dictionary<uint, byte[]> spritesRgba)
    {
        var totalSprites = fg.GetTotalSpriteSlots();
        if (totalSprites > MaxSpriteIndicesPerThing)
            throw new InvalidDataException("Thing exceeds maximum sprite count.");

        fg.SpriteIds = new uint[totalSprites];
        var rgba = new byte[SpritePixelCodec.RgbaBufferLength];

        for (var i = 0u; i < totalSprites; i++)
        {
            var spriteId = reader.ReadU32();
            fg.SpriteIds[i] = spriteId;
            var dataSize = reader.ReadU32();
            if (dataSize > SpritePixelCodec.RgbaBufferLength)
                throw new InvalidDataException("Invalid OBD sprite data size.");

            if (dataSize == SpritePixelCodec.RgbaBufferLength)
            {
                var argb = reader.ReadBytes(SpritePixelCodec.RgbaBufferLength);
                ObdSpritePixels.ObjectBuilderArgbToRgba(argb, rgba);
            }
            else
            {
                var compressed = reader.ReadBytes((int)dataSize);
                ObdSpritePixels.DecompressFromObd(compressed, transparentSprites, rgba);
            }

            spritesRgba[spriteId] = rgba.ToArray();
        }
    }

    private static void WriteEmbeddedSpritesV1(
        LittleEndianStreamWriter writer,
        ThingFrameGroup fg,
        bool transparentSprites,
        IReadOnlyDictionary<uint, byte[]> spritesRgba)
    {
        foreach (var spriteId in fg.SpriteIds)
        {
            if (!spritesRgba.TryGetValue(spriteId, out var rgba))
                throw new InvalidDataException($"Sprite {spriteId} is missing from embedded sprite data.");

            var argb = ObdSpritePixels.RgbaToObjectBuilderArgb(rgba);
            writer.WriteU32(spriteId);
            writer.WriteU32((uint)argb.Length);
            writer.WriteBytes(argb);
        }
    }
}
