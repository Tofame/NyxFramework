using System.Text;
using NyxAssets.Data.Readers;
using NyxAssets.Data.Writers;

namespace NyxAssets.Things.Exchange;

/// <summary>Property-flag stream used in Object Builder OBD v2/v3 (matches <c>OBDEncoder.readProperties</c>).</summary>
internal static class ObdPropertyCodec
{
    private const byte LastFlag = 0xFF;
    private const byte HasCharges = 0xFC;
    private const byte FloorChange = 0xFD;
    private const byte Usable = 0xFE;

    public static void Read(ref LittleEndianSpanReader reader, ThingType thing)
    {
        for (var flag = (byte)0; flag < LastFlag;)
        {
            var previous = flag;
            flag = reader.ReadU8();
            if (flag == LastFlag)
                return;

            switch (flag)
            {
                case 0x00: thing.IsGround = true; thing.GroundSpeed = reader.ReadU16(); break;
                case 0x01: thing.IsGroundBorder = true; break;
                case 0x02: thing.IsOnBottom = true; break;
                case 0x03: thing.IsOnTop = true; break;
                case 0x04: thing.IsContainer = true; break;
                case 0x05: thing.Stackable = true; break;
                case 0x06: thing.ForceUse = true; break;
                case 0x07: thing.MultiUse = true; break;
                case 0x08: thing.Writable = true; thing.MaxTextLength = reader.ReadU16(); break;
                case 0x09: thing.WritableOnce = true; thing.MaxTextLength = reader.ReadU16(); break;
                case 0x0A: thing.IsFluidContainer = true; break;
                case 0x0B: thing.IsFluid = true; break;
                case 0x0C: thing.IsUnpassable = true; break;
                case 0x0D: thing.IsUnmoveable = true; break;
                case 0x0E: thing.BlockMissile = true; break;
                case 0x0F: thing.BlockPathfind = true; break;
                case 0x10: thing.NoMoveAnimation = true; break;
                case 0x11: thing.Pickupable = true; break;
                case 0x12: thing.Hangable = true; break;
                case 0x13: thing.IsVertical = true; break;
                case 0x14: thing.IsHorizontal = true; break;
                case 0x15: thing.Rotatable = true; break;
                case 0x16: thing.HasLight = true; thing.LightLevel = reader.ReadU16(); thing.LightColor = reader.ReadU16(); break;
                case 0x17: thing.DontHide = true; break;
                case 0x18: thing.IsTranslucent = true; break;
                case 0x19: thing.HasOffset = true; thing.OffsetX = reader.ReadI16(); thing.OffsetY = reader.ReadI16(); break;
                case 0x1A: thing.HasElevation = true; thing.Elevation = reader.ReadU16(); break;
                case 0x1B: thing.IsLyingObject = true; break;
                case 0x1C: thing.AnimateAlways = true; break;
                case 0x1D: thing.MiniMap = true; thing.MiniMapColor = reader.ReadU16(); break;
                case 0x1E: thing.IsLensHelp = true; thing.LensHelp = reader.ReadU16(); break;
                case 0x1F: thing.IsFullGround = true; break;
                case 0x20: thing.IgnoreLook = true; break;
                case 0x21: thing.Cloth = true; thing.ClothSlot = reader.ReadU16(); break;
                case 0x22:
                    thing.IsMarketItem = true;
                    thing.MarketCategory = reader.ReadU16();
                    thing.MarketTradeAs = reader.ReadU16();
                    thing.MarketShowAs = reader.ReadU16();
                    thing.MarketName = ReadMarketName(ref reader);
                    thing.MarketRestrictProfession = reader.ReadU16();
                    thing.MarketRestrictLevel = reader.ReadU16();
                    break;
                case 0x23: thing.HasDefaultAction = true; thing.DefaultAction = reader.ReadU16(); break;
                case 0x24: thing.Wrappable = true; break;
                case 0x25: thing.Unwrappable = true; break;
                case 0x26: thing.BottomEffect = true; break;
                case 0x28: thing.DontCenterOutfit = true; break;
                case HasCharges: thing.HasCharges = true; break;
                case FloorChange: thing.FloorChange = true; break;
                case Usable: thing.Usable = true; break;
                default:
                    throw new InvalidDataException(
                        $"Unknown OBD flag 0x{flag:X2} after 0x{previous:X2} for {thing.Kind} id {thing.Id}.");
            }
        }
    }

    public static void Write(LittleEndianStreamWriter writer, ThingType thing)
    {
        if (thing.IsGround)
        {
            writer.WriteU8(0x00);
            writer.WriteU16((ushort)thing.GroundSpeed);
        }
        else if (thing.IsGroundBorder) writer.WriteU8(0x01);
        else if (thing.IsOnBottom) writer.WriteU8(0x02);
        else if (thing.IsOnTop) writer.WriteU8(0x03);

        if (thing.IsContainer) writer.WriteU8(0x04);
        if (thing.Stackable) writer.WriteU8(0x05);
        if (thing.ForceUse) writer.WriteU8(0x06);
        if (thing.MultiUse) writer.WriteU8(0x07);

        if (thing.Writable)
        {
            writer.WriteU8(0x08);
            writer.WriteU16((ushort)thing.MaxTextLength);
        }

        if (thing.WritableOnce)
        {
            writer.WriteU8(0x09);
            writer.WriteU16((ushort)thing.MaxTextLength);
        }

        if (thing.IsFluidContainer) writer.WriteU8(0x0A);
        if (thing.IsFluid) writer.WriteU8(0x0B);
        if (thing.IsUnpassable) writer.WriteU8(0x0C);
        if (thing.IsUnmoveable) writer.WriteU8(0x0D);
        if (thing.BlockMissile) writer.WriteU8(0x0E);
        if (thing.BlockPathfind) writer.WriteU8(0x0F);
        if (thing.NoMoveAnimation) writer.WriteU8(0x10);
        if (thing.Pickupable) writer.WriteU8(0x11);
        if (thing.Hangable) writer.WriteU8(0x12);
        if (thing.IsVertical) writer.WriteU8(0x13);
        if (thing.IsHorizontal) writer.WriteU8(0x14);
        if (thing.Rotatable) writer.WriteU8(0x15);

        if (thing.HasLight)
        {
            writer.WriteU8(0x16);
            writer.WriteU16((ushort)thing.LightLevel);
            writer.WriteU16((ushort)thing.LightColor);
        }

        if (thing.DontHide) writer.WriteU8(0x17);
        if (thing.IsTranslucent) writer.WriteU8(0x18);

        if (thing.HasOffset)
        {
            writer.WriteU8(0x19);
            writer.WriteI16((short)thing.OffsetX);
            writer.WriteI16((short)thing.OffsetY);
        }

        if (thing.HasElevation)
        {
            writer.WriteU8(0x1A);
            writer.WriteU16((ushort)thing.Elevation);
        }

        if (thing.IsLyingObject) writer.WriteU8(0x1B);
        if (thing.AnimateAlways) writer.WriteU8(0x1C);

        if (thing.MiniMap)
        {
            writer.WriteU8(0x1D);
            writer.WriteU16((ushort)thing.MiniMapColor);
        }

        if (thing.IsLensHelp)
        {
            writer.WriteU8(0x1E);
            writer.WriteU16((ushort)thing.LensHelp);
        }

        if (thing.IsFullGround) writer.WriteU8(0x1F);
        if (thing.IgnoreLook) writer.WriteU8(0x20);

        if (thing.Cloth)
        {
            writer.WriteU8(0x21);
            writer.WriteU16((ushort)thing.ClothSlot);
        }

        if (thing.IsMarketItem)
        {
            writer.WriteU8(0x22);
            writer.WriteU16((ushort)thing.MarketCategory);
            writer.WriteU16((ushort)thing.MarketTradeAs);
            writer.WriteU16((ushort)thing.MarketShowAs);
            var name = thing.MarketName ?? "";
            writer.WriteU16((ushort)name.Length);
            writer.WriteBytes(Encoding.Latin1.GetBytes(name));
            writer.WriteU16((ushort)thing.MarketRestrictProfession);
            writer.WriteU16((ushort)thing.MarketRestrictLevel);
        }

        if (thing.HasDefaultAction)
        {
            writer.WriteU8(0x23);
            writer.WriteU16((ushort)thing.DefaultAction);
        }

        if (thing.Wrappable) writer.WriteU8(0x24);
        if (thing.Unwrappable) writer.WriteU8(0x25);
        if (thing.BottomEffect) writer.WriteU8(0x26);
        if (thing.DontCenterOutfit) writer.WriteU8(0x28);
        if (thing.HasCharges) writer.WriteU8(HasCharges);
        if (thing.FloorChange) writer.WriteU8(FloorChange);
        if (thing.Usable) writer.WriteU8(Usable);

        writer.WriteU8(LastFlag);
    }

    private static string ReadMarketName(ref LittleEndianSpanReader reader)
    {
        var len = reader.ReadU16();
        return Encoding.Latin1.GetString(reader.ReadBytes(len));
    }
}
