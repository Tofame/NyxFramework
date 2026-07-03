namespace NyxGui;

/// <summary>
/// Multi-line text entry with vertical scroll; host routes keys via <see cref="NyxGuiRootStack.ProcessKeyboard"/>.
///
/// <b>Caret movement</b> uses two parallel code paths depending on <see cref="Wrap"/>:
/// when wrapping is enabled, display-line mapping goes through
/// <see cref="NyxTextLayout.TryMapCaretToDisplayLine"/> and
/// <see cref="NyxTextLayout.BuildLines"/> to compute visual line breaks;
/// when wrapping is disabled, lines are raw <c>\n</c>-delimited splits.
///
/// <b>Scroll sync</b> is maintained by <see cref="SyncScrollBar"/> which clamps
/// <c>_scrollOffsetY</c> to the content extent.  <see cref="EnsureCaretVisible"/> adjusts
/// scroll so the caret line remains visible.
///
/// <b>Caret blink:</b> the caret is painted when <c>Environment.TickCount64 % 1000 &lt; 500</c>
/// (visible 500ms, hidden 500ms), only when focused and non-readonly.
///
/// <b>CharWidth</b> is a hardcoded 7-pixel approximation for both caret X estimation and
/// text layout.  This assumes monospace; proportional fonts will have imprecise caret positioning.
/// </summary>
public sealed class NyxTextArea : NyxElement, INyxTextEntry
{
    private const int DefaultLineHeight = 14;
    private const int CharWidth = 7;
    private const int DefaultScrollBarWidth = 10;

    private readonly NyxVScrollBar _bar;
    private string _text = string.Empty;
    private int _caret;
    private int _scrollOffsetY;
    private bool _suppressClickFromDrag;
	private int _selectionStart = -1;
	private bool _isSelecting;

    public NyxTextArea(NyxRect bounds, string text = "", uint internalId = 0)
        : base(internalId)
    {
        Focusable = true;
        _text = text ?? string.Empty;
        _caret = _text.Length;
        _bar = new NyxVScrollBar(NyxRect.Empty, NextBarId(internalId));
        _bar.ValueChanged += (_, _) => _scrollOffsetY = _bar.Value;
    }

    // Generate a unique internal id for the scrollbar so it doesn't collide with the text area's own id.
    private static uint NextBarId(uint id) => id == 0 ? 9_200_000u : id + 9_200_000u;

    public string Text
    {
        get => _text;
        set
        {
            var v = value ?? string.Empty;
            if (MaxLength > 0 && v.Length > MaxLength)
                v = v[..MaxLength];
            if (v == _text)
                return;
            _text = v;
            _caret = Math.Clamp(_caret, 0, _text.Length);
            EnsureCaretVisible();
            SyncScrollBar();
            Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
        }
    }

    public int MaxLength { get; set; }

    public bool ReadOnly { get; set; }

    public int LineHeight { get; set; } = DefaultLineHeight;

    /// <summary>When true, wraps long lines to <see cref="TextRect"/> width (logical <c>\n</c> still apply).</summary>
    public bool Wrap { get; set; }

    public NyxTextAlign Align { get; set; } = NyxTextAlign.TopLeft;

    /// <summary>Width reserved for the vertical scrollbar gutter (0 hides the bar).</summary>
    public int ScrollBarWidth { get; set; } = DefaultScrollBarWidth;

    public int ScrollOffsetY
    {
        get => _scrollOffsetY;
        set
        {
            _scrollOffsetY = Math.Max(0, value);
            SyncScrollBar();
        }
    }

    public event EventHandler<NyxTextChangedEventArgs>? Changed;

    private NyxRect ChromeRect => Bounds.Inset(4, 4, 4, 4);

    private int EffectiveScrollBarWidth => ScrollBarWidth > 0 ? ScrollBarWidth : 0;

    private NyxRect TextRect
    {
        get
        {
            var inner = ChromeRect;
            var barW = EffectiveScrollBarWidth;
            if (barW <= 0)
                return inner;
            return new NyxRect(inner.X, inner.Y, Math.Max(0, inner.Width - barW), inner.Height);
        }
    }

    private NyxRect BarRect
    {
        get
        {
            var inner = ChromeRect;
            var barW = EffectiveScrollBarWidth;
            return new NyxRect(inner.Right - barW, inner.Y, barW, inner.Height);
        }
    }

    public override void SetBounds(NyxRect bounds)
    {
        base.SetBounds(bounds);
        SyncScrollBar();
    }

    public override void OnMouseMove(int x, int y)
    {
        base.OnMouseMove(x, y);

        if (EffectiveScrollBarWidth > 0)
            _bar.OnMouseMove(x, y);

		if (_isSelecting && !ReadOnly)
		{
			_caret = EstimateCaretFromPoint(x, y);
			EnsureCaretVisible();
			InvalidateRender();
		}
    }

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        _suppressClickFromDrag = false;
        if (!HitTest(x, y))
            return;

        if (EffectiveScrollBarWidth > 0 && _bar.HitTest(x, y) && ScrollExtent() > 0)
        {
            _bar.OnMouseDown(x, y, NyxMouseButton.Left);
            return;
        }

        if (ReadOnly)
            return;

		if (button == NyxMouseButton.Left)
		{
			PointerPressed = true;
			_isSelecting = true;
			_selectionStart = EstimateCaretFromPoint(x, y);
			_caret = _selectionStart;
			EnsureCaretVisible();
		}
    }

    public override void OnMouseUp(int x, int y, NyxMouseButton button)
    {
        if (EffectiveScrollBarWidth > 0)
            _bar.OnMouseUp(x, y, NyxMouseButton.Left);

		if (button == NyxMouseButton.Left)
		{
			if (_isSelecting && HitTest(x, y) && !_suppressClickFromDrag)
			{
				_caret = EstimateCaretFromPoint(x, y);
				if (_selectionStart == _caret)
					_selectionStart = -1;
				EnsureCaretVisible();
			}

			_isSelecting = false;
			PointerPressed = false;
		}
    }

    public override void OnMouseWheel(int x, int y, int delta)
    {
        if (!HitTest(x, y))
            return;

        if (EffectiveScrollBarWidth > 0 && BarRect.Contains(x, y) && ScrollExtent() > 0)
        {
            _bar.OnMouseWheel(x, y, delta);
            return;
        }

        ApplyWheelScroll(delta);
    }

    private void ApplyWheelScroll(int delta)
    {
        var step = LineHeight;
        if (delta > 0)
            _scrollOffsetY = Math.Max(0, _scrollOffsetY - step);
        else if (delta < 0)
            _scrollOffsetY = Math.Min(ScrollExtent(), _scrollOffsetY + step);
        SyncScrollBar();
    }

    public void HandleKey(NyxGuiKey key, char? character = null)
    {
        if (ReadOnly)
            return;

		if (character.HasValue)
		{
			var c = character.Value;
			if (c == '\u0001') // Ctrl+A
			{
				_selectionStart = 0;
				_caret = _text.Length;
				EnsureCaretVisible();
				return;
			}
			if (c == '\u0003') // Ctrl+C
			{
				if (_selectionStart != -1 && _selectionStart != _caret)
				{
					var start = Math.Min(_selectionStart, _caret);
					var end = Math.Max(_selectionStart, _caret);
					NyxClipboard.SetText(_text.Substring(start, end - start));
				}
				return;
			}
			if (c == '\u0016') // Ctrl+V
			{
				var paste = NyxClipboard.GetText();
				if (!string.IsNullOrEmpty(paste))
				{
					DeleteSelection(out var pos);
					_text = _text.Insert(pos, paste);
					_caret = pos + paste.Length;
					NotifyChanged();
				}
				return;
			}
		}

        switch (key)
        {
            case NyxGuiKey.Backspace:
				if (DeleteSelection(out _))
				{
					NotifyChanged();
				}
				else if (_caret > 0)
				{
					_text = _text.Remove(_caret - 1, 1);
					_caret--;
					NotifyChanged();
				}
                break;
            case NyxGuiKey.Delete:
				if (DeleteSelection(out _))
				{
					NotifyChanged();
				}
				else if (_caret < _text.Length)
				{
					_text = _text.Remove(_caret, 1);
					NotifyChanged();
				}
                break;
            case NyxGuiKey.Left:
				_selectionStart = -1;
                _caret = Math.Max(0, _caret - 1);
                EnsureCaretVisible();
                break;
            case NyxGuiKey.Right:
				_selectionStart = -1;
                _caret = Math.Min(_text.Length, _caret + 1);
                EnsureCaretVisible();
                break;
            case NyxGuiKey.Up:
				_selectionStart = -1;
                MoveCaretVertical(-1);
                break;
            case NyxGuiKey.Down:
				_selectionStart = -1;
                MoveCaretVertical(1);
                break;
            case NyxGuiKey.Home:
				_selectionStart = -1;
                _caret = LineStartIndex(_caret);
                EnsureCaretVisible();
                break;
            case NyxGuiKey.End:
				_selectionStart = -1;
                _caret = LineEndIndex(_caret);
                EnsureCaretVisible();
                break;
            case NyxGuiKey.Enter:
                InsertCharacter('\n');
                break;
            case NyxGuiKey.Tab:
                InsertCharacter('\t');
                break;
            default:
                if (character is { } c && !char.IsControl(c))
                    InsertCharacter(c);
                break;
        }
    }

    private void InsertCharacter(char c)
    {
		DeleteSelection(out _);
        if (MaxLength > 0 && _text.Length >= MaxLength)
            return;
        _text = _text.Insert(_caret, c.ToString());
        _caret++;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        EnsureCaretVisible();
        SyncScrollBar();
        Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
    }

    /// <summary>
    /// Moves caret up/down by one display line.  Maintains approximate column position
    /// when crossing lines of different lengths.  Has separate wrapping vs non-wrapping paths.
    /// </summary>
    private void MoveCaretVertical(int direction)
    {
        var text = TextRect;
        if (Wrap)
        {
            NyxTextLayout.TryMapCaretToDisplayLine(_text, Wrap, text.Width, _caret, CharWidth, out var line, out var col);
            var lines = DisplayLines();
            var targetLine = Math.Clamp(line + direction, 0, Math.Max(0, lines.Count - 1));
            var wrappedCol = targetLine < lines.Count ? Math.Min(col, lines[targetLine].Length) : 0;
            _caret = NyxTextLayout.MapDisplayPositionToIndex(_text, Wrap, text.Width, targetLine, wrappedCol, CharWidth);
            EnsureCaretVisible();
            return;
        }

        var (logicalLine, logicalCol) = GetLineColumn(_caret);
        var logicalLines = SplitLines();
        var targetLogicalLine = Math.Clamp(logicalLine + direction, 0, Math.Max(0, logicalLines.Count - 1));
        var targetCol = Math.Min(logicalCol, logicalLines[targetLogicalLine].Length);
        _caret = IndexFromLineColumn(targetLogicalLine, targetCol);
        EnsureCaretVisible();
    }

    /// <summary>
    /// Converts a screen-space click to a caret index.
    /// Y → display line via <c>(y - text.Y + scrollOffset) / LineHeight</c>.
    /// X → column via <c>x / CharWidth</c> (assumes monospace character width).
    /// </summary>
    private int EstimateCaretFromPoint(int x, int y)
    {
        var text = TextRect;
        var lines = DisplayLines();
        var relY = y - text.Y + _scrollOffsetY;
        var displayLine = Math.Clamp(relY / Math.Max(1, LineHeight), 0, Math.Max(0, lines.Count - 1));
        var relX = Math.Clamp(x - text.X, 0, Math.Max(0, text.Width));
        var col = relX / CharWidth;
        if (displayLine < lines.Count)
            col = Math.Min(col, lines[displayLine].Length);

        return NyxTextLayout.MapDisplayPositionToIndex(_text, Wrap, text.Width, displayLine, col, CharWidth);
    }

    /// <summary>
    /// Adjusts the scroll offset so the caret line is visible.  Scrolls up if the
    /// caret line is above the visible region, down if below.
    /// </summary>
    private void EnsureCaretVisible()
    {
        var text = TextRect;
        NyxTextLayout.TryMapCaretToDisplayLine(_text, Wrap, text.Width, _caret, CharWidth, out var line, out _);
        var caretY = line * LineHeight;
        if (caretY < _scrollOffsetY)
            _scrollOffsetY = caretY;
        else if (caretY + LineHeight > _scrollOffsetY + text.Height)
            _scrollOffsetY = Math.Max(0, caretY + LineHeight - text.Height);
        SyncScrollBar();
    }

    private int ContentHeight() => Math.Max(LineHeight, DisplayLines().Count * LineHeight);

    /// <summary>
    /// Returns display lines.  If wrapping is enabled, uses word-wrap line splitting;
    /// otherwise returns raw <c>\n</c>-delimited lines.
    /// </summary>
    private List<string> DisplayLines()
    {
        if (!Wrap)
            return SplitLines();

        return NyxTextLayout.BuildLines(_text, true, TextRect.Width, CharWidth);
    }

    private int ScrollExtent() => Math.Max(0, ContentHeight() - TextRect.Height);

    private void SyncScrollBar()
    {
        if (EffectiveScrollBarWidth <= 0)
            return;

        var extent = ScrollExtent();
        _scrollOffsetY = Math.Clamp(_scrollOffsetY, 0, extent);
        _bar.SetBounds(BarRect);
        _bar.Configure(extent, _scrollOffsetY, TextRect.Height);
        _bar.Enabled = extent > 0;
    }

    private List<string> SplitLines()
    {
        if (_text.Length == 0)
            return [string.Empty];

        var lines = new List<string>();
        var start = 0;
        for (var i = 0; i < _text.Length; i++)
        {
            if (_text[i] != '\n')
                continue;
            lines.Add(_text[start..i]);
            start = i + 1;
        }

        lines.Add(_text[start..]);
        return lines;
    }

    private (int Line, int Column) GetLineColumn(int index)
    {
        var line = 0;
        var col = 0;
        for (var i = 0; i < index && i < _text.Length; i++)
        {
            if (_text[i] == '\n')
            {
                line++;
                col = 0;
            }
            else
                col++;
        }

        return (line, col);
    }

    private int IndexFromLineColumn(int line, int column)
    {
        var lines = SplitLines();
        if (lines.Count == 0)
            return 0;

        line = Math.Clamp(line, 0, lines.Count - 1);
        column = Math.Clamp(column, 0, lines[line].Length);
        var index = 0;
        for (var i = 0; i < line; i++)
            index += lines[i].Length + 1;
        return Math.Min(_text.Length, index + column);
    }

    private int LineStartIndex(int index)
    {
        var i = Math.Clamp(index, 0, _text.Length);
        while (i > 0 && _text[i - 1] != '\n')
            i--;
        return i;
    }

    private int LineEndIndex(int index)
    {
        var i = Math.Clamp(index, 0, _text.Length);
        while (i < _text.Length && _text[i] != '\n')
            i++;
        return i;
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!TryBeginPaintVisual(out var visual))
            return;

        try
        {
            var border = IsFocused ? theme.InputBorderFocused : theme.InputBorder;
            painter.FillRect(Bounds, theme.InputBackground);
            painter.DrawRect(Bounds, Tint(border, visual), 1);
            PaintChrome(painter, visual);

            SyncScrollBar();
            var text = TextRect;
            painter.PushClip(text);

            var lines = DisplayLines();
            var y = text.Y - _scrollOffsetY;
            var color = Tint(theme.TextPrimary, visual);
            var lineAlign = Align == NyxTextAlign.Center ? NyxTextAlign.TopCenter : Align;

			if (_selectionStart != -1 && _selectionStart != _caret)
			{
				var start = Math.Min(_selectionStart, _caret);
				var end = Math.Max(_selectionStart, _caret);

				NyxTextLayout.TryMapCaretToDisplayLine(_text, Wrap, text.Width, start, CharWidth, out var startLine, out var startCol);
				NyxTextLayout.TryMapCaretToDisplayLine(_text, Wrap, text.Width, end, CharWidth, out var endLine, out var endCol);

				startLine = Math.Clamp(startLine, 0, lines.Count - 1);
				endLine = Math.Clamp(endLine, 0, lines.Count - 1);

				for (var i = startLine; i <= endLine; i++)
				{
					var lineY = text.Y + i * LineHeight - _scrollOffsetY;
					if (lineY + LineHeight < text.Y || lineY > text.Bottom)
						continue;

					var lineText = lines[i];
					var sCol = (i == startLine) ? Math.Clamp(startCol, 0, lineText.Length) : 0;
					var eCol = (i == endLine) ? Math.Clamp(endCol, 0, lineText.Length) : lineText.Length;

					if (eCol > sCol)
					{
						var prefixText = lineText.AsSpan(0, sCol);
						var fontStyle = GetPaintFont();
						painter.MeasureText(prefixText, fontStyle, out var selX0, out _);
						painter.MeasureText(lineText.AsSpan(0, eCol), fontStyle, out var selX1, out _);

						var drawX0 = text.X + selX0;
						var drawX1 = text.X + selX1;

						if (lineAlign == NyxTextAlign.TopCenter)
						{
							painter.MeasureText(lineText, fontStyle, out var lineW, out _);
							var centerOffset = Math.Max(0, (text.Width - lineW) / 2);
							drawX0 += centerOffset;
							drawX1 += centerOffset;
						}

						var x0 = Math.Clamp(drawX0, text.X, text.Right);
						var x1 = Math.Clamp(drawX1, text.X, text.Right);

						if (x1 > x0)
						{
							painter.FillRect(
								new NyxRect(x0, lineY, x1 - x0, LineHeight),
								new NyxColor(51, 153, 255, 100));
						}
					}
				}
			}

            foreach (var line in lines)
            {
                if (y + LineHeight >= text.Y && y <= text.Bottom)
                {
                    var lineRect = new NyxRect(text.X, y, text.Width, LineHeight);
                    painter.DrawText(lineRect, line, lineAlign, color, GetPaintFont());
                }

                y += LineHeight;
                if (y > text.Bottom + LineHeight)
                    break;
            }

            if (!ReadOnly && IsFocused && (Environment.TickCount64 % 1000) < 500)
                PaintCaret(painter, text, lines, lineAlign, visual, theme);

            painter.PopClip();

            if (EffectiveScrollBarWidth > 0 && ScrollExtent() > 0)
                _bar.Paint(painter, theme);
        }
        finally
        {
            EndPaintVisual();
        }
    }

    private void PaintCaret(
        INyxGuiPainter painter,
        NyxRect text,
        IReadOnlyList<string> lines,
        NyxTextAlign lineAlign,
        in NyxWidgetVisual visual,
        NyxGuiTheme theme)
    {
        if (!NyxTextLayout.TryMapCaretToDisplayLine(_text, Wrap, text.Width, _caret, CharWidth, out var lineIdx, out var col))
            return;

        lineIdx = Math.Clamp(lineIdx, 0, Math.Max(0, lines.Count - 1));
        var line = lines[lineIdx];
        col = Math.Clamp(col, 0, line.Length);

        var caretY = text.Y + lineIdx * LineHeight - _scrollOffsetY;
        if (caretY < text.Y - LineHeight || caretY > text.Bottom)
            return;

        var lineRect = new NyxRect(text.X, caretY, text.Width, LineHeight);
        var prefix = line.AsSpan(0, col);
        painter.MeasureText(prefix, GetPaintFont(), out var prefixW, out _);
        var caretX = lineRect.X + prefixW;

        if (lineAlign == NyxTextAlign.TopCenter)
        {
            painter.MeasureText(line, GetPaintFont(), out var lineW, out _);
            caretX = lineRect.X + Math.Max(0, (lineRect.Width - lineW) / 2) + prefixW;
        }

        painter.FillRect(
            new NyxRect(caretX, caretY + 2, 1, Math.Max(4, LineHeight - 4)),
            Tint(theme.Caret, visual));
    }

	private bool DeleteSelection(out int insertPos)
	{
		insertPos = _caret;
		if (_selectionStart == -1 || _selectionStart == _caret)
		{
			return false;
		}
		var start = Math.Min(_selectionStart, _caret);
		var end = Math.Max(_selectionStart, _caret);
		_text = _text.Remove(start, end - start);
		_caret = start;
		_selectionStart = -1;
		insertPos = _caret;
		return true;
	}
}
