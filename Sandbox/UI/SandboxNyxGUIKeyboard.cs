using NyxGui;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>Maps Silk keyboard state into <see cref="NyxGuiRootStack.ProcessKeyboard"/>.</summary>
internal sealed class SandboxNyxGUIKeyboard
{
    private static readonly Key[] TrackedKeys = BuildTrackedKeys();
    private readonly HashSet<Key> Held = [];
	private Key? _repeatingKey;
	private long _nextRepeatTimeMs;

    /// <summary>When true, game movement keys should not act on the player.</summary>
    public bool BlocksGameMovement { get; private set; }

    public void Update(IKeyboard keyboard, NyxGuiRootStack? roots)
    {
		if (_repeatingKey.HasValue && !IsKeyPressed(keyboard, _repeatingKey.Value))
		{
			_repeatingKey = null;
		}
		var uiHasFocus = roots is not null && roots.Focus.FocusedElement is not null;
        BlocksGameMovement = roots is not null && (NyxGuiKeyboardInput.HasFocusedTextEntry(roots) || uiHasFocus);

        if (roots is not null && BlocksGameMovement)
        {
            if (ShouldTrigger(keyboard, Key.Backspace))
                roots.ProcessKeyboard(NyxGuiKey.Backspace);
            if (ShouldTrigger(keyboard, Key.Delete))
                roots.ProcessKeyboard(NyxGuiKey.Delete);
            if (ShouldTrigger(keyboard, Key.Left))
                roots.ProcessKeyboard(NyxGuiKey.Left);
            if (ShouldTrigger(keyboard, Key.Right))
                roots.ProcessKeyboard(NyxGuiKey.Right);
            if (ShouldTrigger(keyboard, Key.Up))
                roots.ProcessKeyboard(NyxGuiKey.Up);
            if (ShouldTrigger(keyboard, Key.Down))
                roots.ProcessKeyboard(NyxGuiKey.Down);
            if (ShouldTrigger(keyboard, Key.Home))
                roots.ProcessKeyboard(NyxGuiKey.Home);
            if (ShouldTrigger(keyboard, Key.End))
                roots.ProcessKeyboard(NyxGuiKey.End);
            if (ShouldTrigger(keyboard, Key.Enter) || ShouldTrigger(keyboard, Key.KeypadEnter))
                roots.ProcessKeyboard(NyxGuiKey.Enter);
            if (ShouldTrigger(keyboard, Key.Escape))
                roots.ProcessKeyboard(NyxGuiKey.Escape);
            if (ShouldTrigger(keyboard, Key.Tab))
                roots.ProcessKeyboard(NyxGuiKey.Tab);

            if (IsKeyPressed(keyboard, Key.ControlLeft) || IsKeyPressed(keyboard, Key.ControlRight))
            {
                if (ShouldTrigger(keyboard, Key.A))
                    roots.ProcessKeyboard(NyxGuiKey.None, '\u0001');
                if (ShouldTrigger(keyboard, Key.C))
                    roots.ProcessKeyboard(NyxGuiKey.None, '\u0003');
                if (ShouldTrigger(keyboard, Key.V))
                    roots.ProcessKeyboard(NyxGuiKey.None, '\u0016');
            }
            else if (!HasModifier(keyboard))
            {
                TryType(keyboard, roots, Key.Space, ' ');
                TryType(keyboard, roots, Key.Period, '.', '>');
                TryType(keyboard, roots, Key.Comma, ',', '<');
                TryType(keyboard, roots, Key.Minus, '-', '_');
                TryType(keyboard, roots, Key.Slash, '/', '?');
                TryType(keyboard, roots, Key.Semicolon, ';', ':');
                TryType(keyboard, roots, Key.Apostrophe, '\'', '"');
                TryType(keyboard, roots, Key.GraveAccent, '`', '~');
                TryType(keyboard, roots, Key.LeftBracket, '[', '{');
                TryType(keyboard, roots, Key.RightBracket, ']', '}');
                TryType(keyboard, roots, Key.BackSlash, '\\', '|');
                TryType(keyboard, roots, Key.Equal, '=', '+');

                for (var i = 0; i < 26; i++)
                {
                    var key = Key.A + i;
                    var lower = (char)('a' + i);
                    TryType(keyboard, roots, key, lower, char.ToUpperInvariant(lower));
                }

                TryType(keyboard, roots, Key.Number1, '1', '!');
                TryType(keyboard, roots, Key.Number2, '2', '@');
                TryType(keyboard, roots, Key.Number3, '3', '#');
                TryType(keyboard, roots, Key.Number4, '4', '$');
                TryType(keyboard, roots, Key.Number5, '5', '%');
                TryType(keyboard, roots, Key.Number6, '6', '^');
                TryType(keyboard, roots, Key.Number7, '7', '&');
                TryType(keyboard, roots, Key.Number8, '8', '*');
                TryType(keyboard, roots, Key.Number9, '9', '(');
                TryType(keyboard, roots, Key.Number0, '0', ')');

                for (var d = 0; d <= 9; d++)
                {
                    TryType(keyboard, roots, Key.Keypad0 + d, (char)('0' + d));
                }
            }
        }

        SyncHeld(keyboard);
    }

    private static Key[] BuildTrackedKeys()
    {
        var keys = new List<Key>(128)
        {
            Key.Backspace, Key.Delete, Key.Left, Key.Right, Key.Up, Key.Down,
            Key.Home, Key.End, Key.Enter, Key.KeypadEnter, Key.Escape, Key.Tab,
            Key.Space, Key.Period, Key.Semicolon, Key.Comma, Key.Minus, Key.Slash,
            Key.BackSlash, Key.Equal, Key.LeftBracket, Key.RightBracket, Key.Apostrophe,
            Key.GraveAccent,
            Key.ShiftLeft, Key.ShiftRight, Key.ControlLeft, Key.ControlRight,
            Key.AltLeft, Key.AltRight,
        };

        for (var i = 0; i < 26; i++)
            keys.Add(Key.A + i);

        for (var d = 0; d <= 9; d++)
        {
            keys.Add(Key.Number0 + d);
            keys.Add(Key.Keypad0 + d);
        }

        return keys.ToArray();
    }

    private static bool HasModifier(IKeyboard keyboard) =>
        IsKeyPressed(keyboard, Key.ControlLeft) ||
        IsKeyPressed(keyboard, Key.ControlRight) ||
        IsKeyPressed(keyboard, Key.AltLeft) ||
        IsKeyPressed(keyboard, Key.AltRight);

    private static bool Shift(IKeyboard keyboard) =>
        IsKeyPressed(keyboard, Key.ShiftLeft) || IsKeyPressed(keyboard, Key.ShiftRight);

    private void TryType(IKeyboard keyboard, NyxGuiRootStack roots, Key key, char lower, char? upper = null)
    {
        if (!ShouldTrigger(keyboard, key))
            return;

        var ch = Shift(keyboard) ? upper ?? lower : lower;
        roots.ProcessKeyboard(NyxGuiKey.None, ch);
    }

	private bool ShouldTrigger(IKeyboard keyboard, Key key)
	{
		if (!IsKeyPressed(keyboard, key))
			return false;

		var now = Environment.TickCount64;

		if (!Held.Contains(key))
		{
			_repeatingKey = key;
			_nextRepeatTimeMs = now + 400;
			return true;
		}

		if (_repeatingKey == key && now >= _nextRepeatTimeMs)
		{
			_nextRepeatTimeMs = now + 40;
			return true;
		}

		return false;
	}

    private bool Edge(IKeyboard keyboard, Key key) =>
        IsKeyPressed(keyboard, key) && !Held.Contains(key);

    private static bool IsKeyPressed(IKeyboard keyboard, Key key)
    {
        if ((int)key < 0)
            return false;

        return keyboard.IsKeyPressed(key);
    }

    private void SyncHeld(IKeyboard keyboard)
    {
        Held.Clear();
        foreach (var key in TrackedKeys)
        {
            if (IsKeyPressed(keyboard, key))
                Held.Add(key);
        }
    }
}
