namespace Sandbox;

/// <summary>Three-column Sandbox layout: left HUD (300px), center game, right HUD (300px).</summary>
internal static class SandboxLayout
{
    public const int SidePanelWidth = 300;

    public readonly struct Regions
    {
        public required int WindowWidth { get; init; }
        public required int WindowHeight { get; init; }
        public required int LeftWidth { get; init; }
        public required int RightWidth { get; init; }
        public required int GameX { get; init; }
        public required int GameY { get; init; }
        public required int GameWidth { get; init; }
        public required int GameHeight { get; init; }

        public int GameWidthClamped => Math.Max(1, GameWidth);
        public int GameHeightClamped => Math.Max(1, GameHeight);
    }

    public static Regions Compute(int windowWidth, int windowHeight)
    {
        var left = Math.Min(SidePanelWidth, Math.Max(0, windowWidth));
        var right = Math.Min(SidePanelWidth, Math.Max(0, windowWidth - left));
        var gameW = Math.Max(0, windowWidth - left - right);

        return new Regions
        {
            WindowWidth = windowWidth,
            WindowHeight = windowHeight,
            LeftWidth = left,
            RightWidth = right,
            GameX = left,
            GameY = 0,
            GameWidth = gameW,
            GameHeight = windowHeight,
        };
    }

    /// <summary>Maps window mouse coordinates into the game panel (for tile picking).</summary>
    public static bool TryMapMouseToGame(Regions layout, float windowX, float windowY, out float gameX, out float gameY)
    {
        gameX = windowX - layout.GameX;
        gameY = windowY - layout.GameY;
        return gameX >= 0 && gameY >= 0 && gameX < layout.GameWidth && gameY < layout.GameHeight;
    }

    /// <summary>OpenGL viewport Y (origin bottom-left) for a top-left logical rectangle.</summary>
    public static int GetGlViewportY(Regions layout) =>
        Math.Max(0, layout.WindowHeight - layout.GameY - layout.GameHeightClamped);
}
