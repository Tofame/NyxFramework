using NyxGui;
using NyxGui.Definitions;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace Sandbox.UI;

internal sealed class SandboxTaskList
{
	private readonly NyxGuiBuiltDocument? _document;
	private readonly NyxElement _root;
	private readonly SandboxShell? _shell;
	private int _lastVpW;
	private int _lastVpH;
	private bool _layoutApplied;
	private bool _tWasDown;
	private readonly Key _toggleKey;

	private struct TaskEntry
	{
		public string Name { get; set; }
		public int Exp { get; set; }
		public string Item { get; set; }
		public int Gold { get; set; }
	}

	public SandboxTaskList(SandboxShell shell, NyxGuiSettings? settings = null)
	{
		_shell = shell;
		_toggleKey = SandboxUIKeyBinding.TryGetToggleKey("task_list") ?? Key.T;

		var loadOptions = SandboxUIDefinitions.CreateLoadOptions(settings);
		var loaded = SandboxUIDefinitions.TryLoad("task_list", loadOptions);
		if (loaded is null)
		{
			Console.WriteLine("NyxGUI: missing resources/ui/task_list.nyxui — task list disabled.");
			_root = new NyxContainer(NyxRect.Empty);
			return;
		}

		_document = loaded.Document;
		_root = _document.Root;
		_shell.AdoptIntoGamePanel(_document);

		var scroll = _document.TryGetScrollablePanel("TaskListScroll");
		if (scroll is not null)
		{
			// Populate with dummy tasks
			var dummyTasks = new List<TaskEntry>
			{
				new() { Name = "Kill 100 Rats", Exp = 20, Item = "Rat Tail", Gold = 5 },
				new() { Name = "Gather 5 Herbs", Exp = 35, Item = "Golden Hue", Gold = 10 },
				new() { Name = "Defeat Dragon", Exp = 2000, Item = "Dragon Shield", Gold = 500 },
				new() { Name = "Fetch Water", Exp = 10, Item = "Bucket", Gold = 2 },
				new() { Name = "Deliver Letter", Exp = 50, Item = "None", Gold = 20 },
				new() { Name = "Mine 10 Iron Ore", Exp = 150, Item = "Iron Bar", Gold = 40 },
				new() { Name = "Slay Goblin King", Exp = 800, Item = "Goblin Crown", Gold = 250 },
				new() { Name = "Rescue Villager", Exp = 400, Item = "Golden Key", Gold = 100 },
				new() { Name = "Catch 10 Fish", Exp = 80, Item = "Fishing Rod", Gold = 15 },
				new() { Name = "Clean the Sewers", Exp = 250, Item = "Golden Coin", Gold = 60 },
			};

			foreach (var task in dummyTasks)
			{
				var cell = new NyxContainer(NyxRect.Empty);
				cell.States.Normal.BackgroundColor = NyxColor.FromRgb(40, 40, 42);
				cell.States.Normal.BorderWidth = 1;
				cell.States.Normal.BorderColor = NyxColor.FromRgb(80, 80, 85);

				// Programmatic Stack Layout inside each cell!
				cell.StackLayout(Orientation.Vertical, spacing: 2, padding: new NyxThickness(4, 4, 4, 4));

				var lblName = new NyxLabel
				{
					Text = task.Name,
					Align = NyxTextAlign.TopLeft
				};
				lblName.FixedHeight(14);

				var lblExpGold = new NyxExtendedLabel
				{
					Align = NyxTextAlign.TopLeft
				};
				lblExpGold.SetMarkup($"Exp: {{#00FF00}}{task.Exp}{{/}} Gold: {{#FFFF00}}{task.Gold}{{/}}", NyxColor.FromRgb(180, 180, 180));
				lblExpGold.FixedHeight(14);

				var lblItem = new NyxExtendedLabel
				{
					Align = NyxTextAlign.TopLeft
				};
				lblItem.SetMarkup($"Item: {{#55AAFF}}{task.Item}{{/}}", NyxColor.FromRgb(180, 180, 180));
				lblItem.FixedHeight(14);

				cell.AddChild(lblName);
				cell.AddChild(lblExpGold);
				cell.AddChild(lblItem);

				scroll.Body.AddChild(cell);
			}
		}

		Console.WriteLine($"NyxGUI: loaded task list \"{loaded.SourcePath}\".");
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
			var t = kb.IsKeyPressed(_toggleKey);
			if (t && !_tWasDown)
				Visible = !Visible;
			_tWasDown = t;
		}
	}
}
