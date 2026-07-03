using NyxDrawer.Creatures;
using NyxDrawer.Distance;
using NyxDrawer.Drawing;
using NyxDrawer.Effects;
using NyxDrawer.Animation;
using NyxDrawer.Items;
using NyxRender;
using NyxAssets.Client;

namespace NyxDrawer;

/// <summary>
/// Entry point for Nyx-style drawing on a <see cref="SpriteRenderer"/>.
/// Register outfit shaders on <paramref name="renderer"/> before drawing creatures.
///
/// Wires together all sub-drawers (creatures, items, effects, distance effects)
/// through a shared <see cref="ThingLayerDrawer"/> that handles per-cell sprite
/// rendering with GPU outfit compositing and CPU fallback.
/// </summary>
public sealed class AssetDrawer
{
    /// <param name="assets">Loaded .dat/.spr client assets for sprite decoding.</param>
    /// <param name="renderer">The NyxRender sprite renderer with registered shaders.</param>
    public AssetDrawer(ClientAssetBundle assets, SpriteRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(renderer);

        var layers = new ThingLayerDrawer(assets, renderer);
        Preloader = new ThingSpritePreloader(assets, renderer);
        Animator = new ThingAnimator();
        Creatures = new CreatureDrawer(layers);
        Items = new ItemDrawer(layers);
        Effects = new EffectDrawer(layers);
        DistanceEffects = new DistanceEffectDrawer(layers);
    }

    /// <summary>Preloads sprites into the GPU atlas for a thing type.</summary>
    public ThingSpritePreloader Preloader { get; }

    /// <summary>Frame animation state machine (idle, walking, effects).</summary>
    public ThingAnimator Animator { get; }

    /// <summary>Draws creatures (outfits with colours, addons, mounts, walk phases).</summary>
    public CreatureDrawer Creatures { get; }

    /// <summary>Draws ground/container items with stack-count patterns.</summary>
    public ItemDrawer Items { get; }

    /// <summary>Draws one-shot effects (spells, explosions, etc.).</summary>
    public EffectDrawer Effects { get; }

    /// <summary>Draws missile/distance effects with linear interpolation.</summary>
    public DistanceEffectDrawer DistanceEffects { get; }
}
