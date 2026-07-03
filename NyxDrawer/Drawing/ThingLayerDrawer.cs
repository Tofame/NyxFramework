using NyxDrawer.Appearance;
using NyxDrawer.Geometry;
using NyxRender;
using NyxRender.Shaders;
using NyxAssets.Client;
using NyxAssets.Things;

namespace NyxDrawer.Drawing;

/// <summary>Shared per-cell layer drawing for items, effects, missiles, and outfit layers.</summary>
public sealed class ThingLayerDrawer
{
    private readonly ClientAssetBundle _assets;
    private readonly SpriteRenderer _renderer;

    public ThingLayerDrawer(ClientAssetBundle assets, SpriteRenderer renderer)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Draws all layers (cells) of a thing frame group at the given anchor position.
    ///
    /// <b>Rendering paths:</b>
    /// <list type="bullet">
    ///   <item><b>GPU path</b> (<c>useGpuPath</c>): When a mask layer AND colors are present, or when
    ///   an effect shader is used.  Attempts <see cref="SpriteRenderer.TryDrawOutfitLayers"/> first;
    ///   falls back to CPU compositing via <see cref="OutfitColorCompositor.TryComposite"/> if the
    ///   GPU path fails (e.g. sprites are in different atlases).</item>
    ///   <item><b>Standard path</b>: Draws each sprite layer directly.  When colours are present,
    ///   only layer 0 + 2+ are drawn (layer 1 is the mask, consumed by the GPU/CPU colour path).</item>
    /// </list>
    ///
    /// <b>Cell ordering:</b> cells are iterated right-to-left, bottom-to-top
    /// (<c>Width-1-cellX</c>, <c>Height-1-cellY</c>), matching NyxClient's south-west-to-north-east
    /// draw order where inner cells are drawn first.
    ///
    /// <b>Resident optimization:</b> if a sprite ID is already in the GPU atlas,
    /// <c>ReadOnlySpan&lt;byte&gt;.Empty</c> is passed to skip re-upload.
    /// </summary>
    public void DrawLayers(
        ThingType thing,
        ThingFrameGroup frameGroup,
        float anchorX,
        float anchorY,
        uint patternX,
        uint patternY,
        uint patternZ,
        uint frame,
        CreatureOutfitAppearance? appearance = null,
        OutfitColorLayout? legacyColors = null,
        string? spriteShader = null)
    {
		if (appearance == null && legacyColors == null)
		{
			// Fast path for items, map tiles, etc.
			var w = frameGroup.Width;
			var h = frameGroup.Height;
			var l = frameGroup.Layers;
			var fastHasSpriteShader = !string.IsNullOrEmpty(spriteShader);

			if (w == 1 && h == 1 && l == 1)
			{
				var displacementX = thing.HasOffset ? thing.OffsetX : 0;
				var displacementY = thing.HasOffset ? thing.OffsetY : 0;
				var screenX = anchorX - displacementX;
				var screenY = anchorY - displacementY;

				var f = frameGroup.Frames != 0 ? frame % frameGroup.Frames : 0u;
				var i = ((f * frameGroup.PatternZ + patternZ) * frameGroup.PatternY + patternY) * frameGroup.PatternX + patternX;

				if (i < frameGroup.SpriteIds.Length)
				{
					var spriteId = frameGroup.SpriteIds[i];
					if (spriteId != 0)
					{
						var r = Sprite.Resident((int)spriteId);
						var success = fastHasSpriteShader
							? _renderer.TryDrawSpriteEffect(in r, screenX, screenY, spriteShader!)
							: _renderer.TryDraw(in r, screenX, screenY);

						if (!success)
						{
							Span<byte> fastDecodeBuffer = stackalloc byte[Sprite.Rgba32Length];
							if (_assets.TryDecodeSpriteById(spriteId, fastDecodeBuffer))
							{
								if (fastHasSpriteShader)
									_renderer.TryDrawSpriteEffect((int)spriteId, fastDecodeBuffer, screenX, screenY, spriteShader!);
								else
									_renderer.TryDraw((int)spriteId, fastDecodeBuffer, screenX, screenY);
							}
						}
					}
				}
				return;
			}
			else
			{
				// General fast path for larger objects or multi-layer items (no appearance/legacyColors)
				ThingDrawGeometry.GetThingSpriteOrigin(thing, frameGroup, anchorX, anchorY, out var fastOriginX, out var fastOriginY);
				var fastStride = ThingDrawGeometry.GetLayoutStridePx(frameGroup);
				Span<byte> fastDecodeBuffer = stackalloc byte[Sprite.Rgba32Length];

				var frames = frameGroup.Frames;
				var f = frames != 0 ? frame % frames : 0u;
				var baseIndex = (f * frameGroup.PatternZ + patternZ) * frameGroup.PatternY + patternY;
				var patternXMul = frameGroup.PatternX;
				var spriteIds = frameGroup.SpriteIds;

				for (var cellY = 0u; cellY < h; cellY++)
				{
					var screenY = fastOriginY + (h - 1 - cellY) * fastStride;
					for (var cellX = 0u; cellX < w; cellX++)
					{
						var screenX = fastOriginX + (w - 1 - cellX) * fastStride;

						for (var layer = 0u; layer < l; layer++)
						{
							// Inline/simplify GetSpriteIndex
							var idx = baseIndex * patternXMul + patternX;
							idx = idx * l + layer;
							idx = idx * h + cellY;
							idx = idx * w + cellX;

							if (idx >= spriteIds.Length)
								continue;

							var spriteId = spriteIds[idx];
							if (spriteId == 0)
								continue;

							var r = Sprite.Resident((int)spriteId);
							var success = fastHasSpriteShader
								? _renderer.TryDrawSpriteEffect(in r, screenX, screenY, spriteShader!)
								: _renderer.TryDraw(in r, screenX, screenY);

							if (success)
								continue;

							if (!_assets.TryDecodeSpriteById(spriteId, fastDecodeBuffer))
								continue;

							if (fastHasSpriteShader)
								_renderer.TryDrawSpriteEffect((int)spriteId, fastDecodeBuffer, screenX, screenY, spriteShader!);
							else
								_renderer.TryDraw((int)spriteId, fastDecodeBuffer, screenX, screenY);
						}
					}
				}
				return;
			}
		}
        ThingDrawGeometry.GetThingSpriteOrigin(thing, frameGroup, anchorX, anchorY, out var originX, out var originY);
        var stride = ThingDrawGeometry.GetLayoutStridePx(frameGroup);
        Span<byte> decodeBuffer = stackalloc byte[Sprite.Rgba32Length];
        Span<byte> maskDecodeBuffer = stackalloc byte[Sprite.Rgba32Length];
        var colors = appearance?.ToColorLayout() ?? legacyColors;
        var shader = appearance?.EffectiveShader ?? spriteShader ?? "outfit_default";
        var hasMaskLayer = frameGroup.Layers > 1 && appearance is not null;
        var useEffectShader = OutfitShaderUtil.IsEffectShader(shader);
        var useGpuPath = (hasMaskLayer && colors.HasValue) || (appearance is not null && useEffectShader);
        var partColors = colors ?? new OutfitColorLayout(Color.White, Color.White, Color.White, Color.White);
        var shaderAvailable = _renderer.Shaders.TryGet(shader, out _);
        var resolvedShader = shaderAvailable ? shader : "outfit_default";
        var hasSpriteShader = !string.IsNullOrEmpty(spriteShader);

        for (var cellY = 0u; cellY < frameGroup.Height; cellY++)
        {
            for (var cellX = 0u; cellX < frameGroup.Width; cellX++)
            {
                // Iterate right-to-left (column-reversed) for correct NyxClient draw order.
                var screenX = originX + (frameGroup.Width - 1 - cellX) * stride;
                var screenY = originY + (frameGroup.Height - 1 - cellY) * stride;

                var drewColoredBase = false;
                if (useGpuPath &&
                    frameGroup.TryGetSpriteId(cellX, cellY, 0, patternX, patternY, patternZ, frame, out var baseSpriteId) && baseSpriteId != 0)
                {
                    var maskSpriteId = baseSpriteId;
                    if (hasMaskLayer)
                    {
                        if (!frameGroup.TryGetSpriteId(cellX, cellY, 1, patternX, patternY, patternZ, frame, out maskSpriteId) || maskSpriteId == 0)
                            maskSpriteId = 0;
                    }

                    if (maskSpriteId != 0)
                    {
                        var baseData = _renderer.IsSpriteResident((int)baseSpriteId)
                            ? ReadOnlySpan<byte>.Empty
                            : _assets.TryDecodeSpriteById(baseSpriteId, decodeBuffer) ? decodeBuffer : ReadOnlySpan<byte>.Empty;
                        var maskData = _renderer.IsSpriteResident((int)maskSpriteId)
                            ? ReadOnlySpan<byte>.Empty
                            : _assets.TryDecodeSpriteById(maskSpriteId, maskDecodeBuffer) ? maskDecodeBuffer : ReadOnlySpan<byte>.Empty;
                        _renderer.LoadSpritePair((int)baseSpriteId, baseData, (int)maskSpriteId, maskData);

                        // First attempt: GPU outfit compositing (shader-based palette remapping).
                        if (_renderer.TryDrawOutfitLayers(
                                (int)baseSpriteId, (int)maskSpriteId, screenX, screenY,
                                partColors.Head, partColors.Body, partColors.Legs, partColors.Feet,
                                resolvedShader,
                                paletteFromMask: hasMaskLayer))
                        {
                            drewColoredBase = true;
                        }
                        // Fallback: CPU compositing via multiplicative tint on decoded pixels.
                        else if (hasMaskLayer && colors.HasValue &&
                                 OutfitColorCompositor.TryComposite(_assets, baseSpriteId, maskSpriteId, colors.Value, decodeBuffer))
                        {
                            var compositeId = OutfitColorCompositor.CompositeSpriteId(baseSpriteId, maskSpriteId);
                            if (_renderer.TryDraw(compositeId, decodeBuffer, screenX, screenY))
                                drewColoredBase = true;
                        }

                        // Draw addon layers (layer 2+) on top of the composited base.
                        if (drewColoredBase)
                        {
                            for (var layer = 2u; layer < frameGroup.Layers; layer++)
                            {
                                if (!frameGroup.TryGetSpriteId(cellX, cellY, layer, patternX, patternY, patternZ, frame, out var addonSpriteId) || addonSpriteId == 0)
                                    continue;
                                DrawSpriteCell(addonSpriteId, screenX, screenY, decodeBuffer, hasSpriteShader, spriteShader);
                            }
                        }
                    }
                }

                if (drewColoredBase)
                    continue;

                // Standard (non-colored) path: draw all or partial layers.
                // When colors are present, layer 1 (mask) is consumed by the color path above.
                var layerCount = appearance is not null && frameGroup.Layers > 1 ? 1u : frameGroup.Layers;
                for (var layer = 0u; layer < layerCount; layer++)
                {
                    if (!frameGroup.TryGetSpriteId(cellX, cellY, layer, patternX, patternY, patternZ, frame, out var spriteId) || spriteId == 0)
                        continue;
                    DrawSpriteCell(spriteId, screenX, screenY, decodeBuffer, hasSpriteShader, spriteShader);
                }
            }
        }
    }

    /// <summary>
    /// Draws a single sprite cell.  If already in the GPU atlas, uses the resident draw path;
    /// otherwise decodes from the asset bundle, uploads, and draws.
    /// </summary>
    private void DrawSpriteCell(uint spriteId, float screenX, float screenY, Span<byte> decodeBuffer, bool hasSpriteShader, string? spriteShader)
    {
        var r = Sprite.Resident((int)spriteId);
        var success = hasSpriteShader
            ? _renderer.TryDrawSpriteEffect(in r, screenX, screenY, spriteShader!)
            : _renderer.TryDraw(in r, screenX, screenY);

        if (success)
            return;

        if (!_assets.TryDecodeSpriteById(spriteId, decodeBuffer))
            return;
        if (hasSpriteShader)
            _renderer.TryDrawSpriteEffect((int)spriteId, decodeBuffer, screenX, screenY, spriteShader!);
        else
            _renderer.TryDraw((int)spriteId, decodeBuffer, screenX, screenY);
    }
}
