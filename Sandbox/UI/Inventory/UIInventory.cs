using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;
using Sandbox.Items;
using Sandbox.UI;

namespace Sandbox.UI.Inventory;

/// <summary>Equipment + backpack mini windows on the right dock. Toggle inventory with <c>I</c>.</summary>
internal sealed class UIInventory
{
    private const int SlotSize = 36;
    private const int SlotGap = 6;
    private const int GridCols = 3;
    private const int GridRows = 4;

    private readonly NyxGuiRenderer _renderer;
    private readonly ItemIconRasterizer _icons;
    private readonly NyxGuiBuiltDocument? _inventoryDoc;
    private readonly NyxGuiBuiltDocument? _backpackDoc;
    private readonly NyxElement _inventoryRoot;
    private readonly NyxElement _backpackRoot;
    private readonly NyxGuiRootStack _guiRoots;
    private readonly NyxGuiLoadOptions _loadOptions;
    private readonly UISlotHost _slotHost;
    private readonly ContainerWindowManager _containerWindows = new();
    private readonly Dictionary<EquipmentSlot, InventorySlotView> _slots = new();
    private UIContainer? _backpack;
    private ItemStorage? _playerBackpackStorage;
    private MapItemSurface? _mapSurface;
    private SandboxShell? _shell;
    private bool _slotsBuilt;
    private readonly Silk.NET.Input.Key _toggleKey =
        SandboxUIKeyBinding.GetToggleKey("inventory", Silk.NET.Input.Key.I);
    private bool _toggleWasDown;
    private bool _leftWasDown;
    private bool _rightWasDown;
    private readonly NyxGuiTheme _theme = new();
    private Position _hoveredMapTile = default;
    private long _mapTileHoverSinceMs = -1;
    private Item _hoveredMapItem = Item.Empty;
    private int _mouseX;
    private int _mouseY;
    private int _viewportWidth;
    private int _viewportHeight;

    public UIInventory(
        NyxGuiRenderer renderer,
        ItemIconRasterizer icons,
        NyxGuiSettings? settings,
        SandboxShell shell,
        NyxGuiRootStack guiRoots)
    {
        _renderer = renderer;
        _icons = icons;
        _guiRoots = guiRoots;
        _shell = shell;
        _slotHost = new UISlotHost(
            icons,
            guiRoots,
            SandboxDefaults.WindowWidth,
            SandboxDefaults.WindowHeight);
        _slotHost.OnSlotRightClick = OnSlotRightClick;

        _loadOptions = new NyxGuiLoadOptions
        {
            Settings = settings ?? NyxGuiSettings.Default,
            UiImagesDirectory = SandboxResources.ImagesUiDirectory,
            ResolveImageSource = SandboxResources.TryGetUiImagePath,
            UiFontsDirectory = SandboxResources.FontsDirectory,
            ResolveFontSource = SandboxResources.FindFontFile,
            InitialWindowWidth = SandboxDefaults.WindowWidth,
            InitialWindowHeight = SandboxDefaults.WindowHeight,
        };

        // Inventory first, backpack second — StackColumn places earlier children higher in the dock.
        _backpackRoot = LoadBackpack(_loadOptions, out _backpackDoc);
        _inventoryRoot = LoadInventory(_loadOptions, out _inventoryDoc);

		if (_inventoryDoc is not null)
			_shell.AdoptIntoRightDock(_inventoryDoc);
		if (_backpackDoc is not null)
			_shell.AdoptIntoRightDock(_backpackDoc);

        if (InventoryMiniWindow is { } invWin)
            invWin.BoundsChanged += (_, _) => OnInventoryWindowBoundsChanged(invWin);
        if (BackpackMiniWindow is { } bpWin)
            bpWin.BoundsChanged += (_, _) => OnBackpackWindowBoundsChanged(bpWin);
    }

    public NyxMiniWindow? InventoryMiniWindow => _inventoryDoc?.Root as NyxMiniWindow;

    public NyxMiniWindow? BackpackMiniWindow => _backpackDoc?.Root as NyxMiniWindow;

    public bool Visible
    {
        get => _inventoryRoot.Visible;
        set => _inventoryRoot.Visible = value;
    }

    public bool BackpackVisible
    {
        get => _backpackRoot.Visible;
        set => _backpackRoot.Visible = value;
    }

    public int WidgetCount => (_inventoryDoc?.ById.Count ?? 0) + (_backpackDoc?.ById.Count ?? 0);

    public void BindShell(SandboxShell shell)
    {
        _shell = shell;
        _containerWindows.BindShell(shell, ResolveContainerDock);
    }

    private NyxDockPanel ResolveContainerDock()
    {
        if (InventoryMiniWindow is { } inv && inv.Parent is NyxDockPanel invDock)
            return invDock;

        if (BackpackMiniWindow is { } bp && bp.Parent is NyxDockPanel bpDock)
            return bpDock;

        return _shell?.RightDock ?? _shell?.LeftDock!;
    }

    public void AttachPlayer(Player player)
    {
        EnsureSlotsBuilt();
        _playerBackpackStorage = player.Backpack;
        foreach (var view in _slots.Values)
            view.BindEquipment(player.Equipment);

        var backpackHost = _backpackDoc?.TryGet<NyxContainer>("BackpackSlots");
        if (backpackHost is not null)
        {
            _backpack = new UIContainer(player.Backpack, _slotHost);
            _backpack.BuildInto(backpackHost);
            if (BackpackMiniWindow is { } bpWin)
                _backpack.ApplyMiniWindowSize(bpWin);

            _backpack.RelayoutSlots();
        }

        SyncFrom(player);
        RelayoutSlots();
    }

    public int GetInventoryDockHeight() =>
        InventoryMiniWindow is { } inv
            ? GetPreferredInventoryHeight(inv.TitleBarHeight)
            : 200;

    public int GetBackpackDockHeight() =>
        BackpackMiniWindow is { } win && _backpack is not null
            ? _backpack.GetPreferredWindowHeight(win.TitleBarHeight)
            : 210;

    public int GetPreferredInventoryHeight(int titleBarHeight)
    {
        var gridH = GridRows * SlotSize + (GridRows - 1) * SlotGap;
        var bottomInset = 0;
        if (InventoryMiniWindow is { Image.ImageBorders: { HasAny: true } borders })
            bottomInset = borders.Bottom;

        if (_inventoryDoc?.TryGet<NyxContainer>("InventorySlots") is not { } host)
            return titleBarHeight + bottomInset + gridH;

        SlotGridLayout.GetHostMargins(host, out var marginTop, out var marginBottom, out _, out _);
        return titleBarHeight + bottomInset + marginTop + marginBottom + gridH;
    }

    /// <summary>Sets window height from TOML margins + equipment grid; width is left to the side dock (or <c>fixed-width</c> in TOML).</summary>
    public void ApplyInventoryMiniWindowSize()
    {
        if (InventoryMiniWindow is not { } win)
            return;

        // User grip resize must not be overwritten (BoundsChanged also fires each drag step).
        if (win.IsResizingHeight)
            return;

        var h = GetPreferredInventoryHeight(win.TitleBarHeight);
        var b = win.Bounds;
        if (b.Height == h)
            return;

        win.SetBounds(new NyxRect(b.X, b.Y, b.Width, h));
    }

    private void OnInventoryWindowBoundsChanged(NyxMiniWindow win)
    {
        if (win.IsResizingHeight)
            return;

        RelayoutSlotContent();
    }

    private void OnBackpackWindowBoundsChanged(NyxMiniWindow win)
    {
        if (win.IsResizingHeight)
            return;

        RelayoutSlotContent();
    }

    /// <summary>Repositions slot chrome only (dock move/resize); does not reset inventory height from TOML.</summary>
    private void RelayoutSlotContent()
    {
        LayoutEquipmentGrid();
        _backpack?.RelayoutSlots();
    }

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _viewportWidth = width;
        _viewportHeight = height;

        _inventoryDoc?.SetWindowSize(width, height);
        _backpackDoc?.SetWindowSize(width, height);
        _slotHost.MoveAmount.UpdateViewport(width, height);
        _containerWindows.UpdateViewport(width, height);
        EnsureSlotsBuilt();
        RelayoutSlots();
    }

    /// <summary>Syncs content-driven inventory height and repositions slot frames.</summary>
    public void RelayoutSlots()
    {
        ApplyInventoryMiniWindowSize();
        RelayoutSlotContent();
    }

    public void SetMap(NyxGameMap.GameMap map, SandboxGameWorld? gameWorld = null)
    {
        Action<int, int, int, ushort, ushort, bool>? sync = gameWorld is not null
            ? (x, y, z, id, count, isPlace) =>
            {
                if (gameWorld.IsNetworkActive)
                    gameWorld.SendItemUpdate(x, y, z, id, count, isPlace);
            }
            : null;
        _mapSurface = new MapItemSurface(map, gameWorld?.ClientAssets?.Things, sync);
        _slotHost.SetMap(_mapSurface);
    }

    public void Update(
        Silk.NET.Input.IInputContext? input,
        SandboxLayout.Regions layout,
        float camXf,
        float camYf,
        NyxGuiRootStack guiRoots)
    {
        _slotHost.SetGameView(layout, camXf, camYf);

        if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } kb)
        {
            var capturesShortcuts = NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots);
            var moveAmountOpen = _slotHost.MoveAmount.IsOpen;

            if (!capturesShortcuts)
            {
                var down = kb.IsKeyPressed(_toggleKey);
                if (down && !_toggleWasDown)
                    Visible = !Visible;
                _toggleWasDown = down;
            }

            if (moveAmountOpen)
                _slotHost.MoveAmount.HandleKeyboard(kb);
            else if (!capturesShortcuts)
                _slotHost.Drag.HandleStackDigitInput(kb);
        }

        if (input is not { Mice.Count: > 0 } || input.Mice[0] is not { } mouse)
        {
            _mapTileHoverSinceMs = -1;
            _hoveredMapTile = default;
            _hoveredMapItem = Item.Empty;
            return;
        }

        var mx = (int)mouse.Position.X;
        var my = (int)mouse.Position.Y;
        _slotHost.Drag.UpdatePointer(mx, my);

        var leftDown = mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Left);
        var rightDown = mouse.IsButtonPressed(Silk.NET.Input.MouseButton.Right);
        var moveAmountOpenMouse = _slotHost.MoveAmount.IsOpen;

        if (leftDown && !_leftWasDown && !_slotHost.Drag.IsActive && !moveAmountOpenMouse &&
            !guiRoots.HitTest(mx, my) &&
            SandboxLayout.TryMapMouseToGame(layout, mx, my, out _, out _) &&
            _slotHost.TryGetMapTileAtPointer(mx, my, out var tilePos))
            _slotHost.Drag.PressMapTile(tilePos);

        if (!leftDown && _leftWasDown && _slotHost.Drag.IsActive)
        {
            _slotHost.Drag.ReleasePointer();
            SyncBackpackVisibilityFromEquipment();
        }

        _leftWasDown = leftDown;

        if (rightDown && !_rightWasDown && !_slotHost.Drag.IsActive && !moveAmountOpenMouse &&
            !guiRoots.HitTest(mx, my) &&
            SandboxLayout.TryMapMouseToGame(layout, mx, my, out _, out _) &&
            _mapSurface is not null &&
            _slotHost.TryGetMapTileAtPointer(mx, my, out var mapPos) &&
            _mapSurface.TryEnsureTopContainer(mapPos, out var mapContainer))
        {
            if (_playerBackpackStorage is not null &&
                ReferenceEquals(mapContainer.Contents, _playerBackpackStorage))
            {
                BackpackVisible = !BackpackVisible;
                if (BackpackVisible)
                    _backpack?.SyncSlots();
            }
            else
            {
                _containerWindows.Open(
                    mapContainer,
                    _slotHost,
                    _loadOptions,
                    new NyxRect(mx, my, 1, 1));
            }
        }

        _rightWasDown = rightDown;

        // Map item hover detection
        var currentHoverItem = Item.Empty;
        var hasTile = false;
        var hoveredPos = new Position(0, 0, 7);

        _mouseX = mx;
        _mouseY = my;

        if (!guiRoots.HitTest(mx, my) &&
            _slotHost.TryGetMapTileAtPointer(mx, my, out hoveredPos) &&
            _mapSurface is not null)
        {
            if (_mapSurface.TryPeekTopItem(hoveredPos, out var topItem))
            {
                currentHoverItem = topItem;
                hasTile = true;
            }
        }

        var activeEl = guiRoots.FindActiveTooltipElement();
        if (activeEl is not null)
        {
            _mapTileHoverSinceMs = -1;
        }
        else if (hasTile && !currentHoverItem.IsEmpty)
        {
            if (hoveredPos == _hoveredMapTile && currentHoverItem.ItemTypeId == _hoveredMapItem.ItemTypeId)
            {
                if (_mapTileHoverSinceMs < 0)
                {
                    _mapTileHoverSinceMs = System.Environment.TickCount64;
                }
            }
            else
            {
                _hoveredMapTile = hoveredPos;
                _hoveredMapItem = currentHoverItem;
                _mapTileHoverSinceMs = System.Environment.TickCount64;
            }
        }
        else
        {
            _hoveredMapTile = default;
            _hoveredMapItem = Item.Empty;
            _mapTileHoverSinceMs = -1;
        }
    }

    public void Draw()
    {
        if (_slotHost.Drag.IsActive)
            _slotHost.Drag.DrawGhost(_renderer, _theme, _icons);

        _slotHost.MoveAmount.Paint(_renderer, _theme);

        // Paint map item tooltip if delay has passed
        if (_mapTileHoverSinceMs >= 0 && !_hoveredMapItem.IsEmpty)
        {
            var elapsed = System.Environment.TickCount64 - _mapTileHoverSinceMs;
            if (elapsed >= 400)
            {
                var font = NyxFontStyle.Default;
                ItemTooltip.GetTooltipInfo(_renderer, _hoveredMapItem, font, out var title, out var attrs, out var descLines, out var tipW, out var tipH);

                var tipX = _mouseX + 12;
                var tipY = _mouseY + 12;

                if (tipX + tipW > _viewportWidth)
                    tipX = _mouseX - tipW - 12;
                if (tipX < 4)
                    tipX = 4;

                if (tipY + tipH > _viewportHeight)
                    tipY = _mouseY - tipH - 12;
                if (tipY < 4)
                    tipY = 4;

                ItemTooltip.Paint(_renderer, tipX, tipY, title, attrs, descLines, tipW, tipH, font);
            }
        }
    }

    public void SyncFrom(Player player)
    {
        foreach (var view in _slots.Values)
            view.Refresh();

        _backpack?.SyncSlots();
    }

    private NyxElement LoadInventory(NyxGuiLoadOptions loadOptions, out NyxGuiBuiltDocument? doc)
    {
        var loaded = SandboxUIDefinitions.TryLoad("inventory", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/inventory.nyxui — inventory disabled.");
            doc = null;
            return new NyxContainer(NyxRect.Empty);
        }

        doc = loaded.Document;
        var root = doc.Root;
        SandboxMiniWindowBehavior.TryAppendChrome(InventoryMiniWindow, doc, loadOptions);
        ApplyInventoryMiniWindowSize();
        Console.WriteLine($"NyxGUI: loaded inventory \"{loaded.SourcePath}\".");
        return root;
    }

    private NyxElement LoadBackpack(NyxGuiLoadOptions loadOptions, out NyxGuiBuiltDocument? doc)
    {
        var loaded = SandboxUIDefinitions.TryLoad("backpack", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/backpack.nyxui — backpack disabled.");
            doc = null;
            return new NyxContainer(NyxRect.Empty);
        }

        doc = loaded.Document;
        var root = doc.Root;
        SandboxMiniWindowBehavior.TryAppendChrome(BackpackMiniWindow, doc, loadOptions);
        Console.WriteLine($"NyxGUI: loaded backpack \"{loaded.SourcePath}\".");
        return root;
    }

    private void EnsureSlotsBuilt()
    {
        if (_slotsBuilt || _inventoryDoc is null)
            return;

        var host = _inventoryDoc.TryGet<NyxContainer>("InventorySlots");
        if (host is null)
        {
            Console.WriteLine("NyxGUI: inventory missing InventorySlots panel.");
            return;
        }

        foreach (var (slot, _, _) in SlotGrid.Places)
        {
            var frame = new NyxContainer(new NyxRect(0, 0, SlotSize, SlotSize))
            {
                Id = $"slot_{slot}",
            };
            var border = NyxColor.FromRgb(80, 80, 90);
            frame.States.Normal.BorderWidth = 1;
            frame.States.Normal.BorderColor = border;

            var uiSlot = new UISlot(new NyxRect(0, 0, SlotSize, SlotSize), _slotHost)
            {
                Id = $"item_{slot}",
            };
            uiSlot.SetChromeFrame(frame, border);
            frame.AddChild(uiSlot);
            host.AddChild(frame);

            _slots[slot] = new InventorySlotView(slot, frame, uiSlot);
        }

        _slotsBuilt = true;
        ApplyInventoryMiniWindowSize();
        Console.WriteLine($"NyxGUI: inventory equipment grid ({_slots.Count} slots).");
    }

    private void OnSlotRightClick(UISlot slot, int x, int y)
    {
        if (IsEquippedPlayerBackpack(slot))
        {
            BackpackVisible = !BackpackVisible;
            if (BackpackVisible)
                _backpack?.SyncSlots();
            return;
        }

        TryOpenContainerInNewWindow(slot, x, y);
    }

    private bool IsEquippedPlayerBackpack(UISlot slot)
    {
        if (_playerBackpackStorage is null)
            return false;

        var item = slot.ReadItem?.Invoke() ?? Item.Empty;
        return item is ItemContainer { Contents: var contents } &&
               ReferenceEquals(contents, _playerBackpackStorage);
    }

    private void SyncBackpackVisibilityFromEquipment()
    {
        if (_playerBackpackStorage is null || !BackpackVisible)
            return;

        var backpackSlot = _slots.GetValueOrDefault(EquipmentSlot.Backpack);
        if (backpackSlot is null)
            return;

        var item = backpackSlot.UiSlot.ReadItem?.Invoke() ?? Item.Empty;
        var stillEquipped = item is ItemContainer { Contents: var contents } &&
                           ReferenceEquals(contents, _playerBackpackStorage);

        if (!stillEquipped)
            BackpackVisible = false;
    }

    private void TryOpenContainerInNewWindow(UISlot slot, int x, int y)
    {
        var item = slot.ReadItem?.Invoke() ?? Item.Empty;
        if (!ItemPlacementRules.TryEnsureOpenable(ref item, out var container))
            return;

        if (IsEquippedPlayerBackpack(slot))
            return;

        slot.SetItem(item);
        slot.NotifyContainerChanged?.Invoke();

        var near = new NyxRect(x, y, 1, 1);
        _containerWindows.Open(container, _slotHost, _loadOptions, near);
    }

    private void LayoutEquipmentGrid()
    {
        var host = _inventoryDoc?.TryGet<NyxContainer>("InventorySlots");
        if (host is null || host.Bounds.Width < SlotSize || host.Bounds.Height < SlotSize)
            return;

        var gridW = GridCols * SlotSize + (GridCols - 1) * SlotGap;
        var originX = Math.Max(0, (host.Bounds.Width - gridW) / 2);

        foreach (var (slot, col, row) in SlotGrid.Places)
        {
            if (!_slots.TryGetValue(slot, out var view))
                continue;

            // Host panel bounds already include TOML margin from layout; rel coords are inside the panel.
            SlotGridLayout.PlaceSlotPair(
                host,
                view.Frame,
                view.UiSlot,
                originX + col * (SlotSize + SlotGap),
                row * (SlotSize + SlotGap),
                SlotSize,
                SlotSize);
        }
    }

    private static class SlotGrid
    {
        public static readonly (EquipmentSlot Slot, int Col, int Row)[] Places =
        [
            (EquipmentSlot.Necklace, 0, 0),
            (EquipmentSlot.Head, 1, 0),
            (EquipmentSlot.Backpack, 2, 0),
            (EquipmentSlot.LeftHand, 0, 1),
            (EquipmentSlot.Body, 1, 1),
            (EquipmentSlot.RightHand, 2, 1),
            (EquipmentSlot.Ring, 0, 2),
            (EquipmentSlot.Legs, 1, 2),
            (EquipmentSlot.Ammo, 2, 2),
            (EquipmentSlot.Feet, 1, 3),
        ];
    }
}
