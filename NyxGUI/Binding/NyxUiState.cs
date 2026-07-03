namespace NyxGui.Binding;

/// <summary>Keyed value bag for <c>bind_key</c> widgets (e.g. <c>sandbox.fps</c>).</summary>
public sealed class NyxUiState : INyxUiValueSource
{
    private readonly Dictionary<string, Func<string>> _providers = new(StringComparer.Ordinal);

    public void Register(string bindKey, Func<string> provider) =>
        _providers[bindKey] = provider;

    public void Register(string bindKey, Func<object> provider) =>
        _providers[bindKey] = () => provider()?.ToString() ?? string.Empty;

    public bool TryGetString(string bindKey, out string value)
    {
        value = string.Empty;
        if (!_providers.TryGetValue(bindKey, out var fn))
            return false;
        value = fn();
        return true;
    }
}
