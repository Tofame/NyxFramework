using System;
using System.IO;
using System.Collections.Generic;
using ZstdSharp;

namespace NyxAssets.Sprites;

/// <summary>
/// Compiles a set of sprites into ZSTD page-based .assets format.
/// </summary>
public sealed class AssetArchiveWriter
{
	private readonly List<byte[]> _sprites = new();

	public void AddSprite(ushort width, ushort height, ReadOnlySpan<byte> rgba)
	{
		if (width == 0 || height == 0)
		{
			_sprites.Add(new byte[] { 0, 0, 0, 0 });
			return;
		}

		byte[] entry = new byte[4 + width * height * 4];
		entry[0] = (byte)(width & 0xFF);
		entry[1] = (byte)((width >> 8) & 0xFF);
		entry[2] = (byte)(height & 0xFF);
		entry[3] = (byte)((height >> 8) & 0xFF);
		rgba.CopyTo(entry.AsSpan(4));
		_sprites.Add(entry);
	}

	public void AddRange(IEnumerable<byte[]> sprites)
	{
		_sprites.AddRange(sprites);
	}

	public void Save(string path, int compressionLevel = 3, uint spritesPerPage = 2048)
	{
		using var fs = File.Create(path);
		using var writer = new BinaryWriter(fs);

		uint spriteCount = (uint)_sprites.Count;
		uint pageCount = (spriteCount + spritesPerPage - 1) / spritesPerPage;

		// Write Header placeholders
		writer.Write(AssetArchive.MagicSignature);
		writer.Write((uint)1); // Version
		writer.Write(pageCount);
		writer.Write(spriteCount);

		// Write Sprite Index placeholders
		long indexPos = fs.Position;
		for (uint i = 0; i < spriteCount; i++)
		{
			writer.Write((uint)0); // PageId
			writer.Write((uint)0); // LocalIndex
		}

		// Write Page Table placeholders
		long pageTablePos = fs.Position;
		for (uint i = 0; i < pageCount; i++)
		{
			writer.Write((ulong)0); // Offset
			writer.Write((uint)0);  // CompressedSize
			writer.Write((uint)0);  // UncompressedSize
			writer.Write((uint)0);  // SpriteCount
		}

		// Prepare arrays to hold resolved values
		var indexEntries = new SpriteIndexEntry[spriteCount];
		var pageEntries = new PageEntry[pageCount];
		var compressedPages = new byte[pageCount][];

		System.Threading.Tasks.Parallel.For(0, (int)pageCount, pageId =>
		{
			uint startIdx = (uint)pageId * spritesPerPage;
			uint endIdx = Math.Min(startIdx + spritesPerPage, spriteCount);
			uint pageSpriteCount = endIdx - startIdx;

			// Build uncompressed page payload
			using var ms = new MemoryStream();
			for (uint idx = startIdx; idx < endIdx; idx++)
			{
				uint localIndex = idx - startIdx;
				indexEntries[idx] = new SpriteIndexEntry
				{
					PageId = (uint)pageId,
					LocalIndex = localIndex
				};

				byte[] spriteBytes = _sprites[(int)idx];
				ms.Write(spriteBytes, 0, spriteBytes.Length);
			}

			byte[] uncompressed = ms.ToArray();
			using var compressor = new Compressor(compressionLevel);
			compressedPages[pageId] = compressor.Wrap(uncompressed).ToArray();

			pageEntries[pageId] = new PageEntry
			{
				UncompressedSize = (uint)uncompressed.Length,
				SpriteCount = pageSpriteCount
			};
		});

		for (uint pageId = 0; pageId < pageCount; pageId++)
		{
			byte[] compressed = compressedPages[pageId];
			ulong offset = (ulong)fs.Position;
			fs.Write(compressed);

			pageEntries[pageId].Offset = offset;
			pageEntries[pageId].CompressedSize = (uint)compressed.Length;
		}

		// Write Sprite Index
		fs.Position = indexPos;
		for (uint i = 0; i < spriteCount; i++)
		{
			writer.Write(indexEntries[i].PageId);
			writer.Write(indexEntries[i].LocalIndex);
		}

		// Write Page Table
		fs.Position = pageTablePos;
		for (uint i = 0; i < pageCount; i++)
		{
			writer.Write(pageEntries[i].Offset);
			writer.Write(pageEntries[i].CompressedSize);
			writer.Write(pageEntries[i].UncompressedSize);
			writer.Write(pageEntries[i].SpriteCount);
		}
	}

	public static void ConvertSprToAssets(string sprPath, string assetsPath, bool extendedSpriteIds, bool transparentPixels, int compressionLevel = 3, uint spritesPerPage = 2048)
	{
		using var sprSource = SpriteArchive.OpenReadOnlyFile(sprPath, extendedSpriteIds, transparentPixels);
		var writer = new AssetArchiveWriter();
		
		var spritesArray = new byte[sprSource.SpriteCount][];
		System.Threading.Tasks.Parallel.For(1, (int)sprSource.SpriteCount + 1, id =>
		{
			if (sprSource.IsEmptySprite((uint)id))
			{
				spritesArray[id - 1] = new byte[] { 0, 0, 0, 0 };
			}
			else
			{
				var rgba = new byte[SpritePixelCodec.RgbaBufferLength];
				if (sprSource.TryDecodeSpriteById((uint)id, rgba))
				{
					byte[] entry = new byte[4 + 32 * 32 * 4];
					entry[0] = 32;
					entry[1] = 0;
					entry[2] = 32;
					entry[3] = 0;
					rgba.CopyTo(entry.AsSpan(4));
					spritesArray[id - 1] = entry;
				}
				else
				{
					spritesArray[id - 1] = new byte[] { 0, 0, 0, 0 };
				}
			}
		});

		writer.AddRange(spritesArray);
		writer.Save(assetsPath, compressionLevel, spritesPerPage);
	}
}
