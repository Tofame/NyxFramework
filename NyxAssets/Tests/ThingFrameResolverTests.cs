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
}
