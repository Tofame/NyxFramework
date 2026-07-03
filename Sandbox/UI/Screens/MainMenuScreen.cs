using System;
using Silk.NET.Input;
using NyxRender;
using NyxGui;
using Sandbox.UI;
using NyxNetwork.Core;
using Sandbox.Networking.Packets;

namespace Sandbox.UI.Screens;

internal sealed class MainMenuScreen : ISandboxScreen
{
	private readonly SandboxApp _app;
	private readonly SandboxUIManager _uiManager;
	private readonly SandboxConfig _config;
	private readonly AppContextInfo _contextInfo;
	private readonly SandboxGameWorld _gameWorld;
	private readonly SpriteRenderer _renderer;
	private readonly GraphicsDevice _graphicsDevice;
	private readonly SandboxMainMenu _mainMenu;

	private System.Threading.Tasks.Task<bool>? _loadingTask;
	private string _loadingProgressText = "Click Play to Enter";
	private bool _playClicked = false;
	private bool _loadingCompleted = false;
	private bool _loadingSuccess = false;

	private readonly SandboxJoinServer _joinServerMenu;
	private NetworkManager? _discoveryManager;
	private NetworkManager? _connectedNetManager;
	private JoinResponsePacket? _pendingJoinResponse;
	private string _connectErrorText = string.Empty;
	private int _lastVpW;
	private int _lastVpH;

	public MainMenuScreen(
		SandboxApp app,
		SandboxUIManager uiManager,
		SandboxConfig config,
		AppContextInfo contextInfo,
		SandboxGameWorld gameWorld,
		SpriteRenderer renderer,
		GraphicsDevice graphicsDevice,
		NyxGuiSettings? nyxGuiSettings)
	{
		_app = app;
		_uiManager = uiManager;
		_config = config;
		_contextInfo = contextInfo;
		_gameWorld = gameWorld;
		_renderer = renderer;
		_graphicsDevice = graphicsDevice;

		_mainMenu = new SandboxMainMenu(_uiManager.GuiRenderer!, nyxGuiSettings, _uiManager.GuiRoots);
		_mainMenu.PlayClicked += OnPlayClicked;
		_mainMenu.JoinClicked += OnJoinClicked;
		_mainMenu.ExitClicked += OnExitClicked;

		_joinServerMenu = new SandboxJoinServer(_uiManager.GuiRoots);
		_joinServerMenu.ConnectRequested += OnConnectRequested;
		_joinServerMenu.BackClicked += OnJoinBackClicked;

		_discoveryManager = new NetworkManager();
		try
		{
			_discoveryManager.StartLanDiscovery(server =>
			{
				_joinServerMenu.AddDiscoveredServer(server);
			});
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[LAN Discovery] Failed to start: {ex.Message}");
		}

		_loadingTask = System.Threading.Tasks.Task.Run(() =>
		{
			return _gameWorld.LoadOffThread(_config, _contextInfo, progress =>
			{
				_loadingProgressText = progress;
			});
		});
	}

	public void OnEnter()
	{
		_uiManager.ShellVisible = false;
		_mainMenu.Visible = true;
		_joinServerMenu.Visible = false;
	}

	public void OnExit()
	{
		_mainMenu.Visible = false;
		_joinServerMenu.Visible = false;
		_discoveryManager?.StopLanDiscovery();
		_discoveryManager?.Dispose();
		_discoveryManager = null;
	}

	private void OnJoinClicked()
	{
		_mainMenu.Visible = false;
		_joinServerMenu.Visible = true;
		_joinServerMenu.UpdateViewport(_lastVpW, _lastVpH);
	}

	private void OnJoinBackClicked()
	{
		_joinServerMenu.Visible = false;
		_mainMenu.Visible = true;
	}

	private void OnConnectRequested(string address)
	{
		_joinServerMenu.SetStatusText("Connecting…");
		_connectErrorText = string.Empty;

		System.Threading.Tasks.Task.Run(async () =>
		{
			try
			{
				string host = "127.0.0.1";
				int port = 8080;
				var parts = address.Split(':');
				if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0])) host = parts[0];
				if (parts.Length > 1 && int.TryParse(parts[1], out var p)) port = p;

				var netManager = new NetworkManager();
				await netManager.ConnectClientAsync(TransportType.Tcp, host, port);

				var appearance = _config.Player.Appearance;
				var joinPacket = new JoinRequestPacket
				{
					PlayerName = _config.Player.Name,
					LookType = (ushort)appearance.LookType,
					LookHead = appearance.LookHead,
					LookBody = appearance.LookBody,
					LookLegs = appearance.LookLegs,
					LookFeet = appearance.LookFeet,
					LookAddons = appearance.LookAddons,
					LookMount = (ushort)appearance.LookMount,
					IsMounted = appearance.HasMount
				};

				var responseSource = new System.Threading.Tasks.TaskCompletionSource<JoinResponsePacket>();
				netManager.ClientConnection!.OnDataReceived += (conn, data) =>
				{
					using var ms = new System.IO.MemoryStream(data);
					using var reader = new System.IO.BinaryReader(ms);
					if (data.Length >= 2)
					{
						ushort packetId = reader.ReadUInt16();
						if (packetId == (ushort)SandboxPacketId.JoinResponse)
						{
							var resp = new JoinResponsePacket();
							resp.Deserialize(reader);
							responseSource.TrySetResult(resp);
						}
					}
				};

				await netManager.ClientConnection.SendAsync(NyxNetwork.Messaging.PacketWriter.WritePacket(joinPacket));

				var completed = await System.Threading.Tasks.Task.WhenAny(responseSource.Task, System.Threading.Tasks.Task.Delay(5000));
				if (completed == responseSource.Task)
				{
					var response = await responseSource.Task;
					_connectedNetManager = netManager;
					_pendingJoinResponse = response;
				}
				else
				{
					throw new TimeoutException("Connection handshake timed out.");
				}
			}
			catch (Exception ex)
			{
				_connectErrorText = ex.Message;
			}
		});
	}

	private void OnPlayClicked()
	{
		_playClicked = true;
	}

	private void OnExitClicked()
	{
		_app.Exit();
	}

	public void Update(double deltaTime, IInputContext input, int winW, int winH, bool blocksMovement)
	{
		_uiManager.ProcessMouse(input);

		_joinServerMenu.Update();

		if (!string.IsNullOrEmpty(_connectErrorText))
		{
			_joinServerMenu.SetStatusText($"Failed: {_connectErrorText}");
			_connectErrorText = string.Empty;
		}

		if (_pendingJoinResponse is not null && _connectedNetManager is not null)
		{
			var response = _pendingJoinResponse;
			var netManager = _connectedNetManager;
			_pendingJoinResponse = null;
			_connectedNetManager = null;

			_gameWorld.InitializeClientNetwork(netManager, response.ClientId);

			bool success = _gameWorld.LoadMainThread(_renderer, _config);
			if (success)
			{
				if (_gameWorld.Player is not null)
				{
					_gameWorld.Player.Position = new Position(response.SpawnX, response.SpawnY, response.SpawnZ);
				}

				_uiManager.OnGameLoaded(_gameWorld);
				_app.TransitionTo(new GameplayScreen(_uiManager, _gameWorld, _renderer, _graphicsDevice));
			}
			else
			{
				_joinServerMenu.SetStatusText("Error: Client load failed.");
				netManager.DisconnectClientAsync().Wait();
			}
		}

		if (!_loadingCompleted && _loadingTask is not null && _loadingTask.IsCompleted)
		{
			_loadingCompleted = true;
			try
			{
				_loadingSuccess = _loadingTask.Result;
				if (!_loadingSuccess)
				{
					_mainMenu.SetStatusText("Error: Loading failed. Check logs.");
				}
				else if (!_playClicked)
				{
					_mainMenu.SetStatusText("Ready!");
				}
			}
			catch (Exception ex)
			{
				_loadingSuccess = false;
				_mainMenu.SetStatusText($"Error: {ex.Message}");
			}
		}

		if (!_loadingCompleted)
		{
			if (_playClicked)
				_mainMenu.SetStatusText(_loadingProgressText);
			else
				_mainMenu.SetStatusText($"Pre-loading: {_loadingProgressText}");
		}
		else
		{
			if (_loadingSuccess)
			{
				if (_playClicked)
				{
					bool success = _gameWorld.LoadMainThread(_renderer, _config);
					if (success)
					{
						_uiManager.OnGameLoaded(_gameWorld);
						_app.TransitionTo(new GameplayScreen(_uiManager, _gameWorld, _renderer, _graphicsDevice));
					}
					else
					{
						_mainMenu.SetStatusText("Error: Main thread initialization failed.");
						_playClicked = false;
					}
				}
				else
				{
					_mainMenu.SetStatusText("Ready!");
				}
			}
		}
	}

	public void Draw(double deltaTime, SpriteRenderer renderer, int winW, int winH)
	{
		_uiManager.Draw(winW, winH);
	}

	public void Resize(int width, int height)
	{
		_mainMenu.UpdateViewport(width, height);
		_joinServerMenu.UpdateViewport(width, height);
		_lastVpW = width;
		_lastVpH = height;
	}

	public void Dispose()
	{
		// _loadingTask is a fire-and-forget Task.Run; no explicit cancellation is wired.
		// SandboxMainMenu and SandboxJoinServer do not implement IDisposable.
	}
}
