# NyxNetwork API Guide

This document outlines how to initialize, configure, and communicate using `NyxNetwork`.

---

## Performance & Efficiency

`NyxNetwork` is designed to be ultra-efficient with minimal garbage collection overhead:
- **Zero-Allocation Stream Reading**: Reuses custom `ReusableMemoryStream` and `BinaryReader` caches thread-locally to parse incoming packet streams without heap allocations.
- **Single-Allocation Fast Path**: When compression and encryption are disabled, packets serialize directly into a reusable thread-local stream buffer and require exactly one final array allocation for the framed buffer.
- **Framing Buffer Reuse**: Reuses socket length headers dynamically to prevent garbage collector pressure during high-frequency networking loops.

---

## 1. Initializing the NetworkManager

The `NetworkManager` is the unified entrance point for network operations:

```csharp
using NyxNetwork.Core;

// Instantiate manager
var netManager = new NetworkManager();
```

---

## 2. Server Hosting & Broadcasting

To spin up a TCP or WebSocket server on a specific port:

```csharp
// Start hosting on port 8080
netManager.StartServer(TransportType.Tcp, 8080, "My Game World");

// Broadcast data to all clients
byte[] broadcastBytes = GetSomePayload();
await netManager.Server.BroadcastAsync(broadcastBytes);

// Stop hosting
await netManager.StopServerAsync();
```

---

## 3. Client Connections

To connect a client to a server:

```csharp
// Connect to TCP server
await netManager.ConnectClientAsync(TransportType.Tcp, "127.0.0.1", 8080);

// Send packet
byte[] sendBytes = GetSomePayload();
await netManager.ClientConnection.SendAsync(sendBytes);

// Disconnect
await netManager.DisconnectClientAsync();
```

---

## 4. LAN Discovery

`NetworkManager` handles background UDP LAN beaconing automatically when hosting. Clients can easily query local servers:

```csharp
// Start LAN scanning
netManager.StartLanDiscovery(server => 
{
	Console.WriteLine($"Discovered LAN World: {server.Name} at {server.IpAddress}:{server.Port}");
});

// Stop scanning
netManager.StopLanDiscovery();
```

---

## 5. Length-Prefixed Packets & Registry

To serialize strongly-typed packets, implement `IPacket`:

```csharp
using NyxNetwork.Messaging;
using System.IO;

public class MyPacket : IPacket
{
	public ushort PacketId => 100;
	public string Message { get; set; } = string.Empty;

	public void Serialize(BinaryWriter writer)
	{
		writer.Write(Message);
	}

	public void Deserialize(BinaryReader reader)
	{
		Message = reader.ReadString();
	}
}
```

Register packets and listen for events:

```csharp
// Register packet type and callback handler
netManager.PacketRegistry.RegisterPacket(100, () => new MyPacket(), (conn, packet) =>
{
	Console.WriteLine($"Received from client {conn.Id}: {packet.Message}");
});

// Bind OnDataReceived to ParseAndDispatch
netManager.OnDataReceived += (conn, data) =>
{
	netManager.PacketRegistry.ParseAndDispatch(conn, data);
};
```

---

## 6. Compression & Encryption Configuration

`NyxNetwork` includes built-in support for packet payload compression (using Deflate compression) and payload encryption (using a standard 128-bit XTEA cipher).

These features can be configured on the `NetworkManager` instance. When enabled, compression and encryption are applied transparently before the packet is framed with its length prefix and transmitted, and automatically reverted upon receipt prior to handler dispatch.

### Configuring Compression & Encryption

```csharp
// Enable Deflate compression
netManager.UseCompression = true;

// Enable XTEA encryption with a 128-bit key (4 unsigned integers)
netManager.UseEncryption = true;
netManager.EncryptionKey = new uint[] { 0x11111111, 0x22222222, 0x33333333, 0x44444444 };
```

> [!WARNING]
> Both client and server must have the exact same compression and encryption configuration (and encryption key) to correctly parse packet streams.

