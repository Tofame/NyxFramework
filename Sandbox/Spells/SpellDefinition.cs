namespace Sandbox.Spells;

internal sealed class SpellDefinition
{
    public required string Name { get; init; }
    public required string Words { get; init; }
    public bool NeedTarget { get; init; }
    public bool SelfTarget { get; init; }
    public bool Direction { get; init; }
    public bool MouseTarget { get; init; }
    public required string ScriptName { get; init; }
}
