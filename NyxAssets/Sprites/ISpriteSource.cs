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
}
