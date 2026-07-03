namespace NyxGui.Lists;

/// <summary>
/// Reusable row pool for <see cref="NyxScrollablePanel"/> lists (plan §6.1 / Phase 3).
/// Diffs by entry key, preserves scroll offset when possible, avoids full <c>set_children</c> swaps.
/// </summary>
public sealed class NyxGuiRowList<TEntry>
{
    private readonly NyxScrollablePanel _scroll;
    private readonly NyxWidgetStates _rowStates;
    private readonly Dictionary<string, (NyxButton Button, TEntry Entry)> _rows = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NyxButton> _order = new();

    public NyxGuiRowList(NyxScrollablePanel scroll, NyxWidgetStates rowStates)
    {
        _scroll = scroll;
        _rowStates = rowStates;
    }

    public int RowHeight { get; set; } = 28;

    public int RowGap { get; set; } = 2;

    public int PadX { get; set; } = 4;

    public int PadTop { get; set; } = 4;

    public IReadOnlyList<NyxButton> Rows => _order;

    /// <summary>Raised when a row button is clicked (entry is the current data for that key).</summary>
    public event Action<NyxButton, TEntry>? RowClicked;

    /// <summary>
    /// Ensures one row per <paramref name="entries"/> item (add / remove / update by key).
    /// </summary>
    public void Sync(
        IReadOnlyList<TEntry> entries,
        Func<TEntry, string> getKey,
        Func<TEntry, string> getTitle,
        Action<NyxButton, TEntry>? onCreate = null,
        Action<NyxButton, TEntry>? onUpdate = null)
    {
        var scrollY = _scroll.ScrollOffsetY;
        var nextKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var key = getKey(entry);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            nextKeys.Add(key);
            if (!_rows.TryGetValue(key, out var row))
            {
var btn = new NyxButton
            {
                Id = $"row_{key}",
                Label = getTitle(entry),
                Description = key,
            };
                _rowStates.CopyTo(btn.States);
                var capturedKey = key;
                btn.Click += (_, _) =>
                {
                    if (_rows.TryGetValue(capturedKey, out var hit))
                        RowClicked?.Invoke(hit.Button, hit.Entry);
                };
                onCreate?.Invoke(btn, entry);
                _scroll.AddToBody(btn);
                row = (btn, entry);
                _rows[key] = row;
            }
            else
            {
                row.Button.Label = getTitle(entry);
                onUpdate?.Invoke(row.Button, entry);
                _rows[key] = (row.Button, entry);
            }
        }

        var toRemove = _rows.Keys.Where(k => !nextKeys.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            if (_rows.Remove(key, out var removed))
            {
                _scroll.Body.RemoveChild(removed.Button);
            }
        }

        _order.Clear();
        foreach (var entry in entries)
        {
            var key = getKey(entry);
            if (_rows.TryGetValue(key, out var row))
                _order.Add(row.Button);
        }

        RelayoutRows();
        RestoreScroll(scrollY);
    }

    /// <summary>Repositions rows without changing the set of entries.</summary>
    public void RelayoutRows()
    {
        if (_order.Count == 0)
            return;

        var clientW = Math.Max(48, _scroll.ClientRect.Width - PadX * 2);
        var localY = PadTop;

        foreach (var btn in _order)
        {
            _scroll.SetBodyChildBounds(btn, PadX, localY, clientW, RowHeight);
            localY += RowHeight + RowGap;
        }

        var scrollY = _scroll.ScrollOffsetY;
        _scroll.RefreshLayout();
        RestoreScroll(scrollY);
    }

    public bool TryGetButton(string key, out NyxButton? button)
    {
        if (_rows.TryGetValue(key, out var row))
        {
            button = row.Button;
            return true;
        }

        button = null;
        return false;
    }

    public void SetSelected(NyxButton? selected)
    {
        foreach (var btn in _order)
            btn.IsSelected = ReferenceEquals(btn, selected);
    }

    private void RestoreScroll(int scrollY)
    {
        var client = _scroll.ClientRect;
        var contentH = Math.Max(_scroll.ContentExtentHeight, client.Height);
        var extent = Math.Max(0, contentH - client.Height);
        var clamped = Math.Clamp(scrollY, 0, extent);
        if (clamped == _scroll.ScrollOffsetY)
            return;

        _scroll.ScrollTo(clamped);
    }
}
