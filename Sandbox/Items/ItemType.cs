using NyxAssets.Things;

namespace Sandbox.Items;

/// <summary>
/// One Nyx client item id: gameplay flags from <c>.dat</c> plus optional metadata from <c>ThingType.ExtraProperties</c>
/// (see <see cref="ItemsManager"/>). Primary frame-group sprite tensor is copied from NyxAssets at load time.
/// </summary>
public readonly struct ItemType : IEquatable<ItemType>
{
    public const ushort DefaultMaxStack = 100;

    public uint DatId { get; init; }

	public byte MinimapColor { get; init; }

    public bool Stackable { get; init; }

    public bool IsContainer { get; init; }

    /// <summary>Client <c>Cloth</c> flag — item is worn equipment.</summary>
    public bool IsCloth { get; init; }

    /// <summary>Raw client <c>ClothSlot</c> when <see cref="IsCloth"/>.</summary>
    public uint ClothSlot { get; init; }

    /// <summary>Resolved wear slot (dat cloth + optional <c>slot</c> from ExtraProperties).</summary>
    public EquipmentSlot? RequiredEquipmentSlot { get; init; }

    public bool IsUnpassable { get; init; }

    public bool BlockMissile { get; init; }

    public bool BlockPathfind { get; init; }

    public ushort MaxStack { get; init; }

    /// <summary>Inner grid size for container items from ExtraProperties <c>max-slots</c>; 0 uses <see cref="ItemPlacementRules.DefaultContainerCapacity"/>.</summary>
    public int MaxSlots { get; init; }

    /// <summary>Resolved container grid capacity for this type.</summary>
    public int ContainerCapacity =>
        MaxSlots > 0 ? MaxSlots : ItemPlacementRules.DefaultContainerCapacity;

    /// <summary>NyxClient <c>Thing::getStackPriority</c> (ground → border → bottom → top → common).</summary>
    public int StackPriority { get; init; }

    public string DisplayName { get; init; }

    public int Attack { get; init; }

    public int Armor { get; init; }

    public float Weight { get; init; }

    public string Description { get; init; }

    public bool FloorChange { get; init; }

    public string? FloorChangeDirection { get; init; }

    /// <summary>Flat sprite id tensor for frame group 0 (same layout as <see cref="ThingFrameGroup.SpriteIds"/>).</summary>
    public uint[]? PrimarySpriteIds { get; init; }

    /// <summary>Frame group 0 dimensions (see <see cref="ThingFrameGroup"/>).</summary>
    public uint PrimaryWidth { get; init; }

    public uint PrimaryHeight { get; init; }

    public uint PrimaryLayers { get; init; }

    public uint PrimaryPatternX { get; init; }

    public uint PrimaryPatternY { get; init; }

    public uint PrimaryPatternZ { get; init; }

    public uint PrimaryFrames { get; init; }

    public static ItemType None => default;

    public bool IsNone => DatId == 0;

    public static ItemType FromThing(ThingType thing)
    {
        ThingFrameGroup? fg = thing.FrameGroups.Count > 0 ? thing.FrameGroups[0] : null;
        uint[]? spriteIds = null;
        uint w = 1, h = 1, layers = 0, px = 0, py = 0, pz = 0, frames = 0;
        if (fg is { SpriteIds.Length: > 0 })
        {
            spriteIds = (uint[])fg.SpriteIds.Clone();
            w = fg.Width == 0 ? 1u : fg.Width;
            h = fg.Height == 0 ? 1u : fg.Height;
            layers = fg.Layers;
            px = fg.PatternX;
            py = fg.PatternY;
            pz = fg.PatternZ;
            frames = fg.Frames;
        }

        var ep = thing.ExtraProperties;
        var displayName = ep.GetString("name");
        var description = ep.GetString("description");
        var attack = ep.GetInt("attack");
        var armor = ep.GetInt("armor");
        var weight = ep.GetFloat("weight");
        var maxSlots = ep.GetInt("max-slots");
        var floorChangeDirection = ep.GetString("floorchange");

        EquipmentSlot? slotOverride = null;
        if (EquipmentSlotMapping.TryParse(ep.GetString("slot"), out var parsed))
            slotOverride = parsed;

        return new ItemType
        {
            DatId = thing.Id,
			MinimapColor = (byte)thing.MiniMapColor,
            Stackable = thing.Stackable,
            IsContainer = thing.IsContainer,
            IsCloth = thing.Cloth,
            ClothSlot = thing.ClothSlot,
            RequiredEquipmentSlot = slotOverride ?? (thing.Cloth ? EquipmentSlotMapping.FromClothSlot(thing.ClothSlot) : null),
            IsUnpassable = thing.IsUnpassable,
            BlockMissile = thing.BlockMissile,
            BlockPathfind = thing.BlockPathfind,
            MaxStack = thing.Stackable ? DefaultMaxStack : (ushort)1,
            MaxSlots = maxSlots,
            StackPriority = StackPriorityFromThing(thing),
            DisplayName = displayName,
            Attack = attack,
            Armor = armor,
            Weight = weight,
            Description = description,
            FloorChange = thing.FloorChange,
            FloorChangeDirection = floorChangeDirection,
            PrimarySpriteIds = spriteIds,
            PrimaryWidth = w,
            PrimaryHeight = h,
            PrimaryLayers = layers,
            PrimaryPatternX = px,
            PrimaryPatternY = py,
            PrimaryPatternZ = pz,
            PrimaryFrames = frames,
        };
    }

    private static int StackPriorityFromThing(ThingType thing)
    {
        if (thing.IsGround)
            return 0;
        if (thing.IsGroundBorder)
            return 1;
        if (thing.IsOnBottom)
            return 2;
        if (thing.IsOnTop)
            return 3;
        return 5;
    }

    /// <summary>Same indexing as <see cref="ThingFrameGroup.TryGetSpriteId(uint, uint, uint, uint, uint, uint, out uint)"/> for frame group 0.</summary>
    public bool TryGetPrimarySpriteId(
        uint innerWidth,
        uint innerHeight,
        uint layer,
        uint patternX,
        uint patternY,
        uint patternZ,
        uint frame,
        out uint spriteId)
    {
        spriteId = 0;
        var ids = PrimarySpriteIds;
        if (ids is null || ids.Length == 0)
            return false;

        var i = GetSpriteIndex(
            PrimaryWidth,
            PrimaryHeight,
            PrimaryLayers,
            PrimaryPatternX,
            PrimaryPatternY,
            PrimaryPatternZ,
            PrimaryFrames,
            innerWidth,
            innerHeight,
            layer,
            patternX,
            patternY,
            patternZ,
            frame);

        if (i >= (uint)ids.Length)
            return false;

        spriteId = ids[i];
        return true;
    }

    /// <inheritdoc cref="TryGetPrimarySpriteId(uint, uint, uint, uint, uint, uint, uint, out uint)"/>
    public bool TryGetPrimarySpriteId(uint layer, uint patternX, uint patternY, uint patternZ, uint frame, out uint spriteId) =>
        TryGetPrimarySpriteId(0, 0, layer, patternX, patternY, patternZ, frame, out spriteId);

    private static uint GetSpriteIndex(
        uint width,
        uint height,
        uint layers,
        uint patternXCount,
        uint patternYCount,
        uint patternZCount,
        uint framesCount,
        uint innerWidth,
        uint innerHeight,
        uint layer,
        uint patternX,
        uint patternY,
        uint patternZ,
        uint frame)
    {
        var f = framesCount != 0 ? frame % framesCount : 0u;
        var i = f * patternZCount + patternZ;
        i = i * patternYCount + patternY;
        i = i * patternXCount + patternX;
        i = i * layers + layer;
        i = i * height + innerHeight;
        i = i * width + innerWidth;
        return i;
    }

    public string GetDisplayLabel() =>
        string.IsNullOrEmpty(DisplayName) ? $"Item {DatId}" : DisplayName;

    public bool Equals(ItemType other) => DatId == other.DatId;

    public override bool Equals(object? obj) => obj is ItemType other && Equals(other);

    public override int GetHashCode() => (int)DatId;

    public static bool operator ==(ItemType left, ItemType right) => left.Equals(right);

    public static bool operator !=(ItemType left, ItemType right) => !left.Equals(right);

    public override string ToString() => IsNone ? "ItemType.None" : $"ItemType({DatId})";
}
