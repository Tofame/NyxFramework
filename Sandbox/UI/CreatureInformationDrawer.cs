using System;
using System.Collections.Generic;
using NyxGui;
using NyxGameCore;
using NyxGameMap;

namespace Sandbox.UI;

internal sealed class CreatureInformationDrawer : IDisposable
{
	private readonly SandboxGameWorld _gameWorld;
	private readonly NyxContainer _gamePanel;
	private readonly Dictionary<string, NyxExtendedLabel> _nameTags = new();
	private readonly List<SpeechBubble> _bubbles = new();
	private bool _disposed;

	public CreatureInformationDrawer(SandboxGameWorld gameWorld, NyxContainer gamePanel)
	{
		_gameWorld = gameWorld;
		_gamePanel = gamePanel;
	}

	public void SpawnSpeechBubble(string speakerId, string speakerName, string text, byte chatType)
	{
		if (_disposed) return;

		// Remove existing bubble for this speaker to avoid overlap
		var existing = _bubbles.Find(b => b.SpeakerId == speakerId);
		if (existing is not null)
		{
			_gamePanel.RemoveChild(existing.Label);
			_bubbles.Remove(existing);
		}

		var actionVerb = chatType == 1 ? "yells" : "says";
		var bubbleText = $"{speakerName} {actionVerb}: {text}";

		var approxCharW = 7;
		var bubbleW = Math.Clamp(bubbleText.Length * approxCharW + 16, 80, 320);
		var bubbleH = 18;

		var label = new NyxExtendedLabel
		{
			Text = bubbleText,
			Align = NyxTextAlign.TopCenter,
			DefaultColor = NyxColor.FromRgb(255, 255, 128),
			Font = new NyxFontStyle { SizePt = 11.0f, Outlined = true },
			Phantom = true // ignore mouse interactions
		};
		label.SetBounds(new NyxRect(0, 0, bubbleW, bubbleH));

		_gamePanel.AddChild(label);
		_gamePanel.MoveChild(label, 0);
		_bubbles.Add(new SpeechBubble
		{
			SpeakerId = speakerId,
			Label = label,
			CreateTime = Environment.TickCount64,
			DurationMs = 4000
		});
	}

	public void Update(float camXf, float camYf, int gameW, int gameH)
	{
		if (_disposed) return;

		// Update player name tags
		var activePlayerIds = new HashSet<string>();
		var playerZ = _gameWorld.Player?.Position.Z ?? 7;

		string localId = _gameWorld.Protocol?.ClientId ?? string.Empty;
		if (string.IsNullOrEmpty(localId))
		{
			localId = "LOCAL";
		}

		if (_gameWorld.Player is not null)
		{
			activePlayerIds.Add(localId);
			if (!_nameTags.TryGetValue(localId, out var label))
			{
				label = new NyxExtendedLabel
				{
					Align = NyxTextAlign.TopCenter,
					DefaultColor = NyxColor.FromRgb(240, 240, 240),
					Font = new NyxFontStyle { SizePt = 11.0f, Outlined = true },
					Phantom = true
				};
				_gamePanel.AddChild(label);
				_gamePanel.MoveChild(label, 0);
				_nameTags[localId] = label;
			}
			label.Text = _gameWorld.Player.Name;

			// Position name tag
			_gameWorld.Player.GetDrawPosition(camXf, camYf, out var px, out var py);
			int z = _gameWorld.Player.Position.Z;
			int dz = playerZ - z;

			if (Math.Abs(dz) > 2)
			{
				label.Visible = false;
			}
			else
			{
				label.Visible = true;
				float floorOffset = dz * 32f;
				var elevPx = GetTileElevationPx(_gameWorld.Player.StackPosition);
				int screenX = (int)(_gamePanel.Bounds.X + px + 16 - 70 - elevPx);
				int screenY = (int)(_gamePanel.Bounds.Y + py - 18 - floorOffset - elevPx);

				// Clamp to game panel bounds
				screenX = Math.Clamp(screenX, _gamePanel.Bounds.X + 5, _gamePanel.Bounds.Right - 140 - 5);
				screenY = Math.Clamp(screenY, _gamePanel.Bounds.Y + 5, _gamePanel.Bounds.Bottom - 18 - 5);

				label.SetBounds(new NyxRect(screenX, screenY, 140, 18));
			}
		}

		foreach (var rp in _gameWorld.RemotePlayers)
		{
			string rpId = rp.ClientId;
			activePlayerIds.Add(rpId);
			if (!_nameTags.TryGetValue(rpId, out var label))
			{
				label = new NyxExtendedLabel
				{
					Align = NyxTextAlign.TopCenter,
					DefaultColor = NyxColor.FromRgb(240, 240, 240),
					Font = new NyxFontStyle { SizePt = 11.0f, Outlined = true },
					Phantom = true
				};
				_gamePanel.AddChild(label);
				_gamePanel.MoveChild(label, 0);
				_nameTags[rpId] = label;
			}
			label.Text = rp.Name;

			// Position name tag
			rp.GetDrawPosition(camXf, camYf, out var px, out var py);
			int z = rp.Position.Z;
			int dz = playerZ - z;

			if (Math.Abs(dz) > 2)
			{
				label.Visible = false;
			}
			else
			{
				label.Visible = true;
				float floorOffset = dz * 32f;
				var elevPx = GetTileElevationPx(rp.Position);
				int screenX = (int)(_gamePanel.Bounds.X + px + 16 - 70 - elevPx);
				int screenY = (int)(_gamePanel.Bounds.Y + py - 18 - floorOffset - elevPx);

				// Clamp to game panel bounds
				screenX = Math.Clamp(screenX, _gamePanel.Bounds.X + 5, _gamePanel.Bounds.Right - 140 - 5);
				screenY = Math.Clamp(screenY, _gamePanel.Bounds.Y + 5, _gamePanel.Bounds.Bottom - 18 - 5);

				label.SetBounds(new NyxRect(screenX, screenY, 140, 18));
			}
		}

		var toRemove = new List<string>();
		foreach (var kvp in _nameTags)
		{
			if (!activePlayerIds.Contains(kvp.Key))
			{
				_gamePanel.RemoveChild(kvp.Value);
				toRemove.Add(kvp.Key);
			}
		}
		foreach (var key in toRemove)
		{
			_nameTags.Remove(key);
		}

		// Update speech bubbles positions and fade outs
		long now = Environment.TickCount64;

		for (int i = _bubbles.Count - 1; i >= 0; i--)
		{
			var bubble = _bubbles[i];
			var elapsed = now - bubble.CreateTime;

			if (elapsed >= bubble.DurationMs)
			{
				_gamePanel.RemoveChild(bubble.Label);
				_bubbles.RemoveAt(i);
				continue;
			}

			// Find current screen coordinates of speaker
			float px = 0f, py = 0f;
			int z = 7;
			bool found = false;
			int elevPx = 0;

			var myId = _gameWorld.Protocol?.ClientId ?? string.Empty;
			if (bubble.SpeakerId == myId || (bubble.SpeakerId == "HOST" && _gameWorld.Protocol?.IsHost == true) || bubble.SpeakerId == "OFFLINE")
			{
				if (_gameWorld.Player is not null)
				{
					_gameWorld.Player.GetDrawPosition(camXf, camYf, out px, out py);
					z = _gameWorld.Player.Position.Z;
					elevPx = GetTileElevationPx(_gameWorld.Player.StackPosition);
					found = true;
				}
			}
			else
			{
				var rp = _gameWorld.RemotePlayers.Find(p => p.ClientId == bubble.SpeakerId);
				if (rp is not null)
				{
					rp.GetDrawPosition(camXf, camYf, out px, out py);
					z = rp.Position.Z;
					elevPx = GetTileElevationPx(rp.Position);
					found = true;
				}
			}

			int dz = playerZ - z;
			// Hide bubble if speaker is not on screen or Z is too far
			if (!found || Math.Abs(dz) > 2)
			{
				bubble.Label.Visible = false;
				continue;
			}

			bubble.Label.Visible = true;

			// Position speech bubble centered above the player's head/name tag
			float floorOffset = dz * 32f;
			int screenX = (int)(_gamePanel.Bounds.X + px + 16 - bubble.Label.Bounds.Width / 2 - elevPx);
			int screenY = (int)(_gamePanel.Bounds.Y + py - 30 - floorOffset - elevPx);

			// Clamp to game panel bounds
			screenX = Math.Clamp(screenX, _gamePanel.Bounds.X + 5, _gamePanel.Bounds.Right - bubble.Label.Bounds.Width - 5);
			screenY = Math.Clamp(screenY, _gamePanel.Bounds.Y + 5, _gamePanel.Bounds.Bottom - bubble.Label.Bounds.Height - 5);

			bubble.Label.SetBounds(new NyxRect(screenX, screenY, bubble.Label.Bounds.Width, bubble.Label.Bounds.Height));

			// Fade out over last 500ms
			var fadeStart = bubble.DurationMs - 500;
			if (elapsed > fadeStart)
			{
				var t = 1.0f - (float)(elapsed - fadeStart) / 500.0f;
				t = Math.Clamp(t, 0f, 1f);
				bubble.Label.Opacity = t;
			}
			else
			{
				bubble.Label.Opacity = 1f;
			}
		}
	}

	private int GetTileElevationPx(Position pos)
	{
		if (_gameWorld.Map is null || _gameWorld.ClientAssets is null)
			return 0;

		var tile = _gameWorld.Map.GetTile(pos);
		if (tile is null)
			return 0;

		var things = _gameWorld.ClientAssets.Things;
		var elevation = 0;
		foreach (var entry in tile.EnumerateStack())
		{
			if (things.TryGetItem(entry.DatId) is not { } thing)
				continue;
			if (!thing.HasElevation)
				continue;
			elevation = Math.Min(elevation + (int)thing.Elevation / 8, 3);
		}
		return elevation * 8;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		foreach (var label in _nameTags.Values)
		{
			_gamePanel.RemoveChild(label);
		}
		_nameTags.Clear();

		foreach (var bubble in _bubbles)
		{
			_gamePanel.RemoveChild(bubble.Label);
		}
		_bubbles.Clear();
	}

	private sealed class SpeechBubble
	{
		public required string SpeakerId { get; set; }
		public required NyxExtendedLabel Label { get; set; }
		public required long CreateTime { get; set; }
		public required int DurationMs { get; set; }
	}
}
