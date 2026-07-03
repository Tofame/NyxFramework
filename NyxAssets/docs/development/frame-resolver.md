# Thing frame resolver

`NyxAssets.Things.Frames.ThingFrameResolver` turns high-level editor/game parameters (direction, walk phase, stack count, missile aim, …) into a **`ThingFrameSelection`**: the frame group, pattern axes, and animation frame you need before decoding `.spr` / `.assets` pixels.

NyxDrawer uses the same rules for in-game drawing; an asset editor can call the resolver directly without referencing GPU code.

## Quick examples

### Outfit (direction + walk + addons + mount)

```csharp
using NyxAssets.Things.Frames;

var selection = ThingFrameResolver.GetOutfitFrame(outfit, new OutfitFrameRequest
{
    Direction = (int)Direction4.South,
    WalkPhase = 2,
    AddonMask = 0b11,   // base + first two addon rows
    Mounted = true,
});

foreach (var slot in selection.EnumerateSpriteSlots())
{
    assets.TryDecodeSpriteById(slot.SpriteId, rgbaBuffer);
}
```

Preview **each addon layer separately** (same order as NyxDrawer):

```csharp
foreach (var slice in ThingFrameResolver.EnumerateOutfitAddonFrames(outfit, request))
{
    // slice.PatternY = 0 base, 1 first addon, …
}
```

**Mount + rider together:**

```csharp
foreach (var (thing, slice, isMount) in ThingFrameResolver.EnumerateMountedOutfitFrames(outfit, mountThing, request))
{
    // draw mount layers first, then rider addon slices
}
```

### Item (stack pile or explicit pattern)

```csharp
var selection = ThingFrameResolver.GetItemFrame(item, new ItemFrameRequest
{
    StackCount = 37,
    Frame = 0,
});

// Or override patterns manually (e.g. rotatable item):
var rotated = ThingFrameResolver.GetItemFrame(item, new ItemFrameRequest
{
    PatternX = 2,
    PatternY = 0,
});
```

Stack rules live in `ItemStackPatterns` (4×2 grid for stackable items).

### Effect (tile-varied + timed frame)

```csharp
var frame = ThingFrameResolver.GetEffectFrameIndex(effect, elapsedMs);
var selection = ThingFrameResolver.GetEffectFrame(effect, new EffectFrameRequest
{
    Frame = frame,
    TileX = 105,
    TileY = 88,
});
```

### Missile / distance effect (8 directions)

```csharp
var selection = ThingFrameResolver.GetMissileFrame(missile, new MissileFrameRequest
{
    Direction = Direction8.NorthEast,
});

// Or derive aim from tile delta:
var aimed = ThingFrameResolver.GetMissileFrame(missile, new MissileFrameRequest
{
    TileDeltaX = 3,
    TileDeltaY = -1,
});
```

Pattern mapping matches NyxClient `Missile::draw` — see `MissileDirectionPatterns`.

## API surface

| Type | Role |
|------|------|
| `ThingFrameResolver` | Main entry: `GetOutfitFrame`, `GetItemFrame`, `GetEffectFrame`, `GetMissileFrame`, … |
| `ThingFrameSelection` | Resolved slice; `EnumerateSpriteSlots()` / `GetSpriteIds()` |
| `OutfitFrameRequest` | Direction, walk phase, addon mask, mounted flag |
| `ItemFrameRequest` | Stack count or explicit pattern overrides |
| `EffectFrameRequest` | Frame + world tile for pattern variation |
| `MissileFrameRequest` | 8-way direction or tile delta |
| `Direction4` / `Direction8` | Facing enums |
| `ItemStackPatterns` | Stack-count → pattern grid |
| `MissileDirectionPatterns` | 8-way aim → pattern X/Y |

## Relationship to NyxDrawer

| NyxDrawer | NyxAssets |
|-----------|-----------|
| `ThingAnimator.ResolveWalkingFrame` | `ThingFrameResolver.ResolveWalkingFrame` |
| `ThingDrawGeometry.GetMountedPatternZ` | `ThingFrameResolver.GetMountedPatternZ` |
| `CreatureOutfitAppearance.HasAddonPattern` | `ThingFrameResolver.IsAddonPatternVisible` |
| `MissileDirectionPatterns` / `ItemStackPatterns` | Now live in NyxAssets (removed from NyxDrawer) |

NyxDrawer still owns **screen placement** and outfit colour compositing. NyxAssets owns **which sprite ids** belong to a logical frame.

## Adding new thing kinds or rules

1. Add a request struct in `ThingFrameRequests.cs` if needed.
2. Add a `Get*Frame` method on `ThingFrameResolver` following NyxClient / Asset Editor behaviour.
3. Add tests under `NyxAssets/Tests/ThingFrameResolverTests.cs`.
4. If NyxDrawer duplicates the rule, refactor it to call the resolver.

## See also

- [overview.md](overview.md) — project layout and extension points
- [formats/things-json.md](../formats/things-json.md) — `frameGroups` schema
- [guides/usage.md](../guides/usage.md) — loading assets and decoding sprites
