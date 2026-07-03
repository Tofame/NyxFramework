using System.Text;
using NyxAssets.Things;

namespace NyxAssets.Data.Readers;

/// <summary>Reads the property-flag byte stream for one <see cref="ThingType"/> (Asset Editor <c>MetadataReader*</c>).</summary>
internal static class ThingPropertyDecoder
{
    private const byte LastFlag = 0xFF;

    public static void Read(ref LittleEndianSpanReader reader, ThingType thing, DatThingFormat format)
    {
        switch (format)
        {
            case DatThingFormat.V1_7_10__7_30:
                ReadV1(ref reader, thing);
                return;
            case DatThingFormat.V2_7_40__7_50:
                ReadV2(ref reader, thing);
                return;
            case DatThingFormat.V3_7_55__7_72:
                ReadV3(ref reader, thing);
                return;
            case DatThingFormat.V4_7_80__8_54:
                ReadV4(ref reader, thing);
                return;
            case DatThingFormat.V5_8_60__9_86:
                ReadV5(ref reader, thing);
                return;
            case DatThingFormat.V6_10_10__10_56:
                ReadV6(ref reader, thing);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static void ThrowUnknown(byte flag, byte previous, ThingType thing) =>
        throw new InvalidDataException(
            $"Unknown .dat flag 0x{flag:X2} after 0x{previous:X2} for {thing.Kind} id {thing.Id}.");

    private static string ReadMarketName(ref LittleEndianSpanReader reader)
    {
        var len = reader.ReadU16();
        var bytes = reader.ReadBytes(len);
        return Encoding.Latin1.GetString(bytes);
    }

    private static void ReadV1(ref LittleEndianSpanReader r, ThingType t)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = r.ReadU8();
            if (flag == LastFlag)
                return;
            switch (flag)
            {
                case 0x00: t.IsGround = true; t.GroundSpeed = r.ReadU16(); break;
                case 0x01: t.IsOnBottom = true; break;
                case 0x02: t.IsOnTop = true; break;
                case 0x03: t.IsContainer = true; break;
                case 0x04: t.Stackable = true; break;
                case 0x05: t.MultiUse = true; break;
                case 0x06: t.ForceUse = true; break;
                case 0x07: t.Writable = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x08: t.WritableOnce = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x09: t.IsFluidContainer = true; break;
                case 0x0A: t.IsFluid = true; break;
                case 0x0B: t.IsUnpassable = true; break;
                case 0x0C: t.IsUnmoveable = true; break;
                case 0x0D: t.BlockMissile = true; break;
                case 0x0E: t.BlockPathfind = true; break;
                case 0x0F: t.Pickupable = true; break;
                case 0x10: t.HasLight = true; t.LightLevel = r.ReadU16(); t.LightColor = r.ReadU16(); break;
                case 0x11: t.FloorChange = true; break;
                case 0x12: t.IsFullGround = true; break;
                case 0x13: t.HasElevation = true; t.Elevation = r.ReadU16(); break;
                case 0x14: t.HasOffset = true; t.OffsetX = 8; t.OffsetY = 8; break;
                case 0x16: t.MiniMap = true; t.MiniMapColor = r.ReadU16(); break;
                case 0x17: t.Rotatable = true; break;
                case 0x18: t.IsLyingObject = true; break;
                case 0x19: t.AnimateAlways = true; break;
                case 0x1A: t.IsLensHelp = true; t.LensHelp = r.ReadU16(); break;
                case 0x24: t.Wrappable = true; break;
                case 0x25: t.Unwrappable = true; break;
                case 0x26: t.BottomEffect = true; break;
                default: ThrowUnknown(flag, previous, t); break;
            }
        }
    }

    private static void ReadV2(ref LittleEndianSpanReader r, ThingType t)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = r.ReadU8();
            if (flag == LastFlag)
                return;
            switch (flag)
            {
                case 0x00: t.IsGround = true; t.GroundSpeed = r.ReadU16(); break;
                case 0x01: t.IsOnBottom = true; break;
                case 0x02: t.IsOnTop = true; break;
                case 0x03: t.IsContainer = true; break;
                case 0x04: t.Stackable = true; break;
                case 0x05: t.MultiUse = true; break;
                case 0x06: t.ForceUse = true; break;
                case 0x07: t.Writable = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x08: t.WritableOnce = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x09: t.IsFluidContainer = true; break;
                case 0x0A: t.IsFluid = true; break;
                case 0x0B: t.IsUnpassable = true; break;
                case 0x0C: t.IsUnmoveable = true; break;
                case 0x0D: t.BlockMissile = true; break;
                case 0x0E: t.BlockPathfind = true; break;
                case 0x0F: t.Pickupable = true; break;
                case 0x10: t.HasLight = true; t.LightLevel = r.ReadU16(); t.LightColor = r.ReadU16(); break;
                case 0x11: t.FloorChange = true; break;
                case 0x12: t.IsFullGround = true; break;
                case 0x13: t.HasElevation = true; t.Elevation = r.ReadU16(); break;
                case 0x14: t.HasOffset = true; t.OffsetX = 8; t.OffsetY = 8; break;
                case 0x16: t.MiniMap = true; t.MiniMapColor = r.ReadU16(); break;
                case 0x17: t.Rotatable = true; break;
                case 0x18: t.IsLyingObject = true; break;
                case 0x19: t.Hangable = true; break;
                case 0x1A: t.IsVertical = true; break;
                case 0x1B: t.IsHorizontal = true; break;
                case 0x1C: t.AnimateAlways = true; break;
                case 0x1D: t.IsLensHelp = true; t.LensHelp = r.ReadU16(); break;
                case 0x24: t.Wrappable = true; break;
                case 0x25: t.Unwrappable = true; break;
                case 0x26: t.BottomEffect = true; break;
                default: ThrowUnknown(flag, previous, t); break;
            }
        }
    }

    private static void ReadV3(ref LittleEndianSpanReader r, ThingType t)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = r.ReadU8();
            if (flag == LastFlag)
                return;
            switch (flag)
            {
                case 0x00: t.IsGround = true; t.GroundSpeed = r.ReadU16(); break;
                case 0x01: t.IsGroundBorder = true; break;
                case 0x02: t.IsOnBottom = true; break;
                case 0x03: t.IsOnTop = true; break;
                case 0x04: t.IsContainer = true; break;
                case 0x05: t.Stackable = true; break;
                case 0x06: t.ForceUse = true; break;
                case 0x07: t.MultiUse = true; break;
                case 0x08: t.Writable = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x09: t.WritableOnce = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x0A: t.IsFluidContainer = true; break;
                case 0x0B: t.IsFluid = true; break;
                case 0x0C: t.IsUnpassable = true; break;
                case 0x0D: t.IsUnmoveable = true; break;
                case 0x0E: t.BlockMissile = true; break;
                case 0x0F: t.BlockPathfind = true; break;
                case 0x10: t.Pickupable = true; break;
                case 0x11: t.Hangable = true; break;
                case 0x12: t.IsVertical = true; break;
                case 0x13: t.IsHorizontal = true; break;
                case 0x14: t.Rotatable = true; break;
                case 0x15: t.HasLight = true; t.LightLevel = r.ReadU16(); t.LightColor = r.ReadU16(); break;
                case 0x17: t.FloorChange = true; break;
                case 0x18: t.HasOffset = true; t.OffsetX = r.ReadI16(); t.OffsetY = r.ReadI16(); break;
                case 0x19: t.HasElevation = true; t.Elevation = r.ReadU16(); break;
                case 0x1A: t.IsLyingObject = true; break;
                case 0x1B: t.AnimateAlways = true; break;
                case 0x1C: t.MiniMap = true; t.MiniMapColor = r.ReadU16(); break;
                case 0x1D: t.IsLensHelp = true; t.LensHelp = r.ReadU16(); break;
                case 0x1E: t.IsFullGround = true; break;
                default: ThrowUnknown(flag, previous, t); break;
            }
        }
    }

    private static void ReadV4(ref LittleEndianSpanReader r, ThingType t)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = r.ReadU8();
            if (flag == LastFlag)
                return;
            switch (flag)
            {
                case 0x00: t.IsGround = true; t.GroundSpeed = r.ReadU16(); break;
                case 0x01: t.IsGroundBorder = true; break;
                case 0x02: t.IsOnBottom = true; break;
                case 0x03: t.IsOnTop = true; break;
                case 0x04: t.IsContainer = true; break;
                case 0x05: t.Stackable = true; break;
                case 0x06: t.ForceUse = true; break;
                case 0x07: t.MultiUse = true; break;
                case 0x08: t.HasCharges = true; break;
                case 0x09: t.Writable = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x0A: t.WritableOnce = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x0B: t.IsFluidContainer = true; break;
                case 0x0C: t.IsFluid = true; break;
                case 0x0D: t.IsUnpassable = true; break;
                case 0x0E: t.IsUnmoveable = true; break;
                case 0x0F: t.BlockMissile = true; break;
                case 0x10: t.BlockPathfind = true; break;
                case 0x11: t.Pickupable = true; break;
                case 0x12: t.Hangable = true; break;
                case 0x13: t.IsVertical = true; break;
                case 0x14: t.IsHorizontal = true; break;
                case 0x15: t.Rotatable = true; break;
                case 0x16: t.HasLight = true; t.LightLevel = r.ReadU16(); t.LightColor = r.ReadU16(); break;
                case 0x17: t.DontHide = true; break;
                case 0x18: t.FloorChange = true; break;
                case 0x19: t.HasOffset = true; t.OffsetX = r.ReadI16(); t.OffsetY = r.ReadI16(); break;
                case 0x1A: t.HasElevation = true; t.Elevation = r.ReadU16(); break;
                case 0x1B: t.IsLyingObject = true; break;
                case 0x1C: t.AnimateAlways = true; break;
                case 0x1D: t.MiniMap = true; t.MiniMapColor = r.ReadU16(); break;
                case 0x1E: t.IsLensHelp = true; t.LensHelp = r.ReadU16(); break;
                case 0x1F: t.IsFullGround = true; break;
                case 0x20: t.IgnoreLook = true; break;
                default: ThrowUnknown(flag, previous, t); break;
            }
        }
    }

    private static void ReadV5(ref LittleEndianSpanReader r, ThingType t)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = r.ReadU8();
            if (flag == LastFlag)
                return;
            switch (flag)
            {
                case 0x00: t.IsGround = true; t.GroundSpeed = r.ReadU16(); break;
                case 0x01: t.IsGroundBorder = true; break;
                case 0x02: t.IsOnBottom = true; break;
                case 0x03: t.IsOnTop = true; break;
                case 0x04: t.IsContainer = true; break;
                case 0x05: t.Stackable = true; break;
                case 0x06: t.ForceUse = true; break;
                case 0x07: t.MultiUse = true; break;
                case 0x08: t.Writable = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x09: t.WritableOnce = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x0A: t.IsFluidContainer = true; break;
                case 0x0B: t.IsFluid = true; break;
                case 0x0C: t.IsUnpassable = true; break;
                case 0x0D: t.IsUnmoveable = true; break;
                case 0x0E: t.BlockMissile = true; break;
                case 0x0F: t.BlockPathfind = true; break;
                case 0x10: t.Pickupable = true; break;
                case 0x11: t.Hangable = true; break;
                case 0x12: t.IsVertical = true; break;
                case 0x13: t.IsHorizontal = true; break;
                case 0x14: t.Rotatable = true; break;
                case 0x15: t.HasLight = true; t.LightLevel = r.ReadU16(); t.LightColor = r.ReadU16(); break;
                case 0x16: t.DontHide = true; break;
                case 0x17: t.IsTranslucent = true; break;
                case 0x18: t.HasOffset = true; t.OffsetX = r.ReadI16(); t.OffsetY = r.ReadI16(); break;
                case 0x19: t.HasElevation = true; t.Elevation = r.ReadU16(); break;
                case 0x1A: t.IsLyingObject = true; break;
                case 0x1B: t.AnimateAlways = true; break;
                case 0x1C: t.MiniMap = true; t.MiniMapColor = r.ReadU16(); break;
                case 0x1D: t.IsLensHelp = true; t.LensHelp = r.ReadU16(); break;
                case 0x1E: t.IsFullGround = true; break;
                case 0x1F: t.IgnoreLook = true; break;
                case 0x20: t.Cloth = true; t.ClothSlot = r.ReadU16(); break;
                case 0x21:
                    t.IsMarketItem = true;
                    t.MarketCategory = r.ReadU16();
                    t.MarketTradeAs = r.ReadU16();
                    t.MarketShowAs = r.ReadU16();
                    t.MarketName = ReadMarketName(ref r);
                    t.MarketRestrictProfession = r.ReadU16();
                    t.MarketRestrictLevel = r.ReadU16();
                    break;
                case 0x24: t.Wrappable = true; break;
                case 0x25: t.Unwrappable = true; break;
                case 0x26: t.BottomEffect = true; break;
                case 0x28: t.DontCenterOutfit = true; break;
                default: ThrowUnknown(flag, previous, t); break;
            }
        }
    }

    private static void ReadV6(ref LittleEndianSpanReader r, ThingType t)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = r.ReadU8();
            if (flag == LastFlag)
                return;
            switch (flag)
            {
                case 0x00: t.IsGround = true; t.GroundSpeed = r.ReadU16(); break;
                case 0x01: t.IsGroundBorder = true; break;
                case 0x02: t.IsOnBottom = true; break;
                case 0x03: t.IsOnTop = true; break;
                case 0x04: t.IsContainer = true; break;
                case 0x05: t.Stackable = true; break;
                case 0x06: t.ForceUse = true; break;
                case 0x07: t.MultiUse = true; break;
                case 0x08: t.Writable = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x09: t.WritableOnce = true; t.MaxTextLength = r.ReadU16(); break;
                case 0x0A: t.IsFluidContainer = true; break;
                case 0x0B: t.IsFluid = true; break;
                case 0x0C: t.IsUnpassable = true; break;
                case 0x0D: t.IsUnmoveable = true; break;
                case 0x0E: t.BlockMissile = true; break;
                case 0x0F: t.BlockPathfind = true; break;
                case 0x10: t.NoMoveAnimation = true; break;
                case 0x11: t.Pickupable = true; break;
                case 0x12: t.Hangable = true; break;
                case 0x13: t.IsVertical = true; break;
                case 0x14: t.IsHorizontal = true; break;
                case 0x15: t.Rotatable = true; break;
                case 0x16: t.HasLight = true; t.LightLevel = r.ReadU16(); t.LightColor = r.ReadU16(); break;
                case 0x17: t.DontHide = true; break;
                case 0x18: t.IsTranslucent = true; break;
                case 0x19: t.HasOffset = true; t.OffsetX = r.ReadI16(); t.OffsetY = r.ReadI16(); break;
                case 0x1A: t.HasElevation = true; t.Elevation = r.ReadU16(); break;
                case 0x1B: t.IsLyingObject = true; break;
                case 0x1C: t.AnimateAlways = true; break;
                case 0x1D: t.MiniMap = true; t.MiniMapColor = r.ReadU16(); break;
                case 0x1E: t.IsLensHelp = true; t.LensHelp = r.ReadU16(); break;
                case 0x1F: t.IsFullGround = true; break;
                case 0x20: t.IgnoreLook = true; break;
                case 0x21: t.Cloth = true; t.ClothSlot = r.ReadU16(); break;
                case 0x22:
                    t.IsMarketItem = true;
                    t.MarketCategory = r.ReadU16();
                    t.MarketTradeAs = r.ReadU16();
                    t.MarketShowAs = r.ReadU16();
                    t.MarketName = ReadMarketName(ref r);
                    t.MarketRestrictProfession = r.ReadU16();
                    t.MarketRestrictLevel = r.ReadU16();
                    break;
                case 0x23: t.HasDefaultAction = true; t.DefaultAction = r.ReadU16(); break;
                case 0x24: t.Wrappable = true; break;
                case 0x25: t.Unwrappable = true; break;
                case 0x26: t.BottomEffect = true; break;
                case 0xFE: t.Usable = true; break;
                default: ThrowUnknown(flag, previous, t); break;
            }
        }
    }
}
