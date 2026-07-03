using NyxGui;

namespace Sandbox.NyxGUI_Extend;

/// <summary>
/// NyxGUI widget that draws a 32×32 RGBA item icon (Nyx / <c>NyxAssets</c> sprite pixels).
/// Set <see cref="SpriteRgba"/> from <see cref="Items.ItemIconRasterizer"/> or any 4096-byte buffer.
/// </summary>
public sealed class UIItem : NyxElement
{
    public const int SpriteBytes = 32 * 32 * 4;

    private byte[]? _ownedRgba;

    public UIItem(NyxRect bounds, uint internalId = 0)
        : base(internalId)
    {
        SetBounds(bounds);
    }

    /// <summary>When false, only the slot chrome is drawn.</summary>
    public bool HasItem { get; set; }

    /// <summary>Stack count overlay (hidden when 0 or 1).</summary>
    public ushort StackCount { get; set; }

    /// <summary>Nearest-neighbor scaling (pixel art). When true, linear filter is used.</summary>
    public bool Smooth { get; set; }

    /// <summary>Optional GPU texture cache key for sprite rendering.</summary>
    public uint CacheKey { get; set; }

    /// <summary>Optional 32×32 RGBA. Does not copy; keep buffer alive while displayed.</summary>
    public ReadOnlyMemory<byte> SpriteRgba { get; set; }

    /// <summary>Copies <paramref name="rgba4096"/> into an owned buffer and displays it.</summary>
    public void SetSprite(ReadOnlySpan<byte> rgba4096)
    {
        if (rgba4096.Length != SpriteBytes)
            throw new ArgumentException($"Expected {SpriteBytes} bytes.", nameof(rgba4096));

        _ownedRgba ??= new byte[SpriteBytes];
        rgba4096.CopyTo(_ownedRgba);
        SpriteRgba = _ownedRgba;
        HasItem = true;
    }

    public void ClearItem()
    {
        HasItem = false;
        SpriteRgba = default;
        StackCount = 0;
        CacheKey = 0;
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual))
            return;

        try
        {
            if (visual.Image is not null)
                PaintBackground(painter, visual);
            else
                painter.FillRect(Bounds, Tint(theme.PanelBackground, visual));

            if (!HasItem || SpriteRgba.Length != SpriteBytes)
                return;

            var dest = FitSquare(Bounds);
            painter.DrawSprite32(dest, SpriteRgba.Span, CacheKey, smooth: Smooth);

            if (StackCount > 1)
                UIItemStackOverlay.Paint(painter, dest, StackCount, ResolveEffectiveFont());
        }
        finally
        {
            EndPaintVisual();
        }
    }

    private const int IconPixels = 32;
    private const int SlotInsetPx = 2;

    private static NyxRect FitSquare(NyxRect bounds)
    {
        var maxSide = Math.Min(bounds.Width, bounds.Height);
        var side = Math.Min(IconPixels, Math.Max(1, maxSide - SlotInsetPx * 2));
        var x = bounds.X + (bounds.Width - side) / 2;
        var y = bounds.Y + (bounds.Height - side) / 2;
        return new NyxRect(x, y, side, side);
    }
}
