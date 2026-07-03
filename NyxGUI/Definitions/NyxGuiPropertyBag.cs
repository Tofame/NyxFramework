using System.Collections;

namespace NyxGui.Definitions;

/// <summary>Unified property access for builder (Lua <c>snake_case</c> keys only).</summary>
internal sealed class NyxGuiPropertyBag : IEnumerable<KeyValuePair<string, object?>>
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    public NyxGuiPropertyBag(IReadOnlyDictionary<string, object?> values) => _values = values;

    public bool TryGetString(string key, out string value)
    {
        value = string.Empty;
        if (!TryGet(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            string s => s,
            _ => obj.ToString() ?? string.Empty
        };
        return true;
    }

    public bool TryGetBool(string key, out bool value)
    {
        value = false;
        if (!TryGet(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            bool b => b,
            string s => bool.TryParse(s, out var b) && b,
            long l => l != 0,
            int i => i != 0,
            double d => d != 0,
            float f => f != 0,
            _ => false
        };
        return true;
    }

    public bool TryGetFloat(string key, out float value)
    {
        value = 0;
        if (!TryGet(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            float f => f,
            double d => (float)d,
            long l => l,
            int i => i,
            string s => float.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0,
            _ => 0
        };
        return true;
    }

    public bool TryGetInt(string key, out int value)
    {
        value = 0;
        if (!TryGet(key, out var obj) || obj is null)
            return false;
        value = obj switch
        {
            int i => i,
            long l => (int)l,
            float f => (int)f,
            double d => (int)d,
            string s => int.TryParse(s, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var i) ? i : 0,
            _ => 0
        };
        return true;
    }

    public bool TryGetNested(string key, out NyxGuiPropertyBag nested)
    {
        nested = new NyxGuiPropertyBag(new Dictionary<string, object?>());
        if (!TryGet(key, out var obj) || obj is null)
            return false;
        if (obj is IReadOnlyDictionary<string, object?> dict)
        {
            nested = new NyxGuiPropertyBag(dict);
            return true;
        }

        return false;
    }

    public IEnumerable<KeyValuePair<string, object?>> Entries =>
        _values.Select(static kv => new KeyValuePair<string, object?>(kv.Key, kv.Value));

    public IEnumerable<string> Keys => _values.Keys;

    public bool TryGetValue(string key, out object? value) => TryGet(key, out value);

    public bool ContainsKey(string key)
    {
        if (TryGet(key, out _))
            return true;
        return false;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static NyxGuiPropertyBag From(IReadOnlyDictionary<string, object?> values) => new(values);

    public static NyxGuiPropertyBag? TryWrap(object? value) => value switch
    {
        NyxGuiPropertyBag bag => bag,
        IReadOnlyDictionary<string, object?> dict => From(dict),
        IDictionary<string, object?> dict => From(new Dictionary<string, object?>(dict)),
        _ => null,
    };

    private bool TryGet(string key, out object? value)
    {
        if (_values.TryGetValue(key, out value))
            return true;

        var normalized = key.Replace("-", "_");
        if (_values.TryGetValue(normalized, out value))
            return true;

        var dashed = key.Replace("_", "-");
        if (_values.TryGetValue(dashed, out value))
            return true;

        return false;
    }
}

internal static class NyxGuiKeyNames
{
    /// <summary>Normalizes Lua/TOML keys to canonical <c>snake_case</c> at load time.</summary>
    public static string ToSnakeCase(string key) => key.Replace("-", "_");

    public static bool IsImagePropertyKey(string name) =>
        name.StartsWith("image_", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("object_fit", StringComparison.OrdinalIgnoreCase);
}
