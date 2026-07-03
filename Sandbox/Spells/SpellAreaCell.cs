namespace Sandbox.Spells;

/// <summary>Cell value in <c>spells_areas.toml</c> patterns.</summary>
internal enum SpellAreaCell : byte
{
    None = 0,
    Effect = 1,
    CasterNoEffect = 2,
    Caster = 3,
}
