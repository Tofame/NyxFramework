using System.Collections.Generic;

namespace NyxAssets.Things;

/// <summary>One object definition from the client <c>.dat</c> (metadata + sprite layout).</summary>
public sealed class ThingType
{
    public uint Id { get; set; }
    public ThingKind Kind { get; set; }

    public bool IsGround { get; set; }
    public uint GroundSpeed { get; set; }
    public bool IsGroundBorder { get; set; }
    public bool IsOnBottom { get; set; }
    public bool IsOnTop { get; set; }
    public bool IsContainer { get; set; }
    public bool Stackable { get; set; }
    public bool ForceUse { get; set; }
    public bool MultiUse { get; set; }
    public bool HasCharges { get; set; }
    public bool Writable { get; set; }
    public bool WritableOnce { get; set; }
    public uint MaxTextLength { get; set; }
    public bool IsFluidContainer { get; set; }
    public bool IsFluid { get; set; }
    public bool IsUnpassable { get; set; }
    public bool IsUnmoveable { get; set; }
    public bool BlockMissile { get; set; }
    public bool BlockPathfind { get; set; }
    public bool NoMoveAnimation { get; set; }
    public bool Pickupable { get; set; }
    public bool Hangable { get; set; }
    public bool IsVertical { get; set; }
    public bool IsHorizontal { get; set; }
    public bool Rotatable { get; set; }
    public bool HasLight { get; set; }
    public uint LightLevel { get; set; }
    public uint LightColor { get; set; }
    public bool DontHide { get; set; }
    public bool IsTranslucent { get; set; }
    public bool FloorChange { get; set; }
    public bool HasOffset { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public bool HasElevation { get; set; }
    public uint Elevation { get; set; }
    public bool IsLyingObject { get; set; }
    public bool AnimateAlways { get; set; }
    public bool MiniMap { get; set; }
    public uint MiniMapColor { get; set; }
    public bool IsLensHelp { get; set; }
    public uint LensHelp { get; set; }
    public bool IsFullGround { get; set; }
    public bool IgnoreLook { get; set; }
    public bool Cloth { get; set; }
    public uint ClothSlot { get; set; }
    public bool IsMarketItem { get; set; }
    public string? MarketName { get; set; }
    public uint MarketCategory { get; set; }
    public uint MarketTradeAs { get; set; }
    public uint MarketShowAs { get; set; }
    public uint MarketRestrictProfession { get; set; }
    public uint MarketRestrictLevel { get; set; }
    public bool HasDefaultAction { get; set; }
    public uint DefaultAction { get; set; }
    public bool Wrappable { get; set; }
    public bool Unwrappable { get; set; }
    public bool BottomEffect { get; set; }
    public bool DontCenterOutfit { get; set; }
    public bool Usable { get; set; }

    public List<ThingFrameGroup> FrameGroups { get; } = new();

    public Dictionary<string, string> ExtraProperties { get; } = new();

    public ThingFrameGroup? GetFrameGroup(int index) =>
        index >= 0 && index < FrameGroups.Count ? FrameGroups[index] : null;

    /// <summary>
    /// Outfit helper: enumerates <c>.spr</c> ids for any slice of the layout tensor.
    /// Arguments default to <see langword="null"/> meaning “all indices on that axis”; pass a value to pin one coordinate (normalized with <c>%</c> like Asset Editor <c>ThingTypeEditor</c>).
    /// </summary>
    /// <param name="frameGroupIndex">Usually <c>0</c> (idle) or <c>1</c> when a walking frame group exists.</param>
    /// <exception cref="InvalidOperationException">If <see cref="Kind"/> is not <see cref="ThingKind.Outfit"/>.</exception>
    public IEnumerable<uint> EnumerateSpriteIdsForOutfit(
        uint? innerWidth = null,
        uint? innerHeight = null,
        uint? layer = null,
        uint? patternX = null,
        uint? patternY = null,
        uint? patternZ = null,
        uint? frame = null,
        int frameGroupIndex = 0)
    {
        if (Kind != ThingKind.Outfit)
            throw new InvalidOperationException($"{nameof(EnumerateSpriteIdsForOutfit)} is defined for outfits only; this thing is {Kind}.");

        var fg = GetFrameGroup(frameGroupIndex) ?? throw new ArgumentOutOfRangeException(nameof(frameGroupIndex), "Frame group missing.");
        return fg.EnumerateSpriteIds(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame);
    }

    /// <summary>Builds an array from <see cref="EnumerateSpriteIdsForOutfit"/> (same parameters).</summary>
    public uint[] GetSpriteIdsForOutfit(
        uint? innerWidth = null,
        uint? innerHeight = null,
        uint? layer = null,
        uint? patternX = null,
        uint? patternY = null,
        uint? patternZ = null,
        uint? frame = null,
        int frameGroupIndex = 0)
    {
        if (Kind != ThingKind.Outfit)
            throw new InvalidOperationException($"{nameof(GetSpriteIdsForOutfit)} is defined for outfits only; this thing is {Kind}.");

        var fg = GetFrameGroup(frameGroupIndex) ?? throw new ArgumentOutOfRangeException(nameof(frameGroupIndex), "Frame group missing.");
        return fg.GetSpriteIds(innerWidth, innerHeight, layer, patternX, patternY, patternZ, frame);
    }
}
