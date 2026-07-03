namespace NyxGui;

/// <summary>
/// Arbitrary payload carried during a drag operation.
/// </summary>
public sealed class NyxDragData
{
    public NyxDragData(object payload)
    {
        Payload = payload;
        DataType = payload.GetType().Name;
    }

    public NyxDragData(object payload, string dataType)
    {
        Payload = payload;
        DataType = dataType;
    }

    /// <summary>The dragged object.</summary>
    public object Payload { get; }

    /// <summary>Type identifier for drop-target filtering (e.g. "item", "equipment").</summary>
    public string DataType { get; }

    /// <summary>Optional source widget that initiated the drag.</summary>
    public NyxElement? Source { get; internal set; }

    public T As<T>() => (T)Payload;
}
