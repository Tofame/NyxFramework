namespace NyxAssets.Things;

/// <summary>How to interpret a pair of client <c>.dat</c> / <c>.spr</c> files for a given build.</summary>
public sealed class ClientDataReadOptions
{
    public required ClientDataVersion ClientVersion { get; init; }

    /// <summary>When null, <see cref="DatThingFormatRules.UsesExtendedSpriteIdsByDefault"/> is used.</summary>
    public bool? ExtendedSpriteIds { get; init; }

    /// <summary>When null, <see cref="DatThingFormatRules.UsesImprovedAnimationsByDefault"/> is used.</summary>
    public bool? ImprovedAnimations { get; init; }

    /// <summary>When null, <see cref="DatThingFormatRules.UsesOutfitFrameGroupsByDefault"/> is used.</summary>
    public bool? OutfitFrameGroups { get; init; }

    /// <summary>Whether .spr payloads include an alpha byte per pixel (client-dependent).</summary>
    public bool TransparentSprites { get; init; }

    public uint ItemsDefaultFrameDurationMs { get; init; } = 150;
    public uint OutfitsDefaultFrameDurationMs { get; init; } = 300;
    public uint EffectsDefaultFrameDurationMs { get; init; } = 100;

    /// <summary>When null, <see cref="DatThingFormatRules.SelectFromClientVersion"/> is used.</summary>
    public DatThingFormat? DatThingFormatOverride { get; init; }

    public bool ResolveExtendedSpriteIds() =>
        ExtendedSpriteIds ?? DatThingFormatRules.UsesExtendedSpriteIdsByDefault(ClientVersion);

    public bool ResolveImprovedAnimations() =>
        ImprovedAnimations ?? DatThingFormatRules.UsesImprovedAnimationsByDefault(ClientVersion);

    public bool ResolveOutfitFrameGroups() =>
        OutfitFrameGroups ?? DatThingFormatRules.UsesOutfitFrameGroupsByDefault(ClientVersion);

    public DatThingFormat ResolveDatThingFormat() =>
        DatThingFormatOverride ?? DatThingFormatRules.SelectFromClientVersion(ClientVersion);

    public uint ResolveDefaultFrameDurationMs(ThingKind kind) => kind switch
    {
        ThingKind.Item => ItemsDefaultFrameDurationMs,
        ThingKind.Outfit => OutfitsDefaultFrameDurationMs,
        ThingKind.Effect => EffectsDefaultFrameDurationMs,
        ThingKind.Missile => EffectsDefaultFrameDurationMs,
        _ => 0,
    };
}
