# Format Specification: `things.json` (Thing Metadata)

> **Maintainers:** JSON serialization is implemented in `ThingTypeJsonMapper`. See [development/json-mapper.md](../development/json-mapper.md) for how to add or change fields.

The `things.json` file is a modern JSON-based metadata format used by `NyxFramework` as a successor to Nyx's legacy `Nyx.dat` file and SERVER server-specific `items.xml` files. It catalogs items, outfits, effects, and missiles in a single, human-readable file, supporting nested custom property serialization.

> **Single-thing exports** use the [`nyx-thing`](nyx-thing-json.md) envelope (`type` + one object, optional embedded sprites) instead of these four arrays. See [development/thing-exchange.md](../development/thing-exchange.md).

---

## 1. High-Level Structure

The file consists of a single root object containing four primary arrays:

```json
{
  "items": [],
  "outfits": [],
  "effects": [],
  "missiles": []
}
```

Each entry in these arrays represents a `ThingType` definition.

---

## 2. Thing Type Object Fields

Only non-default values are written to optimize file size.

### Identifiers
* **`id`** (`integer`): Unique ID of the item, outfit, effect, or missile.

### Boolean Flags (Property Flags)
These represent attributes mapped from original `.dat` flag bytes. They are only present if their value is `true`.
* **`isGround`**: True if the item is a walkable ground tile.
* **`isGroundBorder`**: True if the item is a border overlay.
* **`isOnBottom`**: True if drawn directly on top of ground borders (e.g., gravel, grass patches).
* **`isOnTop`**: True if drawn on top of other objects (e.g., wall signs, pictures).
* **`isContainer`**: True if the item behaves as a container (backpacks, depots).
* **`stackable`**: True if multiple quantities of this item can be stacked together.
* **`forceUse`**: True if client must force use action.
* **`multiUse`**: True if the item can be targeted (e.g., runes, ropes).
* **`hasCharges`**: True if the item has charges or count.
* **`writable`**: True if the item is editable (e.g., letters, blackboards).
* **`writableOnce`**: True if it can only be written to once.
* **`isFluidContainer`**: True if the item can hold fluids (vials, mugs).
* **`isFluid`**: True if the item represents a fluid source.
* **`isUnpassable`**: True if blocks walking.
* **`isUnmoveable`**: True if cannot be picked up or dragged.
* **`blockMissile`**: True if blocks projectiles (e.g., walls).
* **`blockPathfind`**: True if blocks pathfinding.
* **`noMoveAnimation`**: True if moving does not trigger animation.
* **`pickupable`**: True if can be picked up.
* **`hangable`**: True if can be hung on walls.
* **`isVertical`**: True if the item represents a vertical wall piece.
* **`isHorizontal`**: True if the item represents a horizontal wall piece.
* **`rotatable`**: True if can be rotated.
* **`hasLight`**: True if emits light.
* **`dontHide`**: True if it should not be hidden behind taller structures.
* **`isTranslucent`**: True if drawn with transparency overlay.
* **`floorChange`**: True if steps up/down floors.
* **`hasOffset`**: True if drawn offset from tile boundaries.
* **`hasElevation`**: True if elevated off the floor.
* **`isLyingObject`**: True if drawn flat on the ground.
* **`animateAlways`**: True if animated even when static on screen.
* **`miniMap`**: True if shown on minimap.
* **`isLensHelp`**: True if shows lens description.
* **`isFullGround`**: True if ground covers the entire 32x32 space.
* **`ignoreLook`**: True if ignore look command.
* **`cloth`**: True if behaves as equipment.
* **`isMarketItem`**: True if tradeable on the market.
* **`hasDefaultAction`**: True if has a default action (e.g. open, use).
* **`wrappable`**: True if can be wrapped.
* **`unwrappable`**: True if can be unwrapped.
* **`bottomEffect`**: True if effect is rendered on ground level.
* **`dontCenterOutfit`**: True if outfit offset centering is disabled.
* **`usable`**: True if can be used.

### Numeric Properties
Only present if non-zero.
* **`groundSpeed`**: Speed modifier for walking over this tile.
* **`maxTextLength`**: Maximum length of writable text.
* **`lightLevel`**: Radius of emitted light.
* **`lightColor`**: Color of emitted light.
* **`offsetX`**: Horizontal offset in pixels.
* **`offsetY`**: Vertical offset in pixels.
* **`elevation`**: Height elevation in pixels.
* **`miniMapColor`**: Hex/RGB color representation on minimaps.
* **`lensHelp`**: Help category code.
* **`clothSlot`**: Slot placement index for equipment.
* **`marketCategory`**: Market category grouping.
* **`marketTradeAs`**: Item ID this item is traded under.
* **`marketShowAs`**: Item ID shown in the market preview.
* **`marketRestrictProfession`**: Professional restrictions.
* **`marketRestrictLevel`**: Minimum level requirement.
* **`defaultAction`**: Default action type ID.

### String Properties
* **`marketName`**: Name displayed on the market interface.

---

## 3. Frame Groups (`frameGroups`)

Describes sprite configurations, dimensions, animations, and sprite references.

```json
"frameGroups": [
  {
    "width": 1,
    "height": 1,
    "exactSize": 32,
    "layers": 1,
    "patternX": 1,
    "patternY": 1,
    "patternZ": 1,
    "frames": 1,
    "groupTypeId": 0,
    "spriteIds": [1234, 1235]
  }
]
```

### Fields
* **`width`**: Number of horizontal 32x32 grid slices (default 1).
* **`height`**: Number of vertical 32x32 grid slices (default 1).
* **`exactSize`**: Target render size (default 32).
* **`layers`**: Texturing layer count (default 1).
* **`patternX` / `patternY` / `patternZ`**: Sprite layout dimensions (direction, mount riding state, etc.).
* **`frames`**: Number of animation frames.
* **`groupTypeId`**: Frame group category.
* **`isAnimation`**: True if the group animates.
* **`animationMode`**: Loop style descriptor.
* **`loopCount`**: Maximum loops.
* **`startFrame`**: Initial playback index.
* **`frameTimings`**: List of objects specifying min/max timings:
  ```json
  "frameTimings": [
    { "min": 100, "max": 120 }
  ]
  ```
* **`spriteIds`**: Array of 1-based index addresses corresponding to pixel blocks in `.spr` / `.assets`.

---

## 4. Custom Extra Properties (`properties`)

Enables flat keys from metadata loaders (like C++ `items.xml`) to be serialized into structured nested JSON properties.

For instance, the properties `field.value = "fire"` and `field.initdamage = 20` compile to:

```json
"properties": {
  "field": {
    "value": "fire",
    "initdamage": 20
  }
}
```

Values in this section are parsed dynamically back into C# primitive types (`bool`, `long`, `double`, or `string`) based on their token representation.
