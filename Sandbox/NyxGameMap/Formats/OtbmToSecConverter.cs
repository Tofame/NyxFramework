using System.Buffers.Binary;
using System.IO;
using NyxGameCore;

namespace NyxGameMap.Formats;

internal class OtbmReader
{
	private readonly byte[] _data;
	private int _offset;

	public OtbmReader(byte[] data, int startOffset = 0)
	{
		_data = data;
		_offset = startOffset;
	}

	public bool CanRead(int length) => _offset + length <= _data.Length;

	public byte ReadU8() => _data[_offset++];

	public ushort ReadU16()
	{
		ushort v = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_offset, 2));
		_offset += 2;
		return v;
	}

	public uint ReadU32()
	{
		uint v = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_offset, 4));
		_offset += 4;
		return v;
	}

	public void Skip(int count)
	{
		_offset += count;
	}

	public string ReadString()
	{
		ushort len = ReadU16();
		string s = System.Text.Encoding.UTF8.GetString(_data, _offset, len);
		_offset += len;
		return s;
	}

	public void SkipAttributeMap()
	{
		if (!CanRead(2)) return;
		ushort count = ReadU16();
		for (int i = 0; i < count; i++)
		{
			ReadString(); // key
			byte type = ReadU8(); // type
			switch (type)
			{
				case 1: // STRING
					{
						uint len = ReadU32();
						Skip((int)len);
					}
					break;
				case 2: // INTEGER
				case 3: // FLOAT
					Skip(4);
					break;
				case 4: // DOUBLE
					Skip(8);
					break;
				case 5: // BOOLEAN
					Skip(1);
					break;
			}
		}
	}
}

public static class OtbmToSecConverter
{
	public static void Convert(string nbmFilePath, string outputDir)
	{
		if (!File.Exists(nbmFilePath))
			throw new FileNotFoundException("NBM file not found.", nbmFilePath);

		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);

		byte[] rawBytes = File.ReadAllBytes(nbmFilePath);
		OtbmNode root = OtbmParser.Parse(rawBytes);

		if (root.Type != (byte)OtbmNodeType.RootV1)
			throw new InvalidDataException("Invalid NBM root node type");

		// Find Map Data node
		OtbmNode? mapData = null;
		foreach (var child in root.Children)
		{
			if (child.Type == (byte)OtbmNodeType.MapData)
			{
				mapData = child;
				break;
			}
		}

		if (mapData == null)
			throw new InvalidDataException("NBM does not contain MapData node");

		// Group all parsed tiles by sector
		var sectors = new Dictionary<(int CX, int CY, int CZ), SecSector>();

		foreach (var areaNode in mapData.Children)
		{
			if (areaNode.Type != (byte)OtbmNodeType.TileArea)
				continue;

			if (areaNode.Data.Length < 6)
				continue;

			ushort base_x = BinaryPrimitives.ReadUInt16LittleEndian(areaNode.Data.AsSpan(1, 2));
			ushort base_y = BinaryPrimitives.ReadUInt16LittleEndian(areaNode.Data.AsSpan(3, 2));
			byte base_z = areaNode.Data[5];

			foreach (var tileNode in areaNode.Children)
			{
				if (tileNode.Type != (byte)OtbmNodeType.Tile && tileNode.Type != (byte)OtbmNodeType.HouseTile)
					continue;

				if (tileNode.Data.Length < 3)
					continue;

				byte x_offset = tileNode.Data[1];
				byte y_offset = tileNode.Data[2];

				int absX = base_x + x_offset;
				int absY = base_y + y_offset;
				int absZ = base_z;

				// Minecraft chunk mapping: size is 32
				int cx = absX / 32;
				int cy = absY / 32;
				int cz = absZ;

				var key = (cx, cy, cz);
				if (!sectors.TryGetValue(key, out var sector))
				{
					sector = new SecSector(cx, cy, cz);
					sectors[key] = sector;
				}

				int rx = absX % 32;
				int ry = absY % 32;
				var tile = sector.Tiles[rx, ry];


				// Read ground item
				int dataPos = (tileNode.Type == (byte)OtbmNodeType.HouseTile) ? 7 : 3;
				var reader = new OtbmReader(tileNode.Data, dataPos);
				ushort groundId = 0;
				while (reader.CanRead(1))
				{
					byte attr = reader.ReadU8();
					if (attr == (byte)OtbmAttribute.TileFlags)
					{
						reader.ReadU32();
					}
					else if (attr == (byte)OtbmAttribute.Item)
					{
						groundId = reader.ReadU16();
					}
				}

				if (groundId != 0)
				{
					tile.SetGround(new Item(groundId));
				}

				// Read items stacked on tile
				foreach (var itemNode in tileNode.Children)
				{
					if (itemNode.Type != (byte)OtbmNodeType.Item)
						continue;

					if (itemNode.Data.Length < 3)
						continue;

					var itemReader = new OtbmReader(itemNode.Data, 1);
					ushort itemId = itemReader.ReadU16();
					ushort count = 1;

					while (itemReader.CanRead(1))
					{
						byte attr = itemReader.ReadU8();
						switch (attr)
						{
							case (byte)OtbmAttribute.Count:
							case (byte)OtbmAttribute.RuneCharges:
							case (byte)OtbmAttribute.HouseDoorId:
							case (byte)OtbmAttribute.Tier:
								count = itemReader.ReadU8();
								break;
							case (byte)OtbmAttribute.Charges:
								count = itemReader.ReadU16();
								break;
							case (byte)OtbmAttribute.ActionId:
							case (byte)OtbmAttribute.UniqueId:
							case (byte)OtbmAttribute.DepotId:
								itemReader.ReadU16();
								break;
							case (byte)OtbmAttribute.Text:
							case (byte)OtbmAttribute.Desc:
								itemReader.ReadString();
								break;
							case (byte)OtbmAttribute.TeleDest:
								itemReader.ReadU16(); // x
								itemReader.ReadU16(); // y
								itemReader.ReadU8(); // z
								break;
							case (byte)OtbmAttribute.PodiumOutfit:
								itemReader.Skip(15);
								break;
							case (byte)OtbmAttribute.AttributeMap:
								itemReader.SkipAttributeMap();
								break;
						}
					}

					tile.AddItemDirect(new Item(itemId, count));
				}
			}
		}

		// Write sectors to files
		foreach (var kv in sectors)
		{
			var sector = kv.Value;
			string filename = SecSector.GetFileName(sector.ChunkX, sector.ChunkY, sector.ChunkZ);
			string fullPath = Path.Combine(outputDir, filename);
			sector.Write(fullPath);
		}
	}
}
