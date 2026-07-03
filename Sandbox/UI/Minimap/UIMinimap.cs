using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NyxGui;
using NyxGui.Definitions;
using NyxGuiRender;
using Silk.NET.Input;

namespace Sandbox.UI;

internal sealed class UIMinimap
{
	private const string ModuleId = "minimap";

	private readonly NyxGuiRenderer _renderer;
	private readonly NyxGuiTheme _theme = new();
	private readonly NyxGuiSettings _settings;
	private readonly Key _toggleKey;
	private readonly NyxGuiBuiltDocument? _document;
	private readonly NyxContainer _root;
	private readonly ZoomCanvas _canvas;
	private readonly MinimapView _minimapView;

	private Player? _player;
	private bool _toggleWasDown;
	private bool _ctrlPressed;
	private int _lastViewportW;
	private int _lastViewportH;
	private bool _initialCenteringDone;

	public NyxContainer? Root => _root;

	public NyxGuiBuiltDocument? Document => _document;

	public bool Visible
	{
		get => _root.Visible;
		set => _root.Visible = value;
	}

	public int WidgetCount => _document?.ById.Count ?? 0;

	public UIMinimap(
		NyxGuiRenderer renderer,
		GameMap map,
		SandboxShell shell,
		NyxGuiSettings? settings = null,
		NyxGuiRootStack? guiRoots = null)
	{
		_renderer = renderer;
		_settings = settings ?? NyxGuiSettings.Default;
		_toggleKey = SandboxUIKeyBinding.GetToggleKey(ModuleId, Key.M);

		if (!TryLoad(_settings, out var loaded))
		{
			_root = new NyxContainer(NyxRect.Empty);
			_canvas = new ZoomCanvas();
			_minimapView = new MinimapView(map);
			return;
		}

		_document = loaded.Document;
		_root = (_document.Root as NyxContainer)!;

		// Instantiate the ZoomCanvas and MinimapView
		_canvas = new ZoomCanvas();
		_canvas.MinZoom = 0.5f;
		_canvas.MaxZoom = 5.0f;
		_canvas.Zoom = 1.0f;
		_canvas.ZoomSensitivity = 0.15f;

		var contentContainer = _root is NyxMiniWindow mini ? mini.Body : _root;

		// Set layout box so ZoomCanvas stretches to fill the body of the Container below the header
		_canvas.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom),
			Margin = _root is NyxMiniWindow ? new NyxThickness(4, 4, 4, 4) : new NyxThickness(4, 26, 4, 4),
		};


		_minimapView = new MinimapView(map);
		_canvas.AddChild(_minimapView);

		// Reposition ZoomCanvas under the buttons
		var btnCenter = _document.TryGetButton("btnCenter");
		var btnZoomIn = _document.TryGetButton("btnZoomIn");
		var btnZoomOut = _document.TryGetButton("btnZoomOut");

		if (btnCenter != null) contentContainer.RemoveChild(btnCenter);
		if (btnZoomIn != null) contentContainer.RemoveChild(btnZoomIn);
		if (btnZoomOut != null) contentContainer.RemoveChild(btnZoomOut);

		contentContainer.AddChild(_canvas);

		if (btnCenter != null) contentContainer.AddChild(btnCenter);
		if (btnZoomIn != null) contentContainer.AddChild(btnZoomIn);
		if (btnZoomOut != null) contentContainer.AddChild(btnZoomOut);

		// Wire button events
		if (btnCenter != null)
		{
			btnCenter.Click += (s, e) => CenterOnPlayer();
		}
		if (btnZoomIn != null)
		{
			btnZoomIn.Click += (s, e) => _canvas.ZoomIn();
		}
		if (btnZoomOut != null)
		{
			btnZoomOut.Click += (s, e) => _canvas.ZoomOut();
		}

		// Configure scrolling floor change logic
		_canvas.PreventZoom = () => _ctrlPressed;

		_canvas.AddHandler(NyxEventType.MouseWheel, (sender, args) =>
		{
			if (args is NyxMouseWheelEventArgs wheelArgs && _ctrlPressed)
			{
				if (wheelArgs.Delta != 0)
				{
					// Scroll up -> Go UP floors -> Decrement ZLevel (e.g. 7 to 6)
					// Scroll down -> Go DOWN floors -> Increment ZLevel (e.g. 7 to 8)
					int deltaZ = wheelArgs.Delta > 0 ? -1 : 1;
					int newZ = Math.Clamp(_minimapView.ZLevel + deltaZ, 0, 15);
					if (newZ != _minimapView.ZLevel)
					{
						_minimapView.ZLevel = newZ;
						_minimapView.InvalidateCache();
						UpdateTitle();
					}
					wheelArgs.Handled = true;
				}
			}
		});

		UpdateTitle();

		Console.WriteLine(
			$"NyxGUI: minimap loaded from \"{loaded.SourcePath}\" ({_document.ById.Count} widgets).");
	}

	public void UpdateViewport(int width, int height)
	{
		if (width == _lastViewportW && height == _lastViewportH)
			return;

		_lastViewportW = width;
		_lastViewportH = height;
		_document?.SetWindowSize(width, height);
	}

	public void Update(IInputContext? input, Player? player, float camXf, float camYf, int gameW, int gameH, NyxGuiRootStack? guiRoots = null)
	{
		_player = player;
		_minimapView.UpdatePlayer(player);
		_minimapView.UpdateCamera(camXf, camYf, gameW, gameH);

		TryHandleToggle(input, guiRoots);

		if (input != null && input.Keyboards.Count > 0)
		{
			var kb = input.Keyboards[0];
			_ctrlPressed = kb.IsKeyPressed(Key.ControlLeft) || kb.IsKeyPressed(Key.ControlRight);
		}

		if (!_initialCenteringDone && _canvas.Bounds.Width > 0 && player != null)
		{
			CenterOnPlayer();
			_initialCenteringDone = true;
		}
	}

	public void Draw()
	{
	}

	private void CenterOnPlayer()
	{
		if (_player == null)
		{
			_canvas.Center();
		}
		else
		{
			// Convert player tile position to virtual pixels in world space
			float wx = _player.Position.X * 4 + 2;
			float wy = _player.Position.Y * 4 + 2;
			_canvas.CenterOn(wx, wy);
		}
	}

	private void UpdateTitle()
	{
		int level = 7 - _minimapView.ZLevel;
		string levelStr = level == 0 ? "Surface" : (level > 0 ? $"+{level}" : $"{level}");
		if (_root is NyxMiniWindow mini)
		{
			mini.Title = $"Minimap ({levelStr})";
		}
		else
		{
			var lbl = _document?.TryGetLabel("lblMinimapTitle");
			if (lbl != null)
			{
				lbl.Text = $"Minimap ({levelStr})";
			}
		}
	}

	private static bool TryLoad(NyxGuiSettings settings, [NotNullWhen(true)] out SandboxUIDefinitions.LoadResult? loaded)
	{
		loaded = SandboxUIDefinitions.TryLoad(ModuleId, SandboxUIDefinitions.CreateLoadOptions(settings));
		if (loaded is not null)
			return true;

		Console.WriteLine($"NyxGUI: missing resources/ui/{ModuleId}.nyxui — minimap disabled.");
		return false;
	}

	private void TryHandleToggle(IInputContext? input, NyxGuiRootStack? guiRoots)
	{
		if (input is not { Keyboards.Count: > 0 })
			return;

		var keyboard = input.Keyboards[0];
		if (keyboard is null)
			return;

		if (guiRoots is not null && NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots))
			return;

		var pressed = keyboard.IsKeyPressed(_toggleKey);
		if (pressed && !_toggleWasDown)
		{
			Visible = !Visible;
			if (Visible)
			{
				CenterOnPlayer();
			}
		}

		_toggleWasDown = pressed;
	}
}
