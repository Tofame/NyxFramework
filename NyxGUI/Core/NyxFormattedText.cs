namespace NyxGui;

/// <summary>
/// Parses simple inline markup into <see cref="NyxTextRun"/> segments.
///
/// The parser does a single left-to-right pass over the text, tracking:
/// <list type="bullet">
///   <item><c>plainStart</c> — the index after the last closing tag, where plain text resumes.</item>
///   <item><c>currentColor</c> — the active color (null = use default).</item>
///   <item><c>underline</c> — whether underline mode is active.</item>
/// </list>
///
/// When an opening tag <c>{…}</c> is encountered, all text from <c>plainStart</c> to
/// the tag is emitted as a run with the current style.  The tag's effect is then applied
/// and <c>plainStart</c> advances past the closing <c>}</c>.
///
/// <b>Supported tags:</b>
/// <list type="bullet">
///   <item><c>{#rrggbb}</c> — set color, close with <c>{/}</c> or <c>{/color}</c>.</item>
///   <item><c>{color=#rrggbb}</c> — explicit color tag.</item>
///   <item><c>{underline}</c> / <c>{/underline}</c> — toggle underline.</item>
/// </list>
/// </summary>
public static class NyxFormattedText
{
    /// <summary>
    /// Supports <c>{#rrggbb}…{/}</c>, <c>{color=#rrggbb}…{/color}</c>, and <c>{underline}…{/underline}</c>.
    /// </summary>
    public static IReadOnlyList<NyxTextRun> Parse(ReadOnlySpan<char> markup, NyxColor defaultColor) =>
        Parse(markup.ToString(), defaultColor);

    public static IReadOnlyList<NyxTextRun> Parse(string markup, NyxColor defaultColor)
    {
        if (string.IsNullOrEmpty(markup))
            return Array.Empty<NyxTextRun>();

        var runs = new List<NyxTextRun>();
        NyxColor? currentColor = null;
        var underline = false;
        var i = 0;
        var plainStart = 0;

        while (i < markup.Length)
        {
            if (markup[i] != '{')
            {
                i++;
                continue;
            }

            var close = markup.IndexOf('}', i);
            if (close < 0)
                break;

            var tag = markup.AsSpan(i + 1, close - i - 1);
            if (i > plainStart)
                runs.Add(new NyxTextRun(markup[plainStart..i], currentColor ?? defaultColor, underline));
            i = close + 1;

            if (tag.Length == 0)
                continue;

            if (tag[0] == '#')
            {
                if (NyxColor.TryParseHex(tag, out var c))
                    currentColor = c;
                plainStart = i;
                continue;
            }

            if (tag.Equals("/".AsSpan(), StringComparison.Ordinal) ||
                tag.Equals("/color".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                currentColor = null;
                underline = false;
                plainStart = i;
                continue;
            }

            if (tag.Equals("underline".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                underline = true;
                plainStart = i;
                continue;
            }

            if (tag.Equals("/underline".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                underline = false;
                plainStart = i;
                continue;
            }

            if (tag.StartsWith("color=".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                var hex = tag["color=".Length..];
                if (NyxColor.TryParseHex(hex, out var c))
                    currentColor = c;
                plainStart = i;
                continue;
            }

            plainStart = i;
        }

        if (markup.Length > plainStart)
            runs.Add(new NyxTextRun(markup[plainStart..], currentColor ?? defaultColor, underline));

        return runs;
    }
}
