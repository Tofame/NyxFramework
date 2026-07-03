using System.Buffers.Binary;

namespace NyxGameMap.Formats;

public enum OtbmNodeType : byte
{
	RootV1 = 1,
	MapData = 2,
	ItemDef = 3,
	TileArea = 4,
	Tile = 5,
	Item = 6,
	TileSquare = 7,
	TileRef = 8,
	Spawns = 9,
	SpawnArea = 10,
	Monster = 11,
	Towns = 12,
	Town = 13,
	HouseTile = 14,
	Waypoints = 15,
	Waypoint = 16
}

public enum OtbmAttribute : byte
{
	Description = 1,
	ExtFile = 2,
	TileFlags = 3,
	ActionId = 4,
	UniqueId = 5,
	Text = 6,
	Desc = 7,
	TeleDest = 8,
	Item = 9,
	DepotId = 10,
	ExtSpawnFile = 11,
	RuneCharges = 12,
	ExtHouseFile = 13,
	HouseDoorId = 14,
	Count = 15,
	Duration = 16,
	DecayingState = 17,
	WrittenDate = 18,
	WrittenBy = 19,
	SleeperGuid = 20,
	SleepStart = 21,
	Charges = 22,
	ExtSpawnNpcFile = 23,
	PodiumOutfit = 40,
	Tier = 41,
	AttributeMap = 128
}

public sealed class OtbmNode
{
	public byte Type => Data.Length > 0 ? Data[0] : (byte)0;
	public byte[] Data { get; }
	public List<OtbmNode> Children { get; } = new();

	public OtbmNode(byte[] data)
	{
		Data = data;
	}
}

public static class OtbmParser
{
	private const byte NodeStart = 0xFE;
	private const byte NodeEnd = 0xFF;
	private const byte EscapeChar = 0xFD;

	public static OtbmNode Parse(byte[] bytes)
	{
		if (bytes.Length < 4)
			throw new Exception("File too short");

		uint ver = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0, 4));
		int pos = 4;

		var stack = new Stack<OtbmNode>();
		OtbmNode? root = null;

		while (pos < bytes.Length)
		{
			byte b = bytes[pos++];
			if (b == NodeStart)
			{
				var propBytes = new List<byte>();
				while (pos < bytes.Length)
				{
					byte pb = bytes[pos];
					if (pb == NodeStart || pb == NodeEnd)
					{
						break;
					}
					pos++;
					if (pb == EscapeChar)
					{
						if (pos >= bytes.Length)
							throw new Exception("Premature end of file after escape character");
						pb = bytes[pos++];
					}
					propBytes.Add(pb);
				}

				var node = new OtbmNode(propBytes.ToArray());
				if (stack.Count > 0)
				{
					stack.Peek().Children.Add(node);
				}
				else
				{
					root = node;
				}
				stack.Push(node);
			}
			else if (b == NodeEnd)
			{
				if (stack.Count > 0)
				{
					stack.Pop();
				}
				if (stack.Count == 0)
				{
					break;
				}
			}
		}

		return root ?? throw new Exception("No root node found in NBM file");
	}

	public static byte[] Serialize(OtbmNode root, string identifier = "NBM")
	{
		var ms = new MemoryStream();
		using (var writer = new BinaryWriter(ms))
		{
			byte[] idBytes = System.Text.Encoding.ASCII.GetBytes(identifier.PadRight(4).Substring(0, 4));
			writer.Write(idBytes);
			SerializeNode(root, writer);
		}
		return ms.ToArray();
	}

	private static void SerializeNode(OtbmNode node, BinaryWriter writer)
	{
		writer.Write(NodeStart);

		foreach (var b in node.Data)
		{
			if (b == NodeStart || b == NodeEnd || b == EscapeChar)
			{
				writer.Write(EscapeChar);
			}
			writer.Write(b);
		}

		foreach (var child in node.Children)
		{
			SerializeNode(child, writer);
		}

		writer.Write(NodeEnd);
	}
}
