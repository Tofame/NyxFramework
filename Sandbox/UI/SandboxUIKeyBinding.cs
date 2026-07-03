using Silk.NET.Input;

namespace Sandbox.UI;

internal static class SandboxUIKeyBinding
{
    public static Key? TryGetToggleKey(string moduleId)
    {
        if (SandboxUIBootstrap.Current?.Config.Toggles.TryGetValue(moduleId, out var toggle) != true ||
            toggle is null)
            return null;

        return ParseKey(toggle.Key);
    }

    public static Key GetToggleKey(string moduleId, Key fallback) =>
        TryGetToggleKey(moduleId) ?? fallback;

    private static Key? ParseKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return name.Trim().ToUpperInvariant() switch
        {
            "A" => Key.A,
            "B" => Key.B,
            "C" => Key.C,
            "D" => Key.D,
            "E" => Key.E,
            "F" => Key.F,
            "G" => Key.G,
            "H" => Key.H,
            "I" => Key.I,
            "J" => Key.J,
            "K" => Key.K,
            "L" => Key.L,
            "M" => Key.M,
            "N" => Key.N,
            "O" => Key.O,
            "P" => Key.P,
            "Q" => Key.Q,
            "R" => Key.R,
            "S" => Key.S,
            "T" => Key.T,
            "U" => Key.U,
            "V" => Key.V,
            "W" => Key.W,
            "X" => Key.X,
            "Y" => Key.Y,
            "Z" => Key.Z,
            _ => Enum.TryParse<Key>(name, ignoreCase: true, out var key) ? key : null,
        };
    }
}
