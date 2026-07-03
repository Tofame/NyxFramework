namespace NyxGui;

/// <summary>
/// Event arguments for <see cref="NyxFileDialogButton.FileSelected"/>.
/// </summary>
public sealed class NyxFileSelectedEventArgs : EventArgs
{
	public NyxFileSelectedEventArgs(string path) => Path = path;

	/// <summary>The full path selected by the user.</summary>
	public string Path { get; }
}

/// <summary>
/// Compound widget: a clickable button that opens an OS-native file dialog and
/// displays the selected filename next to the button.
/// <para>
/// Layout inside <see cref="NyxElement.Bounds"/>:
/// <code>
/// ┌────────────────┐  ┌─────────────────────────────────────────┐
/// │  Select file…  │  │  filename.png  (or "No file selected")  │
/// └────────────────┘  └─────────────────────────────────────────┘
///   ← ButtonWidth →  gap  ←──────── remaining width ────────────→
/// </code>
/// Set <see cref="ShowSelectedPath"/> to <c>false</c> to render just the button.
/// </para>
/// </summary>
public class NyxFileDialogButton : NyxWidget, ICapturesPointer
{
	private bool _isRunning;
	private int _pointerX;
	private int _pointerY;

	public NyxFileDialogButton(string? id = null) : base(0) { Id = id; }

	// ── Configuration ─────────────────────────────────────────────────

	/// <summary>Text shown on the clickable button face.</summary>
	public string ButtonLabel { get; set; } = "Select file…";

	/// <summary>Pixel width of the button face. The rest of <see cref="NyxElement.Bounds"/> shows the path.</summary>
	public int ButtonWidth { get; set; } = 120;

	/// <summary>Gap in pixels between the button face and the path label.</summary>
	public int Gap { get; set; } = 6;

	/// <summary>When <c>false</c>, the path label is not rendered (the whole widget is just the button).</summary>
	public bool ShowSelectedPath { get; set; } = true;

	/// <summary>Text shown when no file has been selected yet.</summary>
	public string PlaceholderText { get; set; } = "No file selected";

	/// <summary>Dialog mode — <see cref="NyxFileDialogMode.Open"/> or <see cref="NyxFileDialogMode.Save"/>.</summary>
	public NyxFileDialogMode Mode { get; set; } = NyxFileDialogMode.Open;

	/// <summary>Options forwarded to <see cref="NyxFileDialog.ShowAsync"/>.</summary>
	public NyxFileDialogOptions DialogOptions { get; set; } = new();

	// ── State ─────────────────────────────────────────────────────────

	/// <summary>Full path most recently confirmed by the user, or <c>null</c> if none yet.</summary>
	public string? SelectedPath { get; private set; }

	// ── Events ────────────────────────────────────────────────────────

	/// <summary>Raised (on an arbitrary thread-pool thread) after the user confirms a selection.</summary>
	public event EventHandler<NyxFileSelectedEventArgs>? FileSelected;

	// ── Input ─────────────────────────────────────────────────────────

	public override void OnMouseMove(int x, int y)
	{
		_pointerX = x;
		_pointerY = y;
		base.OnMouseMove(x, y);
	}

	public override void OnMouseUp(int x, int y, NyxMouseButton button)
	{
		base.OnMouseUp(x, y, button);

		if (button == NyxMouseButton.Left && GetButtonRect().Contains(x, y) && !_isRunning)
			TriggerDialog();
	}

	private void TriggerDialog()
	{
		_isRunning = true;
		_ = RunDialogAsync();
	}

	private async Task RunDialogAsync()
	{
		try
		{
			var path = await NyxFileDialog.ShowAsync(Mode, DialogOptions).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(path))
			{
				SelectedPath = path;
				FileSelected?.Invoke(this, new NyxFileSelectedEventArgs(path!));
			}
		}
		finally
		{
			_isRunning = false;
		}
	}

	// ── Rects ─────────────────────────────────────────────────────────

	private NyxRect GetButtonRect()
	{
		var w = ShowSelectedPath ? Math.Min(ButtonWidth, Bounds.Width) : Bounds.Width;
		return new NyxRect(Bounds.X, Bounds.Y, w, Bounds.Height);
	}

	private NyxRect GetPathRect()
	{
		if (!ShowSelectedPath) return NyxRect.Empty;
		var btnW = Math.Min(ButtonWidth, Bounds.Width);
		var x = Bounds.X + btnW + Gap;
		var w = Math.Max(0, Bounds.Right - x);
		return new NyxRect(x, Bounds.Y, w, Bounds.Height);
	}

	// ── Paint ─────────────────────────────────────────────────────────

	public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
	{
		if (!TryBeginPaintVisual(out var visual)) return;

		try
		{
			var btnRect = GetButtonRect();

			// Determine button hover/pressed using precise sub-hit on the button rect.
			var btnHover = PointerInside && btnRect.Contains(_pointerX, _pointerY);
			var btnPressed = btnHover && PointerPressed;

			// ── Button face ───────────────────────────────────────────────

			if (visual.Image is not null && !string.IsNullOrEmpty(visual.Image.ImageSource))
			{
				PaintBackground(painter, visual);
			}
			else if (visual.HasBackground)
			{
				painter.FillRect(btnRect, Tint(visual.BackgroundColor!.Value, visual));
			}
			else
			{
				var face = _isRunning
					? theme.ButtonFacePressed
					: btnPressed
						? theme.ButtonFacePressed
						: btnHover ? theme.ButtonFaceHover : theme.ButtonFace;
				painter.FillRect(btnRect, Tint(face, visual));
			}

			if (visual.HasBorder)
				PaintStateBorder(painter, visual);
			else
				painter.DrawRect(btnRect, Tint(theme.ButtonBorder, visual), 1);

			// ── Button label ──────────────────────────────────────────────

			var label = _isRunning ? "…" : ButtonLabel;
			painter.DrawText(
				btnRect,
				label,
				NyxTextAlign.Center,
				Tint(theme.TextPrimary, visual),
				GetPaintFont());

			// ── Path label zone ───────────────────────────────────────────

			if (ShowSelectedPath)
			{
				var pathRect = GetPathRect();
				if (pathRect.Width > 0)
				{
					var hasPath = SelectedPath is not null;
					var pathText = hasPath
						? System.IO.Path.GetFileName(SelectedPath)
						: PlaceholderText;

					var pathColor = hasPath
						? Tint(theme.TextPrimary, visual)
						: Tint(theme.TextMuted, visual);

					painter.DrawText(
						pathRect,
						pathText ?? string.Empty,
						NyxTextAlign.TopLeft,
						pathColor,
						GetPaintFont());
				}
			}
		}
		finally
		{
			EndPaintVisual();
		}
	}
}
