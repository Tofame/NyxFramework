using System;
using System.IO;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using NyxGui;
using NyxGuiRender;
using NyxRender;
using Sandbox.Spells;
using Sandbox.Items;
using Sandbox.UI;
using Sandbox.UI.ActionBar;
using Sandbox.UI.Inventory;

namespace Sandbox;

internal sealed class SandboxUIManager : IDisposable
{
	private NyxGuiRenderer? _guiRenderer;
	private NyxGuiRootStack? _guiRoots;
	private SandboxShell? _shell;
	private UIInventory? _inventory;
	private UIMinimap? _minimap;
	private SandboxActionBar? _actionBar;
	private SandboxFileDialog? _fileDialog;
	private SandboxEngineStats? _engineStats;
	private SandboxPlayerStats? _playerStats;
	private SandboxExpAnalyzer? _expAnalyzer;

	private SandboxBestiary? _bestiary;
	private SandboxQuestLog? _questLog;
	private SandboxTaskList? _taskList;
	private SandboxObjectFit? _objectFit;
	private NyxGuiSettings? _settings;
	private int _lastVpW;
	private int _lastVpH;

	public NyxContainer? GamePanel => _shell?.GamePanel;
	public NyxGuiRootStack? GuiRoots => _guiRoots;
	public NyxGuiRenderer? GuiRenderer => _guiRenderer;

	public bool ShellVisible
	{
		get => _shell?.Visible ?? false;
		set
		{
			if (_shell is not null)
				_shell.Visible = value;
		}
	}


	public void Initialize(
		GL gl,
		SandboxGameWorld gameWorld,
		ExpAnalyzerTracker expAnalyzerTracker,
		NyxGuiSettings settings,
		int width,
		int height)
	{
		_guiRenderer = new NyxGuiRenderer(gl, new NyxGuiFontOptions
		{
			FontFileName = SandboxResources.DefaultUiFontFile,
			ResolveFontPath = file => SandboxResources.FindFontFile(file),
		});
		_guiRenderer.UpdateViewport(width, height);

		if (_guiRenderer.HasFont)
			Console.WriteLine("NyxGUIRender: UI font loaded.");

		_guiRoots = new NyxGuiRootStack();

		var uiBootstrap = SandboxUIBootstrap.Initialize(settings, width, height);

		_shell = new SandboxShell(_guiRenderer, settings, _guiRoots);
		_shell.UpdateViewport(width, height);

		_engineStats = new SandboxEngineStats(_guiRenderer, _shell, settings);
		_engineStats.UpdateViewport(width, height);

		_playerStats = new SandboxPlayerStats(_guiRenderer, settings, null);
		_playerStats.UpdateViewport(width, height);

		_expAnalyzer = new SandboxExpAnalyzer(_guiRenderer, expAnalyzerTracker, settings, null);
		_expAnalyzer.UpdateViewport(width, height);

		_bestiary = new SandboxBestiary(_shell, settings);
		_questLog = new SandboxQuestLog(_shell, settings);
		_taskList = new SandboxTaskList(_shell, settings);
		_objectFit = new SandboxObjectFit(_shell, settings);
		_fileDialog = new SandboxFileDialog(_guiRenderer, _guiRoots, width, height);
		_actionBar = new SandboxActionBar(_shell, settings);
		_actionBar.UpdateViewport(width, height);

		_settings = settings;
		_lastVpW = width;
		_lastVpH = height;

		_shell.Relayout(width, height);
		_bestiary.UpdateViewport(width, height);
		_questLog.UpdateViewport(width, height);
		_taskList.UpdateViewport(width, height);
		_objectFit.UpdateViewport(width, height);

		if (_playerStats.MiniWindow is { } statsMw)
		{
			var side = uiBootstrap.Config.Docks.TryGetValue("player_stats", out var statsDock) ? statsDock.Side : "left";
			if (string.Equals(side, "right", StringComparison.OrdinalIgnoreCase))
				_shell.AdoptIntoRightDock(_playerStats.Document!);
			else
				_shell.AdoptIntoLeftDock(_playerStats.Document!);
		}

		if (_expAnalyzer.MiniWindow is { } expMw)
		{
			var side = uiBootstrap.Config.Docks.TryGetValue("exp_analyzer", out var expDock) ? expDock.Side : "right";
			if (string.Equals(side, "right", StringComparison.OrdinalIgnoreCase))
				_shell.AdoptIntoRightDock(_expAnalyzer.Document!);
			else
				_shell.AdoptIntoLeftDock(_expAnalyzer.Document!);
		}

		_engineStats.RegisterOtherScreens(
			_playerStats.WidgetCount,
			_bestiary.WidgetCount,
			_expAnalyzer.WidgetCount);
	}

	public void OnGameLoaded(SandboxGameWorld gameWorld)
	{
		if (_guiRenderer is null || _shell is null || _guiRoots is null || _settings is null)
			return;

		if (gameWorld.ClientAssets is not null)
		{
			var itemIcons = new ItemIconRasterizer(gameWorld.ClientAssets);
			_inventory = new UIInventory(_guiRenderer, itemIcons, _settings, _shell, _guiRoots);
			_inventory.BindShell(_shell);
			_inventory.UpdateViewport(_lastVpW, _lastVpH);
			_inventory.SetMap(gameWorld.Map!, gameWorld);
			_inventory.AttachPlayer(gameWorld.Player!);
			_inventory.SyncFrom(gameWorld.Player!);
			_inventory.RelayoutSlots();
		}

		if (gameWorld.Map is not null)
		{
			_minimap = new UIMinimap(_guiRenderer, gameWorld.Map, _shell, _settings, _guiRoots);
			if (_minimap.Document is not null)
			{
				_shell.AdoptIntoRightDock(_minimap.Document);
			}
		}

		_actionBar?.SetSpellCatalog(gameWorld.SpellCatalog);
	}

	public void ProcessMouse(IInputContext input)
	{
		if (_guiRoots is not null && input is { Mice.Count: > 0 } && input.Mice[0] is { } guiMouse)
		{
			var wheel = 0;
			foreach (var w in guiMouse.ScrollWheels)
				wheel += (int)w.Y;
			_guiRoots.ProcessMouse(
				(int)guiMouse.Position.X,
				(int)guiMouse.Position.Y,
				guiMouse.IsButtonPressed(MouseButton.Left),
				guiMouse.IsButtonPressed(MouseButton.Right),
				wheel);
		}
	}

	public void Update(
		double deltaTime,
		IInputContext input,
		SandboxGameWorld gameWorld,
		SpriteRenderer? spriteRenderer,
		SandboxLayout.Regions layout,
		float camXf,
		float camYf,
		int gameW,
		int gameH)
	{
		_shell?.Document?.FlushLayout();
		ProcessMouse(input);

		_engineStats?.Update(deltaTime, input, spriteRenderer, _guiRoots);
		_playerStats?.Update(input, gameWorld.Player, _guiRoots);
		_expAnalyzer?.Update(input, gameWorld.Player, _guiRoots);
		_minimap?.Update(input, gameWorld.Player, camXf, camYf, gameW, gameH, _guiRoots);
		_bestiary?.Update(input, _guiRoots);
		_questLog?.Update(input, _guiRoots);
		_taskList?.Update(input, _guiRoots);
		_objectFit?.Update(input, _guiRoots);
		_fileDialog?.Update(input, _guiRoots);

		if (_guiRoots is not null)
		{
			_inventory?.Update(input, layout, camXf, camYf, _guiRoots);
		}

		if (gameWorld.Player is not null && gameWorld.Map is not null)
		{
			_actionBar?.Update(
				input,
				layout,
				gameWorld.Player,
				gameWorld.Map,
				gameWorld.Npcs,
				gameWorld.SpellCatalog,
				gameWorld.ActiveSpellEffects,
				gameWorld.ActiveMissileEffects,
				_guiRoots,
				gameWorld);
		}
	}

	public void Resize(int width, int height)
	{
		_lastVpW = width;
		_lastVpH = height;
		_guiRenderer?.UpdateViewport(width, height);
		_engineStats?.UpdateViewport(width, height);
		_playerStats?.UpdateViewport(width, height);
		_expAnalyzer?.UpdateViewport(width, height);
		_bestiary?.UpdateViewport(width, height);
		_questLog?.UpdateViewport(width, height);
		_taskList?.UpdateViewport(width, height);
		_objectFit?.UpdateViewport(width, height);
		_actionBar?.UpdateViewport(width, height);
		_inventory?.UpdateViewport(width, height);
		_minimap?.UpdateViewport(width, height);
		_shell?.UpdateViewport(width, height);
		_inventory?.RelayoutSlots();
	}

	public void Draw(int windowWidth, int windowHeight)
	{
		if (_guiRenderer is null)
			return;

		_shell?.Document?.FlushLayout();
		_guiRenderer.UpdateViewport(windowWidth, windowHeight);
		_guiRenderer.BeginFrame();

		if (_guiRoots is not null)
		{
			_guiRoots.Paint(_guiRenderer, new NyxGuiTheme());
		}

		_inventory?.Draw();
		_guiRenderer.EndFrame();
	}

	public void Dispose()
	{
		_guiRenderer?.Dispose();
		GC.SuppressFinalize(this);
	}
}
