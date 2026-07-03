using NyxGui;
using NyxGui.Definitions;
using Silk.NET.Input;
using System;

namespace Sandbox.UI;

internal sealed class SandboxObjectFit
{
	private readonly NyxGuiBuiltDocument? _document;
	private readonly NyxElement _root;
	private readonly SandboxShell? _shell;
	private int _lastVpW;
	private int _lastVpH;
	private bool _layoutApplied;
	private bool _oWasDown;
	private readonly Key _toggleKey;

	public SandboxObjectFit(SandboxShell shell, NyxGuiSettings? settings = null)
	{
		_shell = shell;
		_toggleKey = SandboxUIKeyBinding.TryGetToggleKey("object_fit") ?? Key.O;

		var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
		var loaded = SandboxUIDefinitions.TryLoad("object_fit", loadOptions);
		if (loaded is null)
		{
			Console.WriteLine("NyxGUI: missing resources/ui/object_fit.nyxui — object fit test disabled.");
			_root = new NyxContainer(NyxRect.Empty);
			return;
		}

		_document = loaded.Document;
		_root = _document.Root;
		_shell.AdoptIntoGamePanel(_document);

		Console.WriteLine($"NyxGUI: loaded object fit test \"{loaded.SourcePath}\".");
	}

	public bool Visible
	{
		get => _root.Visible;
		set => _root.Visible = value;
	}

	public int WidgetCount => _document?.ById.Count ?? 0;

	public void UpdateViewport(int width, int height)
	{
		if (width <= 0 || height <= 0)
			return;

		var sizeChanged = width != _lastVpW || height != _lastVpH;
		_lastVpW = width;
		_lastVpH = height;

		_shell?.UpdateViewport(width, height);

		if (!sizeChanged && _layoutApplied)
			return;

		_layoutApplied = true;
	}

	public void Update(IInputContext? input, NyxGuiRootStack? guiRoots = null)
	{
		if (input is { Keyboards.Count: > 0 } && input.Keyboards[0] is { } kb
			&& (guiRoots is null || !NyxGuiKeyboardInput.CapturesGlobalShortcuts(guiRoots)))
		{
			var o = kb.IsKeyPressed(_toggleKey);
			if (o && !_oWasDown)
				Visible = !Visible;
			_oWasDown = o;
		}
	}
}
