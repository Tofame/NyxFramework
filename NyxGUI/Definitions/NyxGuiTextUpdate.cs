namespace NyxGui.Definitions;

/// <summary>Text vs layout dirty rules (plan §6.4).</summary>
internal static class NyxGuiTextUpdate
{
    public static void ApplyText(NyxElement element, string text)
    {
        switch (element)
        {
            case NyxLabel label:
                label.Text = text;
                break;
            case NyxExtendedLabel extended:
                extended.Text = text;
                break;
            case NyxButton button:
                button.Label = text;
                break;
            case NyxTextBox box:
                box.Text = text;
                break;
        }
    }

    /// <summary>
    /// Fixed-size, non-wrapping labels with unchanged measured footprint skip relayout.
    /// </summary>
    public static bool CanSkipLayoutForTextChange(NyxLabel label, string oldText, string newText)
    {
        if (label.Wrap)
            return false;

        var box = label.LayoutBox;
        if (box is null)
            return false;

        if (box.FixedWidth <= 0 && box.FixedHeight <= 0)
            return false;

        if (!UsesOnlyFixedAxes(box))
            return false;

        return oldText.Length == newText.Length || !MightChangeLineCount(oldText, newText);
    }

    private static bool UsesOnlyFixedAxes(NyxLayoutBox box) =>
        box.FixedWidth > 0 && box.FixedHeight > 0 &&
        !box.HasAnyAnchor;

    private static bool MightChangeLineCount(string oldText, string newText) =>
        oldText.Contains('\n', StringComparison.Ordinal) != newText.Contains('\n', StringComparison.Ordinal);
}
