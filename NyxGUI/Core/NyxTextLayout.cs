namespace NyxGui;

/// <summary>Source span for one displayed line (for caret mapping).</summary>
public readonly struct NyxTextLineRange
{
    public NyxTextLineRange(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;
}

/// <summary>Word-wrap and multi-line text layout (fixed character width for metrics).
///
/// <b>Word-wrap algorithm (<see cref="WrapParagraphRanges"/>):</b>
/// Each <c>\n</c>-delimited paragraph is split into display lines based on max pixel width.
/// Words separated by spaces are kept together; a word longer than maxWidth is force-broken.
/// Trailing spaces on each line are trimmed.
/// </summary>
public static class NyxTextLayout
{
    public const int DefaultCharWidth = 7;

    public static int MeasureWidth(ReadOnlySpan<char> text, int charWidth = DefaultCharWidth) =>
        text.Length * charWidth;

    /// <summary>Splits on <c>\n</c> and optionally word-wraps each paragraph to <paramref name="maxWidthPx"/>.</summary>
    public static List<string> BuildLines(string text, bool wrap, int maxWidthPx, int charWidth = DefaultCharWidth)
    {
        var ranges = BuildLineRanges(text, wrap, maxWidthPx, charWidth);
        if (ranges.Count == 0)
            return [string.Empty];

        if (string.IsNullOrEmpty(text))
            return [string.Empty];

        var lines = new List<string>(ranges.Count);
        foreach (var range in ranges)
            lines.Add(text.Substring(range.Start, range.Length));

        return lines;
    }

    /// <summary>Maps each displayed line back to indices in the source <paramref name="text"/>.</summary>
    public static List<NyxTextLineRange> BuildLineRanges(string text, bool wrap, int maxWidthPx, int charWidth = DefaultCharWidth)
    {
        var ranges = new List<NyxTextLineRange>();
        if (string.IsNullOrEmpty(text))
        {
            ranges.Add(new NyxTextLineRange(0, 0));
            return ranges;
        }

        var normalized = text.Replace("\r", string.Empty, StringComparison.Ordinal);
        var paragraphs = normalized.Split('\n');
        var maxChars = Math.Max(1, maxWidthPx / Math.Max(1, charWidth));
        var srcIndex = 0;

        for (var p = 0; p < paragraphs.Length; p++)
        {
            var paragraph = paragraphs[p];
            if (!wrap || maxWidthPx <= charWidth)
            {
                ranges.Add(new NyxTextLineRange(srcIndex, paragraph.Length));
            }
            else
                WrapParagraphRanges(paragraph, srcIndex, maxChars, ranges);

            srcIndex += paragraph.Length;
            if (p < paragraphs.Length - 1)
                srcIndex++;
        }

        if (ranges.Count == 0)
            ranges.Add(new NyxTextLineRange(0, 0));

        return ranges;
    }

    public static bool TryMapCaretToDisplayLine(
        string text,
        bool wrap,
        int maxWidthPx,
        int caretIndex,
        int charWidth,
        out int displayLineIndex,
        out int columnInLine)
    {
        displayLineIndex = 0;
        columnInLine = 0;
        var ranges = BuildLineRanges(text, wrap, maxWidthPx, charWidth);
        if (ranges.Count == 0)
            return true;

        caretIndex = Math.Clamp(caretIndex, 0, text.Length);

        for (var i = 0; i < ranges.Count; i++)
        {
            var range = ranges[i];
            if (caretIndex <= range.End || i == ranges.Count - 1)
            {
                displayLineIndex = i;
                columnInLine = Math.Clamp(caretIndex - range.Start, 0, range.Length);
                return true;
            }
        }

        var last = ranges[^1];
        displayLineIndex = ranges.Count - 1;
        columnInLine = Math.Clamp(caretIndex - last.Start, 0, last.Length);
        return true;
    }

    public static int MapDisplayPositionToIndex(
        string text,
        bool wrap,
        int maxWidthPx,
        int displayLineIndex,
        int columnInLine,
        int charWidth = DefaultCharWidth)
    {
        var ranges = BuildLineRanges(text, wrap, maxWidthPx, charWidth);
        if (ranges.Count == 0)
            return 0;

        displayLineIndex = Math.Clamp(displayLineIndex, 0, ranges.Count - 1);
        var range = ranges[displayLineIndex];
        columnInLine = Math.Max(0, columnInLine);
        return Math.Clamp(range.Start + columnInLine, 0, text.Length);
    }

    private static void WrapParagraphRanges(string paragraph, int paragraphStart, int maxChars, List<NyxTextLineRange> ranges)
    {
        if (paragraph.Length == 0)
        {
            ranges.Add(new NyxTextLineRange(paragraphStart, 0));
            return;
        }

        var start = 0;
        while (start < paragraph.Length)
        {
            var remaining = paragraph.Length - start;
            if (remaining <= maxChars)
            {
                ranges.Add(new NyxTextLineRange(paragraphStart + start, remaining));
                return;
            }

            var len = maxChars;
            var slice = paragraph.AsSpan(start, len);
            var breakAt = slice.LastIndexOf(' ');
            if (breakAt > 0)
                len = breakAt;

            var piece = paragraph.Substring(start, len).TrimEnd();
            ranges.Add(new NyxTextLineRange(paragraphStart + start, piece.Length));
            start += len;
            while (start < paragraph.Length && paragraph[start] == ' ')
                start++;
        }
    }

    public static void PaintMultiline(
        INyxGuiPainter painter,
        NyxRect bounds,
        IReadOnlyList<string> lines,
        NyxTextAlign align,
        int lineHeight,
        NyxColor color,
        NyxFontStyle? font = null)
    {
        if (lines.Count == 0)
            return;

        var blockH = lines.Count * lineHeight;
        var y = align == NyxTextAlign.Center
            ? bounds.Y + Math.Max(0, (bounds.Height - blockH) / 2)
            : bounds.Y;

        var lineAlign = align == NyxTextAlign.Center ? NyxTextAlign.TopCenter : align;

        foreach (var line in lines)
        {
            if (y + lineHeight > bounds.Bottom)
                break;

            var lineRect = new NyxRect(bounds.X, y, bounds.Width, lineHeight);
            painter.DrawText(lineRect, line, lineAlign, color, font);
            y += lineHeight;
        }
    }
}
