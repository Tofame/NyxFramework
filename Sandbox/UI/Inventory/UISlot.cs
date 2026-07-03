using NyxGui;
using Sandbox.Items;
using Sandbox.NyxGUI_Extend;

namespace Sandbox.UI.Inventory;

/// <summary>Single inventory slot (equipment or container). Reads/writes via optional delegates.</summary>
internal sealed class UISlot : NyxElement, ICapturesPointer
{
    private static readonly NyxColor DropHighlightColor = NyxColor.FromRgb(255, 255, 255);

    private bool _dropHighlight;
    private NyxColor _normalBorderColor = NyxColor.FromRgb(80, 80, 90);
    private int _normalBorderWidth = 1;

    /// <summary>Avoids re-rasterizing icons when the backing <see cref="Item"/> did not change.</summary>
    private int _paintedVisualSignature = int.MinValue;

    public UISlot(NyxRect bounds, UISlotHost host, uint internalId = 0)
        : base(internalId)
    {
        Host = host;
        SetBounds(bounds);
        ItemIcon = new UIItem(new NyxRect(0, 0, bounds.Width, bounds.Height));
        host.Register(this);
    }

    public UISlotHost Host { get; }

    public UIItem ItemIcon { get; }

    /// <summary>Slot chrome panel (equipment/container frame) for drop-target border.</summary>
    public NyxContainer? ChromeFrame { get; set; }

    public void SetChromeFrame(NyxContainer frame, NyxColor normalBorder, int normalBorderWidth = 1)
    {
        ChromeFrame = frame;
        _normalBorderColor = normalBorder;
        _normalBorderWidth = normalBorderWidth;
    }

    public Func<Item>? ReadItem { get; set; }

    public Action<Item>? WriteItem { get; set; }

    /// <summary>Backing storage for backpack slots. Slot-targeted drops use the hovered index; panel drops use <see cref="ItemStoragePlacement.Insert"/>.</summary>
    public ItemStorage? ContainerStorage { get; set; }

    /// <summary>When set, only items matching this wear slot may be placed here.</summary>
    public EquipmentSlot? BoundEquipmentSlot { get; set; }

    /// <summary>Refreshes all slots after a container-wide insert (set by <see cref="UIContainer"/>).</summary>
    public Action? NotifyContainerChanged { get; set; }

    public Item Item { get; private set; } = Item.Empty;

    public void SetDropHighlight(bool on)
    {
        if (_dropHighlight == on)
            return;

        _dropHighlight = on;
        if (ChromeFrame is null)
            return;

        if (on)
        {
            ChromeFrame.States.Normal.BorderWidth = 2;
            ChromeFrame.States.Normal.BorderColor = DropHighlightColor;
        }
        else
        {
            ChromeFrame.States.Normal.BorderWidth = _normalBorderWidth;
            ChromeFrame.States.Normal.BorderColor = _normalBorderColor;
        }
    }

    public void Refresh()
    {
        var item = ReadItem?.Invoke() ?? Item.Empty;
        if (item.IconDisplaySignature == _paintedVisualSignature)
            return;

        ApplyItem(item, commit: false);
    }

    public void SetItem(Item item) => ApplyItem(item, commit: true);

    private void ApplyItem(Item item, bool commit)
    {
        if (commit)
            WriteItem?.Invoke(item);

        Item = item;
        if (item.IsEmpty)
        {
            ItemIcon.ClearItem();
            _paintedVisualSignature = int.MinValue;
            return;
        }

        var rgba = Host.Icons.GetOrCreateCached(item.ItemTypeId, item.Count);
        if (rgba is not null)
        {
            ItemIcon.SetSprite(rgba);
            ItemIcon.CacheKey = (uint)(item.ItemTypeId | ((uint)item.Count << 16));
            var stackable = item.IsStackable();
            ItemIcon.StackCount = stackable && item.Count > 1 ? item.Count : (ushort)0;
            _paintedVisualSignature = item.IconDisplaySignature;
        }
        else
        {
            ItemIcon.ClearItem();
            _paintedVisualSignature = int.MinValue;
        }
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        var b = Bounds;
        ItemIcon.SetBounds(new NyxRect(b.X, b.Y, b.Width, b.Height));
        ItemIcon.Paint(painter, theme);

        if (_dropHighlight)
            painter.DrawRect(b, DropHighlightColor, thickness: 1);
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        if (!HitTest(x, y) || Host.MoveAmount.IsOpen)
            return;

        PointerPressed = true;
        Host.Drag.PressSlot(this, x, y);
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        if (Host.Drag.IsActive)
        {
            Host.Drag.ReleasePointer();
            PointerPressed = false;
            return;
        }

        if (!PointerPressed)
            return;

        PointerPressed = false;
    }

    public override void OnRightButtonUp(int x, int y)
    {
        if (HitTest(x, y))
            Host.OnSlotRightClick?.Invoke(this, x, y);
    }

    public override bool IsTooltipHovered => PointerInside && !Item.IsEmpty;

    public override void PaintTooltipPopup(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (Item.IsEmpty || !PointerInside || TooltipHoverSinceMs < 0)
            return;
        if (System.Environment.TickCount64 - TooltipHoverSinceMs < TooltipDelayMs)
            return;

        var font = ResolveEffectiveFont();
        ItemTooltip.GetTooltipInfo(painter, Item, font, out var title, out var attrs, out var descLines, out var tipW, out var tipH);

        var parentRoot = FindRoot();
        int viewportW = parentRoot?.Bounds.Width ?? 800;
        int viewportH = parentRoot?.Bounds.Height ?? 600;

        var tipX = Bounds.X + (Bounds.Width - tipW) / 2;
        var tipY = Bounds.Y - tipH - 4;

        // Clamp to viewport
        tipX = System.Math.Max(4, System.Math.Min(tipX, viewportW - tipW - 4));
        if (tipY < 4)
            tipY = Bounds.Bottom + 4;

        ItemTooltip.Paint(painter, tipX, tipY, title, attrs, descLines, tipW, tipH, font);
    }
}

/// <summary>Shared services for drag/drop, icons, and container hit-testing (<see cref="UIInventory"/>).</summary>
internal sealed class UISlotHost
{
    private readonly List<UISlot> _slots = [];
    private readonly List<RegisteredContainer> _containers = [];
    private readonly NyxGuiRootStack _guiRoots;
    private MapItemSurface? _map;
    private SandboxLayout.Regions? _layout;
    private float _camXf;
    private float _camYf;

    public UISlotHost(ItemIconRasterizer icons, NyxGuiRootStack guiRoots, int viewportWidth, int viewportHeight)
    {
        Icons = icons;
        _guiRoots = guiRoots;
        MoveAmount = new ItemMoveAmountDialog(viewportWidth, viewportHeight);
        guiRoots.Add(MoveAmount.Root, () => MoveAmount.IsOpen);
        Drag = new ItemDragService(this);
    }

    public ItemIconRasterizer Icons { get; }

    public ItemMoveAmountDialog MoveAmount { get; }

    public ItemDragService Drag { get; }

    public void SetMap(MapItemSurface map)
    {
        _map = map;
        Drag.SetMap(map);
    }

    public void SetGameView(SandboxLayout.Regions layout, float camXf, float camYf)
    {
        _layout = layout;
        _camXf = camXf;
        _camYf = camYf;
    }

    public bool TryGetMapTileAtPointer(int windowX, int windowY, out Position tilePos)
    {
        tilePos = default;
        if (_map is null || _layout is not { } layout)
            return false;

        if (!SandboxLayout.TryMapMouseToGame(layout, windowX, windowY, out var gameX, out var gameY))
            return false;

        return _map.TryPickTile(gameX, gameY, _camXf, _camYf, out tilePos);
    }

    public void PromptDropAmount(ushort maxCount, ushort? initialAmount, Action<ushort?> onChosen)
    {
        _guiRoots.BringToFront(MoveAmount.Root);
        MoveAmount.Show(maxCount, initialAmount, onChosen);
    }

    public void Register(UISlot slot) => _slots.Add(slot);

    public Action<UISlot, int, int>? OnSlotRightClick { get; set; }

    public void RegisterContainer(ItemStorage storage, NyxContainer host, Action sync) =>
        _containers.Add(new RegisteredContainer(storage, host, sync));

    public bool TryPickContainerAt(int x, int y, out ItemStorage storage, out Action? sync)
    {
        for (var i = _containers.Count - 1; i >= 0; i--)
        {
            var c = _containers[i];
            if (!c.Host.Visible || !c.Host.HitTestSubtree(x, y))
                continue;

            storage = c.Storage;
            sync = c.Sync;
            return true;
        }

        storage = null!;
        sync = null;
        return false;
    }

    public UISlot? PickSlotAt(int x, int y)
    {
        UISlot? found = null;
        foreach (var slot in _slots)
        {
            if (slot.Visible && slot.HitTest(x, y))
                found = slot;
        }

        return found;
    }

    private sealed class RegisteredContainer(ItemStorage storage, NyxContainer host, Action sync)
    {
        public ItemStorage Storage { get; } = storage;
        public NyxContainer Host { get; } = host;
        public Action Sync { get; } = sync;
    }
}
