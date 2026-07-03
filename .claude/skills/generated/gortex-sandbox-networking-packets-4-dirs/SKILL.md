---
name: gortex-sandbox-networking-packets-4-dirs
description: "Work in the Sandbox\Networking\Packets +4 dirs area — 180 symbols across 12 files (93% cohesion)"
---

# Sandbox\Networking\Packets +4 dirs

180 symbols | 12 files | 93% cohesion

## When to Use

Use this skill when working on files in:
- `NyxNetwork\Core\INyxConnection.cs`
- `NyxNetwork\Messaging\IPacket.cs`
- `NyxNetwork\Messaging\PacketWriter.cs`
- `Sandbox\Networking\Packets\ItemUpdatePacket.cs`
- `Sandbox\Networking\Packets\JoinRequestPacket.cs`
- `Sandbox\Networking\Packets\PlayerChatPacket.cs`
- `Sandbox\Networking\Packets\PlayerDisconnectPacket.cs`
- `Sandbox\Networking\Packets\PlayerMovePacket.cs`
- `Sandbox\Networking\Packets\SandboxPacketId.cs`
- `Sandbox\Networking\Packets\SpellCastPacket.cs`
- `Sandbox\Networking\SandboxProtocolGame.cs`
- `Sandbox\SandboxGameWorld.cs`

## Key Files

| File | Symbols |
|------|---------|
| `NyxNetwork\Core\INyxConnection.cs` | INyxConnection |
| `NyxNetwork\Messaging\IPacket.cs` | IPacket |
| `NyxNetwork\Messaging\PacketWriter.cs` | PacketWriter |
| `Sandbox\Networking\Packets\ItemUpdatePacket.cs` | ItemTypeId, Count, Y, IsPlacement, Z, ... |
| `Sandbox\Networking\Packets\JoinRequestPacket.cs` | writer, LookMount, LookBody, LookHead, PlayerName, ... |
| `Sandbox\Networking\Packets\PlayerChatPacket.cs` | PacketId, ChatType, SenderName, Serialize, PlayerChatPacket, ... |
| `Sandbox\Networking\Packets\PlayerDisconnectPacket.cs` | Serialize, reader, PacketId, Deserialize, ClientId, ... |
| `Sandbox\Networking\Packets\PlayerMovePacket.cs` | reader, Serialize, PlayerMovePacket, writer, LookLegs, ... |
| `Sandbox\Networking\Packets\SandboxPacketId.cs` | PlayerMove, ItemUpdate, SandboxPacketId, PlayerDisconnect, SpellCast, ... |
| `Sandbox\Networking\Packets\SpellCastPacket.cs` | reader, TargetZ, Deserialize, CasterId, TargetY, ... |
| `Sandbox\Networking\SandboxProtocolGame.cs` | world, packet, rx, _isHost, ry, ... |
| `Sandbox\SandboxGameWorld.cs` | count, x, itemTypeId, isPlacement, SendItemUpdate, ... |

## Entry Points

- `Sandbox\Networking\Packets\PlayerMovePacket.cs::PlayerMovePacket.Serialize`
- `Sandbox\Networking\Packets\PlayerMovePacket.cs::PlayerMovePacket.Deserialize`

## How to Explore

```
get_communities with id: "community-126"
smart_context with task: "understand Sandbox\Networking\Packets +4 dirs", format: "gcx"
find_usages with id: "Sandbox\Networking\Packets\PlayerMovePacket.cs::PlayerMovePacket.Serialize", format: "gcx"
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
