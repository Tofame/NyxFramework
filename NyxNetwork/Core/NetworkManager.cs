using System;
using System.Threading.Tasks;
using NyxNetwork.Core;
using NyxNetwork.Transports.Tcp;
using NyxNetwork.Transports.WebSocket;
using NyxNetwork.Messaging;

namespace NyxNetwork.Core;

public class NetworkManager : IDisposable
{
	private INyxServer? _server;
	private INyxConnection? _clientConnection;
	private LanBeacon? _lanBeacon;
	private LanDiscoveryListener? _lanDiscoveryListener;
	
	public INyxServer? Server => _server;
	public INyxConnection? ClientConnection => _clientConnection;
	
	public PacketRegistry PacketRegistry { get; } = new();
	
	public bool UseCompression
	{
		get => NyxNetworkConfig.UseCompression;
		set => NyxNetworkConfig.UseCompression = value;
	}

	public bool UseEncryption
	{
		get => NyxNetworkConfig.UseEncryption;
		set => NyxNetworkConfig.UseEncryption = value;
	}

	public uint[] EncryptionKey
	{
		get => NyxNetworkConfig.EncryptionKey;
		set => NyxNetworkConfig.EncryptionKey = value;
	}
	
	public event Action<INyxConnection>? OnClientConnected;
	public event Action<INyxConnection>? OnClientDisconnected;
	public event Action<INyxConnection, byte[]>? OnDataReceived;
	
	public bool IsServerRunning => _server?.IsRunning ?? false;
	public bool IsClientConnected => _clientConnection?.IsConnected ?? false;
	
	public void StartServer(TransportType transport, int port, string serverName = "Nyx Game Server")
	{
		if (transport == TransportType.Tcp)
		{
			_server = new TcpNyxListener();
		}
		else
		{
			_server = new WsNyxListener();
		}
		
		_server.OnClientConnected += (conn) => OnClientConnected?.Invoke(conn);
		_server.OnClientDisconnected += (conn) => OnClientDisconnected?.Invoke(conn);
		_server.OnDataReceived += (conn, data) => OnDataReceived?.Invoke(conn, data);
		
		_server.StartAsync(port).Wait();
		
		_lanBeacon = new LanBeacon(port, serverName);
		_lanBeacon.Start();
	}
	
	public async Task StopServerAsync()
	{
		if (_lanBeacon != null)
		{
			_lanBeacon.Stop();
			_lanBeacon = null;
		}
		if (_server != null)
		{
			await _server.StopAsync();
			_server.Dispose();
			_server = null;
		}
	}
	
	public async Task ConnectClientAsync(TransportType transport, string hostOrUri, int port = 0)
	{
		if (transport == TransportType.Tcp)
		{
			var conn = await TcpNyxConnection.ConnectAsync(hostOrUri, port);
			_clientConnection = conn;
		}
		else
		{
			var conn = await WsNyxConnection.ConnectAsync(hostOrUri);
			_clientConnection = conn;
		}
		
		_clientConnection.OnDataReceived += (conn, data) => OnDataReceived?.Invoke(conn, data);
		_clientConnection.OnDisconnected += (conn) => OnClientDisconnected?.Invoke(conn);
	}
	
	public async Task DisconnectClientAsync()
	{
		if (_clientConnection != null)
		{
			await _clientConnection.DisconnectAsync();
			_clientConnection.Dispose();
			_clientConnection = null;
		}
	}
	
	public void StartLanDiscovery(Action<DiscoveredServer> onServerDiscovered)
	{
		_lanDiscoveryListener = new LanDiscoveryListener();
		_lanDiscoveryListener.ServerDiscovered += onServerDiscovered;
		_lanDiscoveryListener.Start();
	}
	
	public void StopLanDiscovery()
	{
		if (_lanDiscoveryListener != null)
		{
			_lanDiscoveryListener.Stop();
			_lanDiscoveryListener = null;
		}
	}

	public void Dispose()
	{
		_ = StopServerAsync();
		_ = DisconnectClientAsync();
		_lanDiscoveryListener?.Dispose();
		_lanBeacon?.Dispose();
	}
}
