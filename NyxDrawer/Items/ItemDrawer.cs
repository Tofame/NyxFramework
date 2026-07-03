using NyxDrawer.Drawing;
using NyxAssets.Things;

namespace NyxDrawer.Items;

/// <summary>
/// Draws Nyx items (map tiles, objects on ground).
/// Items always use <c>FrameGroups[0]</c> with frame wrapping.
/// </summary>
public sealed class ItemDrawer
{
    private readonly ThingLayerDrawer _layers;

    public ItemDrawer(ThingLayerDrawer layers)
    {
        ArgumentNullException.ThrowIfNull(layers);
        _layers = layers;
    }

    public bool Draw(ItemDrawRequest request)
    {
        var item = request.Item;
        if (item.FrameGroups.Count == 0)
            return false;

        var frameGroup = item.FrameGroups[0];
        var frame = frameGroup.Frames > 0 ? request.Frame % frameGroup.Frames : 0u;
        _layers.DrawLayers(
            item, frameGroup, request.AnchorX, request.AnchorY,
            request.PatternX, request.PatternY, request.PatternZ, frame,
            spriteShader: request.SpriteShader);
        return true;
    }
}
