namespace NyxGui;

/// <summary>One styled segment for <see cref="NyxExtendedLabel"/>.</summary>
public readonly struct NyxTextRun
{
    public NyxTextRun(ReadOnlySpan<char> text, NyxColor? color = null, bool underline = false)
    {
        Text = text.ToString();
        Color = color;
        Underline = underline;
    }

    public NyxTextRun(string text, NyxColor? color = null, bool underline = false)
    {
        Text = text;
        Color = color;
        Underline = underline;
    }

    public string Text { get; }
    public NyxColor? Color { get; }
    public bool Underline { get; }
}
