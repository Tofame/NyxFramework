using NyxGui;
using NyxGui.Definitions;
using NyxDrawer.Creatures;
using Sandbox;
using Sandbox.Spells;
using Silk.NET.Input;

namespace Sandbox.UI.ActionBar;

/// <summary>Bottom-center action bar in <c>gamePanel</c> — three spell slots with tooltips and key rebinding.</summary>
internal sealed class SandboxActionBar
{
    private readonly SandboxShell? _shell;
    private readonly NyxGuiBuiltDocument? _document;
    private readonly NyxElement _root;
    private readonly NyxButton?[] _buttons = new NyxButton?[SpellActionBindings.SlotCount];
    private readonly NyxContextMenu _contextMenu = new() { Id = "actionBarContextMenu" };
    private readonly NyxContainer _contextPopupLayer = new(NyxRect.Empty) { Visible = false };
    private readonly SandboxKeyBindDialog _keyBindDialog;
    private readonly SpellActionBindings _bindings = new();
    private readonly bool[] _keyWasDown = new bool[SpellActionBindings.SlotCount];
    private int _rebindSlot = -1;
    private SpellCatalog? _catalog;
    private Player? _player;
    private NyxGameMap.GameMap? _map;
    private IReadOnlyList<Npc>? _npcs;
    private IInputContext? _input;
    private SandboxLayout.Regions _layout;
    private ActiveSpellEffects? _spellEffects;
    private ActiveMissileEffects? _missileEffects;
    private int _lastVpW;
    private int _lastVpH;
    private SandboxGameWorld? _gameWorld;

    public SandboxActionBar(SandboxShell shell, NyxGuiSettings? settings = null)
    {
        _shell = shell;
        _keyBindDialog = new SandboxKeyBindDialog(SandboxDefaults.WindowWidth, SandboxDefaults.WindowHeight);

        var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
        var loaded = SandboxUIDefinitions.TryLoad("action_bar", loadOptions);
        if (loaded is null)
        {
            Console.WriteLine("NyxGUI: missing resources/ui/action_bar.nyxui — action bar disabled.");
            _root = new NyxContainer(NyxRect.Empty);
            return;
        }

        _document = loaded.Document;
        _root = _document.Root;
        _shell.AdoptIntoGamePanel(_document);

        _buttons[0] = _document.TryGetButton("btnAction1");
        _buttons[1] = _document.TryGetButton("btnAction2");
        _buttons[2] = _document.TryGetButton("btnAction3");

        _contextPopupLayer.AddChild(_contextMenu);
        if (_shell.GamePanel is { } gamePanel)
            gamePanel.AddChild(_contextPopupLayer);
        else
            ((NyxContainer)_root).AddChild(_contextPopupLayer);

        ((NyxContainer)_root).AddChild(_keyBindDialog.Root);

        WireSlots();
        RefreshFromCatalog();
        SyncContextPopupLayer();

        Console.WriteLine($"NyxGUI: loaded action bar \"{loaded.SourcePath}\".");
    }

    public void SetSpellCatalog(SpellCatalog? catalog)
    {
        _catalog = catalog;
        RefreshFromCatalog();
    }

    public void UpdateViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        var changed = width != _lastVpW || height != _lastVpH;
        _lastVpW = width;
        _lastVpH = height;

        if (changed)
        {
            _shell?.UpdateViewport(width, height);
            _keyBindDialog.UpdateViewport(width, height);
            SyncContextPopupLayer();
        }
    }

    private void SyncContextPopupLayer()
    {
        if (_shell?.GamePanel is not { } panel)
            return;

        _contextPopupLayer.SetBounds(panel.Bounds);
    }

    public void Update(
        IInputContext? input,
        SandboxLayout.Regions layout,
        Player? player,
        NyxGameMap.GameMap? map,
        IReadOnlyList<Npc>? npcs,
        SpellCatalog? catalog,
        ActiveSpellEffects spellEffects,
        ActiveMissileEffects missileEffects,
        NyxGuiRootStack? guiRoots,
        SandboxGameWorld? gameWorld = null)
    {
        if (catalog is not null && !ReferenceEquals(_catalog, catalog))
            SetSpellCatalog(catalog);

        if (_keyBindDialog.IsOpen)
        {
            if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } kb)
                _keyBindDialog.HandleKeyboard(kb);
            return;
        }

        DismissPopupsOnOutsideClick(input);

        _player = player;
        _map = map;
        _npcs = npcs;
        _input = input;
        _layout = layout;
        _spellEffects = spellEffects;
        _missileEffects = missileEffects;
        _gameWorld = gameWorld;

        if (player is null || map is null || npcs is null || _catalog is null)
            return;

        if (guiRoots is not null && NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots))
            return;

        SpellCastInput.GetCameraOrigin(player, layout.GameWidthClamped, layout.GameHeightClamped, out var camXf, out var camYf);

        float? aimGameX = null;
        float? aimGameY = null;
        if (input is { Mice.Count: > 0 } &&
            SandboxLayout.TryMapMouseToGame(
                layout,
                (float)input.Mice[0].Position.X,
                (float)input.Mice[0].Position.Y,
                out var gx,
                out var gy))
        {
            aimGameX = gx;
            aimGameY = gy;
        }

        if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } keyboard)
        {
            for (var slot = 0; slot < SpellActionBindings.SlotCount; slot++)
            {
                var bound = _bindings.GetKey(slot);
                var down = keyboard.IsKeyPressed(bound);
                if (down && !_keyWasDown[slot])
                    TryCastSlot(slot, player, npcs, map, input, camXf, camYf, aimGameX, aimGameY, spellEffects, missileEffects);
                _keyWasDown[slot] = down;
            }
        }
    }

    private void WireSlots()
    {
        for (var slot = 0; slot < SpellActionBindings.SlotCount; slot++)
        {
            var index = slot;
            var btn = _buttons[slot];
            if (btn is null)
                continue;

            btn.Click += (_, _) => TryCastSlotFromUi(index);

            btn.RightClick += (_, e) => ShowContextMenu(index, e.X, e.Y);
        }
    }

    private void TryCastSlotFromUi(int slot)
    {
        if (_player is null || _map is null || _npcs is null || _catalog is null ||
            _spellEffects is null || _missileEffects is null)
            return;

        SpellCastInput.GetCameraOrigin(
            _player,
            _layout.GameWidthClamped,
            _layout.GameHeightClamped,
            out var camXf,
            out var camYf);

        float? aimGameX = null;
        float? aimGameY = null;
        if (_input is { Mice.Count: > 0 } &&
            SandboxLayout.TryMapMouseToGame(
                _layout,
                (float)_input.Mice[0].Position.X,
                (float)_input.Mice[0].Position.Y,
                out var gx,
                out var gy))
        {
            aimGameX = gx;
            aimGameY = gy;
        }

        TryCastSlot(slot, _player, _npcs, _map, _input, camXf, camYf, aimGameX, aimGameY, _spellEffects, _missileEffects);
    }

    private void TryCastSlot(
        int slot,
        Player player,
        IReadOnlyList<Npc> npcs,
        NyxGameMap.GameMap map,
        IInputContext? input,
        float camXf,
        float camYf,
        float? aimGameX,
        float? aimGameY,
        ActiveSpellEffects spellEffects,
        ActiveMissileEffects missileEffects)
    {
        if (_catalog is null || slot >= _catalog.Spells.Count)
            return;

        SpellCastActions.TryCastSlot(
            slot,
            _catalog,
            player,
            npcs,
            map,
            input,
            camXf,
            camYf,
            aimGameX,
            aimGameY,
            spellEffects,
            missileEffects,
            _gameWorld);
    }

    private void ShowContextMenu(int slot, int x, int y)
    {
        SyncContextPopupLayer();
        if (_shell?.GamePanel is { } panel)
            panel.BringChildToFront(_contextPopupLayer);

        _contextMenu.SetItems(
        [
            ("Change Bound Key", () =>
            {
                CloseContextMenu();
                OpenKeyBindDialog(slot);
            }),
        ]);
        _contextPopupLayer.Visible = true;
        _contextPopupLayer.BringChildToFront(_contextMenu);
        _contextMenu.Open(x, y);
    }

    private void OpenKeyBindDialog(int slot)
    {
        _rebindSlot = slot;
        var spellName = _catalog is not null && slot < _catalog.Spells.Count
            ? _catalog.Spells[slot].Name
            : $"Slot {slot + 1}";

        _keyBindDialog.Open(spellName, chosen =>
        {
            if (chosen is { } key && _rebindSlot >= 0)
            {
                _bindings.SetKey(_rebindSlot, key);
                RefreshButtonLabels();
            }

            _rebindSlot = -1;
        });
    }

    private void RefreshFromCatalog()
    {
        if (_catalog is null)
            return;

        for (var slot = 0; slot < SpellActionBindings.SlotCount; slot++)
        {
            var btn = _buttons[slot];
            if (btn is null)
                continue;

            if (slot < _catalog.Spells.Count)
            {
                btn.Tooltip = SpellCastActions.BuildTooltipText(_catalog.Spells[slot]);
                btn.Visible = true;
            }
            else
            {
                btn.Tooltip = null;
                btn.Visible = false;
            }
        }

        RefreshButtonLabels();
    }

    private void RefreshButtonLabels()
    {
        for (var slot = 0; slot < SpellActionBindings.SlotCount; slot++)
        {
            var btn = _buttons[slot];
            if (btn is null || !btn.Visible)
                continue;

            btn.Label = SpellActionBindings.FormatKey(_bindings.GetKey(slot));
        }
    }

    private void DismissPopupsOnOutsideClick(IInputContext? input)
    {
        if (!_contextMenu.IsOpen || input is not { Mice.Count: > 0 } || input.Mice[0] is not { } mouse)
            return;

        var mx = (int)mouse.Position.X;
        var my = (int)mouse.Position.Y;

        if (_contextMenu.HitTestSubtree(mx, my))
            return;

        if (mouse.IsButtonPressed(MouseButton.Left))
            CloseContextMenu();
    }

    private void CloseContextMenu()
    {
        _contextMenu.Close();
        _contextPopupLayer.Visible = false;
    }
}
