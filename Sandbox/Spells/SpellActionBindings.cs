using Silk.NET.Input;

namespace Sandbox.Spells;

/// <summary>Keyboard bindings for the three sandbox action-bar spell slots.</summary>
internal sealed class SpellActionBindings
{
    private readonly Key[] _keys = [Key.F1, Key.F2, Key.F3];

    public const int SlotCount = 3;

    public Key GetKey(int slot) => _keys[slot];

    public void SetKey(int slot, Key key) => _keys[slot] = key;

    public static string FormatKey(Key key) =>
        key switch
        {
            >= Key.F1 and <= Key.F24 => key.ToString(),
            Key.Space => "Space",
            Key.Escape => "Esc",
            >= Key.Number0 and <= Key.Number9 => ((char)('0' + (key - Key.Number0))).ToString(),
            >= Key.A and <= Key.Z => key.ToString(),
            _ => key.ToString(),
        };
}
