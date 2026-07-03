namespace NyxGui;

/// <summary>Multi-segment label with per-run colour and underline (see <see cref="NyxFormattedText"/>).</summary>
public sealed class NyxExtendedLabel : NyxElement
{
    private IReadOnlyList<NyxTextRun> _runs = Array.Empty<NyxTextRun>();

    public NyxExtendedLabel(NyxRect bounds, uint internalId = 0)
        : base(internalId)
    {
    }

    public NyxExtendedLabel(string? id = null) : base(0) { Id = id; }

    public NyxTextAlign Align { get; set; } = NyxTextAlign.TopLeft;

    /// <summary>When true, wraps combined run text (single colour per line).</summary>
    public bool Wrap { get; set; }

    public int LineHeight { get; set; } = 14;

	public NyxColor DefaultColor { get; set; } = NyxColor.FromRgb(220, 220, 225);

	public string Text
	{
		get => string.Concat(_runs.Select(r => r.Text));
		set => SetMarkup(value, DefaultColor);
	}

	public void SetMarkup(string markup, NyxColor defaultColor)
	{
		_runs = NyxFormattedText.Parse(markup, defaultColor);
		InvalidateLayout();
		InvalidateRender();
	}

	public void SetRuns(IReadOnlyList<NyxTextRun> runs)
	{
		_runs = runs;
		InvalidateLayout();
		InvalidateRender();
	}

	public int GetCalculatedHeight(int width)
	{
		if (_runs.Count == 0)
			return 0;

		const int charWidth = NyxTextLayout.DefaultCharWidth;
		var combined = string.Concat(_runs.Select(r => r.Text));
		var ranges = NyxTextLayout.BuildLineRanges(combined, Wrap, width, charWidth);
		return ranges.Count * LineHeight;
	}

	public override void SetBounds(NyxRect bounds)
	{
		if (Wrap && bounds.Width > 0)
		{
			var calculatedHeight = GetCalculatedHeight(bounds.Width);
			if (calculatedHeight != bounds.Height)
			{
				bounds = new NyxRect(bounds.X, bounds.Y, bounds.Width, calculatedHeight);
			}
		}
		base.SetBounds(bounds);
	}

	public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
	{
		if (!TryBeginPaintVisual(out var visual))
			return;

		try
		{
			PaintChrome(painter, visual);
			if (_runs.Count == 0)
				return;

			const int charWidth = NyxTextLayout.DefaultCharWidth;
			var combined = string.Concat(_runs.Select(r => r.Text));
			var ranges = NyxTextLayout.BuildLineRanges(combined, Wrap, Bounds.Width, charWidth);

			var blockH = ranges.Count * LineHeight;
			var y = (Align == NyxTextAlign.Center)
				? Bounds.Y + Math.Max(0, (Bounds.Height - blockH) / 2)
				: Bounds.Y;

			foreach (var range in ranges)
			{
				if (y + LineHeight > Bounds.Bottom)
					break;

				var lineLength = range.Length;
				var lineStart = range.Start;
				var lineEnd = range.End;

				var lineText = combined.Substring(lineStart, lineLength);
				var fontStyle = GetPaintFont();

				painter.MeasureText(lineText, fontStyle, out var lineW, out _);

				var lineX = Bounds.X;
				if (Align == NyxTextAlign.TopCenter || Align == NyxTextAlign.Center)
				{
					lineX += Math.Max(0, (Bounds.Width - lineW) / 2);
				}
				else if (Align == NyxTextAlign.TopRight)
				{
					lineX += Math.Max(0, Bounds.Width - lineW);
				}

				var x = lineX;
				var runStart = 0;
				foreach (var run in _runs)
				{
					var runEnd = runStart + run.Text.Length;

					var intersectStart = Math.Max(lineStart, runStart);
					var intersectEnd = Math.Min(lineEnd, runEnd);

					if (intersectStart < intersectEnd)
					{
						var subText = run.Text.Substring(intersectStart - runStart, intersectEnd - intersectStart);
						if (!string.IsNullOrEmpty(subText))
						{
							painter.MeasureText(subText, fontStyle, out var w, out _);
							var slice = new NyxRect(x, y, w, LineHeight);
							var color = Tint(run.Color ?? theme.TextPrimary, visual);
							painter.DrawText(slice, subText, NyxTextAlign.TopLeft, color, GetPaintFont());
							if (run.Underline)
								painter.FillRect(new NyxRect(x, y + LineHeight - 2, w, 1), color);
							x += w;
						}
					}

					runStart = runEnd;
				}

				y += LineHeight;
			}
		}
		finally
		{
			EndPaintVisual();
		}
	}
}
