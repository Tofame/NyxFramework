using Silk.NET.OpenGL;

namespace NyxRender
{
    /// <summary>
    /// Grid-packed texture atlas for 32×32 sprites.
    /// Free slots are tracked as a queue for O(1) allocation and deallocation.
    ///
    /// Slots are pushed onto the free stack in reverse row-major order (bottom-right
    /// first) so the <c>Pop</c>-based allocation fills top-left to bottom-right.
    /// This ensures sprites loaded together are placed adjacently, improving cache
    /// locality for typical draw orders.
    ///
    /// The atlas texture is initially blank (zeroed RGBA) and filled incrementally
    /// via <see cref="Texture.UploadSubImage"/> as sprites are added.
    /// </summary>
    internal sealed class SpriteAtlas : IDisposable
    {
        private readonly int _atlasSize;
        private readonly int _spritesPerRow;
        private readonly Stack<Point> _freeSlots;
        private Texture _texture;
        private int _usedSlots;

        public int Id { get; }
        public int Capacity => _spritesPerRow * _spritesPerRow;
        public int UsedSlots => _usedSlots;
        public bool IsFull => _freeSlots.Count == 0;

        public SpriteAtlas(GL gl, int id, int atlasSize)
        {
            Id = id;
            _atlasSize = atlasSize;
            _spritesPerRow = atlasSize / Sprite.Size;

            var cap = _spritesPerRow * _spritesPerRow;
            _freeSlots = new Stack<Point>(cap);

            // Push slots in reverse row-major so Pop yields top-left first.
            for (var y = _spritesPerRow - 1; y >= 0; y--)
                for (var x = _spritesPerRow - 1; x >= 0; x--)
                    _freeSlots.Push(new Point(x, y));

            _usedSlots = 0;

            // Allocate uninitialized memory then clear to avoid zero-init overhead.
            var blank = GC.AllocateUninitializedArray<byte>(atlasSize * atlasSize * 4);
            blank.AsSpan().Clear();
            _texture = new Texture(gl, atlasSize, atlasSize, blank);
        }

        public Point AddSprite(ReadOnlySpan<byte> rgbaData)
        {
            if (rgbaData.Length != Sprite.Size * Sprite.Size * 4)
                throw new ArgumentException("Invalid sprite data size");

            if (_freeSlots.Count == 0)
                throw new InvalidOperationException("Atlas is full");

            var slot = _freeSlots.Pop();
            _usedSlots++;
            _texture.UploadSubImage(slot.X * Sprite.Size, slot.Y * Sprite.Size, Sprite.Size, Sprite.Size, rgbaData);
            return slot;
        }

        /// <summary>
        /// Returns a grid slot to the free pool.
        /// Uses a combined uint cast to check both coordinates against the row count
        /// in a single branch: <c>(uint)x &lt; (uint)_spritesPerRow</c> is false if x is negative.
        /// </summary>
        public void FreeSlot(Point gridPosition)
        {
            var x = gridPosition.X;
            var y = gridPosition.Y;
            if ((uint)x >= (uint)_spritesPerRow || (uint)y >= (uint)_spritesPerRow)
                return;

            _freeSlots.Push(gridPosition);
            _usedSlots--;
        }

        /// <summary>Binds the atlas texture to the currently active unit.</summary>
        public void Bind() => _texture.BindFast();

        public void Dispose() => _texture?.Dispose();
    }
}
