using Silk.NET.Input;

namespace Sandbox.UI.ActionBar;

internal static class SandboxKeyBindCapture
{
    public static readonly Key[] CandidateKeys = BuildCandidates();

    public static bool IsModifierKey(Key key) =>
        key is Key.ShiftLeft or Key.ShiftRight
            or Key.ControlLeft or Key.ControlRight
            or Key.AltLeft or Key.AltRight;

    private static Key[] BuildCandidates()
    {
        var keys = new List<Key>(128);
        for (var f = Key.F1; f <= Key.F12; f++)
            keys.Add(f);

        for (var k = Key.A; k <= Key.Z; k++)
            keys.Add(k);

        for (var d = Key.Number0; d <= Key.Number9; d++)
            keys.Add(d);

        keys.Add(Key.Space);
        keys.Add(Key.Tab);
        keys.Add(Key.Enter);
        keys.Add(Key.KeypadEnter);
        return keys.ToArray();
    }
}
