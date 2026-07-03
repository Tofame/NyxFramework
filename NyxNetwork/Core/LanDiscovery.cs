using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NyxNetwork.Core;

public class DiscoveredServer
{
	public string Name { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public int Port { get; set; }
	public DateTime LastSeen { get; set; }
}

public class LanBeacon : IDisposable
{
	private readonly int _gamePort;
	private readonly string _serverName;
	private UdpClient? _udpClient;
	private CancellationTokenSource? _cts;
	private readonly int _beaconPort = 14445;

	public LanBeacon(int gamePort, string serverName)
	{
		_gamePort = gamePort;
		_serverName = serverName;
	}

	public void Start()
	{
		_cts = new CancellationTokenSource();
		_udpClient = new UdpClient();
		_udpClient.EnableBroadcast = true;
		
		Task.Run(() => BeaconLoopAsync(_cts.Token));
	}

	private async Task BeaconLoopAsync(CancellationToken token)
	{
		var endpoint = new IPEndPoint(IPAddress.Broadcast, _beaconPort);
		byte[] data = Encoding.UTF8.GetBytes($"NYX_SERVER|{_gamePort}|{_serverName}");
		
		while (!token.IsCancellationRequested)
		{
			try
			{
				await _udpClient!.SendAsync(data, data.Length, endpoint);
			}
			catch
			{
				// Ignore send errors
			}
			await Task.Delay(1500, token);
		}
	}

	public void Stop()
	{
		_cts?.Cancel();
		_udpClient?.Dispose();
		_udpClient = null;
	}

	public void Dispose()
	{
		Stop();
	}
}

public class LanDiscoveryListener : IDisposable
{
	private UdpClient? _udpClient;
	private CancellationTokenSource? _cts;
	private readonly int _beaconPort = 14445;

	public event Action<DiscoveredServer>? ServerDiscovered;

	public void Start()
	{
		_cts = new CancellationTokenSource();
		try
		{
			_udpClient = new UdpClient();
			_udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_udpClient.ExclusiveAddressUse = false;
			_udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _beaconPort));
			
			Task.Run(() => ListenLoopAsync(_cts.Token));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[LAN Discovery] Failed to bind to discovery port: {ex.Message}");
		}
	}

	private async Task ListenLoopAsync(CancellationToken token)
	{
		if (_udpClient == null) return;
		while (!token.IsCancellationRequested)
		{
			try
			{
				var result = await _udpClient.ReceiveAsync(token);
				string message = Encoding.UTF8.GetString(result.Buffer);
				if (message.StartsWith("NYX_SERVER|"))
				{
					string[] parts = message.Split('|');
					if (parts.Length >= 3)
					{
						if (int.TryParse(parts[1], out int gamePort))
						{
							string serverName = parts[2];
							string ipAddress = result.RemoteEndPoint.Address.ToString();
							
							var server = new DiscoveredServer
							{
								Name = serverName,
								IpAddress = ipAddress,
								Port = gamePort,
								LastSeen = DateTime.UtcNow
							};
							
							ServerDiscovered?.Invoke(server);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
				// Ignore receive errors
			}
		}
	}

	public void Stop()
	{
		_cts?.Cancel();
		_udpClient?.Dispose();
		_udpClient = null;
	}

	public void Dispose()
	{
		Stop();
	}
}
