namespace Sandbox.Items;

/// <summary>Maps Nyx client <c>ClothSlot</c> (NyxClient <c>InventorySlot*</c>) to <see cref="EquipmentSlot"/>.</summary>
public static class EquipmentSlotMapping
{
    /// <summary>NyxClient: Head=1 … Ammo=10.</summary>
    public static EquipmentSlot? FromClothSlot(uint clothSlot) =>
        clothSlot switch
        {
            1 => EquipmentSlot.Head,
            2 => EquipmentSlot.Necklace,
            3 => EquipmentSlot.Backpack,
            4 => EquipmentSlot.Body,
            5 => EquipmentSlot.RightHand,
            6 => EquipmentSlot.LeftHand,
            7 => EquipmentSlot.Legs,
            8 => EquipmentSlot.Feet,
            9 => EquipmentSlot.Ring,
            10 => EquipmentSlot.Ammo,
            _ => null,
        };

    public static bool TryParse(string? value, out EquipmentSlot slot)
    {
        slot = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        switch (value.Trim().ToLowerInvariant().Replace("_", "-"))
        {
            case "head":
            case "helmet":
                slot = EquipmentSlot.Head;
                return true;
            case "neck":
            case "necklace":
                slot = EquipmentSlot.Necklace;
                return true;
            case "back":
            case "backpack":
            case "bag":
                slot = EquipmentSlot.Backpack;
                return true;
            case "body":
            case "armor":
            case "torso":
                slot = EquipmentSlot.Body;
                return true;
            case "right":
            case "right-hand":
            case "righthand":
                slot = EquipmentSlot.RightHand;
                return true;
            case "left":
            case "left-hand":
            case "lefthand":
                slot = EquipmentSlot.LeftHand;
                return true;
            case "legs":
            case "leg":
                slot = EquipmentSlot.Legs;
                return true;
            case "feet":
            case "boots":
            case "shoes":
                slot = EquipmentSlot.Feet;
                return true;
            case "ring":
            case "finger":
                slot = EquipmentSlot.Ring;
                return true;
            case "ammo":
            case "ammunition":
                slot = EquipmentSlot.Ammo;
                return true;
            default:
                return false;
        }
    }
}
