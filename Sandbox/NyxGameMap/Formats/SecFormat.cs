using System.IO;
using NyxGameCore;

namespace NyxGameMap.Formats;

public sealed class SecSector
{
	public const int SizeX = 32;
	public const int SizeY = 32;

	public int ChunkX { get; }
	public int ChunkY { get; }
	public int ChunkZ { get; }

	public Tile[,] Tiles { get; } = new Tile[SizeX, SizeY];

	public SecSector(int cx, int cy, int cz)
	{
		ChunkX = cx;
		ChunkY = cy;
		ChunkZ = cz;
		for (int y = 0; y < SizeY; y++)
		{
			for (int x = 0; x < SizeX; x++)
			{
				Tiles[x, y] = new Tile();
			}
		}
	}

	public static string GetFileName(int cx, int cy, int cz)
	{
		return $"{cx:D4}_{cy:D4}_{cz:D4}.sec";
	}

	public static SecSector Read(string filePath)
	{
		var filename = Path.GetFileNameWithoutExtension(filePath);
		var parts = filename.Split('_');
		int cx = 0, cy = 0, cz = 0;
		if (parts.Length >= 3 && int.TryParse(parts[0], out int px) && int.TryParse(parts[1], out int py) && int.TryParse(parts[2], out int pz))
		{
			cx = px;
			cy = py;
			cz = pz;
		}

		var sector = new SecSector(cx, cy, cz);
		using var stream = File.OpenRead(filePath);
		using var reader = new BinaryReader(stream);

		byte[] magic = reader.ReadBytes(6);
		if (magic.Length != 6 || System.Text.Encoding.ASCII.GetString(magic) != "NYXSEC")
			throw new InvalidDataException("Invalid SEC header magic");

		byte version = reader.ReadByte();
		if (version != 2)
			throw new InvalidDataException($"Unsupported SEC version: {version}");

		byte sizeX = reader.ReadByte();
		byte sizeY = reader.ReadByte();
		if (sizeX != SizeX || sizeY != SizeY)
			throw new InvalidDataException($"Invalid SEC dimensions: {sizeX}x{sizeY}");

		reader.ReadBytes(2);

		for (int y = 0; y < SizeY; y++)
		{
			for (int x = 0; x < SizeX; x++)
			{
				var tile = sector.Tiles[x, y];
				ushort groundId = reader.ReadUInt16();
				if (groundId != 0)
				{
					tile.SetGround(new Item(groundId));
				}

				byte itemCount = reader.ReadByte();
				for (int i = 0; i < itemCount; i++)
				{
					ushort itemId = reader.ReadUInt16();
					ushort count = reader.ReadUInt16();
					tile.AddItemDirect(new Item(itemId, count));
				}
			}
		}

		return sector;
	}

	public void Write(string filePath)
	{
		using var stream = File.Create(filePath);
		using var writer = new BinaryWriter(stream);

		writer.Write(System.Text.Encoding.ASCII.GetBytes("NYXSEC"));
		writer.Write((byte)2); // Version
		writer.Write((byte)SizeX);
		writer.Write((byte)SizeY);
		writer.Write((ushort)0);

		for (int y = 0; y < SizeY; y++)
		{
			for (int x = 0; x < SizeX; x++)
			{
				var tile = Tiles[x, y];
				writer.Write((ushort)tile.GroundId);

				int itemCount = tile.Items.Count;
				writer.Write((byte)itemCount);
				for (int i = 0; i < itemCount; i++)
				{
					var item = tile.Items[i];
					writer.Write((ushort)item.ItemTypeId);
					writer.Write((ushort)item.Count);
				}
			}
		}
	}
}
