namespace NyxAssets.Things.Exchange;

/// <summary>String labels for <see cref="ThingKind"/> used in portable JSON / OBD interchange.</summary>
public static class ThingKindNames
{
    public const string Item = "item";
    public const string Outfit = "outfit";
    public const string Effect = "effect";
    public const string Missile = "missile";

    public static string ToName(ThingKind kind) => kind switch
    {
        ThingKind.Item => Item,
        ThingKind.Outfit => Outfit,
        ThingKind.Effect => Effect,
        ThingKind.Missile => Missile,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static ThingKind FromName(string name) =>
        name.ToLowerInvariant() switch
        {
            Item => ThingKind.Item,
            Outfit => ThingKind.Outfit,
            Effect => ThingKind.Effect,
            Missile => ThingKind.Missile,
            _ => throw new ArgumentException($"Unknown thing type '{name}'. Expected item, outfit, effect, or missile.", nameof(name)),
        };

    internal static byte ToObdCategory(ThingKind kind) => (byte)kind;
}
