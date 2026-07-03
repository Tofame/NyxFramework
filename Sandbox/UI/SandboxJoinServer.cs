using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NyxGui;
using NyxNetwork.Core;

namespace Sandbox.UI;

internal sealed class SandboxJoinServer
{
	private const int PanelW = 400;
	private const int PanelH = 420;

	private readonly NyxContainer _root;
	private readonly NyxContainer _panel;
	private readonly NyxLabel _title;
	private readonly NyxLabel _addressLabel;
	private readonly NyxTextBox _addressInput;
	private readonly NyxButton _connectBtn;
	private readonly NyxLabel _scanHeader;
	private readonly NyxContainer _lanServersPanel;
	private readonly NyxButton _backBtn;
	private readonly NyxLabel _statusLabel;

	private int _lastVpW;
	private int _lastVpH;
	private bool _lanListChanged = false;
	private readonly ConcurrentDictionary<string, DiscoveredServer> _discoveredServers = new();
	private readonly List<DiscoveredServer> _activeLanServers = new();

	public event Action<string>? ConnectRequested;
	public event Action? BackClicked;

	public SandboxJoinServer(NyxGuiRootStack? guiRoots = null)
	{
		_root = new NyxContainer(NyxRect.Empty)
		{
			Visible = false
		};
		guiRoots?.Add(_root, () => true);

		// Main Panel
		_panel = new NyxContainer(new NyxRect(0, 0, PanelW, PanelH));
		_panel.States.Normal.BackgroundColor = NyxColor.FromRgb(30, 41, 59); // slate-800
		_panel.States.Normal.BorderWidth = 1;
		_panel.States.Normal.BorderColor = NyxColor.FromRgb(51, 65, 85); // slate-700

		// Title
		_title = new NyxLabel { Align = NyxTextAlign.TopCenter };
		_title.SetBounds(new NyxRect(12, 15, PanelW - 24, 30));
		_title.Text = "MULTIPLAYER CONNECTION";

		// Address Label
		_addressLabel = new NyxLabel { Align = NyxTextAlign.TopLeft };
		_addressLabel.SetBounds(new NyxRect(20, 55, PanelW - 40, 20));
		_addressLabel.Text = "Server Address (IP:Port):";

		// Address Input
		_addressInput = new NyxTextBox { Align = NyxTextAlign.TopLeft };
		_addressInput.SetBounds(new NyxRect(20, 80, PanelW - 40, 26));
		_addressInput.Text = "127.0.0.1:8080";

		// Connect Button
		_connectBtn = new NyxButton { Label = "Connect to Server" };
		_connectBtn.SetBounds(new NyxRect(20, 115, PanelW - 40, 36));
		_connectBtn.States.Normal.BackgroundColor = NyxColor.FromRgb(5, 150, 105); // emerald-600
		_connectBtn.States.Hover.BackgroundColor = NyxColor.FromRgb(16, 185, 129);  // emerald-500
		_connectBtn.States.Pressed.BackgroundColor = NyxColor.FromRgb(4, 120, 87);  // emerald-700
		_connectBtn.Click += (_, _) => ConnectRequested?.Invoke(_addressInput.Text);

		// LAN World Header
		_scanHeader = new NyxLabel { Align = NyxTextAlign.TopLeft };
		_scanHeader.SetBounds(new NyxRect(20, 165, PanelW - 40, 20));
		_scanHeader.Text = "LAN Worlds on Local Network:";

		// LAN Worlds List Panel
		_lanServersPanel = new NyxContainer(new NyxRect(20, 190, PanelW - 40, 150));
		_lanServersPanel.States.Normal.BackgroundColor = NyxColor.FromRgb(15, 23, 42); // slate-900
		_lanServersPanel.States.Normal.BorderWidth = 1;
		_lanServersPanel.States.Normal.BorderColor = NyxColor.FromRgb(51, 65, 85); // slate-700

		// Back Button
		_backBtn = new NyxButton { Label = "Back to Main Menu" };
		_backBtn.SetBounds(new NyxRect(20, 350, PanelW - 40, 36));
		_backBtn.States.Normal.BackgroundColor = NyxColor.FromRgb(71, 85, 105); // slate-600
		_backBtn.States.Hover.BackgroundColor = NyxColor.FromRgb(100, 116, 139);  // slate-500
		_backBtn.States.Pressed.BackgroundColor = NyxColor.FromRgb(51, 65, 85);   // slate-700
		_backBtn.Click += (_, _) => BackClicked?.Invoke();

		// Status Label
		_statusLabel = new NyxLabel { Align = NyxTextAlign.TopCenter };
		_statusLabel.SetBounds(new NyxRect(20, 395, PanelW - 40, 20));
		_statusLabel.Text = "";

		_panel.AddChild(_title);
		_panel.AddChild(_addressLabel);
		_panel.AddChild(_addressInput);
		_panel.AddChild(_connectBtn);
		_panel.AddChild(_scanHeader);
		_panel.AddChild(_lanServersPanel);
		_panel.AddChild(_backBtn);
		_panel.AddChild(_statusLabel);
		_root.AddChild(_panel);
	}

	public bool Visible
	{
		get => _root.Visible;
		set
		{
			_root.Visible = value;
			if (value)
			{
				_statusLabel.Text = "";
				_discoveredServers.Clear();
				_activeLanServers.Clear();
				_lanServersPanel.ClearChildren();
			}
		}
	}

	public void SetStatusText(string text)
	{
		_statusLabel.Text = text;
	}

	public void UpdateViewport(int width, int height)
	{
		if (width <= 0 || height <= 0)
			return;

		_lastVpW = width;
		_lastVpH = height;

		_root.SetBounds(new NyxRect(0, 0, width, height));
		_panel.SetBounds(new NyxRect((width - PanelW) / 2, (height - PanelH) / 2, PanelW, PanelH));
	}

	public void AddDiscoveredServer(DiscoveredServer server)
	{
		var key = $"{server.IpAddress}:{server.Port}";
		_discoveredServers[key] = server;
		_lanListChanged = true;
	}

	public void Update()
	{
		if (!Visible) return;

		// 1. Prune expired servers
		var now = DateTime.UtcNow;
		var expiredKeys = new List<string>();
		foreach (var kvp in _discoveredServers)
		{
			if (now - kvp.Value.LastSeen > TimeSpan.FromSeconds(5))
			{
				expiredKeys.Add(kvp.Key);
			}
		}

		foreach (var key in expiredKeys)
		{
			_discoveredServers.TryRemove(key, out _);
			_lanListChanged = true;
		}

		// 2. Rebuild list if changed
		if (_lanListChanged)
		{
			_lanListChanged = false;
			_activeLanServers.Clear();
			_activeLanServers.AddRange(_discoveredServers.Values);

			_lanServersPanel.ClearChildren();
			
			if (_activeLanServers.Count == 0)
			{
				var scanLabel = new NyxLabel { Align = NyxTextAlign.Center };
				scanLabel.SetBounds(new NyxRect(_lanServersPanel.Bounds.X + 10, _lanServersPanel.Bounds.Y + 60, PanelW - 60, 20));
				scanLabel.Text = "Searching for LAN worlds…";
				_lanServersPanel.AddChild(scanLabel);
			}
			else
			{
				for (int i = 0; i < _activeLanServers.Count; i++)
				{
					var server = _activeLanServers[i];
					var btn = new NyxButton
					{
						Label = $"{server.Name} ({server.IpAddress}:{server.Port})"
					};
					btn.SetBounds(new NyxRect(_lanServersPanel.Bounds.X + 10, _lanServersPanel.Bounds.Y + 10 + i * 32, PanelW - 60, 26));
					btn.States.Normal.BackgroundColor = NyxColor.FromRgb(30, 41, 59); // slate-800
					btn.States.Hover.BackgroundColor = NyxColor.FromRgb(71, 85, 105);  // slate-600
					btn.Click += (_, _) =>
					{
						_addressInput.Text = $"{server.IpAddress}:{server.Port}";
					};
					_lanServersPanel.AddChild(btn);
				}
			}
		}
	}
}
