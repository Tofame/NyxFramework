using System;
using System.Collections.Generic;
using System.Linq;
using NyxGui;
using Silk.NET.Input;
using Sandbox.Networking.Packets;

namespace Sandbox.UI;

internal sealed class SandboxChat
{
	private const int OverlayW = 450;
	private const int OverlayH = 180;

	private readonly NyxMiniWindow _window;
	private readonly NyxScrollablePanel _scroll;
	private readonly NyxTextBox _inputBox;
	private readonly SandboxGameWorld _gameWorld;
	private readonly NyxContainer _gamePanel;

	private readonly NyxButton _btnLocal;
	private readonly NyxButton _btnGlobal;
	private readonly NyxButton _btnSystem;

	private int _activeTab = 0; // 0 = Local, 1 = Global, 2 = System
	private int _nextLogY = 0;
	private readonly List<ChatMessage> _messageHistory = new();
	private readonly List<NyxExtendedLabel> _logLabels = new();
	private readonly CreatureInformationDrawer? _creatureInfoDrawer;

	public SandboxChat(int viewportWidth, int viewportHeight, SandboxGameWorld gameWorld, NyxContainer gamePanel, CreatureInformationDrawer? creatureInfoDrawer, NyxGuiRootStack? guiRoots = null)
	{
		_gameWorld = gameWorld;
		_gamePanel = gamePanel;
		_creatureInfoDrawer = creatureInfoDrawer;

		// 1. Create the mini-window
		_window = new NyxMiniWindow("ChatWindow")
		{
			Title = "Chat Console",
			ShowCloseButton = false,
			ShowMinimizeButton = true,
			ShowLockButton = true,
			Resizable = true,
			MinExpandedHeight = 120,
			MaxExpandedHeight = 400
		};

		// Premium glassmorphism window styling
		_window.States.Normal.BackgroundColor = new NyxColor(15, 23, 42, 200); // slate-900 with opacity
		_window.States.Normal.BorderColor = NyxColor.FromRgb(51, 65, 85); // slate-700
		_window.States.Normal.BorderWidth = 1;

		// 2. Tab buttons (Local, Global, System)
		_btnLocal = new NyxButton("btnTabLocal") { Label = "Local", IsSelected = true };
		_btnGlobal = new NyxButton("btnTabGlobal") { Label = "Global" };
		_btnSystem = new NyxButton("btnTabSystem") { Label = "System" };

		StyleTabButton(_btnLocal);
		StyleTabButton(_btnGlobal);
		StyleTabButton(_btnSystem);

		// Anchor tab buttons
		_btnLocal.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Margin = new NyxThickness(6, 6, 0, 0),
			FixedWidth = 75,
			FixedHeight = 20
		};
		_btnGlobal.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Margin = new NyxThickness(85, 6, 0, 0),
			FixedWidth = 75,
			FixedHeight = 20
		};
		_btnSystem.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Margin = new NyxThickness(164, 6, 0, 0),
			FixedWidth = 75,
			FixedHeight = 20
		};

		_btnLocal.Click += (_, _) => SetActiveTab(0);
		_btnGlobal.Click += (_, _) => SetActiveTab(1);
		_btnSystem.Click += (_, _) => SetActiveTab(2);

		_window.Body.AddChild(_btnLocal);
		_window.Body.AddChild(_btnGlobal);
		_window.Body.AddChild(_btnSystem);

		var scrollbar = new NyxVScrollBar(NyxRect.Empty)
		{
			Id = "ChatScrollBar"
		};
		scrollbar.LayoutBox = new NyxLayoutBox
		{
			Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom),
			Margin = new NyxThickness(0, 32, 6, 30),
			FixedWidth = 12
		};
		_window.Body.AddChild(scrollbar);

		// 3. Scrollable history panel
		_scroll = new NyxScrollablePanel(NyxRect.Empty)
		{
			Id = "ChatLogScroll"
		};
		_scroll.States.Normal.BackgroundColor = NyxColor.Transparent;
		_scroll.States.Normal.BorderWidth = 0;
		_scroll.VerticalScrollBar = scrollbar;

		_scroll.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Right = NyxLayoutAnchor.WidgetEdge("ChatScrollBar", NyxAnchorEdge.Left),
			Top = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Top),
			Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom),
			Margin = new NyxThickness(6, 32, 4, 30)
		};
		_window.Body.AddChild(_scroll);

		// 4. Input Box
		_inputBox = new NyxTextBox("ChatInput")
		{
			Align = NyxTextAlign.TopLeft,
			MaxLength = 100
		};
		_inputBox.LayoutBox = new NyxLayoutBox
		{
			Left = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Left),
			Right = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Right),
			Bottom = NyxLayoutAnchor.ParentEdge(NyxAnchorEdge.Bottom),
			Margin = new NyxThickness(6, 0, 6, 4),
			FixedHeight = 20
		};

		_inputBox.States.Normal.BackgroundColor = new NyxColor(30, 41, 59, 120); // slate-800 semi-transparent
		_inputBox.States.Normal.BorderColor = NyxColor.FromRgb(71, 85, 105); // slate-600
		_inputBox.States.Normal.BorderWidth = 1;

		_inputBox.EnterPressed += OnInputSubmitted;
		_window.Body.AddChild(_inputBox);

		// Position the window initially
		var startY = viewportHeight - OverlayH - 10;
		_window.SetBounds(new NyxRect(10, startY, OverlayW, OverlayH));

		// Register with global roots
		guiRoots?.Add(_window, () => _window.Visible);

		// Register networking events
		if (_gameWorld.Protocol is not null)
		{
			_gameWorld.Protocol.OnMessageReceived += OnPacketMessageReceived;
		}

		// Initial System Welcome Message
		AddChatMessage(string.Empty, "Welcome to Nyx Sandbox! Press ENTER to chat.", 2);
	}

	private static void StyleTabButton(NyxButton btn)
	{
		btn.States.Normal.BackgroundColor = new NyxColor(30, 41, 59, 120); // slate-800 semi-transparent
		btn.States.Normal.BorderColor = NyxColor.FromRgb(71, 85, 105); // slate-600
		btn.States.Normal.BorderWidth = 1;

		btn.States.On.BackgroundColor = new NyxColor(59, 130, 246, 200); // blue-500
		btn.States.On.BorderColor = NyxColor.FromRgb(96, 165, 250); // blue-400
		btn.States.On.BorderWidth = 1;

		btn.States.Hover.BackgroundColor = new NyxColor(51, 65, 85, 160); // slate-700
	}

	public void UpdateViewport(int width, int height)
	{
		var w = _window.Bounds.Width;
		var h = _window.Bounds.Height;
		var y = height - h - 10;
		_window.SetBounds(new NyxRect(10, y, w, h));
	}

	public void ToggleFocus(NyxGuiRootStack roots)
	{
		if (_inputBox.IsFocused)
		{
			OnInputSubmitted(this, EventArgs.Empty);
			roots.Focus.Clear();
		}
		else
		{
			roots.Focus.SetFocus(_inputBox);
		}
	}

	private void OnInputSubmitted(object? sender, EventArgs e)
	{
		var text = _inputBox.Text.Trim();
		_inputBox.Text = string.Empty;

		if (string.IsNullOrEmpty(text))
			return;

		if (ChatCommandRegistry.TryExecute(text, _gameWorld, this))
			return;

		byte type = 0; // Speak
		if (text.StartsWith("#y ", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/y ", StringComparison.OrdinalIgnoreCase))
		{
			type = 1; // Yell
			text = text[3..].Trim();
		}

		if (string.IsNullOrEmpty(text))
			return;

		_gameWorld.Protocol?.SendChatRequest(text, type);
	}

	private void OnPacketMessageReceived(PlayerChatPacket packet)
	{
		AddChatMessage(packet.SenderName, packet.Message, packet.ChatType);

		if (packet.ChatType < 2)
		{
			_creatureInfoDrawer?.SpawnSpeechBubble(packet.SenderId, packet.SenderName, packet.Message, packet.ChatType);
		}
	}

	private void SetActiveTab(int tabIndex)
	{
		_activeTab = tabIndex;
		_btnLocal.IsSelected = (tabIndex == 0);
		_btnGlobal.IsSelected = (tabIndex == 1);
		_btnSystem.IsSelected = (tabIndex == 2);

		RebuildChatHistory();
	}

	private void RebuildChatHistory()
	{
		_scroll.Body.ClearChildren();
		_logLabels.Clear();
		_nextLogY = 0;

		foreach (var msg in _messageHistory)
		{
			if (ShouldShowMessageInActiveTab(msg.Type))
			{
				RenderChatMessage(msg.Sender, msg.Message, msg.Type);
			}
		}

		_scroll.ContentExtentHeight = _nextLogY;
		_scroll.ScrollTo(99999);
	}

	private bool ShouldShowMessageInActiveTab(byte type)
	{
		if (_activeTab == 0) // Local
		{
			return type == 0 || type == 1 || type == 2; // Speak, Yell, or System
		}
		if (_activeTab == 2) // System
		{
			return type == 2; // System only
		}
		return true; // Global shows everything
	}

	public void AddChatMessage(string sender, string message, byte type)
	{
		var msg = new ChatMessage { Sender = sender, Message = message, Type = type };
		_messageHistory.Add(msg);

		if (_messageHistory.Count > 200)
		{
			_messageHistory.RemoveAt(0);
		}

		if (ShouldShowMessageInActiveTab(type))
		{
			RenderChatMessage(sender, message, type);
			_scroll.ContentExtentHeight = _nextLogY;
			_scroll.ScrollTo(99999);
		}
	}

	private void RenderChatMessage(string sender, string message, byte type)
	{
		var color = NyxColor.FromRgb(241, 245, 249); // slate-100 (Speak)
		var prefix = string.IsNullOrEmpty(sender) ? "" : $"[{sender}]: ";
		var text = $"{prefix}{message}";

		if (type == 1) // Yell
		{
			color = NyxColor.FromRgb(251, 146, 60); // orange-400 (Yell)
		}
		else if (type == 2) // System
		{
			color = NyxColor.FromRgb(52, 211, 153); // emerald-400 (System)
		}

		var label = new NyxExtendedLabel
		{
			Text = text,
			Align = NyxTextAlign.TopLeft,
			DefaultColor = color
		};

		// Add spacing/margin and use AddToBody to correctly translate bounds relative to scroll panel body
		var labelWidth = 400;
		label.SetBounds(new NyxRect(0, 0, labelWidth, 16));
		_scroll.AddToBody(label, 4, _nextLogY);
		_nextLogY += 18;

		_logLabels.Add(label);

		if (_logLabels.Count > 100)
		{
			var old = _logLabels[0];
			_logLabels.RemoveAt(0);
			_scroll.Body.RemoveChild(old);

			_nextLogY = 0;
			for (int i = 0; i < _logLabels.Count; i++)
			{
				_scroll.SetBodyChildBounds(_logLabels[i], 4, _nextLogY, labelWidth, 16);
				_nextLogY += 18;
			}
		}
	}

	private sealed class ChatMessage
	{
		public required string Sender { get; set; }
		public required string Message { get; set; }
		public required byte Type { get; set; } // 0 = Speak, 1 = Yell, 2 = System
	}
}
