namespace NyxGui;

/// <summary>Single keyboard-focus owner for a <see cref="NyxGuiRootStack"/>.</summary>
public sealed class NyxGuiFocus
{
    private NyxElement? _focused;

    public NyxElement? FocusedElement => _focused;

    public INyxTextEntry? FocusedTextEntry => _focused as INyxTextEntry;

    /// <summary>True when an editable <see cref="INyxTextEntry"/> has focus (typing should capture keys).</summary>
    public bool HasEditableTextEntry => _focused switch
    {
        NyxTextBox { ReadOnly: false } => true,
        NyxTextArea { ReadOnly: false } => true,
        _ => false,
    };

    /// <summary>When true, host shortcuts (panel toggles, movement, etc.) should not run.</summary>
    public bool CapturesGlobalShortcuts => HasEditableTextEntry;

    public void Clear() => SetFocus(null);

    public void SetFocus(NyxElement? element)
    {
        if (element is { Focusable: false }) element = null;
        if (ReferenceEquals(_focused, element)) return;

        var old = _focused;
        _focused = element;

        // Raise focus events
        old?.RaiseEvent(new NyxFocusEventArgs(NyxEventType.FocusLost, old, element));
        _focused?.RaiseEvent(new NyxFocusEventArgs(NyxEventType.FocusGained, _focused, old));

        // Update IsFocused flag
        if (old is not null) old.IsFocused = false;
        if (_focused is not null) _focused.IsFocused = true;
    }
}
