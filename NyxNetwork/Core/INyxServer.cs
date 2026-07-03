using System;
using System.Threading.Tasks;

namespace NyxNetwork.Core;

public interface INyxServer : IDisposable
{
	bool IsRunning { get; }
	int Port { get; }
	event Action<INyxConnection> OnClientConnected;
	event Action<INyxConnection> OnClientDisconnected;
	event Action<INyxConnection, byte[]> OnDataReceived;
	Task StartAsync(int port);
	Task StopAsync();
	Task BroadcastAsync(byte[] data);
}
