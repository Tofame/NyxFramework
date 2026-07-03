using System;
using System.IO;
using Silk.NET.Input;
using Silk.NET.Windowing;
using NyxRender;
using NyxGui.Definitions;
using Sandbox.UI;
using Sandbox.UI.Screens;

namespace Sandbox;

internal sealed class SandboxApp : IDisposable
{
	private readonly IWindow _window;
	private readonly GraphicsDevice _graphicsDevice;

	private SpriteRenderer? _renderer;
	private IInputContext? _input;
	private readonly SandboxGameWorld _gameWorld = new();
	private readonly SandboxUIManager _uiManager = new();
	private readonly ExpAnalyzerTracker _expAnalyzerTracker = new();
	private readonly SandboxNyxGUIKeyboard _guiKeyboard = new();

	private ISandboxScreen? _activeScreen;
	private SandboxConfig? _demoConfig;
	private AppContextInfo _contextInfo;

	public SandboxApp()
	{
		var options = WindowOptions.Default;
		options.Size = new Silk.NET.Maths.Vector2D<int>(SandboxDefaults.WindowWidth, SandboxDefaults.WindowHeight);
		options.Title = "Sandbox — map + NPCs + player (WASD)";
		options.VSync = false;
		options.ShouldSwapAutomatically = true;

		if (Environment.OSVersion.Platform == PlatformID.Unix &&
			Environment.GetEnvironmentVariable("DISPLAY") == null &&
			Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") == null)
		{
			throw new InvalidOperationException("No display server detected; this sample needs a GPU window.");
		}

		_window = Window.Create(options);
		_graphicsDevice = new GraphicsDevice(_window);

		_window.Load += OnLoad;
		_window.Resize += OnResize;
		_window.Render += OnRender;
		_window.Closing += OnClosing;
	}

	public void Run()
	{
		Console.WriteLine("Starting Sandbox (map + creatures)…");
		_window.Run();
	}

	private void OnLoad()
	{
		var configPath = Path.Combine(AppContext.BaseDirectory, "config.toml");
		_demoConfig = SandboxConfig.Load(configPath);
		_contextInfo = new AppContextInfo { ConfigPath = configPath };

		var cache = new SpriteRendererCacheOptions(
			maxResidentSprites: 12_288,
			maxAtlasCount: 6,
			maxIdleFrames: 600); // ~10 s at 60 FPS

		_renderer = new SpriteRenderer(_graphicsDevice.GL, SandboxDefaults.WindowWidth, SandboxDefaults.WindowHeight, cache);
		SandboxEffectShaders.RegisterAll(_renderer.Shaders, _demoConfig.Client.ShadersDir);

		var nyxGuiSettingsPath = SandboxResources.TryGetUiDefinitionPath(SandboxNyxGUISettingsLoader.DefaultFileName);
		var nyxGuiSettings = SandboxNyxGUISettingsLoader.LoadOrDefault(nyxGuiSettingsPath);
		Console.WriteLine(
			$"NyxGUI: settings{(nyxGuiSettingsPath is not null ? $" from \"{nyxGuiSettingsPath}\"" : " (defaults)")}, drag opacity {nyxGuiSettings.PanelDragOpacity:F2}.");

		try
		{
			_input = _window.CreateInput();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Input not available: {ex.Message}");
		}

		// Subscribe exp analyzer tracker to exp gained events
		_gameWorld.ExpGained += gain => _expAnalyzerTracker.AddGain(gain);

		_uiManager.Initialize(
			_graphicsDevice.GL,
			_gameWorld,
			_expAnalyzerTracker,
			nyxGuiSettings,
			SandboxDefaults.WindowWidth,
			SandboxDefaults.WindowHeight);

		TransitionTo(new MainMenuScreen(
			this,
			_uiManager,
			_demoConfig,
			_contextInfo,
			_gameWorld,
			_renderer!,
			_graphicsDevice,
			nyxGuiSettings));
	}

	public void TransitionTo(ISandboxScreen nextScreen)
	{
		_activeScreen?.OnExit();
		_activeScreen?.Dispose();
		_activeScreen = nextScreen;
		_activeScreen.OnEnter();
		_activeScreen.Resize(_window.Size.X, _window.Size.Y);
	}

	public void Exit()
	{
		_window.Close();
	}

	private void OnResize(Silk.NET.Maths.Vector2D<int> size)
	{
		_renderer?.UpdateViewport(size.X, size.Y);
		_uiManager.Resize(size.X, size.Y);
		_activeScreen?.Resize(size.X, size.Y);
	}

	private void OnRender(double deltaTime)
	{
		var winW = _window.Size.X;
		var winH = _window.Size.Y;

		if (_input is { Keyboards.Count: > 0 } && _input.Keyboards[0] is { } guiKb)
			_guiKeyboard.Update(guiKb, _uiManager.GuiRoots);

		_activeScreen?.Update(deltaTime, _input!, winW, winH, _guiKeyboard.BlocksGameMovement);

		_graphicsDevice.Clear(new Color(20, 20, 20, 255));

		if (_renderer is null)
			return;

		_activeScreen?.Draw(deltaTime, _renderer, winW, winH);
	}

	private void OnClosing()
	{
		_activeScreen?.Dispose();
		_gameWorld.Dispose();
		_uiManager.Dispose();
		_renderer?.Dispose();
		_graphicsDevice.Dispose();
	}

	public void Dispose()
	{
		_window.Dispose();
	}
}
