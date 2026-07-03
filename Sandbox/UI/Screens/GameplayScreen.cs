using Silk.NET.Input;
using Silk.NET.OpenGL;
using NyxRender;
using Sandbox.UI;
using Sandbox.Spells;

namespace Sandbox.UI.Screens;

internal sealed class GameplayScreen : ISandboxScreen
{
	private readonly SandboxUIManager _uiManager;
	private readonly SandboxGameWorld _gameWorld;
	private readonly SpriteRenderer _renderer;
	private readonly GraphicsDevice _graphicsDevice;
	private float _shaderTime = 0f;
	private readonly SandboxLanDialog _lanDialog;
	private readonly CreatureInformationDrawer? _creatureInfoDrawer;
	private readonly SandboxChat? _chat;
	private bool _enterWasDown;

	public GameplayScreen(
		SandboxUIManager uiManager,
		SandboxGameWorld gameWorld,
		SpriteRenderer renderer,
		GraphicsDevice graphicsDevice)
	{
		_uiManager = uiManager;
		_gameWorld = gameWorld;
		_renderer = renderer;
		_graphicsDevice = graphicsDevice;

		_lanDialog = new SandboxLanDialog(SandboxDefaults.WindowWidth, SandboxDefaults.WindowHeight, _gameWorld, _uiManager.GuiRoots);
		if (_uiManager.GamePanel is not null)
		{
			_creatureInfoDrawer = new CreatureInformationDrawer(_gameWorld, _uiManager.GamePanel);
			_chat = new SandboxChat(SandboxDefaults.WindowWidth, SandboxDefaults.WindowHeight, _gameWorld, _uiManager.GamePanel, _creatureInfoDrawer, _uiManager.GuiRoots);
		}
	}

	public void OnEnter()
	{
		_uiManager.ShellVisible = true;
	}

	public void OnExit()
	{
		_uiManager.ShellVisible = false;
		_lanDialog.Close();
	}

	public void Update(double deltaTime, IInputContext input, int winW, int winH, bool blocksMovement)
	{
		if (input.Keyboards.Count > 0)
		{
			_lanDialog.HandleKeyboard(input.Keyboards[0], blocksMovement);

			var enterPressed = input.Keyboards[0].IsKeyPressed(Key.Enter);
			if (enterPressed && !_enterWasDown && _chat is not null)
			{
				if (_uiManager.GuiRoots is not null)
				{
					_chat.ToggleFocus(_uiManager.GuiRoots);
				}
			}
			_enterWasDown = enterPressed;
		}

		_gameWorld.Update(deltaTime, input, blocksMovement);

		var layout = SandboxLayout.Compute(winW, winH);
		var gameW = _uiManager.GamePanel?.Bounds.Width ?? layout.GameWidthClamped;
		var gameH = _uiManager.GamePanel?.Bounds.Height ?? layout.GameHeightClamped;

		var camXf = 0f;
		var camYf = 0f;
		if (_gameWorld.Player is not null && _gameWorld.Map is not null)
			SpellCastInput.GetCameraOrigin(_gameWorld.Player, gameW, gameH, out camXf, out camYf);

		_uiManager.Update(
			deltaTime,
			input,
			_gameWorld,
			_renderer,
			layout,
			camXf,
			camYf,
			gameW,
			gameH);

		_creatureInfoDrawer?.Update(camXf, camYf, gameW, gameH);
	}

	public void Draw(double deltaTime, SpriteRenderer renderer, int winW, int winH)
	{
		_shaderTime += (float)deltaTime;

		var gamePanel = _uiManager.GamePanel;
		if (gamePanel is not null)
		{
			var bounds = gamePanel.Bounds;
			int vpX = bounds.X;
			int vpY = winH - (bounds.Y + bounds.Height);
			int vpW = bounds.Width;
			int vpH = bounds.Height;

			_graphicsDevice.GL.Viewport(vpX, vpY, (uint)vpW, (uint)vpH);
			_graphicsDevice.GL.Enable(EnableCap.ScissorTest);
			_graphicsDevice.GL.Scissor(vpX, vpY, (uint)vpW, (uint)vpH);

			_graphicsDevice.Clear(new Color(30, 40, 50, 255));

			_renderer.UpdateViewport(vpW, vpH);
			_renderer.BeginFrame(_shaderTime);
			_gameWorld.Draw(_renderer, bounds, _shaderTime);
			_renderer.EndFrame();

			_graphicsDevice.GL.Disable(EnableCap.ScissorTest);
		}

		_uiManager.Draw(winW, winH);
	}

	public void Resize(int width, int height)
	{
		_lanDialog.UpdateViewport(width, height);
		_chat?.UpdateViewport(width, height);
	}

	public void Dispose()
	{
		// SandboxLanDialog and SandboxChat do not implement IDisposable.
		_creatureInfoDrawer?.Dispose();
	}
}
