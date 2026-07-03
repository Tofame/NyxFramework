namespace NyxGui;

/// <summary>
/// Marker interface for widgets that want to prevent parent-level drag handling.
/// In the new event system, this is used by the input router to determine
/// whether a widget should intercept pointer events before they reach container-level handlers.
/// </summary>
public interface ICapturesPointer;
