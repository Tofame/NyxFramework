using NyxDrawer.Drawing;
using NyxAssets.Things;

namespace NyxDrawer.Effects;

/// <summary>
/// Draws Nyx magic effects.
///
/// <b>Pattern X/Y:</b> derived from tile coordinates using positive modulo
/// (<c>((tilePos % N) + N) % N</c>) to guarantee non-negative results even for
/// negative tile positions.
///
/// <b>Frame:</b> wraps with modulo on the frame group's frame count.
/// </summary>
public sealed class EffectDrawer
{
    private readonly ThingLayerDrawer _layers;

    public EffectDrawer(ThingLayerDrawer layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _layers = layers;
    }

    public bool Draw(EffectDrawRequest request)
    {
        var effect = request.Effect;
        if (effect.Kind != ThingKind.Effect || effect.FrameGroups.Count == 0)
            return false;

        var frameGroup = effect.FrameGroups[0];
        var frame = frameGroup.Frames > 0 ? request.Frame % frameGroup.Frames : 0u;

        var patternX = frameGroup.PatternX > 0 ? (uint)((request.TileX % (int)frameGroup.PatternX + (int)frameGroup.PatternX) % (int)frameGroup.PatternX) : 0u;
        var patternY = frameGroup.PatternY > 0 ? (uint)((request.TileY % (int)frameGroup.PatternY + (int)frameGroup.PatternY) % (int)frameGroup.PatternY) : 0u;

        _layers.DrawLayers(effect, frameGroup, request.AnchorX, request.AnchorY, patternX, patternY, 0, frame);
        return true;
    }
}
