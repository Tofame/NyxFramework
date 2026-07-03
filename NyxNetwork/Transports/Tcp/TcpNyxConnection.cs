using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NyxNetwork.Core;
using NyxNetwork.Messaging;

namespace NyxNetwork.Transports.Tcp;

public class TcpNyxConnection : INyxConnection
{
	private readonly TcpClient _client;
	private readonly NetworkStream _stream;
	private readonly CancellationTokenSource _cts = new();
	private bool _isClosed = false;
	private bool _readLoopStarted = false;
	private readonly byte[] _lengthBuffer = new byte[4];
	private event Action<INyxConnection, byte[]>? _onDataReceived;
	private event Action<INyxConnection>? _onDisconnected;

	public Guid Id { get; } = Guid.NewGuid();
	public bool IsConnected => _client.Connected && !_isClosed;

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

	public TcpNyxConnection(TcpClient client)
	{
		_client = client;
		_stream = client.GetStream();
	}

	private void EnsureReadLoopStarted()
	{
		if (!_readLoopStarted)
		{
			_readLoopStarted = true;
			Task.Run(ReadLoopAsync);
		}
	}

	public static async Task<TcpNyxConnection> ConnectAsync(string host, int port)
	{
		var client = new TcpClient();
		await client.ConnectAsync(host, port);
		return new TcpNyxConnection(client);
	}

	private async Task ReadLoopAsync()
	{
		try
		{
			while (IsConnected)
			{
				await PacketReader.ReadExactlyAsync(_stream, _lengthBuffer, 0, 4, _cts.Token);
				int length = BitConverter.ToInt32(_lengthBuffer, 0);

				if (length < 0 || length > 10 * 1024 * 1024)
				{
					throw new InvalidDataException("Invalid packet length prefix received.");
				}

				byte[] payloadBuffer = new byte[length];
				await PacketReader.ReadExactlyAsync(_stream, payloadBuffer, 0, length, _cts.Token);

				_onDataReceived?.Invoke(this, payloadBuffer);
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
			await _stream.WriteAsync(data, _cts.Token);
			await _stream.FlushAsync(_cts.Token);
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

		try { _stream.Dispose(); } catch {}
		try { _client.Close(); } catch {}

		_onDisconnected?.Invoke(this);
	}

	public void Dispose()
	{
		_ = CloseAsync();
	}
}
