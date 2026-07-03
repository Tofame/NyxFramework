namespace NyxGui;

/// <summary>Small popup menu shown at a screen position (e.g. right-click on a widget).</summary>
public sealed class NyxContextMenu : NyxContainer
{
    private const int Pad = 4;
    private const int RowHeight = 22;
    private const int MinWidth = 160;

    private readonly List<Action> _actions = new();

    public NyxContextMenu(uint internalId = 0)
        : base(NyxRect.Empty, internalId)
    {
        Visible = false;
        States.Normal.BackgroundColor = NyxColor.FromRgb(45, 45, 48);
        States.Normal.BorderWidth = 1;
        States.Normal.BorderColor = NyxColor.FromRgb(100, 100, 110);
    }

    public bool IsOpen => Visible;

    public void SetItems(IReadOnlyList<(string Label, Action OnClick)> items)
    {
        ClearChildren();
        _actions.Clear();

        if (items.Count == 0)
        {
            SetBounds(NyxRect.Empty);
            return;
        }

        var maxLabelW = 0;
        foreach (var (label, _) in items)
            maxLabelW = Math.Max(maxLabelW, label.Length * 7);

        var w = Math.Max(MinWidth, maxLabelW + Pad * 2 + 16);
        var y = Pad;

        for (var i = 0; i < items.Count; i++)
        {
            var (label, onClick) = items[i];
            var btn = new NyxButton(label) { Bounds = new NyxRect(Pad, y, w - Pad * 2, RowHeight) };
            var index = i;
            btn.Click += (_, _) =>
            {
                var action = _actions[index];
                Close();
                action();
            };
            AddChild(btn);
            _actions.Add(onClick);
            y += RowHeight + 2;
        }

        SetBounds(new NyxRect(0, 0, w, y + Pad));
    }

    public void Open(int x, int y)
    {
        if (Children.Count == 0)
            return;

        SetBounds(new NyxRect(x, y, Bounds.Width, Bounds.Height));
        Visible = true;
        BringToFront();
    }

    public void Close() => Visible = false;

    private void BringToFront()
    {
        if (Parent is NyxContainer parent)
            parent.BringChildToFront(this);
    }
}
