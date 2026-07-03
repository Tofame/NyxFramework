# NyxNetwork

`NyxNetwork` is a transport-agnostic, packet-oriented networking class library for the NyxFramework ecosystem. It provides the building blocks to support TCP and WebSockets messaging with strict length-prefixed stream framing.

## Features

- **Transport Agnosticism**: Unified abstractions for client connections and server listeners (TCP & WebSocket).
- **Explicit Framing**: 4-byte length-prefixing payload architecture preventing TCP stream fragmentation/coalescing.
- **Compression & Encryption**: Support for packet payload compression (Deflate) and 128-bit XTEA cipher encryption.
- **Zero-Allocation Stream Reading**: Reuses custom `ReusableMemoryStream` and `BinaryReader` thread-locally to parse incoming packet streams with zero heap allocations.
- **Generic Routing Layer**: PacketRegistry maps packet IDs to custom factory/handlers.
- **LAN Discovery**: Inherent UDP beaconing and scanning support for LAN co-op/multiplayer sessions.

## Directory Structure

```
NyxNetwork/
├── Core/
│   ├── INyxConnection.cs       # Lifecycle state contract for remote endpoints
│   ├── INyxServer.cs           # Listener and connection manager abstraction
│   ├── NetworkManager.cs       # Combined high-level networking wrapper
│   ├── NyxNetworkConfig.cs     # Static compression/encryption options
│   ├── TransportType.cs        # Enum { Tcp, WebSocket }
│   └── LanDiscovery.cs         # UDP Beaconing & Listener for LAN scanning
├── Transports/
│   ├── Tcp/
│   │   ├── TcpNyxConnection.cs # Wraps System.Net.Sockets.TcpClient
│   │   └── TcpNyxListener.cs   # Wraps System.Net.Sockets.TcpListener
│   └── WebSocket/
│       ├── WsNyxConnection.cs  # Wraps System.Net.WebSockets.ClientWebSocket
│       └── WsNyxListener.cs    # Wraps HttpListener upgrade
└── Messaging/
    ├── IPacket.cs              # Binary serialization layout blueprint
    ├── Xtea.cs                 # XTEA block cipher implementation
    ├── PacketProcessor.cs      # Packet compression and encryption wrapper
    ├── ReusableMemoryStream.cs # Zero-allocation incoming stream reader
    ├── PacketReader.cs         # Stream helper for exact TCP packet reads
    ├── PacketWriter.cs         # Stream helper for standard packet composition
    └── PacketRegistry.cs       # Typed Message-to-Handler routing dictionary
```

## API Reference

For detailed library usage, see the [NyxNetwork API Guide](docs/api_guide.md).
