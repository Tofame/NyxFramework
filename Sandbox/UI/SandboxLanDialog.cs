using System;
using NyxGui;
using Silk.NET.Input;
using NyxNetwork.Core;

namespace Sandbox.UI;

internal sealed class SandboxLanDialog
{
	private const int PanelW = 320;
	private const int PanelH = 200;

	private readonly NyxContainer _root;
	private readonly NyxContainer _panel;
	private readonly NyxLabel _title;
	private readonly NyxLabel _portLabel;
	private readonly NyxTextBox _portInput;
	private readonly NyxLabel _nameLabel;
	private readonly NyxTextBox _nameInput;
	private readonly NyxButton _actionBtn;
	private readonly NyxButton _closeBtn;
	private readonly NyxLabel _infoLabel;

	private readonly SandboxGameWorld _gameWorld;
	private bool _lWasDown;

	public SandboxLanDialog(int viewportWidth, int viewportHeight, SandboxGameWorld gameWorld, NyxGuiRootStack? guiRoots = null)
	{
		_gameWorld = gameWorld;

		_root = new NyxContainer(new NyxRect(0, 0, viewportWidth, viewportHeight))
		{
			Visible = false
		};
		guiRoots?.Add(_root, () => true);

		// Panel
		_panel = new NyxContainer(new NyxRect(0, 0, PanelW, PanelH))
		{
			Draggable = true
		};
		_panel.States.Normal.BackgroundColor = NyxColor.FromRgb(30, 41, 59); // slate-800
		_panel.States.Normal.BorderWidth = 1;
		_panel.States.Normal.BorderColor = NyxColor.FromRgb(51, 65, 85); // slate-700

		// Title
		_title = new NyxLabel { Align = NyxTextAlign.TopCenter };
		_title.SetBounds(new NyxRect(12, 10, PanelW - 24, 20));
		_title.Text = "LAN WORLD SETTINGS";

		// Port Input & Label
		_portLabel = new NyxLabel { Align = NyxTextAlign.TopLeft };
		_portLabel.SetBounds(new NyxRect(15, 38, 90, 20));
		_portLabel.Text = "Port:";

		_portInput = new NyxTextBox { Align = NyxTextAlign.TopLeft };
		_portInput.SetBounds(new NyxRect(110, 35, 195, 24));
		_portInput.Text = "8080";

		// Name Input & Label
		_nameLabel = new NyxLabel { Align = NyxTextAlign.TopLeft };
		_nameLabel.SetBounds(new NyxRect(15, 68, 90, 20));
		_nameLabel.Text = "World Name:";

		_nameInput = new NyxTextBox { Align = NyxTextAlign.TopLeft };
		_nameInput.SetBounds(new NyxRect(110, 65, 195, 24));
		_nameInput.Text = "LAN Sandbox";

		// Info Label
		_infoLabel = new NyxLabel { Align = NyxTextAlign.TopCenter };
		_infoLabel.SetBounds(new NyxRect(15, 95, PanelW - 30, 20));
		_infoLabel.Text = "This world is currently local only.";

		// Action Button
		_actionBtn = new NyxButton { Label = "Start LAN Server" };
		_actionBtn.SetBounds(new NyxRect(15, 120, PanelW - 30, 30));
		_actionBtn.States.Normal.BackgroundColor = NyxColor.FromRgb(5, 150, 105); // emerald-600
		_actionBtn.States.Hover.BackgroundColor = NyxColor.FromRgb(16, 185, 129);  // emerald-500
		_actionBtn.States.Pressed.BackgroundColor = NyxColor.FromRgb(4, 120, 87);  // emerald-700
		_actionBtn.Click += OnActionClicked;

		// Close Button
		_closeBtn = new NyxButton { Label = "Close Settings" };
		_closeBtn.SetBounds(new NyxRect(15, 155, PanelW - 30, 30));
		_closeBtn.States.Normal.BackgroundColor = NyxColor.FromRgb(71, 85, 105); // slate-600
		_closeBtn.States.Hover.BackgroundColor = NyxColor.FromRgb(100, 116, 139);  // slate-500
		_closeBtn.States.Pressed.BackgroundColor = NyxColor.FromRgb(51, 65, 85);   // slate-700
		_closeBtn.Click += (_, _) => _root.Visible = false;

		_panel.AddChild(_title);
		_panel.AddChild(_portLabel);
		_panel.AddChild(_portInput);
		_panel.AddChild(_nameLabel);
		_panel.AddChild(_nameInput);
		_panel.AddChild(_infoLabel);
		_panel.AddChild(_actionBtn);
		_panel.AddChild(_closeBtn);
		_root.AddChild(_panel);
	}

	public bool IsOpen => _root.Visible;

	public void Open()
	{
		_root.Visible = true;
		RefreshUI();
	}

	public void Close()
	{
		_root.Visible = false;
	}

	public void UpdateViewport(int width, int height)
	{
		if (width <= 0 || height <= 0)
			return;

		_root.SetBounds(new NyxRect(0, 0, width, height));
		_panel.SetBounds(new NyxRect((width - PanelW) / 2, (height - PanelH) / 2, PanelW, PanelH));
	}

	public void HandleKeyboard(IKeyboard keyboard, bool blocksMovement = false)
	{
		var lPressed = keyboard.IsKeyPressed(Key.L);
		if (lPressed && !_lWasDown && !blocksMovement)
		{
			if (_root.Visible)
				Close();
			else
				Open();
		}
		_lWasDown = lPressed;
	}

	private void RefreshUI()
	{
		if (_gameWorld.IsNetworkClient && !_gameWorld.IsNetworkHost)
		{
			// Client mode: show connected server info, disable actions
			_infoLabel.Text = "Connected to remote server.";
			_portInput.ReadOnly = true;
			_nameInput.ReadOnly = true;
			_actionBtn.Enabled = false;
			_actionBtn.Label = "LAN Server Unavailable";
		}
		else if (_gameWorld.IsNetworkHost)
		{
			// Host mode: show server info, allow stopping
			_infoLabel.Text = $"Hosting LAN server on port {_gameWorld.HostPort}!";
			_portInput.ReadOnly = true;
			_nameInput.ReadOnly = true;
			_actionBtn.Enabled = true;
			_actionBtn.Label = "Stop LAN Server";
			_actionBtn.States.Normal.BackgroundColor = NyxColor.FromRgb(220, 38, 38);  // red-600
			_actionBtn.States.Hover.BackgroundColor = NyxColor.FromRgb(239, 68, 68);   // red-500
			_actionBtn.States.Pressed.BackgroundColor = NyxColor.FromRgb(185, 28, 28); // red-700
		}
		else
		{
			// Offline/Ready to host mode
			_infoLabel.Text = "This world is currently local only.";
			_portInput.ReadOnly = false;
			_nameInput.ReadOnly = false;
			_actionBtn.Enabled = true;
			_actionBtn.Label = "Start LAN Server";
			_actionBtn.States.Normal.BackgroundColor = NyxColor.FromRgb(5, 150, 105); // emerald-600
			_actionBtn.States.Hover.BackgroundColor = NyxColor.FromRgb(16, 185, 129);  // emerald-500
			_actionBtn.States.Pressed.BackgroundColor = NyxColor.FromRgb(4, 120, 87);  // emerald-700
		}
	}

	private void OnActionClicked(object? sender, NyxClickEventArgs e)
	{
		if (_gameWorld.IsNetworkHost)
		{
			// Stop server
			_gameWorld.StopHosting();
			RefreshUI();
		}
		else
		{
			// Start server
			if (int.TryParse(_portInput.Text, out int port) && port > 0 && port < 65536)
			{
				string name = string.IsNullOrWhiteSpace(_nameInput.Text) ? "LAN Sandbox" : _nameInput.Text;
				if (_gameWorld.StartHosting(port, name))
				{
					RefreshUI();
				}
				else
				{
					_infoLabel.Text = "Failed to start host server.";
				}
			}
			else
			{
				_infoLabel.Text = "Invalid port value (1-65535).";
			}
		}
	}
}
