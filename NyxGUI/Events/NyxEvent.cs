namespace NyxGui;

/// <summary>
/// Event types for the routed event system.
/// Each event specifies whether it bubbles up the tree.
/// </summary>
public enum NyxEventType
{
    // Mouse events
    MouseDown,        // bubbles
    MouseUp,          // bubbles
    MouseMove,        // does not bubble
    MouseWheel,       // bubbles
    MouseEnter,       // does not bubble
    MouseLeave,       // does not bubble

    // Keyboard events
    KeyDown,          // bubbles
    KeyUp,            // bubbles
    TextInput,        // bubbles

    // Focus events
    FocusGained,      // does not bubble
    FocusLost,        // does not bubble

    // Drag events
    DragStart,        // bubbles
    DragEnter,        // bubbles
    DragLeave,        // bubbles
    DragOver,         // bubbles
    Drop,             // bubbles
    DragEnd,          // bubbles

    // Widget-specific
    Click,            // bubbles
    RightClick,       // bubbles
    TextChanged,      // does not bubble
    SelectionChanged, // does not bubble
    ValueChanged,     // does not bubble
    Checked,          // bubbles
    Unchecked,        // bubbles
	DoubleClick,      // bubbles
}

/// <summary>Metadata about an event type used by the routing system.</summary>
public static class NyxEventMetadata
{
    private static readonly Dictionary<NyxEventType, bool> _bubbleMap = new()
    {
        [NyxEventType.MouseDown] = true,
        [NyxEventType.MouseUp] = true,
        [NyxEventType.MouseMove] = false,
        [NyxEventType.MouseWheel] = true,
        [NyxEventType.MouseEnter] = false,
        [NyxEventType.MouseLeave] = false,
        [NyxEventType.KeyDown] = true,
        [NyxEventType.KeyUp] = true,
        [NyxEventType.TextInput] = true,
        [NyxEventType.FocusGained] = false,
        [NyxEventType.FocusLost] = false,
        [NyxEventType.DragStart] = true,
        [NyxEventType.DragEnter] = true,
        [NyxEventType.DragLeave] = true,
        [NyxEventType.DragOver] = true,
        [NyxEventType.Drop] = true,
        [NyxEventType.DragEnd] = true,
        [NyxEventType.Click] = true,
        [NyxEventType.RightClick] = true,
        [NyxEventType.TextChanged] = false,
        [NyxEventType.SelectionChanged] = false,
        [NyxEventType.ValueChanged] = false,
        [NyxEventType.Checked] = true,
        [NyxEventType.Unchecked] = true,
		[NyxEventType.DoubleClick] = true,
    };

    /// <summary>True if this event type bubbles from target to root.</summary>
    public static bool Bubbles(NyxEventType type) => _bubbleMap.TryGetValue(type, out var b) && b;
}
