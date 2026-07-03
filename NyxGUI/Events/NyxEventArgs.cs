namespace NyxGui;

/// <summary>Base class for all routed event arguments.</summary>
public class NyxEventArgs
{
    public NyxEventArgs(NyxEventType type, NyxElement source)
    {
        EventType = type;
        Source = source;
    }

    /// <summary>The type of event being raised.</summary>
    public NyxEventType EventType { get; }

    /// <summary>The widget that originated this event.</summary>
    public NyxElement Source { get; }

    /// <summary>The widget currently receiving the event during routing.</summary>
    public NyxElement? CurrentTarget { get; internal set; }

    /// <summary>When true, stops further propagation of this event.</summary>
    public bool Handled { get; set; }
}

/// <summary>Arguments for mouse events.</summary>
public class NyxMouseEventArgs : NyxEventArgs
{
    public NyxMouseEventArgs(NyxEventType type, NyxElement source, int x, int y, NyxMouseButton button = NyxMouseButton.Left)
        : base(type, source)
    {
        X = x;
        Y = y;
        Button = button;
    }

    public int X { get; }
    public int Y { get; }
    public NyxMouseButton Button { get; }
}

/// <summary>Arguments for mouse wheel events.</summary>
public class NyxMouseWheelEventArgs : NyxMouseEventArgs
{
    public NyxMouseWheelEventArgs(NyxElement source, int x, int y, int delta)
        : base(NyxEventType.MouseWheel, source, x, y)
    {
        Delta = delta;
    }

    public int Delta { get; }
}

/// <summary>Arguments for keyboard events.</summary>
public class NyxKeyEventArgs : NyxEventArgs
{
    public NyxKeyEventArgs(NyxEventType type, NyxElement source, NyxGuiKey key, char? character = null)
        : base(type, source)
    {
        Key = key;
        Character = character;
    }

    public NyxGuiKey Key { get; }
    public char? Character { get; }
}

/// <summary>Arguments for text input events.</summary>
public class NyxTextInputEventArgs : NyxEventArgs
{
    public NyxTextInputEventArgs(NyxElement source, char character)
        : base(NyxEventType.TextInput, source)
    {
        Character = character;
    }

    public char Character { get; }
}

/// <summary>Arguments for focus events.</summary>
public class NyxFocusEventArgs : NyxEventArgs
{
    public NyxFocusEventArgs(NyxEventType type, NyxElement source, NyxElement? relatedTarget = null)
        : base(type, source)
    {
        RelatedTarget = relatedTarget;
    }

    /// <summary>The element gaining focus (for FocusLost) or losing focus (for FocusGained).</summary>
    public NyxElement? RelatedTarget { get; }
}

/// <summary>Arguments for drag-and-drop events.</summary>
public class NyxDragEventArgs : NyxEventArgs
{
    public NyxDragEventArgs(NyxEventType type, NyxElement source, NyxDragData? data)
        : base(type, source)
    {
        Data = data;
    }

    /// <summary>The payload being dragged.</summary>
    public NyxDragData? Data { get; }

    /// <summary>The widget currently under the cursor (for DragEnter/DragLeave/DragOver/Drop).</summary>
    public NyxElement? Target { get; internal set; }
}

/// <summary>Arguments for the DragEnd event.</summary>
public class NyxDragEndEventArgs : NyxDragEventArgs
{
    public NyxDragEndEventArgs(NyxEventType type, NyxElement source, NyxDragData? data, bool dropped)
        : base(type, source, data)
    {
        Dropped = dropped;
    }

    /// <summary>True if the drag ended with a successful drop on a target.</summary>
    public bool Dropped { get; }
}

/// <summary>Arguments for click events.</summary>
public class NyxClickEventArgs : NyxMouseEventArgs
{
    public NyxClickEventArgs(NyxEventType type, NyxElement source, int x, int y)
        : base(type, source, x, y) { }
}

/// <summary>Arguments for value/text change events.</summary>
public class NyxChangedEventArgs : NyxEventArgs
{
    public NyxChangedEventArgs(NyxEventType type, NyxElement source)
        : base(type, source) { }
}
