using System;
using System.Collections.Generic;
using NyxGui;
using Sandbox.Items;

namespace Sandbox.UI;

internal class MinimapView : NyxWidget
{
	private const int TileSize = 4;
	private readonly GameMap _map;
	private readonly Dictionary<(int cx, int cy, int z), byte[]> _chunkCache = new();
	private Player? _player;
	private float _camXf;
	private float _camYf;
	private int _gameW;
	private int _gameH;

	public void UpdateCamera(float camXf, float camYf, int gameW, int gameH)
	{
		_camXf = camXf;
		_camYf = camYf;
		_gameW = gameW;
		_gameH = gameH;
		InvalidateRender();
	}

	public MinimapView(GameMap map) : base(0)
	{
		_map = map;
		ZLevel = 7; // Ground level by default

		// Set initial virtual bounds of the widget in virtual world pixels
		// Virtual canvas is effectively infinite — sectors determine actual content.
		SetBounds(new NyxRect(0, 0, int.MaxValue / 2, int.MaxValue / 2));
	}

	public int ZLevel { get; set; }

	public void UpdatePlayer(Player? player)
	{
		_player = player;
		InvalidateRender();
	}

	public void InvalidateCache()
	{
		_chunkCache.Clear();
		InvalidateRender();
	}

	public override void Paint(INyxGuiPainter painter, NyxGuiTheme theme)
	{
		var canvas = Parent as ZoomCanvas;
		if (canvas == null) return;

		// Perform viewport culling: find the visible world area in virtual pixels
		var visibleWorldRect = canvas.CanvasToWorld(canvas.Bounds);

		// Convert virtual pixels to tile coordinates
		int minTileX = Math.Max(0, visibleWorldRect.X / TileSize);
		int minTileY = Math.Max(0, visibleWorldRect.Y / TileSize);
		int maxTileX = (visibleWorldRect.Right + TileSize - 1) / TileSize;
		int maxTileY = (visibleWorldRect.Bottom + TileSize - 1) / TileSize;

		// Convert tile coordinates to chunk indices (each chunk is 32x32 tiles)
		int minChunkX = minTileX / 32;
		int minChunkY = minTileY / 32;
		int maxChunkX = maxTileX / 32;
		int maxChunkY = maxTileY / 32;

		for (int cy = minChunkY; cy <= maxChunkY; cy++)
		{
			for (int cx = minChunkX; cx <= maxChunkX; cx++)
			{
				var chunkData = GetChunkData(cx, cy, ZLevel);

				// Chunk virtual bounds:
				var chunkWorldRect = new NyxRect(cx * 32 * TileSize, cy * 32 * TileSize, 32 * TileSize, 32 * TileSize);

				// Map virtual bounds to screen bounds:
				var chunkScreenRect = canvas.WorldToCanvas(chunkWorldRect);

				// Draw using efficient 32x32 hardware sprite caching
				uint cacheKey = (uint)((cx & 0x7FFF) | ((cy & 0x7FFF) << 15) | ((ZLevel & 0xF) << 30));
				painter.DrawSprite32(chunkScreenRect, chunkData, cacheKey);
			}
		}

		// Draw the player indicator on top
		if (_player != null && ZLevel == 7)
		{
			DrawPlayerIndicator(painter, canvas, _player);
		}

		// Draw camera viewport box
		if (_gameW > 0 && _gameH > 0 && ZLevel == 7)
		{
			float vcx = _camXf * TileSize;
			float vcy = _camYf * TileSize;
			float vcw = (_gameW / 32f) * TileSize;
			float vch = (_gameH / 32f) * TileSize;

			var camWorldRect = new NyxRect((int)Math.Round(vcx), (int)Math.Round(vcy), (int)Math.Round(vcw), (int)Math.Round(vch));
			var camScreenRect = canvas.WorldToCanvas(camWorldRect);

			var boxColor = NyxColor.FromRgb(255, 255, 0); // Yellow box for camera viewport
			painter.DrawRect(camScreenRect, boxColor, 1);
		}
	}

	private byte[] GetChunkData(int cx, int cy, int z)
	{
		var key = (cx, cy, z);
		if (_chunkCache.TryGetValue(key, out var cachedData))
			return cachedData;

		var data = new byte[32 * 32 * 4];

		for (int y = 0; y < 32; y++)
		{
			int tileY = cy * 32 + y;
			for (int x = 0; x < 32; x++)
			{
				int tileX = cx * 32 + x;
				int offset = (y * 32 + x) * 4;

				if (z == 7 && _map.IsInside(new Position(tileX, tileY, 7)))
				{
					var tile = _map.GetTile(new Position(tileX, tileY, 7));
					byte colorId = GetTileMinimapColor(tile);
					var color = GetMinimapColor(colorId);

					data[offset] = color.R;
					data[offset + 1] = color.G;
					data[offset + 2] = color.B;
					data[offset + 3] = 255;
				}
				else
				{
					// Unexplored tiles are drawn black
					data[offset] = 0;
					data[offset + 1] = 0;
					data[offset + 2] = 0;
					data[offset + 3] = 255;
				}
			}
		}

		_chunkCache[key] = data;
		return data;
	}

	private byte GetTileMinimapColor(Tile tile)
	{
		byte color = 0;
		if (!tile.Ground.IsEmpty)
		{
			color = tile.Ground.GetItemType().MinimapColor;
		}

		foreach (var item in tile.Items)
		{
			if (!item.IsEmpty)
			{
				var itemColor = item.GetItemType().MinimapColor;
				if (itemColor != 0)
				{
					color = itemColor;
				}
			}
		}

		return color;
	}

	private NyxColor GetMinimapColor(byte colorId)
	{
		if (colorId == 0) return NyxColor.FromRgb(0, 0, 0);

		int index = colorId;
		if (index >= 216) index = index % 216;

		int b = (index % 6) * 51;
		int g = ((index / 6) % 6) * 51;
		int r = ((index / 36) % 6) * 51;

		return NyxColor.FromRgb((byte)r, (byte)g, (byte)b);
	}

	private void DrawPlayerIndicator(INyxGuiPainter painter, ZoomCanvas canvas, Player player)
	{
		float wx = player.Position.X * TileSize + TileSize / 2f;
		float wy = player.Position.Y * TileSize + TileSize / 2f;
		var (cx, cy) = canvas.WorldToCanvas(wx, wy);

		int icx = (int)MathF.Round(cx);
		int icy = (int)MathF.Round(cy);

		var crossColor = NyxColor.FromRgb(255, 0, 0);
		painter.FillRect(new NyxRect(icx - 3, icy - 1, 7, 3), crossColor);
		painter.FillRect(new NyxRect(icx - 1, icy - 3, 3, 7), crossColor);
		painter.FillRect(new NyxRect(icx - 1, icy - 1, 3, 3), NyxColor.FromRgb(255, 255, 255));
	}
}
