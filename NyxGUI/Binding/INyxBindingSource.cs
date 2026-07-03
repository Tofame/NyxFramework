namespace NyxGui;

/// <summary>
/// Data source interface for the binding engine.
/// Host implements this to provide reactive data to widgets.
/// </summary>
public interface INyxBindingSource
{
    /// <summary>Resolves a dot-separated path to a value (e.g. "player.health" → 100).</summary>
    object? ResolvePath(string path);

    /// <summary>
    /// Subscribes to changes at a path. The callback fires when the value changes.
    /// Returns a disposable that unsubscribes when disposed.
    /// </summary>
    IDisposable Subscribe(string path, Action<object?> onChanged);
}
