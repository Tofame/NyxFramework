using NyxRender;
using NyxAssets.Client;
using NyxAssets.Things;

namespace NyxDrawer;

/// <summary>
/// Uploads all sprites referenced by a <see cref="ThingType"/> into the renderer atlas.
///
/// <see cref="Preload(ThingType, Span{byte})"/> accepts a caller-provided decode buffer to
/// avoid per-call heap allocations (the zero-arg overload uses <c>stackalloc</c>).
///
/// <b>Atlas capacity:</b> sprites that cannot fit into the atlas (atlas full) are silently
/// skipped via catch of <c>InvalidOperationException</c>.  This means not every sprite is
/// guaranteed to be preloaded — check <see cref="SpriteRenderer.IsSpriteResident"/> afterward.
/// </summary>
public sealed class ThingSpritePreloader
{
    private readonly ClientAssetBundle _assets;
    private readonly SpriteRenderer _renderer;

    public ThingSpritePreloader(ClientAssetBundle assets, SpriteRenderer renderer)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Preloads all sprites for a thing type.  Skips sprites already resident in the atlas.
    /// If the atlas is full, remaining sprites are silently ignored.
    /// </summary>
    public void Preload(ThingType thing, Span<byte> decodeScratch)
    {
        foreach (var frameGroup in thing.FrameGroups)
        {
            foreach (var spriteId in frameGroup.EnumerateSpriteIds())
            {
                if (spriteId == 0)
                    continue;
                if (!_assets.TryDecodeSpriteById(spriteId, decodeScratch))
                    continue;
                if (_renderer.IsSpriteResident((int)spriteId))
                    continue;
                try
                {
                    _renderer.LoadSpriteRgba((int)spriteId, decodeScratch);
                }
                catch (InvalidOperationException)
                {
                    /* atlas full */
                }
            }
        }
    }

    /// <summary>
    /// Preloads all sprites using a stack-allocated decode buffer.
    /// </summary>
    public void Preload(ThingType thing) =>
        Preload(thing, stackalloc byte[Sprite.Rgba32Length]);
}
