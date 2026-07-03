namespace NyxAssets.Things;

/// <summary>Which section of the client <c>.dat</c> a <see cref="ThingType"/> belongs to.</summary>
public enum ThingKind : byte
{
    Item = 1,
    Outfit = 2,
    Effect = 3,
    Missile = 4,
}
