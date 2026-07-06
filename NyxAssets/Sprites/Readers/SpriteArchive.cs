using System.IO.MemoryMappedFiles;
using NyxAssets.Data.Readers;
using NyxAssets.Things;

namespace NyxAssets.Sprites;

/// <summary>
/// Random access to a Nyx <c>.spr</c> sheet (Asset Editor <c>SpriteReader</c>).
/// The file starts with a <b>lookup table</b>: for each 1-based sprite id, a <c>uint32</c> byte offset
/// points to that sprite’s compressed blob. You can decode <b>one</b> sprite by id without decoding
/// any other sprite (see <see cref="TryDecodeSpriteById"/>).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><see cref="Load(ReadOnlyMemory{byte}, ClientDataReadOptions)"/> — keeps the full <c>.spr</c> in managed memory (typical when you already have a <c>byte[]</c>).</description></item>
/// <item><description><see cref="OpenReadOnlyFile(string, ClientDataReadOptions)"/> — memory-maps the file so you avoid allocating one huge <c>byte[]</c>; the OS loads pages on demand when you read a sprite.</description></item>
/// </list>
/// </remarks>
public sealed class SpriteArchive : ISpriteSource
{
    private readonly ReadOnlyMemory<byte> _memory;
    private readonly bool _isMemoryBacked;
    private readonly MemoryMappedFile? _mappedFile;
    private readonly MemoryMappedViewAccessor? _mappedView;
    private readonly int _headerSize;
    private readonly byte[][]? _preloadedSprites;
    private byte[]?[]? _spriteBuffers;
    private bool _disposed;

    private SpriteArchive(
        ReadOnlyMemory<byte> memory,
        bool isMemoryBacked,
        MemoryMappedFile? mappedFile,
        MemoryMappedViewAccessor? mappedView,
        bool extendedSpriteIds,
        bool transparentPixels,
        uint signature,
        uint spriteCount,
        byte[][]? preloadedSprites)
    {
        _memory = memory;
        _isMemoryBacked = isMemoryBacked;
        _mappedFile = mappedFile;
        _mappedView = mappedView;
        UsesExtendedSpriteIds = extendedSpriteIds;
        TransparentPixels = transparentPixels;
        Signature = signature;
        SpriteCount = spriteCount;
        _headerSize = extendedSpriteIds ? 8 : 6;
        _preloadedSprites = preloadedSprites;
        if (preloadedSprites is not null)
        {
            var buffers = new byte[(int)spriteCount + 1][];
            for (var id = 1u; id <= spriteCount; id++)
                buffers[id] = preloadedSprites[(int)id - 1];
            _spriteBuffers = buffers;
        }
    }

    public uint Signature { get; }
    public uint SpriteCount { get; private set; }
    public bool UsesExtendedSpriteIds { get; }
    public bool TransparentPixels { get; }

    /// <summary>True when this instance owns a memory-mapped view (dispose to release).</summary>
    public bool IsMemoryMapped => !_isMemoryBacked;

    public static SpriteArchive Load(ReadOnlyMemory<byte> sprFile, ClientDataReadOptions options, bool preloadSprites = false) =>
        Load(sprFile, options.ResolveExtendedSpriteIds(), options.TransparentSprites, preloadSprites);

    public static SpriteArchive Load(ReadOnlyMemory<byte> sprFile, bool extendedSpriteIds, bool transparentPixels, bool preloadSprites = false)
    {
        if (sprFile.Length < 6)
            throw new InvalidDataException("SPR file too small.");

        var (sig, count) = ReadHeaderFromSpan(sprFile.Span, sprFile.Length, extendedSpriteIds);

        byte[][]? preloadedSprites = null;
        if (preloadSprites && count > 0)
        {
            preloadedSprites = new byte[count][];
            var headerSize = extendedSpriteIds ? 8 : 6;
            System.Threading.Tasks.Parallel.For(1, (int)count + 1, i =>
            {
                if (TryDecodeSpriteFromBacking(sprFile.Span, sprFile.Length, headerSize, (uint)i, transparentPixels, out var decompressed))
                {
                    if (decompressed != null)
                        preloadedSprites[i - 1] = decompressed;
                }
            });
        }

        return new SpriteArchive(sprFile, isMemoryBacked: true, null, null, extendedSpriteIds, transparentPixels, sig, count, preloadedSprites);
    }

    /// <summary>
    /// Opens the <c>.spr</c> read-only via a memory-mapped view so the whole file is not copied into one <c>byte[]</c>.
    /// Per-sprite reads still use the same lookup table + on-demand RLE decode.
    /// </summary>
    public static SpriteArchive OpenReadOnlyFile(string sprPath, ClientDataReadOptions options, bool preloadSprites = false) =>
        OpenReadOnlyFile(sprPath, options.ResolveExtendedSpriteIds(), options.TransparentSprites, preloadSprites);

    public static SpriteArchive OpenReadOnlyFile(string sprPath, bool extendedSpriteIds, bool transparentPixels, bool preloadSprites = false)
    {
        if (sprPath is null)
            throw new ArgumentNullException(nameof(sprPath));

        var mmf = MemoryMappedFile.CreateFromFile(
            sprPath,
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
            if (length < 6)
                throw new InvalidDataException("SPR file too small.");

            var sig = view.ReadUInt32(0);
            uint count;
            if (extendedSpriteIds)
            {
                if (length < 8)
                    throw new InvalidDataException("SPR file too small for extended header.");
                count = view.ReadUInt32(4);
            }
            else
                count = view.ReadUInt16(4);

            byte[][]? preloadedSprites = null;
            if (preloadSprites && count > 0)
            {
                preloadedSprites = new byte[count][];
                var headerSize = extendedSpriteIds ? 8 : 6;
                System.Threading.Tasks.Parallel.For(1, (int)count + 1, i =>
                {
                    if (!TryReadSpriteCompressedBlock(view, length, headerSize, (uint)i, out var compressedOffset, out var pixelBlockLength, out _))
                        return;

                    var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(pixelBlockLength);
                    try
                    {
                        view.ReadArray(compressedOffset, rented, 0, pixelBlockLength);
                        if (TryDecodeCompressedSprite(rented.AsSpan(0, pixelBlockLength), transparentPixels, out var decompressed))
                            preloadedSprites[i - 1] = decompressed;
                    }
                    finally
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(rented);
                    }
                });
            }

            return new SpriteArchive(
                default,
                isMemoryBacked: false,
                mmf,
                view,
                extendedSpriteIds,
                transparentPixels,
                sig,
                count,
                preloadedSprites);
        }
        catch
        {
            view.Dispose();
            mmf.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Looks up <paramref name="spriteId"/> in the offset table, reads only that sprite’s blob, and decodes to 32×32 RGBA.
    /// Does not decode other sprites.
    /// </summary>
    public bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination) =>
        TryCopySpriteRgba(spriteId, rgbaDestination);

    public bool TryCopySpriteRgba(uint spriteId, Span<byte> rgbaDestination)
    {
        ThrowIfDisposed();
        if (spriteId == 0 || spriteId > SpriteCount)
            return false;
        if (rgbaDestination.Length < SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException("Buffer must be at least 4096 bytes.", nameof(rgbaDestination));

        if (_spriteBuffers != null)
        {
            if (spriteId >= (uint)_spriteBuffers.Length)
            {
                rgbaDestination.Clear();
                return true;
            }

            var buffer = _spriteBuffers[spriteId];
            if (buffer == null)
            {
                rgbaDestination.Clear();
                return true;
            }

            buffer.AsSpan().CopyTo(rgbaDestination);
            return true;
        }

        if (_preloadedSprites != null)
        {
            var preloaded = _preloadedSprites[spriteId - 1];
            if (preloaded == null)
            {
                rgbaDestination.Clear();
                return true;
            }
            preloaded.AsSpan().CopyTo(rgbaDestination);
            return true;
        }

        var fileLength = GetFileLength();
        long compressedOffset;
        int pixelBlockLength;
        bool isEmpty;

        if (_isMemoryBacked)
        {
            if (!TryReadSpriteCompressedBlock(_memory.Span, fileLength, _headerSize, spriteId, out compressedOffset, out pixelBlockLength, out isEmpty))
                return false;

            if (isEmpty)
            {
                rgbaDestination.Clear();
                return true;
            }

            try
            {
                SpritePixelCodec.UncompressToRgba(_memory.Span.Slice((int)compressedOffset, pixelBlockLength), TransparentPixels, rgbaDestination);
            }
            catch (InvalidDataException)
            {
                return false;
            }

            return true;
        }

        if (!TryReadSpriteCompressedBlock(_mappedView!, fileLength, _headerSize, spriteId, out compressedOffset, out pixelBlockLength, out isEmpty))
            return false;

        if (isEmpty)
        {
            rgbaDestination.Clear();
            return true;
        }

        try
        {
            var rented = System.Buffers.ArrayPool<byte>.Shared.Rent(pixelBlockLength);
            try
            {
                _mappedView!.ReadArray(compressedOffset, rented, 0, pixelBlockLength);
                SpritePixelCodec.UncompressToRgba(rented.AsSpan(0, pixelBlockLength), TransparentPixels, rgbaDestination);
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
            }
        }
        catch (InvalidDataException)
        {
            return false;
        }

        return true;
    }

    public void PutSprite(uint spriteId, byte[] rgba)
    {
        ArgumentNullException.ThrowIfNull(rgba);
        if (spriteId == 0)
            throw new ArgumentOutOfRangeException(nameof(spriteId));
        if (rgba.Length != SpritePixelCodec.RgbaBufferLength)
            throw new ArgumentException($"Sprite buffer must be {SpritePixelCodec.RgbaBufferLength} bytes.", nameof(rgba));

        EnsureMutableSpriteTable();
        if (spriteId >= (uint)_spriteBuffers!.Length)
        {
            var expanded = new byte[]?[(int)spriteId + 1];
            Array.Copy(_spriteBuffers, expanded, _spriteBuffers.Length);
            _spriteBuffers = expanded;
        }

        var copy = new byte[SpritePixelCodec.RgbaBufferLength];
        rgba.AsSpan().CopyTo(copy);
        _spriteBuffers![spriteId] = copy;
        if (spriteId > SpriteCount)
            SpriteCount = spriteId;
    }

    public bool RemoveSprite(uint spriteId)
    {
        if (spriteId == 0)
            return false;

        EnsureMutableSpriteTable();
        if (spriteId >= (uint)_spriteBuffers!.Length)
            return false;

        var existed = _spriteBuffers[spriteId] != null;
        _spriteBuffers[spriteId] = null;
        if (existed && spriteId == SpriteCount)
        {
            while (SpriteCount > 0 && (SpriteCount >= (uint)_spriteBuffers.Length || _spriteBuffers[SpriteCount] == null))
                SpriteCount--;
        }

        return existed;
    }

    public void WriteToStream(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        EnsureMutableSpriteTable();

        var rgbaPerSpriteIdOneBased = new byte[]?[(int)SpriteCount + 1];
        for (var id = 1u; id <= SpriteCount; id++)
            rgbaPerSpriteIdOneBased[id] = _spriteBuffers![id];

        SpriteSheetCompiler.WriteToStream(output, Signature, UsesExtendedSpriteIds, TransparentPixels, rgbaPerSpriteIdOneBased);
    }

    private void EnsureMutableSpriteTable()
    {
        if (_spriteBuffers != null)
            return;

        var buffers = new byte[]?[(int)SpriteCount + 1];
        for (var id = 1u; id <= SpriteCount; id++)
        {
            var rgba = new byte[SpritePixelCodec.RgbaBufferLength];
            if (TryCopySpriteRgba(id, rgba))
                buffers[id] = IsEmptySprite(id) ? null : rgba;
        }

        _spriteBuffers = buffers;
    }

    private static bool TryDecodeSpriteFromBacking(
        ReadOnlySpan<byte> span,
        long fileLength,
        int headerSize,
        uint spriteId,
        bool transparentPixels,
        out byte[]? decompressed)
    {
        decompressed = null;
        if (!TryReadSpriteCompressedBlock(span, fileLength, headerSize, spriteId, out var compressedOffset, out var pixelBlockLength, out var isEmpty))
            return false;

        if (isEmpty)
            return true;

        decompressed = new byte[SpritePixelCodec.RgbaBufferLength];
        try
        {
            SpritePixelCodec.UncompressToRgba(span.Slice((int)compressedOffset, pixelBlockLength), transparentPixels, decompressed);
            return true;
        }
        catch (InvalidDataException)
        {
            decompressed = null;
            return false;
        }
    }

    private static bool TryDecodeCompressedSprite(ReadOnlySpan<byte> compressed, bool transparentPixels, out byte[] decompressed)
    {
        decompressed = new byte[SpritePixelCodec.RgbaBufferLength];
        try
        {
            SpritePixelCodec.UncompressToRgba(compressed, transparentPixels, decompressed);
            return true;
        }
        catch (InvalidDataException)
        {
            decompressed = Array.Empty<byte>();
            return false;
        }
    }

    private static bool TryReadSpriteCompressedBlock(
        ReadOnlySpan<byte> span,
        long fileLength,
        int headerSize,
        uint spriteId,
        out long compressedOffset,
        out int pixelBlockLength,
        out bool isEmpty)
    {
        compressedOffset = 0;
        pixelBlockLength = 0;
        isEmpty = false;

        var addrOffset = (long)headerSize + (spriteId - 1) * 4L;
        if (addrOffset + 4 > fileLength)
            return false;

        var address = BinaryLittleEndian.ReadUInt32(span, (int)addrOffset);
        if (address == 0 || address >= (ulong)fileLength)
            return false;

        var p = (long)address + 3;
        if (p + 2 > fileLength)
            return false;

        var blockLength = BinaryLittleEndian.ReadUInt16(span, (int)p);
        if (blockLength == 0)
        {
            isEmpty = true;
            return true;
        }

        p += 2;
        if (p + blockLength > fileLength)
            return false;

        compressedOffset = p;
        pixelBlockLength = blockLength;
        return true;
    }

    private static bool TryReadSpriteCompressedBlock(
        MemoryMappedViewAccessor view,
        long fileLength,
        int headerSize,
        uint spriteId,
        out long compressedOffset,
        out int pixelBlockLength,
        out bool isEmpty)
    {
        compressedOffset = 0;
        pixelBlockLength = 0;
        isEmpty = false;

        var addrOffset = (long)headerSize + (spriteId - 1) * 4L;
        if (addrOffset + 4 > fileLength)
            return false;

        var address = view.ReadUInt32(addrOffset);
        if (address == 0 || address >= (ulong)fileLength)
            return false;

        var p = (long)address + 3;
        if (p + 2 > fileLength)
            return false;

        var blockLength = view.ReadUInt16(p);
        if (blockLength == 0)
        {
            isEmpty = true;
            return true;
        }

        p += 2;
        if (p + blockLength > fileLength)
            return false;

        compressedOffset = p;
        pixelBlockLength = blockLength;
        return true;
    }

    /// <summary>1-based id; allocates <c>byte[4096]</c> RGBA.</summary>
    public byte[] DecodeSpriteById(uint spriteId)
    {
        var buf = new byte[SpritePixelCodec.RgbaBufferLength];
        if (!TryCopySpriteRgba(spriteId, buf))
            throw new InvalidDataException($"Sprite {spriteId} is missing or invalid.");
        return buf;
    }

    /// <inheritdoc cref="DecodeSpriteById"/>
    public byte[] GetSpriteRgbaPixels(uint spriteId) => DecodeSpriteById(spriteId);

    /// <summary>1-based sprite index; empty slots return true.</summary>
    public bool IsEmptySprite(uint spriteId)
    {
        ThrowIfDisposed();
        if (spriteId == 0 || spriteId > SpriteCount)
            return true;

        if (_spriteBuffers != null)
        {
            if (spriteId >= (uint)_spriteBuffers.Length)
                return true;
            return _spriteBuffers[spriteId] == null;
        }

        if (_preloadedSprites != null)
        {
            return _preloadedSprites[spriteId - 1] == null;
        }

        var fileLength = GetFileLength();
        bool isEmpty;
        if (_isMemoryBacked)
        {
            return !TryReadSpriteCompressedBlock(_memory.Span, fileLength, _headerSize, spriteId, out _, out _, out isEmpty)
                || isEmpty;
        }

        return !TryReadSpriteCompressedBlock(_mappedView!, fileLength, _headerSize, spriteId, out _, out _, out isEmpty)
            || isEmpty;
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
            throw new ObjectDisposedException(nameof(SpriteArchive));
    }

    private long GetFileLength() =>
        _isMemoryBacked ? _memory.Length : _mappedView!.Capacity;

    private static (uint signature, uint count) ReadHeaderFromSpan(ReadOnlySpan<byte> span, long fileLength, bool extendedSpriteIds)
    {
        if (fileLength < 6)
            throw new InvalidDataException("SPR file too small.");
        var sig = BinaryLittleEndian.ReadUInt32(span, 0);
        uint count;
        if (extendedSpriteIds)
        {
            if (fileLength < 8)
                throw new InvalidDataException("SPR file too small for extended header.");
            count = BinaryLittleEndian.ReadUInt32(span, 4);
        }
        else
            count = BinaryLittleEndian.ReadUInt16(span, 4);

        return (sig, count);
    }
}
