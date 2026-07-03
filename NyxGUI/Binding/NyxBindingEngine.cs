namespace NyxGui;

/// <summary>
/// Resolves and manages widget bindings. Connects widget properties to data sources.
/// Supports one-way bindings: data → widget.
/// </summary>
public sealed class NyxBindingEngine
{
    private readonly List<BindingSubscription> _subscriptions = new();
    private INyxBindingSource? _source;

    /// <summary>The data source that provides values for bindings.</summary>
    public INyxBindingSource? Source
    {
        get => _source;
        set
        {
            _source = value;
            RefreshAll();
        }
    }

    /// <summary>Binds a widget's text property to a data path.</summary>
    public void BindText(NyxElement widget, string path)
    {
        if (widget is not NyxWidget w) return;

        var sub = new BindingSubscription(
            path,
            () =>
            {
                var value = _source?.ResolvePath(path)?.ToString() ?? string.Empty;
                if (w is NyxLabel label) label.Text = value;
                else if (w is NyxButton btn) btn.Label = value;
            },
            _source?.Subscribe(path, _ =>
            {
                var value = _source?.ResolvePath(path)?.ToString() ?? string.Empty;
                if (w is NyxLabel label) label.Text = value;
                else if (w is NyxButton btn) btn.Label = value;
            }));

        _subscriptions.Add(sub);
        sub.Update();
    }

    /// <summary>Binds a widget's visibility to a boolean data path.</summary>
    public void BindVisible(NyxElement widget, string path)
    {
        var sub = new BindingSubscription(
            path,
            () =>
            {
                var value = _source?.ResolvePath(path);
                widget.Visible = value is bool b && b;
            },
            _source?.Subscribe(path, _ =>
            {
                var value = _source?.ResolvePath(path);
                widget.Visible = value is bool b && b;
            }));

        _subscriptions.Add(sub);
        sub.Update();
    }

    /// <summary>Binds a widget's enabled state to a boolean data path.</summary>
    public void BindEnabled(NyxElement widget, string path)
    {
        var sub = new BindingSubscription(
            path,
            () =>
            {
                var value = _source?.ResolvePath(path);
                widget.Enabled = value is bool b && b;
            },
            _source?.Subscribe(path, _ =>
            {
                var value = _source?.ResolvePath(path);
                widget.Enabled = value is bool b && b;
            }));

        _subscriptions.Add(sub);
        sub.Update();
    }

    /// <summary>Re-evaluates all bindings from the current source.</summary>
    public void RefreshAll()
    {
        foreach (var sub in _subscriptions)
            sub.Update();
    }

    /// <summary>Removes all bindings and disposes subscriptions.</summary>
    public void Clear()
    {
        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();
    }

    private sealed class BindingSubscription : IDisposable
    {
        private readonly Action _update;
        private readonly IDisposable? _subscription;

        public BindingSubscription(string path, Action update, IDisposable? subscription = null)
        {
            _update = update;
            _subscription = subscription;
        }

        public void Update() => _update();
        public void Dispose() => _subscription?.Dispose();
    }
}
