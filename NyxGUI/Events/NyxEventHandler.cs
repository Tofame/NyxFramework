namespace NyxGui;

/// <summary>Delegate for routed event handlers.</summary>
public delegate void NyxEventHandler(object? sender, NyxEventArgs args);

/// <summary>
/// Event handler registration on a widget. Each widget maintains a list of handlers
/// per event type. Handlers are invoked during the bubble phase (target → root).
/// </summary>
public static class NyxEventHandlerExtensions
{
    private static readonly Dictionary<NyxElement, Dictionary<NyxEventType, List<NyxEventHandler>>> _handlers = new();

    /// <summary>Registers a handler for an event type on this widget.</summary>
    public static void AddHandler(this NyxElement element, NyxEventType eventType, NyxEventHandler handler)
    {
        if (!_handlers.TryGetValue(element, out var map))
        {
            map = new Dictionary<NyxEventType, List<NyxEventHandler>>();
            _handlers[element] = map;
        }

        if (!map.TryGetValue(eventType, out var list))
        {
            list = new List<NyxEventHandler>();
            map[eventType] = list;
        }

        list.Add(handler);
    }

    /// <summary>Removes a previously registered handler.</summary>
    public static void RemoveHandler(this NyxElement element, NyxEventType eventType, NyxEventHandler handler)
    {
        if (_handlers.TryGetValue(element, out var map) && map.TryGetValue(eventType, out var list))
            list.Remove(handler);
    }

    /// <summary>Removes all handlers from this widget (called on dispose).</summary>
    internal static void ClearHandlers(this NyxElement element)
    {
        _handlers.Remove(element);
    }

    /// <summary>Invokes handlers registered on this widget for the given event type.</summary>
    internal static void InvokeHandlers(this NyxElement element, NyxEventArgs args)
    {
        if (_handlers.TryGetValue(element, out var map) && map.TryGetValue(args.EventType, out var list))
        {
            args.CurrentTarget = element;
            foreach (var handler in list)
            {
                if (args.Handled) return;
                handler(element, args);
            }
        }
    }

    /// <summary>Raises a routed event. Bubbles from target to root if the event type bubbles.</summary>
    public static void RaiseEvent(this NyxElement target, NyxEventArgs args)
    {
        // Invoke handlers on the target
        target.InvokeHandlers(args);
        if (args.Handled) return;

        // Bubble phase: walk up the tree
        if (NyxEventMetadata.Bubbles(args.EventType))
        {
            for (var parent = target.Parent; parent is not null; parent = parent.Parent)
            {
                parent.InvokeHandlers(args);
                if (args.Handled) return;
            }
        }
    }
}
