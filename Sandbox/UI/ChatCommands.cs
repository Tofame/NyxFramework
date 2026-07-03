using System;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox.UI;

internal delegate void ChatCommandExecute(SandboxGameWorld world, SandboxChat chat, string[] args);

internal sealed class ChatCommand
{
	public string Name { get; }
	public string Description { get; }
	public ChatCommandExecute Execute { get; }

	public ChatCommand(string name, string description, ChatCommandExecute execute)
	{
		Name = name;
		Description = description;
		Execute = execute;
	}
}

internal static class ChatCommandRegistry
{
	private static readonly Dictionary<string, ChatCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

	static ChatCommandRegistry()
	{
		Register(new ChatCommand("help", "Lists all available commands.", (world, chat, args) =>
		{
			chat.AddChatMessage(string.Empty, "Available Commands:", 2);
			foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
			{
				chat.AddChatMessage(string.Empty, $"  /{cmd.Name} - {cmd.Description}", 2);
			}
		}));

		Register(new ChatCommand("name", "Changes your player name. Usage: /name <newname>", (world, chat, args) =>
		{
			if (args.Length == 0)
			{
				chat.AddChatMessage(string.Empty, "Usage: /name <newname>", 2);
				return;
			}
			var newName = string.Join(" ", args).Trim();
			if (string.IsNullOrEmpty(newName))
			{
				chat.AddChatMessage(string.Empty, "Usage: /name <newname>", 2);
				return;
			}
			if (world.Player is not null)
			{
				world.Player.Name = newName;
				chat.AddChatMessage(string.Empty, $"Your name has been changed to: {newName}", 2);
				world.SendLocalPlayerMoveUpdate();
			}
		}));
	}

	public static void Register(ChatCommand cmd)
	{
		_commands[cmd.Name] = cmd;
	}

	public static bool TryExecute(string text, SandboxGameWorld world, SandboxChat chat)
	{
		if (!text.StartsWith("/")) return false;
		if (text.StartsWith("/y ", StringComparison.OrdinalIgnoreCase)) return false;

		var parts = text[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return false;

		var cmdName = parts[0];
		if (_commands.TryGetValue(cmdName, out var cmd))
		{
			var args = parts.Skip(1).ToArray();
			try
			{
				cmd.Execute(world, chat, args);
			}
			catch (Exception ex)
			{
				chat.AddChatMessage(string.Empty, $"Error executing command /{cmdName}: {ex.Message}", 2);
			}
			return true;
		}

		chat.AddChatMessage(string.Empty, $"Unknown command: /{cmdName}", 2);
		return true;
	}
}
