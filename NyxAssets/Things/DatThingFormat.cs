namespace NyxAssets.Things;

/// <summary>
/// Which .dat flag decoder to use; chosen from client build the same way as Asset Editor
/// <see href="https://github.com/ottools/ObjectBuilder">ThingTypeStorage.load</see>.
/// </summary>
public enum DatThingFormat
{
    V1_7_10__7_30 = 1,
    V2_7_40__7_50 = 2,
    V3_7_55__7_72 = 3,
    V4_7_80__8_54 = 4,
    V5_8_60__9_86 = 5,
    V6_10_10__10_56 = 6,
}

/// <summary>Derives .dat / .spr layout defaults from <see cref="ClientDataVersion"/>.</summary>
public static class DatThingFormatRules
{
    public static DatThingFormat SelectFromClientVersion(ClientDataVersion v)
    {
        var x = v.Value;
        if (x <= 730) return DatThingFormat.V1_7_10__7_30;
        if (x <= 750) return DatThingFormat.V2_7_40__7_50;
        if (x <= 772) return DatThingFormat.V3_7_55__7_72;
        if (x <= 854) return DatThingFormat.V4_7_80__8_54;
        if (x <= 986) return DatThingFormat.V5_8_60__9_86;
        return DatThingFormat.V6_10_10__10_56;
    }

    public static bool UsesExtendedSpriteIdsByDefault(ClientDataVersion v) => v.Value >= 960;

    public static bool UsesImprovedAnimationsByDefault(ClientDataVersion v) => v.Value >= 1050;

    public static bool UsesOutfitFrameGroupsByDefault(ClientDataVersion v) => v.Value >= 1057;
}
