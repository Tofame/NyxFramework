using System.IO;

namespace NyxAssets.Sprites;

/// <summary>
/// Format-agnostic contract for random-access sprite decoding.
/// Implementations may back onto a <c>.spr</c> file, a PNG atlas, a JSON manifest, or any other storage.
/// </summary>
public interface ISpriteSource : IDisposable
{
    uint SpriteCount { get; }

    bool TryDecodeSpriteById(uint spriteId, Span<byte> rgbaDestination);

    byte[] DecodeSpriteById(uint spriteId);

    bool IsEmptySprite(uint spriteId);

    /// <summary>Stores or replaces a sprite entry by 1-based id.</summary>
    void PutSprite(uint spriteId, byte[] rgba);

    /// <summary>Removes a sprite entry by 1-based id when present.</summary>
    bool RemoveSprite(uint spriteId);

    /// <summary>Writes the current sprite set to a stream.</summary>
    void WriteToStream(Stream output);
}
