using NyxGui;
using Sandbox.Items;

namespace Sandbox.UI.Inventory;

/// <summary>Backpack / chest slot grid inside a scrollable <see cref="NyxMiniWindow"/> body.</summary>
internal sealed class UIContainer
{
    public const int DefaultColumns = 6;
    public const int MaxVisibleRows = 4;
    public const int ScrollBarWidth = 12;
    public const int SlotSize = 36;
    public const int SlotGap = 4;

    private readonly ItemStorage _storage;
    private readonly UISlotHost _slotHost;
    private readonly UISlot[] _slots;
    private readonly NyxContainer[] _frames;
    private NyxContainer? _outerHost;
    private NyxScrollablePanel? _scroll;
    private bool _built;

    public UIContainer(ItemStorage storage, UISlotHost slotHost, int columns = DefaultColumns)
    {
        _storage = storage;
        _slotHost = slotHost;
        Columns = Math.Max(1, columns);
        _slots = new UISlot[storage.Capacity];
        _frames = new NyxContainer[storage.Capacity];
    }

    public int Columns { get; }

    public int RowCount => (_storage.Capacity + Columns - 1) / Columns;

    /// <summary>Rows shown in the viewport before scrolling (capped by <see cref="MaxVisibleRows"/>).</summary>
    public int ViewportRows => Math.Clamp(RowCount, 1, MaxVisibleRows);

    public static int RowPitch => SlotSize + SlotGap;

    public int GridWidth => Columns * SlotSize + (Columns - 1) * SlotGap;

    public int ContentHeight => RowCount > 0
        ? RowCount * SlotSize + (RowCount - 1) * SlotGap
        : 0;

    public int ViewportHeight => ViewportRows > 0
        ? ViewportRows * SlotSize + (ViewportRows - 1) * SlotGap
        : SlotSize;

    public bool NeedsVerticalScroll => RowCount > ViewportRows;

    public void BuildInto(NyxContainer backpackSlotsHost)
    {
        if (_built)
            return;

        _built = true;
        _outerHost = backpackSlotsHost;
        backpackSlotsHost.ClearChildren();

        _scroll = new NyxScrollablePanel(NyxRect.Empty)
        {
            Id = "ContainerScroll",
        };
        _scroll.LayoutBox = new NyxLayoutBox
        {
            Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
            Right = NyxLayoutAnchor.WidgetEdge("ContainerScrollBar", NyxAnchorEdge.Left),
            Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
            Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom),
        };
		var scrollbar = new NyxVScrollBar(NyxRect.Empty)
		{
			Id = "ContainerScrollBar",
		};
		scrollbar.LayoutBox = new NyxLayoutBox
		{
			Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom),
			FixedWidth = ScrollBarWidth,
		};
		_scroll.VerticalScrollBar = scrollbar;
        _scroll.ScrollChanged += (_, _) => RelayoutSlots();
		backpackSlotsHost.AddChild(scrollbar);
        backpackSlotsHost.AddChild(_scroll);
        _slotHost.RegisterContainer(_storage, backpackSlotsHost, SyncSlots);

        for (var i = 0; i < _storage.Capacity; i++)
        {
            var border = NyxColor.FromRgb(72, 68, 78);
            var frame = new NyxContainer(new NyxRect(0, 0, SlotSize, SlotSize))
            {
                Id = $"container_slot_frame_{i}",
            };
            frame.States.Normal.BorderWidth = 1;
            frame.States.Normal.BorderColor = border;
            frame.States.Normal.BackgroundColor = NyxColor.FromRgb(32, 30, 36);

            var index = i;
            var slot = new UISlot(new NyxRect(0, 0, SlotSize, SlotSize), _slotHost)
            {
                Id = $"container_slot_{i}",
            };
            slot.SetChromeFrame(frame, border);
            slot.ReadItem = () => _storage[index];
            slot.WriteItem = item => _storage[index] = item;
            slot.ContainerStorage = _storage;
            slot.NotifyContainerChanged = SyncSlots;
            frame.AddChild(slot);
            _scroll.Body.AddChild(frame);
            _frames[i] = frame;
            _slots[i] = slot;
        }

        RelayoutSlots();
    }

    public void RelayoutSlots()
    {
        if (_outerHost is null || _scroll is null)
            return;

        var scrollbar = _scroll.VerticalScrollBar;
        if (scrollbar is not null)
        {
            scrollbar.Visible = NeedsVerticalScroll;
            if (scrollbar.Visible)
            {
                _scroll.LayoutBox!.Right = NyxLayoutAnchor.WidgetEdge("ContainerScrollBar", NyxAnchorEdge.Left);
            }
            else
            {
                _scroll.LayoutBox!.Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right);
            }
        }
        _scroll.ContentExtentHeight = ContentHeight;
        _scroll.RefreshLayout();

        var client = _scroll.ClientRect;
        if (client.Width < SlotSize || client.Height < SlotSize)
            return;

        var originX = Math.Max(0, (client.Width - GridWidth) / 2);
        var body = _scroll.Body;

        for (var i = 0; i < _storage.Capacity; i++)
        {
            var col = i % Columns;
            var row = i / Columns;
            SlotGridLayout.PlaceSlotPair(
                body,
                _frames[i],
                _slots[i],
                originX + col * (SlotSize + SlotGap),
                row * (SlotSize + SlotGap),
                SlotSize,
                SlotSize);
        }
    }

    public int GetPreferredWindowWidth()
    {
        var bar = NeedsVerticalScroll ? ScrollBarWidth : 0;
        if (_outerHost?.LayoutBox is { } box)
            return GridWidth + box.Margin.Left + box.Margin.Right + bar;

        return GridWidth + bar;
    }

    public int GetPreferredWindowHeight(int titleBarHeight)
    {
        var bottomInset = 0;
        if (FindMiniWindow(_outerHost) is { Image.ImageBorders: { HasAny: true } borders } mini)
            bottomInset = borders.Bottom;

        SlotGridLayout.GetHostMargins(_outerHost!, out var marginTop, out var marginBottom, out _, out _);
        return titleBarHeight + bottomInset + marginTop + marginBottom + ViewportHeight;
    }

    public void ApplyMiniWindowSize(NyxMiniWindow window)
    {
        var w = GetPreferredWindowWidth();
        var h = GetPreferredWindowHeight(window.TitleBarHeight);
        var b = window.Bounds;
        window.SetBounds(new NyxRect(b.X, b.Y, w, h));
        RelayoutSlots();
    }

    public void SyncSlots()
    {
        foreach (var slot in _slots)
            slot?.Refresh();
    }

    private static NyxMiniWindow? FindMiniWindow(NyxElement? node)
    {
        for (var n = node; n is not null; n = n.Parent)
        {
            if (n is NyxMiniWindow mini)
                return mini;
        }

        return null;
    }
}
