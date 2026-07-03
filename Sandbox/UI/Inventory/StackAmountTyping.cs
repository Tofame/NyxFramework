namespace Sandbox.UI.Inventory;

/// <summary>NyxClient-style digit entry while moving a stack (7 then 1 → 71).</summary>
internal sealed class StackAmountTyping
{
    private string _digits = "";

    public void Clear() => _digits = "";

    public bool HasDigits => _digits.Length > 0;

    public void AppendDigitSequence(int value, int maxCount)
    {
        _digits = "";
        foreach (var ch in value.ToString())
        {
            if (ch is >= '0' and <= '9')
                AppendDigit(ch - '0', maxCount);
        }
    }

    public void AppendDigit(int digit, int maxCount)
    {
        if (digit is < 0 or > 9)
            return;

        _digits += (char)('0' + digit);
        if (_digits.Length > 5)
            _digits = _digits[^5..];

        var parsed = ParseOrZero();
        if (parsed > maxCount)
            _digits = maxCount.ToString();
        else if (parsed == 0 && _digits.Length > 1)
            _digits = "0";
    }

    public ushort? TryGetAmount(int maxCount)
    {
        if (_digits.Length == 0)
            return null;

        var n = ParseOrZero();
        if (n < 1)
            return 1;
        return (ushort)Math.Min(n, maxCount);
    }

    private int ParseOrZero() => int.TryParse(_digits, out var n) ? n : 0;
}
