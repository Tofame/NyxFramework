using NyxDrawer.Animation;
using NyxDrawer.Drawing;
using NyxDrawer.Geometry;
using NyxAssets.Things;

namespace NyxDrawer.Creatures;

/// <summary>
/// Draws Nyx outfits (addons, colors, mount, walk phases).
///
/// <b>Mount drawing:</b> The mount is drawn first (below the rider).  Both mount and
/// rider get their own walking frame group and direction pattern.  The rider's position
/// is offset from the mount via <see cref="ThingDrawGeometry.GetMountedDrawAnchors"/>.
///
/// <b>Addon pattern filtering:</b> pattern Y values are skipped unless the corresponding
/// bit is set in the <c>LookAddons</c> bitmask (checked via <see cref="CreatureOutfitAppearance.HasAddonPattern"/>).
/// Pattern Y=0 is always drawn (base outfit).
///
/// <b>Walking frames:</b> <see cref="ThingAnimator.ResolveWalkingFrame"/> selects between
/// idle (FrameGroups[0]) and walking (FrameGroups[1]) based on <c>WalkPhase &gt; 0</c>.
/// </summary>
public sealed class CreatureDrawer
{
    private readonly ThingLayerDrawer _layers;

    public CreatureDrawer(ThingLayerDrawer layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _layers = layers;
    }

    /// <summary>
    /// Draws a creature with outfit colours, addons, optional mount, and walk animation.
    /// Returns false if the outfit kind is not <see cref="ThingKind.Outfit"/> or has no frame groups.
    /// </summary>
    public bool Draw(CreatureDrawRequest request)
    {
        var outfit = request.Outfit;
        if (outfit.Kind != ThingKind.Outfit || outfit.FrameGroups.Count == 0)
            return false;

        ThingAnimator.ResolveWalkingFrame(outfit, request.WalkPhase, out var frameGroup, out var frame);
        var dir = (uint)(request.Direction % Math.Max(1, (int)frameGroup.PatternX));
        var mounted = request.Mounted && request.Appearance.HasMount;
        var patternZ = ThingDrawGeometry.GetMountedPatternZ(frameGroup, mounted);

        var riderX = request.AnchorX;
        var riderY = request.AnchorY;

        if (mounted && request.Mount is { FrameGroups.Count: > 0 } mount)
        {
            ThingDrawGeometry.GetMountedDrawAnchors(
                outfit, mount, request.AnchorX, request.AnchorY,
                out var mountAnchorX, out var mountAnchorY, out riderX, out riderY);

            ThingAnimator.ResolveWalkingFrame(mount, request.WalkPhase, out var mountGroup, out var mountFrame);
            var mountDir = (uint)(request.Direction % Math.Max(1, (int)mountGroup.PatternX));
            for (var yPattern = 0; yPattern < (int)mountGroup.PatternY; yPattern++)
            {
                _layers.DrawLayers(mount, mountGroup, mountAnchorX, mountAnchorY, mountDir, (uint)yPattern, 0, mountFrame);
            }
        }

        for (var yPattern = 0; yPattern < (int)frameGroup.PatternY; yPattern++)
        {
            if (!request.Appearance.HasAddonPattern(yPattern))
                continue;
            _layers.DrawLayers(outfit, frameGroup, riderX, riderY, dir, (uint)yPattern, patternZ, frame, request.Appearance);
        }

        return true;
    }
}
