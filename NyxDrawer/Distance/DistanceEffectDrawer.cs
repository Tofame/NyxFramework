using NyxDrawer.Drawing;
using NyxDrawer.Geometry;
using NyxAssets.Things;
using NyxAssets.Things.Frames;

namespace NyxDrawer.Distance;

/// <summary>
/// Draws Nyx missiles (distance / projectile effects).
///
/// <b>Position interpolation:</b> the missile is positioned along the tile-delta vector
/// at <c>progress</c> (0..1): <c>anchor = fromAnchor + delta * cellPx * progress</c>.
/// At progress=0 the missile is at the source tile; at progress=1 it's at the destination.
///
/// <b>Direction:</b> if <c>request.Direction</c> is null, it's derived from tile delta
/// via <see cref="MissileDirectionPatterns.DirectionFromTileDelta"/>, which uses Atan2 sector binning.
///
/// <b>Duration:</b> <c>150 * sqrt(dx² + dy²)</c> ms — NyxClient missile travel time formula.
/// </summary>
public sealed class DistanceEffectDrawer
{
    private readonly ThingLayerDrawer _layers;

    public DistanceEffectDrawer(ThingLayerDrawer layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _layers = layers;
    }

    public bool Draw(DistanceEffectDrawRequest request)
    {
        var missile = request.Missile;
        if (missile.Kind != ThingKind.Missile || missile.FrameGroups.Count == 0)
            return false;

        var frameGroup = missile.FrameGroups[0];
        var direction = request.Direction.HasValue
            ? (Direction8)request.Direction.Value
            : MissileDirectionPatterns.DirectionFromTileDelta(request.TileDeltaX, request.TileDeltaY);
        var (patternX, patternY) = MissileDirectionPatterns.GetPattern(direction);

        var progress = Math.Clamp(request.Progress, 0f, 1f);
        var offsetX = request.TileDeltaX * ThingDrawGeometry.DefaultCellPx * progress;
        var offsetY = request.TileDeltaY * ThingDrawGeometry.DefaultCellPx * progress;
        var anchorX = request.FromAnchorX + offsetX;
        var anchorY = request.FromAnchorY + offsetY;

        _layers.DrawLayers(missile, frameGroup, anchorX, anchorY, (uint)patternX, (uint)patternY, 0, 0);
        return true;
    }

    /// <summary>NyxClient duration for a tile-distance shot.</summary>
    public static float DurationMs(int tileDeltaX, int tileDeltaY) =>
        MissileDirectionPatterns.DurationMsFromTileDelta(tileDeltaX, tileDeltaY);
}
