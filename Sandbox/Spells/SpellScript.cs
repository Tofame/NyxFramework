namespace Sandbox.Spells;

internal sealed class SpellScript
{
    public required string Name { get; init; }
    public uint? EffectId { get; init; }
    public uint? MissileId { get; init; }
    public SpellAreaPattern? Area { get; init; }

    public bool IsAreaSpell => EffectId is not null && Area is not null;
    public bool IsMissileSpell => MissileId is not null;
}
