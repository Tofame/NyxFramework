using Silk.NET.Input;

namespace Sandbox.UI.Inventory;

/// <summary>Edge-detects 0–9 on main row and numpad.</summary>
internal static class StackDigitInput
{
    private static readonly bool[] WasDown = new bool[10];

    private static readonly Key[] Keys =
    [
        Key.Number0, Key.Number1, Key.Number2, Key.Number3, Key.Number4,
        Key.Number5, Key.Number6, Key.Number7, Key.Number8, Key.Number9,
    ];

    public static bool TryPoll(IKeyboard keyboard, out int digit)
    {
        digit = -1;
        for (var d = 0; d <= 9; d++)
        {
            var down = keyboard.IsKeyPressed(Keys[d]);
            if (down && !WasDown[d])
            {
                WasDown[d] = true;
                digit = d;
                return true;
            }

            WasDown[d] = down;
        }

        return false;
    }
}
