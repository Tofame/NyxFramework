# NyxDrawer

Nyx / NyxClient–style **thing drawing** on top of [NyxAssets](../NyxAssets) (`.dat` / `.spr`) and [NyxRender](../NyxRender) (GPU sprites).

## Layers

| Project | Role |
|---------|------|
| **NyxRender** | 32×32 sprites, atlases, outfit shaders |
| **NyxAssets** | Thing types, frame groups, sprite decode |
| **NyxDrawer** | *Where* and *how* to draw creatures, items, effects, missiles |
| **Sandbox** | Demo app (map, player, config) |

## Entry point

```csharp
var drawer = new AssetDrawer(assets, renderer);
drawer.Preloader.Preload(outfitThing);

drawer.Creatures.Draw(new CreatureDrawRequest
{
    Outfit = outfitThing,
    Mount = mountThing,
    Mounted = player.IsMounted,
    Appearance = appearance,
    AnchorX = px,
    AnchorY = py,
    Direction = dir,
    WalkPhase = walkPhase,
});

drawer.Items.Draw(new ItemDrawRequest { Item = item, AnchorX = x, AnchorY = y, Frame = 0 });

drawer.Effects.Draw(new EffectDrawRequest
{
    Effect = effectThing,
    AnchorX = x,
    AnchorY = y,
    TileX = tileX,
    TileY = tileY,
    Frame = drawer.Animator.GetEffectFrame(effectThing, elapsedMs),
});

drawer.DistanceEffects.Draw(new DistanceEffectDrawRequest
{
    Missile = missileThing,
    FromAnchorX = fromX,
    FromAnchorY = fromY,
    TileDeltaX = dx,
    TileDeltaY = dy,
    Progress = elapsed / DistanceEffectDrawer.DurationMs(dx, dy),
});
```

Register outfit shaders on `renderer.Shaders` before drawing creatures (see Sandbox `SandboxEffectShaders`).

**Memory:** NyxRender keeps only a **working set** of sprites in GPU atlases (LRU + idle eviction). Draw without `Preloader` — decode from NyxAssets on first use; evicted ids are re-uploaded on the next draw.
