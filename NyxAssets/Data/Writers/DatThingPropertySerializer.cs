using System.Text;
using NyxAssets.Things;

namespace NyxAssets.Data.Writers;

/// <summary>
/// Serializes thing property bytes for all <see cref="DatThingFormat"/> values, matching Asset Editor
/// <c>MetadataWriter1</c>–<c>MetadataWriter6</c>.
/// </summary>
internal static class DatThingPropertySerializer
{
    private const byte Last = 0xFF;

    public static void WriteItem(LittleEndianStreamWriter w, ThingType t, DatThingFormat format)
    {
        if (t.Kind != ThingKind.Item)
            throw new ArgumentException("Expected item.", nameof(t));

        switch (format)
        {
            case DatThingFormat.V1_7_10__7_30:
                WriteItemV1(w, t);
                return;
            case DatThingFormat.V2_7_40__7_50:
                WriteItemV2(w, t);
                return;
            case DatThingFormat.V3_7_55__7_72:
                WriteItemV3(w, t);
                return;
            case DatThingFormat.V4_7_80__8_54:
                WriteItemV4(w, t);
                return;
            case DatThingFormat.V5_8_60__9_86:
                WriteItemV5(w, t);
                return;
            case DatThingFormat.V6_10_10__10_56:
                WriteItemV6(w, t);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    public static void WriteNonItem(LittleEndianStreamWriter w, ThingType t, DatThingFormat format)
    {
        if (t.Kind == ThingKind.Item)
            throw new ArgumentException("Use WriteItem for items.", nameof(t));

        switch (format)
        {
            case DatThingFormat.V1_7_10__7_30:
                WriteNonItemV1(w, t);
                return;
            case DatThingFormat.V2_7_40__7_50:
                WriteNonItemV2(w, t);
                return;
            case DatThingFormat.V3_7_55__7_72:
            case DatThingFormat.V4_7_80__8_54:
                WriteNonItemV3V4(w, t);
                return;
            case DatThingFormat.V5_8_60__9_86:
                WriteNonItemV5(w, t);
                return;
            case DatThingFormat.V6_10_10__10_56:
                WriteNonItemV6(w, t);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static void WriteNonItemV1(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.HasLight)
        {
            w.WriteU8(0x10);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.HasOffset)
            w.WriteU8(0x14);

        if (t.AnimateAlways)
            w.WriteU8(0x19);

        w.WriteU8(Last);
    }

    private static void WriteNonItemV2(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.HasLight)
        {
            w.WriteU8(0x10);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.HasOffset)
            w.WriteU8(0x14);

        if (t.AnimateAlways)
            w.WriteU8(0x1C);

        w.WriteU8(Last);
    }

    private static void WriteNonItemV3V4(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.HasLight)
        {
            w.WriteU8(0x15);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.HasOffset)
        {
            w.WriteU8(0x18);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.AnimateAlways)
            w.WriteU8(0x1B);

        w.WriteU8(Last);
    }

    private static void WriteNonItemV5(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.HasLight)
        {
            w.WriteU8(0x15);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.HasOffset)
        {
            w.WriteU8(0x18);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.AnimateAlways)
            w.WriteU8(0x1B);

        if (t.BottomEffect && t.Kind == ThingKind.Effect)
            w.WriteU8(0x26);

        if (t.DontCenterOutfit && t.Kind == ThingKind.Outfit)
            w.WriteU8(0x28);

        w.WriteU8(Last);
    }

    private static void WriteNonItemV6(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.HasLight)
        {
            w.WriteU8(0x16);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.HasOffset)
        {
            w.WriteU8(0x19);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.AnimateAlways)
            w.WriteU8(0x1C);

        if (t.BottomEffect && t.Kind == ThingKind.Effect)
            w.WriteU8(0x26);

        w.WriteU8(Last);
    }

    private static void WriteItemV1(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.IsGround)
        {
            w.WriteU8(0x00);
            w.WriteU16((ushort)t.GroundSpeed);
        }
        else if (t.IsOnBottom)
            w.WriteU8(0x01);
        else if (t.IsOnTop)
            w.WriteU8(0x02);

        if (t.IsContainer)
            w.WriteU8(0x03);
        if (t.Stackable)
            w.WriteU8(0x04);
        if (t.MultiUse)
            w.WriteU8(0x05);
        if (t.ForceUse)
            w.WriteU8(0x06);
        if (t.Writable)
        {
            w.WriteU8(0x07);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.WritableOnce)
        {
            w.WriteU8(0x08);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.IsFluidContainer)
            w.WriteU8(0x09);
        if (t.IsFluid)
            w.WriteU8(0x0A);
        if (t.IsUnpassable)
            w.WriteU8(0x0B);
        if (t.IsUnmoveable)
            w.WriteU8(0x0C);
        if (t.BlockMissile)
            w.WriteU8(0x0D);
        if (t.BlockPathfind)
            w.WriteU8(0x0E);
        if (t.Pickupable)
            w.WriteU8(0x0F);
        if (t.HasLight)
        {
            w.WriteU8(0x10);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.FloorChange)
            w.WriteU8(0x11);
        if (t.IsFullGround)
            w.WriteU8(0x12);
        if (t.HasElevation)
        {
            w.WriteU8(0x13);
            w.WriteU16((ushort)t.Elevation);
        }

        if (t.HasOffset)
            w.WriteU8(0x14);
        if (t.MiniMap)
        {
            w.WriteU8(0x16);
            w.WriteU16((ushort)t.MiniMapColor);
        }

        if (t.Rotatable)
            w.WriteU8(0x17);
        if (t.IsLyingObject)
            w.WriteU8(0x18);
        if (t.AnimateAlways)
            w.WriteU8(0x19);
        if (t.IsLensHelp)
        {
            w.WriteU8(0x1A);
            w.WriteU16((ushort)t.LensHelp);
        }

        if (t.Wrappable)
            w.WriteU8(0x24);
        if (t.Unwrappable)
            w.WriteU8(0x25);

        w.WriteU8(Last);
    }

    private static void WriteItemV2(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.IsGround)
        {
            w.WriteU8(0x00);
            w.WriteU16((ushort)t.GroundSpeed);
        }
        else if (t.IsOnBottom)
            w.WriteU8(0x01);
        else if (t.IsOnTop)
            w.WriteU8(0x02);

        if (t.IsContainer)
            w.WriteU8(0x03);
        if (t.Stackable)
            w.WriteU8(0x04);
        if (t.MultiUse)
            w.WriteU8(0x05);
        if (t.ForceUse)
            w.WriteU8(0x06);
        if (t.Writable)
        {
            w.WriteU8(0x07);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.WritableOnce)
        {
            w.WriteU8(0x08);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.IsFluidContainer)
            w.WriteU8(0x09);
        if (t.IsFluid)
            w.WriteU8(0x0A);
        if (t.IsUnpassable)
            w.WriteU8(0x0B);
        if (t.IsUnmoveable)
            w.WriteU8(0x0C);
        if (t.BlockMissile)
            w.WriteU8(0x0D);
        if (t.BlockPathfind)
            w.WriteU8(0x0E);
        if (t.Pickupable)
            w.WriteU8(0x0F);
        if (t.HasLight)
        {
            w.WriteU8(0x10);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.FloorChange)
            w.WriteU8(0x11);
        if (t.IsFullGround)
            w.WriteU8(0x12);
        if (t.HasElevation)
        {
            w.WriteU8(0x13);
            w.WriteU16((ushort)t.Elevation);
        }

        if (t.HasOffset)
            w.WriteU8(0x14);
        if (t.MiniMap)
        {
            w.WriteU8(0x16);
            w.WriteU16((ushort)t.MiniMapColor);
        }

        if (t.Rotatable)
            w.WriteU8(0x17);
        if (t.IsLyingObject)
            w.WriteU8(0x18);
        if (t.Hangable)
            w.WriteU8(0x19);
        if (t.IsVertical)
            w.WriteU8(0x1A);
        if (t.IsHorizontal)
            w.WriteU8(0x1B);
        if (t.AnimateAlways)
            w.WriteU8(0x1C);
        if (t.IsLensHelp)
        {
            w.WriteU8(0x1D);
            w.WriteU16((ushort)t.LensHelp);
        }

        if (t.Wrappable)
            w.WriteU8(0x24);
        if (t.Unwrappable)
            w.WriteU8(0x25);

        w.WriteU8(Last);
    }

    private static void WriteItemV3(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.IsGround)
        {
            w.WriteU8(0x00);
            w.WriteU16((ushort)t.GroundSpeed);
        }
        else if (t.IsGroundBorder)
            w.WriteU8(0x01);
        else if (t.IsOnBottom)
            w.WriteU8(0x02);
        else if (t.IsOnTop)
            w.WriteU8(0x03);

        if (t.IsContainer)
            w.WriteU8(0x04);
        if (t.Stackable)
            w.WriteU8(0x05);
        if (t.MultiUse)
            w.WriteU8(0x07);
        if (t.ForceUse)
            w.WriteU8(0x06);
        if (t.Writable)
        {
            w.WriteU8(0x08);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.WritableOnce)
        {
            w.WriteU8(0x09);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.IsFluidContainer)
            w.WriteU8(0x0A);
        if (t.IsFluid)
            w.WriteU8(0x0B);
        if (t.IsUnpassable)
            w.WriteU8(0x0C);
        if (t.IsUnmoveable)
            w.WriteU8(0x0D);
        if (t.BlockMissile)
            w.WriteU8(0x0E);
        if (t.BlockPathfind)
            w.WriteU8(0x0F);
        if (t.Pickupable)
            w.WriteU8(0x10);
        if (t.Hangable)
            w.WriteU8(0x11);
        if (t.IsVertical)
            w.WriteU8(0x12);
        if (t.IsHorizontal)
            w.WriteU8(0x13);
        if (t.Rotatable)
            w.WriteU8(0x14);
        if (t.HasLight)
        {
            w.WriteU8(0x15);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.FloorChange)
            w.WriteU8(0x17);
        if (t.HasOffset)
        {
            w.WriteU8(0x18);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.HasElevation)
        {
            w.WriteU8(0x19);
            w.WriteU16((ushort)t.Elevation);
        }

        if (t.IsLyingObject)
            w.WriteU8(0x1A);
        if (t.AnimateAlways)
            w.WriteU8(0x1B);
        if (t.MiniMap)
        {
            w.WriteU8(0x1C);
            w.WriteU16((ushort)t.MiniMapColor);
        }

        if (t.IsLensHelp)
        {
            w.WriteU8(0x1D);
            w.WriteU16((ushort)t.LensHelp);
        }

        if (t.IsFullGround)
            w.WriteU8(0x1E);

        w.WriteU8(Last);
    }

    private static void WriteItemV4(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.IsGround)
        {
            w.WriteU8(0x00);
            w.WriteU16((ushort)t.GroundSpeed);
        }
        else if (t.IsGroundBorder)
            w.WriteU8(0x01);
        else if (t.IsOnBottom)
            w.WriteU8(0x02);
        else if (t.IsOnTop)
            w.WriteU8(0x03);

        if (t.IsContainer)
            w.WriteU8(0x04);
        if (t.Stackable)
            w.WriteU8(0x05);
        if (t.ForceUse)
            w.WriteU8(0x06);
        if (t.MultiUse)
            w.WriteU8(0x07);
        if (t.HasCharges)
            w.WriteU8(0x08);
        if (t.Writable)
        {
            w.WriteU8(0x09);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.WritableOnce)
        {
            w.WriteU8(0x0A);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.IsFluidContainer)
            w.WriteU8(0x0B);
        if (t.IsFluid)
            w.WriteU8(0x0C);
        if (t.IsUnpassable)
            w.WriteU8(0x0D);
        if (t.IsUnmoveable)
            w.WriteU8(0x0E);
        if (t.BlockMissile)
            w.WriteU8(0x0F);
        if (t.BlockPathfind)
            w.WriteU8(0x10);
        if (t.Pickupable)
            w.WriteU8(0x11);
        if (t.Hangable)
            w.WriteU8(0x12);
        if (t.IsVertical)
            w.WriteU8(0x13);
        if (t.IsHorizontal)
            w.WriteU8(0x14);
        if (t.Rotatable)
            w.WriteU8(0x15);
        if (t.HasLight)
        {
            w.WriteU8(0x16);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.DontHide)
            w.WriteU8(0x17);
        if (t.FloorChange)
            w.WriteU8(0x18);
        if (t.HasOffset)
        {
            w.WriteU8(0x19);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.HasElevation)
        {
            w.WriteU8(0x1A);
            w.WriteU16((ushort)t.Elevation);
        }

        if (t.IsLyingObject)
            w.WriteU8(0x1B);
        if (t.AnimateAlways)
            w.WriteU8(0x1C);
        if (t.MiniMap)
        {
            w.WriteU8(0x1D);
            w.WriteU16((ushort)t.MiniMapColor);
        }

        if (t.IsLensHelp)
        {
            w.WriteU8(0x1E);
            w.WriteU16((ushort)t.LensHelp);
        }

        if (t.IsFullGround)
            w.WriteU8(0x1F);
        if (t.IgnoreLook)
            w.WriteU8(0x20);

        w.WriteU8(Last);
    }

    private static void WriteLatin1Name(LittleEndianStreamWriter w, string? name)
    {
        var bytes = Encoding.Latin1.GetBytes(name ?? string.Empty);
        if (bytes.Length > ushort.MaxValue)
            throw new InvalidOperationException("Market name too long.");
        w.WriteU16((ushort)bytes.Length);
        w.WriteBytes(bytes);
    }

    private static void WriteItemV5(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.IsGround)
        {
            w.WriteU8(0x00);
            w.WriteU16((ushort)t.GroundSpeed);
        }
        else if (t.IsGroundBorder)
            w.WriteU8(0x01);
        else if (t.IsOnBottom)
            w.WriteU8(0x02);
        else if (t.IsOnTop)
            w.WriteU8(0x03);

        if (t.IsContainer)
            w.WriteU8(0x04);
        if (t.Stackable)
            w.WriteU8(0x05);
        if (t.ForceUse)
            w.WriteU8(0x06);
        if (t.MultiUse)
            w.WriteU8(0x07);
        if (t.Writable)
        {
            w.WriteU8(0x08);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.WritableOnce)
        {
            w.WriteU8(0x09);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.IsFluidContainer)
            w.WriteU8(0x0A);
        if (t.IsFluid)
            w.WriteU8(0x0B);
        if (t.IsUnpassable)
            w.WriteU8(0x0C);
        if (t.IsUnmoveable)
            w.WriteU8(0x0D);
        if (t.BlockMissile)
            w.WriteU8(0x0E);
        if (t.BlockPathfind)
            w.WriteU8(0x0F);
        if (t.Pickupable)
            w.WriteU8(0x10);
        if (t.Hangable)
            w.WriteU8(0x11);
        if (t.IsVertical)
            w.WriteU8(0x12);
        if (t.IsHorizontal)
            w.WriteU8(0x13);
        if (t.Rotatable)
            w.WriteU8(0x14);
        if (t.HasLight)
        {
            w.WriteU8(0x15);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.DontHide)
            w.WriteU8(0x16);
        if (t.IsTranslucent)
            w.WriteU8(0x17);
        if (t.HasOffset)
        {
            w.WriteU8(0x18);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.HasElevation)
        {
            w.WriteU8(0x19);
            w.WriteU16((ushort)t.Elevation);
        }

        if (t.IsLyingObject)
            w.WriteU8(0x1A);
        if (t.AnimateAlways)
            w.WriteU8(0x1B);
        if (t.MiniMap)
        {
            w.WriteU8(0x1C);
            w.WriteU16((ushort)t.MiniMapColor);
        }

        if (t.IsLensHelp)
        {
            w.WriteU8(0x1D);
            w.WriteU16((ushort)t.LensHelp);
        }

        if (t.IsFullGround)
            w.WriteU8(0x1E);
        if (t.IgnoreLook)
            w.WriteU8(0x1F);
        if (t.Cloth)
        {
            w.WriteU8(0x20);
            w.WriteU16((ushort)t.ClothSlot);
        }

        if (t.IsMarketItem)
        {
            w.WriteU8(0x21);
            w.WriteU16((ushort)t.MarketCategory);
            w.WriteU16((ushort)t.MarketTradeAs);
            w.WriteU16((ushort)t.MarketShowAs);
            WriteLatin1Name(w, t.MarketName);
            w.WriteU16((ushort)t.MarketRestrictProfession);
            w.WriteU16((ushort)t.MarketRestrictLevel);
        }

        if (t.Wrappable)
            w.WriteU8(0x24);
        if (t.Unwrappable)
            w.WriteU8(0x25);

        w.WriteU8(Last);
    }

    private static void WriteItemV6(LittleEndianStreamWriter w, ThingType t)
    {
        if (t.IsGround)
        {
            w.WriteU8(0x00);
            w.WriteU16((ushort)t.GroundSpeed);
        }
        else if (t.IsGroundBorder)
            w.WriteU8(0x01);
        else if (t.IsOnBottom)
            w.WriteU8(0x02);
        else if (t.IsOnTop)
            w.WriteU8(0x03);

        if (t.IsContainer)
            w.WriteU8(0x04);
        if (t.Stackable)
            w.WriteU8(0x05);
        if (t.ForceUse)
            w.WriteU8(0x06);
        if (t.MultiUse)
            w.WriteU8(0x07);
        if (t.Writable)
        {
            w.WriteU8(0x08);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.WritableOnce)
        {
            w.WriteU8(0x09);
            w.WriteU16((ushort)t.MaxTextLength);
        }

        if (t.IsFluidContainer)
            w.WriteU8(0x0A);
        if (t.IsFluid)
            w.WriteU8(0x0B);
        if (t.IsUnpassable)
            w.WriteU8(0x0C);
        if (t.IsUnmoveable)
            w.WriteU8(0x0D);
        if (t.BlockMissile)
            w.WriteU8(0x0E);
        if (t.BlockPathfind)
            w.WriteU8(0x0F);
        if (t.NoMoveAnimation)
            w.WriteU8(0x10);
        if (t.Pickupable)
            w.WriteU8(0x11);
        if (t.Hangable)
            w.WriteU8(0x12);
        if (t.IsVertical)
            w.WriteU8(0x13);
        if (t.IsHorizontal)
            w.WriteU8(0x14);
        if (t.Rotatable)
            w.WriteU8(0x15);
        if (t.HasLight)
        {
            w.WriteU8(0x16);
            w.WriteU16((ushort)t.LightLevel);
            w.WriteU16((ushort)t.LightColor);
        }

        if (t.DontHide)
            w.WriteU8(0x17);
        if (t.IsTranslucent)
            w.WriteU8(0x18);
        if (t.HasOffset)
        {
            w.WriteU8(0x19);
            w.WriteI16((short)t.OffsetX);
            w.WriteI16((short)t.OffsetY);
        }

        if (t.HasElevation)
        {
            w.WriteU8(0x1A);
            w.WriteU16((ushort)t.Elevation);
        }

        if (t.IsLyingObject)
            w.WriteU8(0x1B);
        if (t.AnimateAlways)
            w.WriteU8(0x1C);
        if (t.MiniMap)
        {
            w.WriteU8(0x1D);
            w.WriteU16((ushort)t.MiniMapColor);
        }

        if (t.IsLensHelp)
        {
            w.WriteU8(0x1E);
            w.WriteU16((ushort)t.LensHelp);
        }

        if (t.IsFullGround)
            w.WriteU8(0x1F);
        if (t.IgnoreLook)
            w.WriteU8(0x20);
        if (t.Cloth)
        {
            w.WriteU8(0x21);
            w.WriteU16((ushort)t.ClothSlot);
        }

        if (t.IsMarketItem)
        {
            w.WriteU8(0x22);
            w.WriteU16((ushort)t.MarketCategory);
            w.WriteU16((ushort)t.MarketTradeAs);
            w.WriteU16((ushort)t.MarketShowAs);
            WriteLatin1Name(w, t.MarketName);
            w.WriteU16((ushort)t.MarketRestrictProfession);
            w.WriteU16((ushort)t.MarketRestrictLevel);
        }

        if (t.HasDefaultAction)
        {
            w.WriteU8(0x23);
            w.WriteU16((ushort)t.DefaultAction);
        }

        if (t.Wrappable)
            w.WriteU8(0x24);
        if (t.Unwrappable)
            w.WriteU8(0x25);
        if (t.Usable)
            w.WriteU8(0xFE);

        w.WriteU8(Last);
    }
}
