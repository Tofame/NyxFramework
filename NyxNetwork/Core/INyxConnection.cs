using System;
using System.Threading.Tasks;

namespace NyxNetwork.Core;

public interface INyxConnection : IDisposable
{
	Guid Id { get; }
	bool IsConnected { get; }
	event Action<INyxConnection, byte[]> OnDataReceived;
	event Action<INyxConnection> OnDisconnected;
	Task SendAsync(byte[] data);
	Task DisconnectAsync();
}
