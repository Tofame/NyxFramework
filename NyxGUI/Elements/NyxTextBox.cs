namespace NyxGui;

public sealed class NyxTextChangedEventArgs : EventArgs
{
    public NyxTextChangedEventArgs(string text) => Text = text;
    public string Text { get; }
}

/// <summary>
/// Single-line text entry. Host routes keys via <see cref="NyxGuiRootStack.ProcessKeyboard"/>.
///
/// <b>Caret positioning</b> uses <see cref="EstimateCaretFromX"/> with a hardcoded
/// <c>approxCharW=7</c> pixel character width.  Caret blink uses
/// <c>Environment.TickCount64 % 1000 &lt; 500</c> (500ms on, 500ms off).
/// </summary>
public sealed class NyxTextBox : NyxWidget, INyxTextEntry, ICapturesPointer
{
    public NyxTextBox(string? id = null) : base(0)
    {
        Id = id;
        Focusable = true;
    }

    private string _text = string.Empty;
    private int _caret;
	private int[]? _charPositions;
	private int _scrollOffsetX;
	private int _selectionStart = -1;
	private bool _isSelecting;

    public string Text
    {
        get => _text;
        set
        {
            var v = value ?? string.Empty;
            if (MaxLength > 0 && v.Length > MaxLength) v = v[..MaxLength];
            if (v == _text) return;
            _text = v;
            _caret = Math.Clamp(_caret, 0, _text.Length);
            Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
            InvalidateRender();
        }
    }

    public int MaxLength { get; set; }
    public bool ReadOnly { get; set; }
    public NyxTextAlign Align { get; set; } = NyxTextAlign.TopLeft;

    public event EventHandler<NyxTextChangedEventArgs>? Changed;
    public event EventHandler? EnterPressed;

    private NyxRect InnerRect => new(
		Bounds.X + 4 + TextOffsetX,
		Bounds.Y + 2 + TextOffsetY,
		Math.Max(0, Bounds.Width - 8),
		Math.Max(0, Bounds.Height - 4));

    public override void OnMouseDown(int x, int y, NyxMouseButton button)
    {
        base.OnMouseDown(x, y, button);
        if (!HitTest(x, y) || ReadOnly) return;
		if (button == NyxMouseButton.Left)
		{
			PointerPressed = true;
			_isSelecting = true;
			_selectionStart = EstimateCaretFromX(x);
			_caret = _selectionStart;
			InvalidateRender();
		}
    }

	public override void OnMouseUp(int x, int y, NyxMouseButton button)
	{
		base.OnMouseUp(x, y, button);
		if (button == NyxMouseButton.Left)
		{
			PointerPressed = false;
			_isSelecting = false;
			if (_selectionStart == _caret)
			{
				_selectionStart = -1;
			}
			InvalidateRender();
		}
	}

	public override void OnMouseMove(int x, int y)
	{
		base.OnMouseMove(x, y);
		if (_isSelecting && !ReadOnly)
		{
			_caret = EstimateCaretFromX(x);
			InvalidateRender();
		}
	}

    public void HandleKey(NyxGuiKey key, char? character = null)
    {
        if (ReadOnly) return;

		if (character.HasValue)
		{
			var c = character.Value;
			if (c == '\u0001') // Ctrl+A
			{
				_selectionStart = 0;
				_caret = _text.Length;
				InvalidateRender();
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
					Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
					InvalidateRender();
				}
				return;
			}
		}

        switch (key)
        {
            case NyxGuiKey.Backspace:
				if (DeleteSelection(out _))
				{
					Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
					InvalidateRender();
				}
				else if (_caret > 0)
				{
					_text = _text.Remove(_caret - 1, 1);
					_caret--;
					Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
					InvalidateRender();
				}
                break;
            case NyxGuiKey.Delete:
				if (DeleteSelection(out _))
				{
					Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
					InvalidateRender();
				}
				else if (_caret < _text.Length)
				{
					_text = _text.Remove(_caret, 1);
					Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
					InvalidateRender();
				}
                break;
            case NyxGuiKey.Left:
				_selectionStart = -1;
                _caret = Math.Max(0, _caret - 1);
                InvalidateRender();
                break;
            case NyxGuiKey.Right:
				_selectionStart = -1;
                _caret = Math.Min(_text.Length, _caret + 1);
                InvalidateRender();
                break;
            case NyxGuiKey.Home:
				_selectionStart = -1;
                _caret = 0;
                InvalidateRender();
                break;
            case NyxGuiKey.End:
				_selectionStart = -1;
                _caret = _text.Length;
                InvalidateRender();
                break;
            case NyxGuiKey.Enter:
                EnterPressed?.Invoke(this, EventArgs.Empty);
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
        if (MaxLength > 0 && _text.Length >= MaxLength) return;
        _text = _text.Insert(_caret, c.ToString());
        _caret++;
        Changed?.Invoke(this, new NyxTextChangedEventArgs(_text));
        InvalidateRender();
    }

    private int EstimateCaretFromX(int x)
    {
        if (_text.Length == 0) return 0;
        var inner = InnerRect;
		if (_charPositions == null || _charPositions.Length != _text.Length + 1)
		{
			var rel = Math.Clamp(x - inner.X, 0, Math.Max(0, inner.Width));
			var approxCharW = 7;
			return Math.Clamp(rel / approxCharW, 0, _text.Length);
		}
		var fullW = _charPositions[_text.Length];
		var textStartX = Align switch
		{
			NyxTextAlign.TopCenter => inner.X + Math.Max(0, (inner.Width - fullW) / 2),
			NyxTextAlign.TopRight => inner.X + Math.Max(0, inner.Width - fullW),
			NyxTextAlign.Center => inner.X + Math.Max(0, (inner.Width - fullW) / 2),
			_ => inner.X,
		};
		var relX = x - textStartX + _scrollOffsetX;
		var bestCaret = 0;
		var minDiff = int.MaxValue;
		for (var i = 0; i <= _text.Length; i++)
		{
			var diff = Math.Abs(_charPositions[i] - relX);
			if (diff < minDiff)
			{
				minDiff = diff;
				bestCaret = i;
			}
		}
		return bestCaret;
    }

	private void EnsureCaretVisible(int innerWidth, int fullW)
	{
		if (Align != NyxTextAlign.TopLeft)
		{
			_scrollOffsetX = 0;
			return;
		}

		var caret = Math.Clamp(_caret, 0, _text.Length);
		var prefixW = (_charPositions != null && caret < _charPositions.Length) ? _charPositions[caret] : 0;

		if (prefixW < _scrollOffsetX)
		{
			_scrollOffsetX = prefixW;
		}
		else if (prefixW > _scrollOffsetX + innerWidth)
		{
			_scrollOffsetX = prefixW - innerWidth;
		}

		_scrollOffsetX = Math.Clamp(_scrollOffsetX, 0, Math.Max(0, fullW - innerWidth));
	}

    private void PaintCaret(INyxGuiPainter painter, NyxRect inner, in NyxWidgetVisual visual, NyxGuiTheme theme, int fullW, int fullH)
    {
        var caret = Math.Clamp(_caret, 0, _text.Length);
		var prefixW = (_charPositions != null && caret < _charPositions.Length) ? _charPositions[caret] : 0;

        var caretX = Align switch
        {
            NyxTextAlign.TopCenter => inner.X + Math.Max(0, (inner.Width - fullW) / 2) + prefixW - _scrollOffsetX,
            NyxTextAlign.TopRight => inner.X + Math.Max(0, inner.Width - fullW) + prefixW - _scrollOffsetX,
            _ => inner.X + prefixW - _scrollOffsetX,
        };

		var caretY = inner.Y + (inner.Height - fullH) * 0.5f;

        painter.FillRect(
            new NyxRect(caretX, (int)MathF.Round(caretY), 1, fullH),
            Tint(theme.Caret, visual));
    }

    public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
    {
        if (!Visible) return;

        var border = IsFocused ? theme.InputBorderFocused : theme.InputBorder;
        painter.FillRect(Bounds, theme.InputBackground);
        painter.DrawRect(Bounds, border, 1);

        if (TryBeginPaintVisual(out var visual))
        {
            try
            {
                var inner = InnerRect;
				var font = GetPaintFont();
				painter.MeasureText(_text.Length > 0 ? _text : "A", font, out var fullW, out var fullH);
				if (_text.Length == 0) fullW = 0;

				if (_charPositions == null || _charPositions.Length != _text.Length + 1)
				{
					_charPositions = new int[_text.Length + 1];
				}
				_charPositions[0] = 0;
				for (var i = 1; i <= _text.Length; i++)
				{
					painter.MeasureText(_text.AsSpan(0, i), font, out var w, out _);
					_charPositions[i] = w;
				}

				EnsureCaretVisible(inner.Width, fullW);

				painter.PushClip(inner);

				var caretY = inner.Y + (inner.Height - fullH) * 0.5f;

				if (_selectionStart != -1 && _selectionStart != _caret)
				{
					var start = Math.Min(_selectionStart, _caret);
					var end = Math.Max(_selectionStart, _caret);

					var selX0 = (_charPositions != null && start < _charPositions.Length) ? _charPositions[start] : 0;
					var selX1 = (_charPositions != null && end < _charPositions.Length) ? _charPositions[end] : 0;

					var textStartX = Align switch
					{
						NyxTextAlign.TopCenter => (inner.Width - fullW) / 2,
						NyxTextAlign.TopRight => inner.Width - fullW,
						_ => 0,
					};

					var drawX0 = inner.X + Math.Max(0, textStartX) + selX0 - _scrollOffsetX;
					var drawX1 = inner.X + Math.Max(0, textStartX) + selX1 - _scrollOffsetX;

					var x0 = Math.Clamp(drawX0, inner.X, inner.Right);
					var x1 = Math.Clamp(drawX1, inner.X, inner.Right);

					if (x1 > x0)
					{
						painter.FillRect(
							new NyxRect(x0, (int)MathF.Round(caretY), x1 - x0, fullH),
							new NyxColor(51, 153, 255, 100));
					}
				}

				var textRect = new NyxRect(inner.X - _scrollOffsetX, (int)MathF.Round(caretY), inner.Width + _scrollOffsetX, fullH);
				painter.DrawText(textRect, _text, Align, Tint(theme.TextPrimary, visual), font);

                if (!ReadOnly && IsFocused && (Environment.TickCount64 % 1000) < 500)
                    PaintCaret(painter, inner, visual, theme, fullW, fullH);
				painter.PopClip();
            }
            finally
            {
                EndPaintVisual();
            }
        }
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
