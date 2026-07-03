# AGENTS.md — NyxNetwork

This project manages TCP/UDP network communication, host/client discovery, and packet serialization.

## Architecture & Responsibilities

- **Sockets**: Handles low-level network connections and incoming data streams.
- **Packet Serialization**: Serializes game packets and structures into binary network streams.
- **Message Queue**: Manages asynchronous packet queues to sync incoming network events with the main game thread.
