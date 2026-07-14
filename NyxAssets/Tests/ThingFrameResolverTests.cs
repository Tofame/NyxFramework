using NyxAssets.Things;
using NyxAssets.Things.Frames;
using Xunit;

namespace NyxAssets.Tests;

public class ThingFrameResolverTests
{
    [Theory]
    [InlineData(Direction8.North, 1u, 0u)]
    [InlineData(Direction8.East, 2u, 1u)]
    [InlineData(Direction8.SouthWest, 0u, 2u)]
    public void GetMissileFrame_MapsDirectionToPattern(Direction8 direction, uint expectedX, uint expectedY)
    {
        var missile = CreateMissile();
        var selection = ThingFrameResolver.GetMissileFrame(missile, new MissileFrameRequest { Direction = direction });

        Assert.Equal(expectedX, selection.PatternX);
        Assert.Equal(expectedY, selection.PatternY);
    }

    [Theory]
    [InlineData(1, 0u, 0u)]
    [InlineData(4, 3u, 0u)]
    [InlineData(10, 1u, 1u)]
    public void GetItemFrame_ResolvesStackPile(int stackCount, uint expectedX, uint expectedY)
    {
        var item = CreateStackableItem();
        var selection = ThingFrameResolver.GetItemFrame(item, new ItemFrameRequest { StackCount = stackCount });

        Assert.Equal(expectedX, selection.PatternX);
        Assert.Equal(expectedY, selection.PatternY);
    }

    [Fact]
    public void GetOutfitFrame_WalkingPhase_UsesSecondFrameGroup()
    {
        var outfit = CreateOutfitWithWalkGroup();
        var idle = ThingFrameResolver.GetOutfitFrame(outfit, new OutfitFrameRequest { WalkPhase = 0 });
        var walking = ThingFrameResolver.GetOutfitFrame(outfit, new OutfitFrameRequest { WalkPhase = 2 });

        Assert.Equal(0, idle.FrameGroupIndex);
        Assert.Equal(1, walking.FrameGroupIndex);
        Assert.Equal(1u, walking.Frame);
    }

    [Fact]
    public void EnumerateOutfitAddonFrames_FiltersByAddonMask()
    {
        var outfit = CreateOutfitWithAddons();
        var slices = ThingFrameResolver.EnumerateOutfitAddonFrames(
            outfit,
            new OutfitFrameRequest { AddonMask = 0b01 }).Select(s => s.PatternY).ToArray();

        Assert.Equal(new uint[] { 0, 1 }, slices);
    }

    [Fact]
    public void GetEffectFrame_UsesPositiveTileModulo()
    {
        var effect = CreateEffectWithPatterns();
        var selection = ThingFrameResolver.GetEffectFrame(effect, new EffectFrameRequest { TileX = -1, TileY = 3, Frame = 0 });

        Assert.Equal(2u, selection.PatternX);
        Assert.Equal(1u, selection.PatternY);
    }

    [Fact]
    public void GetEffectFrameIndex_ClampsBetweenZeroAndFramesMinus1()
    {
        var effect = CreateEffectWithFrames(4);

        // At 0 ms → frame 0
        Assert.Equal(0u, ThingFrameResolver.GetEffectFrameIndex(effect, 0f));

        // Clamped at Frames - 1 (3) for very large elapsed time
        Assert.Equal(3u, ThingFrameResolver.GetEffectFrameIndex(effect, 99999f));

        // Advances with each 75 ms tick
        Assert.Equal(1u, ThingFrameResolver.GetEffectFrameIndex(effect, 75f));
        Assert.Equal(2u, ThingFrameResolver.GetEffectFrameIndex(effect, 150f));
    }

    [Fact]
    public void GetCyclicFrameIndex_WrapsAround()
    {
        var thing = CreateEffectWithFrames(3);

        var frameAt0 = ThingFrameResolver.GetCyclicFrameIndex(thing, 0f);
        var frameAt333 = ThingFrameResolver.GetCyclicFrameIndex(thing, 333f);
        var frameAt999 = ThingFrameResolver.GetCyclicFrameIndex(thing, 999f);

        Assert.Equal(0u, frameAt0);
        Assert.Equal(1u, frameAt333);
        // After 3 full cycles returns to frame 0
        Assert.Equal(0u, frameAt999);
    }

    [Theory]
    [InlineData(-1, 4u, 3u)]  // -1 mod 4 → 3
    [InlineData(-4, 4u, 0u)]  // -4 mod 4 → 0
    public void NormalizeDirection_NegativeInput_ReturnsPositiveMod(int direction, uint patternCount, uint expected)
    {
        Assert.Equal(expected, ThingFrameResolver.NormalizeDirection(direction, patternCount));
    }

    [Theory]
    [InlineData(-1, 3u, 2u)]  // -1 mod 3 → 2
    [InlineData(-3, 3u, 0u)]  // -3 mod 3 → 0
    public void PositiveMod_NegativeInput_ReturnsPositiveMod(int value, uint count, uint expected)
    {
        Assert.Equal(expected, ThingFrameResolver.PositiveMod(value, count));
    }

    [Fact]
    public void GetMissileFrame_FromTileDelta_ResolvesCorrectDirection()
    {
        var missile = CreateMissile();

        // dx=1, dy=0 → East direction → pattern (2, 1)
        var selection = ThingFrameResolver.GetMissileFrame(missile, new MissileFrameRequest { TileDeltaX = 1, TileDeltaY = 0 });
        Assert.Equal(2u, selection.PatternX);
        Assert.Equal(1u, selection.PatternY);

        // dx=0, dy=-1 → North direction → pattern (1, 0)
        var northSelection = ThingFrameResolver.GetMissileFrame(missile, new MissileFrameRequest { TileDeltaX = 0, TileDeltaY = -1 });
        Assert.Equal(1u, northSelection.PatternX);
        Assert.Equal(0u, northSelection.PatternY);
    }

    [Fact]
    public void EnumerateMountedOutfitFrames_EmitsMountBeforeRider()
    {
        var outfit = CreateOutfitWithWalkGroup();
        var mount = CreateOutfitWithWalkGroup();

        var frames = ThingFrameResolver.EnumerateMountedOutfitFrames(outfit, mount).ToArray();

        Assert.True(frames.Length >= 2);
        // Mount emitted first
        Assert.True(frames[0].IsMount);
        // At least one rider frame follows
        Assert.Contains(frames, f => !f.IsMount);
    }

    [Fact]
    public void GetOutfitFrame_WrongKind_ThrowsArgumentException()
    {
        var item = new ThingType { Id = 1, Kind = ThingKind.Item };
        item.FrameGroups.Add(new ThingFrameGroup { SpriteIds = new uint[] { 1 } });

        Assert.Throws<ArgumentException>(() => ThingFrameResolver.GetOutfitFrame(item));
    }

    [Theory]
    [InlineData(1, 0, 150f)]   // 1 tile → 150 ms
    [InlineData(0, 2, 300f)]   // 2 tiles → 300 ms
    public void DurationMsFromTileDelta_MatchesExpected(int dx, int dy, float expected)
    {
        Assert.Equal(expected, MissileDirectionPatterns.DurationMsFromTileDelta(dx, dy), precision: 1);
    }

    [Theory]
    [InlineData(4u, 2u, true, true)]    // 4×2, stackable → uses grid
    [InlineData(4u, 2u, false, false)]  // 4×2, not stackable → no grid
    [InlineData(3u, 2u, true, false)]   // wrong patternX → no grid
    [InlineData(4u, 1u, true, false)]   // wrong patternY → no grid
    public void ItemStackPatterns_UsesStackCountGrid_Boundaries(uint patternX, uint patternY, bool stackable, bool expected)
    {
        Assert.Equal(expected, ItemStackPatterns.UsesStackCountGrid(patternX, patternY, stackable));
    }

    private static ThingType CreateMissile()
    {
        var thing = new ThingType { Id = 1, Kind = ThingKind.Missile };
        thing.FrameGroups.Add(new ThingFrameGroup
        {
            PatternX = 3,
            PatternY = 3,
            SpriteIds = new uint[] { 1 },
        });
        return thing;
    }

    private static ThingType CreateStackableItem()
    {
        var thing = new ThingType { Id = 100, Kind = ThingKind.Item, Stackable = true };
        thing.FrameGroups.Add(new ThingFrameGroup
        {
            PatternX = 4,
            PatternY = 2,
            SpriteIds = Enumerable.Range(1, 8).Select(i => (uint)i).ToArray(),
        });
        return thing;
    }

    private static ThingType CreateOutfitWithWalkGroup()
    {
        var thing = new ThingType { Id = 128, Kind = ThingKind.Outfit };
        thing.FrameGroups.Add(new ThingFrameGroup { PatternX = 4, Frames = 1, SpriteIds = new uint[] { 1 } });
        thing.FrameGroups.Add(new ThingFrameGroup { PatternX = 4, Frames = 3, SpriteIds = new uint[] { 2, 3, 4 } });
        return thing;
    }

    private static ThingType CreateOutfitWithAddons()
    {
        var thing = new ThingType { Id = 128, Kind = ThingKind.Outfit };
        thing.FrameGroups.Add(new ThingFrameGroup
        {
            PatternX = 1,
            PatternY = 3,
            SpriteIds = new uint[] { 1, 2, 3 },
        });
        return thing;
    }

    private static ThingType CreateEffectWithPatterns()
    {
        var thing = new ThingType { Id = 1, Kind = ThingKind.Effect };
        thing.FrameGroups.Add(new ThingFrameGroup
        {
            PatternX = 3,
            PatternY = 2,
            SpriteIds = new uint[] { 10 },
        });
        return thing;
    }

    private static ThingType CreateEffectWithFrames(uint frames)
    {
        var thing = new ThingType { Id = 1, Kind = ThingKind.Effect };
        thing.FrameGroups.Add(new ThingFrameGroup
        {
            PatternX = 1,
            PatternY = 1,
            Frames = frames,
            SpriteIds = Enumerable.Range(1, (int)frames).Select(i => (uint)i).ToArray(),
        });
        return thing;
    }
}
