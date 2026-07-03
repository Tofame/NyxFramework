using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NyxNetwork.Core;

namespace NyxNetwork.Transports.WebSocket;

public class WsNyxListener : INyxServer
{
	private HttpListener? _httpListener;
	private readonly CancellationTokenSource _cts = new();
	private readonly ConcurrentDictionary<Guid, INyxConnection> _connections = new();
	private bool _isRunning = false;

	public bool IsRunning => _isRunning;
	public int Port { get; private set; }

	public event Action<INyxConnection>? OnClientConnected;
	public event Action<INyxConnection>? OnClientDisconnected;
	public event Action<INyxConnection, byte[]>? OnDataReceived;

	public async Task StartAsync(int port)
	{
		if (_isRunning) return;
		Port = port;
		_httpListener = new HttpListener();
		_httpListener.Prefixes.Add($"http://localhost:{port}/");
		_httpListener.Start();
		_isRunning = true;

		_ = Task.Run(AcceptLoopAsync);
		await Task.CompletedTask;
	}

	private async Task AcceptLoopAsync()
	{
		if (_httpListener == null) return;
		try
		{
			while (_isRunning)
			{
				HttpListenerContext context = await _httpListener.GetContextAsync();
				if (context.Request.IsWebSocketRequest)
				{
					HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
					var connection = new WsNyxConnection(wsContext.WebSocket);
					_connections[connection.Id] = connection;

					connection.OnDataReceived += (conn, data) => OnDataReceived?.Invoke(conn, data);
					connection.OnDisconnected += (conn) =>
					{
						_connections.TryRemove(conn.Id, out _);
						OnClientDisconnected?.Invoke(conn);
						conn.Dispose();
					};

					OnClientConnected?.Invoke(connection);
				}
				else
				{
					context.Response.StatusCode = 400;
					context.Response.Close();
				}
			}
		}
		catch
		{
			// Listen stopped
		}
	}

	public async Task StopAsync()
	{
		if (!_isRunning) return;
		_isRunning = false;
		_cts.Cancel();
		_httpListener?.Stop();

		foreach (var conn in _connections.Values)
		{
			await conn.DisconnectAsync();
		}
		_connections.Clear();
	}

	public async Task BroadcastAsync(byte[] data)
	{
		var tasks = new List<Task>();
		foreach (var conn in _connections.Values)
		{
			tasks.Add(conn.SendAsync(data));
		}
		await Task.WhenAll(tasks);
	}

	public void Dispose()
	{
		_ = StopAsync();
	}
}
