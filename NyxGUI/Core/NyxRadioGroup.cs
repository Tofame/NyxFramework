namespace NyxGui;

/// <summary>
/// Mutual exclusion for <see cref="NyxRadioButton"/> widgets sharing a <see cref="NyxRadioButton.Group"/> name.
///
/// The static <c>Groups</c> dictionary maps group names to lists of radio buttons.
/// When a button in a group is selected, all other buttons in the same group are unselected.
/// Buttons auto-register on construction and unregister on disposal/detachment.
/// Groups are automatically cleaned up when the last button is removed.
/// </summary>
public static class NyxRadioGroup
{
    private static readonly Dictionary<string, List<NyxRadioButton>> Groups = new(StringComparer.OrdinalIgnoreCase);

    internal static void Register(NyxRadioButton button)
    {
        if (string.IsNullOrWhiteSpace(button.Group))
            return;

        if (!Groups.TryGetValue(button.Group, out var list))
        {
            list = [];
            Groups[button.Group] = list;
        }

        if (!list.Contains(button))
            list.Add(button);
    }

    internal static void Unregister(NyxRadioButton button)
    {
        if (string.IsNullOrWhiteSpace(button.Group))
            return;

        if (!Groups.TryGetValue(button.Group, out var list))
            return;

        list.Remove(button);
        if (list.Count == 0)
            Groups.Remove(button.Group);
    }

    internal static void Select(NyxRadioButton selected)
    {
        if (string.IsNullOrWhiteSpace(selected.Group))
            return;

        if (!Groups.TryGetValue(selected.Group, out var list))
            return;

        foreach (var rb in list)
        {
            if (ReferenceEquals(rb, selected))
                continue;
            if (rb.IsChecked)
                rb.SetCheckedSilently(false);
        }
    }
}
