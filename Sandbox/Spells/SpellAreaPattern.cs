namespace Sandbox.Spells;

internal sealed class SpellAreaPattern
{
    public required string Name { get; init; }
    public required int[,] Cells { get; init; }
    public required int CasterRow { get; init; }
    public required int CasterCol { get; init; }

    public bool TryGetCasterAnchor(out int casterRow, out int casterCol)
    {
        casterRow = CasterRow;
        casterCol = CasterCol;
        return CasterRow >= 0;
    }
}
