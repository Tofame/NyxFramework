using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Buffers;
using System.Collections.Generic;
using NyxAssets.Data.Readers;
using ZstdSharp;

namespace NyxAssets.Sprites;

public struct SpriteIndexEntry
{
	public uint PageId;
	public uint LocalIndex;
}

public struct PageEntry
{
	public ulong Offset;
	public uint CompressedSize;
	public uint UncompressedSize;
	public uint SpriteCount;
}

/// <summary>
/// Alternate sprite source using ZSTD page-based compression.
/// </summary>
public sealed class AssetArchive : ISpriteSource
{
	private readonly ReadOnlyMemory<byte> _memory;
	private readonly bool _isMemoryBacked;
	private readonly MemoryMappedFile? _mappedFile;
	private readonly MemoryMappedViewAccessor? _mappedView;
	private bool _disposed;

	public const uint MagicSignature = 0x54535341; // 'ASST'
	private const int HeaderSize = 16; // 4 bytes signature + 4 bytes version + 4 bytes pageCount + 4 bytes spriteCount

	private readonly SpriteIndexEntry[] _index;
	private readonly PageEntry[] _pageTable;
	private readonly byte[][]? _preloadedPages;
	private readonly Dictionary<uint, int[]> _pageSpriteOffsets = new();

	private static readonly ThreadLocal<Decompressor> SharedDecompressor = new(() => new Decompressor());

	// LRU Cache variables
	private sealed class CacheEntry
	{
		public uint PageId { get; }
		public byte[] DecompressedPayload { get; }

		public CacheEntry(uint pageId, byte[] decompressedPayload)
		{
			PageId = pageId;
			DecompressedPayload = decompressedPayload;
		}
	}

	private readonly Dictionary<uint, LinkedListNode<CacheEntry>> _cache = new();
	private readonly LinkedList<CacheEntry> _lruList = new();
	private int _maxCachedPages = 64;

	private AssetArchive(
		ReadOnlyMemory<byte> memory,
		bool isMemoryBacked,
		MemoryMappedFile? mappedFile,
		MemoryMappedViewAccessor? mappedView,
		uint signature,
		uint version,
		uint pageCount,
		uint spriteCount,
		SpriteIndexEntry[] index,
		PageEntry[] pageTable,
		byte[][]? preloadedPages)
	{
		_memory = memory;
		_isMemoryBacked = isMemoryBacked;
		_mappedFile = mappedFile;
		_mappedView = mappedView;
		Signature = signature;
		Version = version;
		PageCount = pageCount;
		SpriteCount = spriteCount;
		_index = index;
		_pageTable = pageTable;
		_preloadedPages = preloadedPages;
	}

	public uint Signature { get; }
	public uint Version { get; }
	public uint PageCount { get; }
	public uint SpriteCount { get; }

	public static AssetArchive Load(ReadOnlyMemory<byte> fileData, bool preloadPages = false)
	{
		if (fileData.Length < HeaderSize)
			throw new InvalidDataException("Asset archive file too small.");

		var sig = BinaryLittleEndian.ReadUInt32(fileData.Span, 0);
		if (sig != MagicSignature)
			throw new InvalidDataException($"Invalid asset archive signature. Expected 0x{MagicSignature:X8}, got 0x{sig:X8}.");

		var version = BinaryLittleEndian.ReadUInt32(fileData.Span, 4);
		if (version != 1)
			throw new InvalidDataException($"Unsupported asset archive version. Expected 1, got {version}.");

		var pageCount = BinaryLittleEndian.ReadUInt32(fileData.Span, 8);
		var spriteCount = BinaryLittleEndian.ReadUInt32(fileData.Span, 12);

		var index = new SpriteIndexEntry[spriteCount];
		long offset = HeaderSize;
		for (uint i = 0; i < spriteCount; i++)
		{
			index[i] = new SpriteIndexEntry
			{
				PageId = BinaryLittleEndian.ReadUInt32(fileData.Span, (int)offset),
				LocalIndex = BinaryLittleEndian.ReadUInt32(fileData.Span, (int)offset + 4)
			};
			offset += 8;
		}

		var pageTable = new PageEntry[pageCount];
		for (uint i = 0; i < pageCount; i++)
		{
			pageTable[i] = new PageEntry
			{
				Offset = BinaryLittleEndian.ReadUInt64(fileData.Span, (int)offset),
				CompressedSize = BinaryLittleEndian.ReadUInt32(fileData.Span, (int)offset + 8),
				UncompressedSize = BinaryLittleEndian.ReadUInt32(fileData.Span, (int)offset + 12),
				SpriteCount = BinaryLittleEndian.ReadUInt32(fileData.Span, (int)offset + 16)
			};
			offset += 20;
		}

		byte[][]? preloadedPages = null;
		if (preloadPages && pageCount > 0)
		{
			preloadedPages = new byte[pageCount][];
			System.Threading.Tasks.Parallel.For(0, (int)pageCount, i =>
			{
				var entry = pageTable[i];
				var decompressed = new byte[entry.UncompressedSize];
				var slice = fileData.Span.Slice((int)entry.Offset, (int)entry.CompressedSize);
				SharedDecompressor.Value!.Unwrap(slice, decompressed);
				preloadedPages[i] = decompressed;
			});
		}

		return new AssetArchive(fileData, isMemoryBacked: true, null, null, sig, version, pageCount, spriteCount, index, pageTable, preloadedPages);
	}

	public static AssetArchive OpenReadOnlyFile(string filePath, bool preloadPages = false)
	{
		if (filePath is null)
			throw new ArgumentNullException(nameof(filePath));

		var mmf = MemoryMappedFile.CreateFromFile(
			filePath,
			FileMode.Open,
			mapName: null,
			capacity: 0,
			MemoryMappedFileAccess.Read);

		MemoryMappedViewAccessor view;
		try
		{
			view = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
		}
		catch
		{
			mmf.Dispose();
			throw;
		}

		try
		{
			var length = view.Capacity;
			if (length < HeaderSize)
				throw new InvalidDataException("Asset archive file too small.");

			var sig = view.ReadUInt32(0);
			if (sig != MagicSignature)
				throw new InvalidDataException($"Invalid asset archive signature. Expected 0x{MagicSignature:X8}, got 0x{sig:X8}.");

			var version = view.ReadUInt32(4);
			if (version != 1)
				throw new InvalidDataException($"Unsupported asset archive version. Expected 1, got {version}.");

			var pageCount = view.ReadUInt32(8);
			var spriteCount = view.ReadUInt32(12);

			var index = new SpriteIndexEntry[spriteCount];
			long offset = HeaderSize;
			for (uint i = 0; i < spriteCount; i++)
			{
				index[i] = new SpriteIndexEntry
				{
					PageId = view.ReadUInt32(offset),
					LocalIndex = view.ReadUInt32(offset + 4)
				};
				offset += 8;
			}

			var pageTable = new PageEntry[pageCount];
			for (uint i = 0; i < pageCount; i++)
			{
				pageTable[i] = new PageEntry
				{
					Offset = view.ReadUInt64(offset),
					CompressedSize = view.ReadUInt32(offset + 8),
					UncompressedSize = view.ReadUInt32(offset + 12),
					SpriteCount = view.ReadUInt32(offset + 16)
				};
				offset += 20;
			}

			byte[][]? preloadedPages = null;
			if (preloadPages && pageCount > 0)
			{
				preloadedPages = new byte[pageCount][];
				System.Threading.Tasks.Parallel.For(0, (int)pageCount, i =>
				{
					var entry = pageTable[i];
					var compressedBuffer = new byte[entry.CompressedSize];
					view.ReadArray((long)entry.Offset, compressedBuffer, 0, (int)entry.CompressedSize);

					var decompressed = new byte[entry.UncompressedSize];
					SharedDecompressor.Value!.Unwrap(compressedBuffer.AsSpan(0, (int)entry.CompressedSize), decompressed);
					preloadedPages[i] = decompressed;
				});
			}

			return new AssetArchive(default, isMemoryBacked: false, mmf, view, sig, version, pageCount, spriteCount, index, pageTable, preloadedPages);
		}
		catch
		{
			view.Dispose();
			mmf.Dispose();
			throw;
		}
	}

	public void SetMaxCachedPages(int count)
	{
		lock (_cache)
		{
			_maxCachedPages = count;
			EvictIfNeeded();
		}
	}

	private byte[]? GetPageFromCache(uint pageId)
	{
		lock (_cache)
		{
			if (_cache.TryGetValue(pageId, out var node))
			{
				_lruList.Remove(node);
				_lruList.AddFirst(node);
				return node.Value.DecompressedPayload;
			}
			return null;
		}
	}

	private void AddPageToCache(uint pageId, byte[] decompressedPayload)
	{
		lock (_cache)
		{
			if (_cache.TryGetValue(pageId, out var existingNode))
			{
				_lruList.Remove(existingNode);
				_lruList.AddFirst(existingNode);
				return;
			}

			var entry = new CacheEntry(pageId, decompressedPayload);
			var node = new LinkedListNode<CacheEntry>(entry);
			_lruList.AddFirst(node);
			_cache[pageId] = node;

			EvictIfNeeded();
		}
	}

	private void EvictIfNeeded()
	{
		while (_cache.Count > _maxCachedPages && _lruList.Last is not null)
		{
			var last = _lruList.Last;
			_lruList.RemoveLast();
			_cache.Remove(last.Value.PageId);
		}
	}

	private byte[]? GetPageDecompressedPayload(uint pageId, PageEntry pageEntry)
	{
		var cached = GetPageFromCache(pageId);
		if (cached != null)
			return cached;

		var compressed = ArrayPool<byte>.Shared.Rent((int)pageEntry.CompressedSize);
		var decompressed = new byte[pageEntry.UncompressedSize];
		try
		{
			if (_isMemoryBacked)
			{
				_memory.Span.Slice((int)pageEntry.Offset, (int)pageEntry.CompressedSize).CopyTo(compressed);
			}
			else
			{
				_mappedView!.ReadArray((long)pageEntry.Offset, compressed, 0, (int)pageEntry.CompressedSize);
			}

			var decompressedSize = SharedDecompressor.Value!.Unwrap(compressed.AsSpan(0, (int)pageEntry.CompressedSize), decompressed);
			if (decompressedSize != pageEntry.UncompressedSize)
			{
				throw new InvalidDataException($"ZSTD decompression returned length {decompressedSize}, expected {pageEntry.UncompressedSize}.");
			}

			AddPageToCache(pageId, decompressed);
			return decompressed;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(compressed);
		}
	}

	public bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination)
	{
		ThrowIfDisposed();
		if (spriteId == 0 || spriteId > SpriteCount)
			return false;

		var indexEntry = _index[spriteId - 1];
		if (indexEntry.PageId >= PageCount)
			return false;

		var pageEntry = _pageTable[indexEntry.PageId];
		if (indexEntry.LocalIndex >= pageEntry.SpriteCount)
			return false;

		var decompressedPayload = _preloadedPages != null
			? _preloadedPages[indexEntry.PageId]
			: GetPageDecompressedPayload(indexEntry.PageId, pageEntry);
		if (decompressedPayload == null)
			return false;

		var spriteOffsets = GetOrBuildSpriteOffsets(indexEntry.PageId, decompressedPayload.AsSpan(), pageEntry.SpriteCount);
		if (indexEntry.LocalIndex >= spriteOffsets.Length)
			return false;

		var span = decompressedPayload.AsSpan();
		if (!AssetPageLayout.TryGetSpriteBounds(span, spriteOffsets[indexEntry.LocalIndex], out var pixelOffset, out var width, out var height))
			return false;

		if (width == 0 || height == 0)
		{
			rgbaDestination.Clear();
			return true;
		}

		var pixelBytesCount = width * height * 4;
		if (pixelOffset + pixelBytesCount > span.Length)
			return false;

		if (rgbaDestination.Length < pixelBytesCount)
			throw new ArgumentException($"Buffer must be at least {pixelBytesCount} bytes.", nameof(rgbaDestination));

		span.Slice(pixelOffset, pixelBytesCount).CopyTo(rgbaDestination);
		return true;
	}

	public byte[] DecodeSpriteById(uint spriteId)
	{
		var buf = new byte[SpritePixelCodec.RgbaBufferLength];
		if (!TryDecodeSpriteById(spriteId, buf))
			throw new InvalidDataException($"Sprite {spriteId} is missing or invalid.");
		return buf;
	}

	public bool IsEmptySprite(uint spriteId)
	{
		ThrowIfDisposed();
		if (spriteId == 0 || spriteId > SpriteCount)
			return true;

		var indexEntry = _index[spriteId - 1];
		if (indexEntry.PageId >= PageCount)
			return true;

		var pageEntry = _pageTable[indexEntry.PageId];
		if (indexEntry.LocalIndex >= pageEntry.SpriteCount)
			return true;

		var decompressedPayload = _preloadedPages != null
			? _preloadedPages[indexEntry.PageId]
			: GetPageDecompressedPayload(indexEntry.PageId, pageEntry);
		if (decompressedPayload == null)
			return true;

		var spriteOffsets = GetOrBuildSpriteOffsets(indexEntry.PageId, decompressedPayload.AsSpan(), pageEntry.SpriteCount);
		if (indexEntry.LocalIndex >= spriteOffsets.Length)
			return true;

		return !AssetPageLayout.TryGetSpriteBounds(
			decompressedPayload.AsSpan(),
			spriteOffsets[indexEntry.LocalIndex],
			out _,
			out var width,
			out var height)
			|| width == 0
			|| height == 0;
	}

	private int[] GetOrBuildSpriteOffsets(uint pageId, ReadOnlySpan<byte> pagePayload, uint spriteCount)
	{
		if (_pageSpriteOffsets.TryGetValue(pageId, out var offsets))
			return offsets;

		offsets = AssetPageLayout.BuildSpriteOffsets(pagePayload, spriteCount);
		_pageSpriteOffsets[pageId] = offsets;
		return offsets;
	}

	public void Dispose()
	{
		if (_isMemoryBacked)
			return;
		if (_disposed)
			return;
		_disposed = true;
		_mappedView?.Dispose();
		_mappedFile?.Dispose();
	}

	private void ThrowIfDisposed()
	{
		if (!_isMemoryBacked && _disposed)
			throw new ObjectDisposedException(nameof(AssetArchive));
	}
}
