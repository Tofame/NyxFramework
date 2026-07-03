using NyxAssets.Things.Frames;

namespace NyxDrawer.Appearance;

/// <summary>
/// Nyx / NyxClient creature outfit fields (<c>lookType</c>, <c>lookHead</c>, …).
///
/// <see cref="Shader"/> is whitespace-trimmed and nullified if empty.
/// <see cref="EffectiveShader"/> returns <c>"outfit_default"</c> as a fallback.
/// <see cref="ToColorLayout"/> converts the 4 palette indices to RGBA via <see cref="OutfitColor.FromIndex"/>.
/// <see cref="HasAddonPattern"/> uses a bitmask check: the addon bit at <c>(yPattern - 1)</c>
/// in <see cref="LookAddons"/> must be set for patterns Y >= 1.
/// </summary>
public readonly struct CreatureOutfitAppearance
{
    public CreatureOutfitAppearance(
        uint lookType,
        byte lookHead = 0,
        byte lookBody = 0,
        byte lookLegs = 0,
        byte lookFeet = 0,
        byte lookAddons = 0,
        uint lookMount = 0,
        string? shader = null)
    {
        LookType = lookType;
        LookHead = lookHead;
        LookBody = lookBody;
        LookLegs = lookLegs;
        LookFeet = lookFeet;
        LookAddons = lookAddons;
        LookMount = lookMount;
        Shader = string.IsNullOrWhiteSpace(shader) ? null : shader.Trim();
    }

    /// <summary>NyxClient lookType (outfit sprite ID).</summary>
    public uint LookType { get; }

    /// <summary>NyxClient lookMount (mount sprite ID). 0 = no mount.</summary>
    public uint LookMount { get; }

    /// <summary>Head colour palette index (0–132).</summary>
    public byte LookHead { get; }

    /// <summary>Body colour palette index (0–132).</summary>
    public byte LookBody { get; }

    /// <summary>Legs colour palette index (0–132).</summary>
    public byte LookLegs { get; }

    /// <summary>Feet colour palette index (0–132).</summary>
    public byte LookFeet { get; }

    /// <summary>Addon bitmask.  Bit 0 = addon 1, bit 1 = addon 2.</summary>
    public byte LookAddons { get; }

    /// <summary>Outfit shader name. Null or empty = use default.</summary>
    public string? Shader { get; }

    /// <summary>The shader name to use, falling back to "outfit_default".</summary>
    public string EffectiveShader => string.IsNullOrEmpty(Shader) ? "outfit_default" : Shader!;

    /// <summary>Converts the 4 palette indices to RGB colours for the GPU shader.</summary>
    public OutfitColorLayout ToColorLayout() => new(
        OutfitColor.FromIndex(LookHead),
        OutfitColor.FromIndex(LookBody),
        OutfitColor.FromIndex(LookLegs),
        OutfitColor.FromIndex(LookFeet));

    /// <summary>True if a mount is configured (LookMount > 0).</summary>
    public bool HasMount => LookMount > 0;

    /// <summary>
    /// Returns true if the given pattern Y should be drawn.
    /// Y=0 (base outfit) is always drawn.  Y >= 1 requires the corresponding addon bit.
    /// </summary>
    public bool HasAddonPattern(int yPattern) =>
        ThingFrameResolver.IsAddonPatternVisible(yPattern, LookAddons);
}
