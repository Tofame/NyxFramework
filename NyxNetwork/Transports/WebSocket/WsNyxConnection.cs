using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using NyxNetwork.Core;

namespace NyxNetwork.Transports.WebSocket;

public class WsNyxConnection : INyxConnection
{
	private readonly System.Net.WebSockets.WebSocket _webSocket;
	private readonly CancellationTokenSource _cts = new();
	private bool _isClosed = false;
	private bool _readLoopStarted = false;
	private event Action<INyxConnection, byte[]>? _onDataReceived;
	private event Action<INyxConnection>? _onDisconnected;

	public Guid Id { get; } = Guid.NewGuid();
	public bool IsConnected => _webSocket.State == WebSocketState.Open && !_isClosed;

	public event Action<INyxConnection, byte[]>? OnDataReceived
	{
		add
		{
			lock (this)
			{
				_onDataReceived += value;
				EnsureReadLoopStarted();
			}
		}
		remove
		{
			lock (this)
			{
				_onDataReceived -= value;
			}
		}
	}

	public event Action<INyxConnection>? OnDisconnected
	{
		add
		{
			lock (this)
			{
				_onDisconnected += value;
				EnsureReadLoopStarted();
			}
		}
		remove
		{
			lock (this)
			{
				_onDisconnected -= value;
			}
		}
	}

	public WsNyxConnection(System.Net.WebSockets.WebSocket webSocket)
	{
		_webSocket = webSocket;
	}

	private void EnsureReadLoopStarted()
	{
		if (!_readLoopStarted)
		{
			_readLoopStarted = true;
			Task.Run(ReadLoopAsync);
		}
	}

	public static async Task<WsNyxConnection> ConnectAsync(string uri)
	{
		var client = new ClientWebSocket();
		await client.ConnectAsync(new Uri(uri), CancellationToken.None);
		return new WsNyxConnection(client);
	}

	private async Task ReadLoopAsync()
	{
		var buffer = new byte[4096];
		using var ms = new MemoryStream();
		try
		{
			while (IsConnected)
			{
				ms.SetLength(0);
				WebSocketReceiveResult result;
				do
				{
					result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
					if (result.MessageType == WebSocketMessageType.Close)
					{
						await CloseAsync();
						return;
					}
					ms.Write(buffer, 0, result.Count);
				} while (!result.EndOfMessage);

				if (ms.Length > 0)
				{
					byte[] rawBytes = ms.ToArray();
					if (rawBytes.Length >= 4)
					{
						int length = BitConverter.ToInt32(rawBytes, 0);
						if (rawBytes.Length >= 4 + length)
						{
							byte[] payload = new byte[length];
							Buffer.BlockCopy(rawBytes, 4, payload, 0, length);
							_onDataReceived?.Invoke(this, payload);
						}
					}
				}
			}
		}
		catch
		{
			await CloseAsync();
		}
	}

	public async Task SendAsync(byte[] data)
	{
		if (!IsConnected) return;
		try
		{
			await _webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, _cts.Token);
		}
		catch
		{
			await CloseAsync();
		}
	}

	public async Task DisconnectAsync()
	{
		await CloseAsync();
	}

	private async Task CloseAsync()
	{
		if (_isClosed) return;
		_isClosed = true;
		_cts.Cancel();

		try
		{
			if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
			{
				await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
			}
		}
		catch {}
		try { _webSocket.Dispose(); } catch {}

		_onDisconnected?.Invoke(this);
	}

	public void Dispose()
	{
		_ = CloseAsync();
	}
}
