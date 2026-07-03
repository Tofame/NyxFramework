namespace NyxRender;

/// <summary>
/// A 32×32 sprite: stable <see cref="Id"/> for atlas caching plus optional <see cref="Rgba"/> payload
/// (<c>R,G,B,A</c> per pixel, exactly <see cref="Rgba32Length"/> bytes). When <see cref="HasPixels"/> is false,
/// <see cref="SpriteRenderer.Draw"/> expects the id to already be resident (e.g. after <see cref="SpriteRenderer.LoadSpriteRgba"/>).
/// </summary>
public readonly struct Sprite
{
    public const int Size = 32;
    public const int Rgba32Length = Size * Size * 4;

    /// <param name="rgba">Use <see cref="default"/> for “already uploaded” draws (<see cref="Resident"/>).</param>
    public Sprite(int id, ReadOnlyMemory<byte> rgba)
    {
        if (!rgba.IsEmpty && rgba.Length != Rgba32Length)
            throw new ArgumentException($"RGBA must be empty or exactly {Rgba32Length} bytes.", nameof(rgba));
        Id = id;
        Rgba = rgba;
    }

    public int Id { get; }
    public ReadOnlyMemory<byte> Rgba { get; }

    public bool HasPixels => !Rgba.IsEmpty;

    /// <summary>Draw helper: id must already exist in the renderer (preload path).</summary>
    public static Sprite Resident(int id) => new(id, default);

    /// <summary>Copies <paramref name="rgba4096"/> into a new <see cref="Sprite"/> (heap allocates once).</summary>
    public static Sprite FromRgba(int id, ReadOnlySpan<byte> rgba4096)
    {
        if (rgba4096.Length != Rgba32Length)
            throw new ArgumentException($"Expected {Rgba32Length} bytes.", nameof(rgba4096));
        var owned = new byte[Rgba32Length];
        rgba4096.CopyTo(owned);
        return new Sprite(id, owned);
    }
}
