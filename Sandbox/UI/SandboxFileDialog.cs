using System.Xml.Schema;
using NyxGui;
using NyxGuiRender;
using Silk.NET.Input;

namespace Sandbox.UI;

/// <summary>
/// Programmatic demo window for <see cref="NyxFileDialogButton"/>.
/// Toggle with <b>F</b> (or the key configured under <c>[toggles.file_dialog]</c> in
/// <c>sandbox_ui_config.toml</c>).
/// </summary>
internal sealed class SandboxFileDialog
{
	private readonly NyxGuiRenderer _renderer;
	private readonly NyxGuiTheme _theme = new();
	private readonly NyxMiniWindow _window;
	private readonly NyxLabel _resultLabel;
	private bool _fWasDown;
	private readonly Key _toggleKey;

	public bool Visible
	{
		get => _window.Visible;
		set => _window.Visible = value;
	}

	public NyxMiniWindow Window => _window;

	public SandboxFileDialog(NyxGuiRenderer renderer, NyxGuiRootStack guiRoots, int vpWidth, int vpHeight)
	{
		_renderer = renderer;
		_toggleKey = SandboxUIKeyBinding.GetToggleKey("file_dialog", Key.F);

		// ── Window ────────────────────────────────────────────────────
		_window = new NyxMiniWindow("fileDialogDemo")
		{
			Title = "File Dialog Demo",
			ShowCloseButton = true,
			ShowMinimizeButton = true,
			ShowLockButton = false,
			Resizable = false,
		};

		// Window body background — same dark panel color used by other modules.
		_window.States.Normal.BackgroundColor = NyxColor.FromRgb(30, 30, 35);
		_window.States.Normal.BorderColor = NyxColor.FromRgb(70, 70, 85);
		_window.States.Normal.BorderWidth = 1;

		// Row heights and layout constants.
		const int padX = 10;   // horizontal padding inside body
		const int padY = 8;    // top padding inside body
		const int rowH = 22;
		const int smallH = 14;
		const int sepH = 1;
		const int gap = 8;
		const int winW = 380;

		var startX = Math.Max(0, (vpWidth - winW) / 2);
		var startY = Math.Max(0, vpHeight / 4);

		// Total body content height.
		var bodyH =
			padY +
			smallH + gap +    // "Open a file" header
			rowH + gap +      // open button
			sepH + gap +      // separator
			smallH + gap +    // "Save a file" header
			rowH + gap +      // save button
			sepH + gap +      // separator
			smallH + gap +    // "Last selected path" header
			smallH +          // result label
			padY;

		_window.SetBounds(new NyxRect(startX, startY, winW, _window.TitleBarHeight + bodyH));

		// ── Build children with LayoutBox anchors ─────────────────────
		// Each child anchors left/right to parent and top to parent with a top-margin
		// equal to its vertical offset within the body. This way SyncBodyLayout()
		// correctly repositions all children when the window is dragged.
		var topOffset = padY;

		// ── "Open a file" header
		AddChild(_window, MakeSectionHeader("Open a file"),
			padX, topOffset, padX, smallH);
		topOffset += smallH + gap;

		// ── Open-file button
		var openBtn = new NyxFileDialogButton("fileDialogOpen")
		{
			ButtonLabel = "Browse…",
			ButtonWidth = 80,
			Mode = NyxFileDialogMode.Open,
			DialogOptions = new NyxFileDialogOptions
			{
				Title = "Open File",
				Extensions = new[] { "png", "jpg", "jpeg", "bmp", "dat", "spr" },
				FilterLabel = "Supported files",
			},
		};
		openBtn.FileSelected += OnFileSelected;
		AddChild(_window, openBtn, padX, topOffset, padX, rowH);
		topOffset += rowH + gap;

		// ── Separator
		AddChild(_window, new NyxSeparator(NyxRect.Empty), padX, topOffset, padX, sepH);
		topOffset += sepH + gap;

		// ── "Save a file" header
		AddChild(_window, MakeSectionHeader("Save a file"),
			padX, topOffset, padX, smallH);
		topOffset += smallH + gap;

		// ── Save-file button
		var saveBtn = new NyxFileDialogButton("fileDialogSave")
		{
			ButtonLabel = "Save as…",
			ButtonWidth = 80,
			Mode = NyxFileDialogMode.Save,
			DialogOptions = new NyxFileDialogOptions
			{
				Title = "Save File",
				Extensions = new[] { "txt", "log" },
				FilterLabel = "Text files",
				DefaultExtension = "txt",
			},
		};
		saveBtn.FileSelected += OnFileSelected;
		AddChild(_window, saveBtn, padX, topOffset, padX, rowH);
		topOffset += rowH + gap;

		// ── Separator
		AddChild(_window, new NyxSeparator(NyxRect.Empty), padX, topOffset, padX, sepH);
		topOffset += sepH + gap;

		// ── "Last selected path" header
		AddChild(_window, MakeSectionHeader("Last selected path"),
			padX, topOffset, padX, smallH);
		topOffset += smallH + gap;

		// ── Result label
		_resultLabel = new NyxLabel
		{
			Text = "(none)",
			Wrap = true,
			Align = NyxTextAlign.TopLeft,
		};
		AddChild(_window, _resultLabel, padX, topOffset, padX, smallH);

		// Trigger SyncBodyLayout now that all children are in the body.
		// The initial SetBounds call ran before children were added, so anchors
		// were not resolved. Re-calling SetBounds forces a second SyncBodyLayout pass.
		_window.SetBounds(_window.Bounds);

		// ── Register with root stack ──────────────────────────────────
		guiRoots.Add(_window, () => _window.Visible);
		_window.Closed += (_, _) => _window.Visible = false;

		_window.Visible = false;
		Console.WriteLine($"NyxGUI: File Dialog Demo ready. Toggle with [{_toggleKey}].");
	}

	// ── Event handlers ────────────────────────────────────────────────

	private void OnFileSelected(object? sender, NyxFileSelectedEventArgs e)
	{
		// NyxLabel.Text is a plain string; safe to write from the dialog thread-pool thread.
		_resultLabel.Text = e.Path;
	}

	// ── Draw ──────────────────────────────────────────────────────────

	public void Draw()
	{
		if (!Visible) return;
		_window.Paint(_renderer, _theme);
	}

	// ── Update ────────────────────────────────────────────────────────

	public void Update(IInputContext? input, NyxGuiRootStack? guiRoots = null)
	{
		if (input is not { Keyboards.Count: > 0 }) return;
		var kb = input.Keyboards[0];

		if (guiRoots is not null && NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots)) return;

		var fDown = kb.IsKeyPressed(_toggleKey);
		if (fDown && !_fWasDown)
			Visible = !Visible;
		_fWasDown = fDown;
	}

	// ── Helpers ───────────────────────────────────────────────────────

	/// <summary>
	/// Adds a child to the mini-window body and assigns a LayoutBox so that
	/// <see cref="NyxMiniWindow"/> SyncBodyLayout() re-resolves its position
	/// (using parent-anchored edge resolution) whenever the window is dragged.
	/// </summary>
	/// <param name="window">Target mini-window.</param>
	/// <param name="child">Child element to add.</param>
	/// <param name="marginLeft">Left inset from parent body edge.</param>
	/// <param name="marginTop">Top inset from parent body top edge (vertical offset).</param>
	/// <param name="marginRight">Right inset from parent body right edge.</param>
	/// <param name="fixedHeight">Fixed height for the child row.</param>
	private static void AddChild(
		NyxMiniWindow window,
		NyxElement child,
		int marginLeft,
		int marginTop,
		int marginRight,
		int fixedHeight)
	{
		child.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Margin = new NyxThickness(marginLeft, marginTop, marginRight, 0),
			FixedHeight = fixedHeight,
		};
		window.Body.AddChild(child);
	}

	private static NyxExtendedLabel MakeSectionHeader(string text)
	{
		var lbl = new NyxExtendedLabel
		{
			Align = NyxTextAlign.TopLeft,
			LineHeight = 14,
		};
		lbl.SetMarkup(text, NyxColor.FromRgb(160, 200, 255));
		return lbl;
	}
}
