namespace NyxAssets.Things.Frames;

/// <summary>
/// NyxClient <c>Item::calculatePatterns</c> for stackable items with a 4×2 pattern grid
/// (single coin, small pile, medium pile, large pile on row 0; higher counts on row 1).
/// </summary>
public static class ItemStackPatterns
{
    /// <summary>Nyx stack count piles: 4 columns × 2 rows of full 32×32 sprites in <c>.dat</c>.</summary>
    public static bool UsesStackCountGrid(ThingFrameGroup frameGroup, bool stackable) =>
        UsesStackCountGrid(frameGroup.PatternX, frameGroup.PatternY, stackable);

    public static bool UsesStackCountGrid(uint patternXCount, uint patternYCount, bool stackable) =>
        stackable && patternXCount == 4 && patternYCount == 2;

    public static void Resolve(ThingFrameGroup frameGroup, bool stackable, int count, out uint patternX, out uint patternY) =>
        Resolve(frameGroup.PatternX, frameGroup.PatternY, stackable, count, out patternX, out patternY);

    public static void Resolve(uint patternXCount, uint patternYCount, bool stackable, int count, out uint patternX, out uint patternY)
    {
        patternX = 0;
        patternY = 0;

        if (!UsesStackCountGrid(patternXCount, patternYCount, stackable))
            return;

        if (count <= 0)
            return;

        if (count < 5)
        {
            patternX = (uint)(count - 1);
            return;
        }

        patternY = 1;
        patternX = count switch
        {
            < 10 => 0,
            < 25 => 1,
            < 50 => 2,
            _ => 3,
        };
    }
}
