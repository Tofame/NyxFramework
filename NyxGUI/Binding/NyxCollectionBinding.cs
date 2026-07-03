namespace NyxGui;

/// <summary>
/// Binds a collection to a container's children.
/// Creates/destroys child widgets to match the collection using diff-based sync.
/// </summary>
public sealed class NyxCollectionBinding<T>
{
    private readonly NyxContainer _container;
    private readonly Func<T, NyxElement> _itemFactory;
    private readonly Action<NyxElement, T>? _itemUpdater;
    private readonly Dictionary<int, NyxElement> _itemWidgets = new();
    private IReadOnlyList<T>? _collection;

    public NyxCollectionBinding(NyxContainer container, Func<T, NyxElement> itemFactory, Action<NyxElement, T>? itemUpdater = null)
    {
        _container = container;
        _itemFactory = itemFactory;
        _itemUpdater = itemUpdater;
    }

    /// <summary>Syncs children to match the collection. Call when the collection changes.</summary>
    public void Sync(IReadOnlyList<T> collection)
    {
        _collection = collection;

        // Remove widgets for items that no longer exist
        var toRemove = new List<int>();
        foreach (var kvp in _itemWidgets)
        {
            if (kvp.Key >= collection.Count)
                toRemove.Add(kvp.Key);
        }
        foreach (var idx in toRemove)
        {
            _container.RemoveChild(_itemWidgets[idx]);
            _itemWidgets.Remove(idx);
        }

        // Add/update widgets for current items
        for (var i = 0; i < collection.Count; i++)
        {
            if (!_itemWidgets.TryGetValue(i, out var widget))
            {
                widget = _itemFactory(collection[i]);
                _container.AddChild(widget);
                _itemWidgets[i] = widget;
            }
            else if (_itemUpdater is not null)
            {
                _itemUpdater(widget, collection[i]);
            }
        }
    }

    /// <summary>Removes all bound children and clears the binding.</summary>
    public void Clear()
    {
        foreach (var widget in _itemWidgets.Values)
            _container.RemoveChild(widget);
        _itemWidgets.Clear();
        _collection = null;
    }
}
