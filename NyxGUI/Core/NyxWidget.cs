namespace NyxGui;

/// <summary>
/// Base class for leaf widgets (no children). Provides default Measure/Arrange
/// that use fixed or auto-sizing. Subclasses override Measure to compute desired size.
/// </summary>
public abstract class NyxWidget : NyxElement
{
    protected NyxWidget(uint internalId = 0) : base(internalId) { }

    // ── Sizing ────────────────────────────────────────────────────────

    /// <summary>Explicit fixed width. When &gt; 0, overrides auto-sizing in Measure.</summary>
    public int FixedWidth { get; set; }

    /// <summary>Explicit fixed height. When &gt; 0, overrides auto-sizing in Measure.</summary>
    public int FixedHeight { get; set; }

    /// <summary>Convenience: set both fixed dimensions at once.</summary>
    public void SetFixedSize(int width, int height)
    {
        FixedWidth = width;
        FixedHeight = height;
    }

    // ── Layout ────────────────────────────────────────────────────────

	public override void Measure(NyxSize availableSize)
	{
		var border = GetBorderWidth();
		var padding = LayoutBox?.Padding ?? NyxThickness.Zero;
		var horizontalPaddingAndBorder = padding.Left + padding.Right + 2 * border;
		var verticalPaddingAndBorder = padding.Top + padding.Bottom + 2 * border;

		var w = FixedWidth > 0
			? (BoxSizing == NyxBoxSizing.ContentBox ? FixedWidth + horizontalPaddingAndBorder : FixedWidth)
			: availableSize.Width;
		var h = FixedHeight > 0
			? (BoxSizing == NyxBoxSizing.ContentBox ? FixedHeight + verticalPaddingAndBorder : FixedHeight)
			: availableSize.Height;
		DesiredSize = new NyxSize(
			Math.Min(w, availableSize.Width),
			Math.Min(h, availableSize.Height));
	}

	public override void Arrange(NyxRect finalRect)
	{
		var border = GetBorderWidth();
		var padding = LayoutBox?.Padding ?? NyxThickness.Zero;
		var horizontalPaddingAndBorder = padding.Left + padding.Right + 2 * border;
		var verticalPaddingAndBorder = padding.Top + padding.Bottom + 2 * border;

		var w = FixedWidth > 0
			? (BoxSizing == NyxBoxSizing.ContentBox ? FixedWidth + horizontalPaddingAndBorder : FixedWidth)
			: finalRect.Width;
		var h = FixedHeight > 0
			? (BoxSizing == NyxBoxSizing.ContentBox ? FixedHeight + verticalPaddingAndBorder : FixedHeight)
			: finalRect.Height;
		SetBounds(new NyxRect(finalRect.X, finalRect.Y, w, h));
	}
}
