using NyxAssets.Things;
using NyxAssets.Things.Frames;

namespace NyxDrawer.Geometry;

/// <summary>NyxClient-style sprite origin (32 px cell grid, displacement from <c>.dat</c>).</summary>
public static class ThingDrawGeometry
{
    public const float DefaultCellPx = 32f;

    public static float GetLayoutStridePx(ThingFrameGroup frameGroup) => DefaultCellPx;

    public static (int X, int Y) GetDisplacement(ThingType thing) =>
        thing.HasOffset ? (thing.OffsetX, thing.OffsetY) : (0, 0);

    /// <summary>
    /// NyxClient <c>outfit.cpp</c> mount pass: <c>dest -= mountDisp</c> before mount draw,
    /// then <c>dest += outfitDisp</c> before rider (pattern Z). Rider ends up at tile − mountDisp.
    /// </summary>
    public static void GetMountedDrawAnchors(
        ThingType outfit,
        ThingType mount,
        float tileAnchorX,
        float tileAnchorY,
        out float mountAnchorX,
        out float mountAnchorY,
        out float riderAnchorX,
        out float riderAnchorY)
    {
        var (mountDisplacementX, mountDisplacementY) = GetDisplacement(mount);
        var (outfitDisplacementX, outfitDisplacementY) = GetDisplacement(outfit);
        mountAnchorX = tileAnchorX - mountDisplacementX;
        mountAnchorY = tileAnchorY - mountDisplacementY;
        riderAnchorX = tileAnchorX - mountDisplacementX + outfitDisplacementX;
        riderAnchorY = tileAnchorY - mountDisplacementY + outfitDisplacementY;
    }

    /// <summary>
    /// Top-left screen pixel for the south-west inner cell. Matches NyxClient
    /// <c>dest - displacement - (size - 1) * TILE_PIXELS</c> before per-sprite placement.
    /// </summary>
    public static void GetThingSpriteOrigin(
        ThingType thing,
        ThingFrameGroup frameGroup,
        float tileTopLeftX,
        float tileTopLeftY,
        out float originX,
        out float originY)
    {
        var w = frameGroup.Width == 0 ? 1u : frameGroup.Width;
        var h = frameGroup.Height == 0 ? 1u : frameGroup.Height;
        var backX = (w - 1) * DefaultCellPx;
        var backY = (h - 1) * DefaultCellPx;

        var displacementX = thing.HasOffset ? thing.OffsetX : 0;
        var displacementY = thing.HasOffset ? thing.OffsetY : 0;
        originX = tileTopLeftX - displacementX - backX;
        originY = tileTopLeftY - displacementY - backY;
    }

    /// <summary>NyxClient <c>zPattern = mount ? min(1, numPatternZ - 1) : 0</c>.</summary>
    public static uint GetMountedPatternZ(ThingFrameGroup frameGroup, bool mounted) =>
        ThingFrameResolver.GetMountedPatternZ(frameGroup, mounted);
}
