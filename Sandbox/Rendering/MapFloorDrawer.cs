using System.Buffers;
using NyxDrawer;
using NyxDrawer.Items;
using NyxAssets.Things.Frames;
using NyxAssets.Client;
using NyxAssets.Things;

namespace Sandbox;

/// <summary>
/// NyxClient <c>MapView::drawFloor</c> / <c>Tile::drawGround|drawBottom|drawCreatures|drawTop</c>
/// (ground-first path with lying-object top correction).
/// </summary>
internal static class MapFloorDrawer
{
	private const int MaxElevation = 24;
	private const int ElevationPx = 8;
	private const float TilePx = 32f;

	/// <summary>Max floors closer to sky to render above player.</summary>
	private const int MaxFloorsAbove = 4;

	/// <summary>Max floors deeper underground to render below player.</summary>
	private const int MaxFloorsBelow = 4;

	[ThreadStatic]
	private static int[]? _cachedTopCorrection;
	[ThreadStatic]
	private static int[]? _cachedTopDraws;
	[ThreadStatic]
	private static CachedTile[]? _cachedTileCache;

	public static unsafe void Draw(
		ClientAssetBundle assets,
		AssetDrawer drawer,
		NyxGameMap.GameMap map,
		float camXf,
		float camYf,
		int winW,
		int winH,
		Position playerPos,
		Action<Position, float, float, int> drawCreaturesAtTile)
	{
		var things = assets.Things;
		// Viewport tile range for array sizing (worst case, not per-z).
		var viewTilesX = (int)Math.Ceiling(winW / TilePx) + 10;
		var viewTilesY = (int)Math.Ceiling(winH / TilePx) + 10;
		var stride = viewTilesX;
		var required = stride * viewTilesY;

		if (_cachedTopCorrection == null || _cachedTopCorrection.Length < required)
			_cachedTopCorrection = new int[required];
		if (_cachedTopDraws == null || _cachedTopDraws.Length < required)
			_cachedTopDraws = new int[required];
		if (_cachedTileCache == null || _cachedTileCache.Length < required)
			_cachedTileCache = new CachedTile[required];

		var topCorrection = _cachedTopCorrection;
		var topDraws = _cachedTopDraws;
		var tileCache = _cachedTileCache;

		Span<TileStackEntry> tempStack = stackalloc TileStackEntry[64];

		// Compute z-range with floor limits.
			int ceilingZ = FindCeilingZ(map, things, playerPos, 0);
			int startZ = playerPos.Z > 7 ? playerPos.Z + 2 : 7;
			startZ = Math.Clamp(startZ, 0, 15);
			if (startZ < ceilingZ)
				startZ = ceilingZ;
			int endZ = ceilingZ;

			for (var z = startZ; z >= endZ; z--)
			{

				var dz = playerPos.Z - z;
				var minVisualTx = (int)Math.Floor(camXf) - 3;
				var minVisualTy = (int)Math.Floor(camYf) - 3;
				var maxVisualTx = (int)Math.Ceiling(camXf + winW / TilePx) + 3;
				var maxVisualTy = (int)Math.Ceiling(camYf + winH / TilePx) + 3;

				var minTx = Math.Max(0, minVisualTx + dz);
				var minTy = Math.Max(0, minVisualTy + dz);
				var maxTx = Math.Max(0, maxVisualTx + dz);
				var maxTy = Math.Max(0, maxVisualTy + dz);

				// Only zero the slice we use (not the entire rented array).
				topCorrection.AsSpan(0, required).Clear();
				topDraws.AsSpan(0, required).Clear();
				tileCache.AsSpan(0, required).Clear();

				// Populate tile cache for the current floor
				for (var ty = minTy; ty <= maxTy; ty++)
				{
					for (var tx = minTx; tx <= maxTx; tx++)
					{
						var idx = (ty - minTy) * stride + (tx - minTx);
						var tile = map.GetTile(new Position(tx, ty, z));
						tileCache[idx].Tile = tile;

						int cnt = tile.FillStack(tempStack);
						tileCache[idx].StackCount = cnt;
						for (int i = 0; i < cnt; i++)
						{
							tileCache[idx].Stack[i] = ((uint)tempStack[i].DatId << 16) | tempStack[i].Count;
						}
					}
				}

				var (maxRedrawW, maxRedrawH) = things.GetMaxLyingItemRedrawSpan();
				if (maxRedrawW > 0 || maxRedrawH > 0)
				{
					CalculateTopCorrections(
						map, z, things, topCorrection, tileCache,
						minTx, minTy, stride,
						maxTx + maxRedrawW,
						maxTy + maxRedrawH,
						maxTx,
						maxTy);
				}

				// Reusable tile-stack buffer is no longer needed on heap as we stackalloc for each tile.
				for (var ty = minTy; ty <= maxTy; ty++)
				{
					var visualTy = ty - dz;
					var sy = (visualTy - camYf) * TilePx;
					if (sy >= winH || sy + TilePx <= 0f)
						continue;

					for (var tx = minTx; tx <= maxTx; tx++)
					{
						var visualTx = tx - dz;
						var sx = (visualTx - camXf) * TilePx;
						if (sx >= winW || sx + TilePx <= 0f)
							continue;

						var dest = new Point((int)sx, (int)sy);
						var idx = (ty - minTy) * stride + (tx - minTx);
						DrawGround(things, drawer, ref tileCache[idx], dest);
					}
				}

				for (var ty = minTy; ty <= maxTy; ty++)
				{
					var visualTy = ty - dz;
					var sy = (visualTy - camYf) * TilePx;
					if (sy >= winH || sy + TilePx <= 0f)
						continue;

					for (var tx = minTx; tx <= maxTx; tx++)
					{
						var visualTx = tx - dz;
						var sx = (visualTx - camXf) * TilePx;
						if (sx >= winW || sx + TilePx <= 0f)
							continue;

						var dest = new Point((int)sx, (int)sy);
						var idx = (ty - minTy) * stride + (tx - minTx);
						topDraws[idx] = 0;
						var tileElevation = DrawBottom(
							things, drawer, ref tileCache[idx], tileCache, map, z, dz, topCorrection, topDraws,
							minTx, minTy, maxTx, maxTy, stride,
							tx, ty, visualTx, visualTy, dest, camXf, camYf, winW, winH, drawCreaturesAtTile);
						DrawCreaturesPass(topCorrection, topDraws, idx, new Position(tx, ty, z), dest, drawCreaturesAtTile, tileElevation);
						DrawTop(things, drawer, ref tileCache[idx], z, topCorrection, topDraws, minTx, minTy, stride, tx, ty, dest, drawCreaturesAtTile, tileElevation);
					}
				}
			}
	}

	private static void CalculateTopCorrections(
		NyxGameMap.GameMap map,
		int z,
		ThingCatalog things,
		int[] topCorrection,
		CachedTile[] tileCache,
		int minTx,
		int minTy,
		int stride,
		int maxTx,
		int maxTy,
		int cacheMaxTx,
		int cacheMaxTy)
	{
		for (var ty = minTy; ty <= maxTy; ty++)
		{
			for (var tx = minTx; tx <= maxTx; tx++)
			{
				var tile = (tx >= minTx && tx <= cacheMaxTx && ty >= minTy && ty <= cacheMaxTy)
					? tileCache[(ty - minTy) * stride + (tx - minTx)].Tile
					: map.GetTile(new Position(tx, ty, z));
				var items = tile.Items;
				if (items.Count == 0)
					continue;

				var redrawW = 0;
				var redrawH = 0;
				var hasLying = false;

				for (var i = 0; i < items.Count; i++)
				{
					var itemId = items[i].ItemTypeId;
					if (things.TryGetItem(itemId) is not { } thing || !thing.IsLyingObject)
						continue;

					var (w, h) = GetThingSize(thing);
					redrawW = Math.Max(w - 1, redrawW);
					redrawH = Math.Max(h - 1, redrawH);
					hasLying = true;
				}

				if (hasLying)
				{
					for (var ox = -redrawW; ox <= 0; ox++)
					{
						for (var oy = -redrawH; oy <= 0; oy++)
						{
							if (ox == 0 && oy == 0)
								continue;
							var nx = tx + ox;
							var ny = ty + oy;
							if (!map.IsInside(new Position(nx, ny, z)))
								continue;
							var corrIdx = (ny - minTy) * stride + (nx - minTx);
							if (corrIdx >= 0 && corrIdx < topCorrection.Length)
								topCorrection[corrIdx]++;
						}
					}
				}
			}
		}
	}

	private static unsafe void DrawGround(
		ThingCatalog things,
		AssetDrawer drawer,
		ref CachedTile tile,
		Point dest)
	{
		var elevation = 0;
		for (var i = 0; i < tile.StackCount; i++)
		{
			uint packed = tile.Stack[i];
			ushort datId = (ushort)(packed >> 16);
			ushort count = (ushort)(packed & 0xFFFF);
			if (things.TryGetItem(datId) is not { } thing)
				continue;
			if (!thing.IsGround && !thing.IsGroundBorder && !thing.IsOnBottom)
				break;
			DrawItem(drawer, thing, count, dest, elevation);
			elevation = AddElevation(elevation, thing);
		}
	}

	private static unsafe int DrawBottom(
		ThingCatalog things,
		AssetDrawer drawer,
		ref CachedTile tile,
		CachedTile[] tileCache,
		NyxGameMap.GameMap map,
		int z,
		int dz,
		int[] topCorrection,
		int[] topDraws,
		int minTx,
		int minTy,
		int maxTx,
		int maxTy,
		int stride,
		int tileX,
		int tileY,
		int visualTileX,
		int visualTileY,
		Point dest,
		float camXf,
		float camYf,
		int winW,
		int winH,
		Action<Position, float, float, int> drawCreaturesAtTile)
	{
		var elevation = 0;

		var afterBottom = false;
		for (var i = 0; i < tile.StackCount; i++)
		{
			uint packed = tile.Stack[i];
			ushort datId = (ushort)(packed >> 16);
			ushort count = (ushort)(packed & 0xFFFF);
			if (things.TryGetItem(datId) is not { } thing)
				continue;
			if (thing.IsOnBottom)
				afterBottom = true;
			if (!thing.IsGround && !thing.IsGroundBorder && !thing.IsOnBottom)
				break;
			if (!afterBottom)
				continue;
			DrawItem(drawer, thing, count, dest, elevation);
			elevation = AddElevation(elevation, thing);
		}

		var redrawW = 0;
		var redrawH = 0;
		var stopDrawing = false;
		for (var i = tile.StackCount - 1; i >= 0; i--)
		{
			uint packed = tile.Stack[i];
			ushort datId = (ushort)(packed >> 16);
			ushort count = (ushort)(packed & 0xFFFF);
			if (things.TryGetItem(datId) is not { } thing)
				continue;
			if (thing.IsLyingObject)
			{
				var (w, h) = GetThingSize(thing);
				redrawW = Math.Max(w - 1, redrawW);
				redrawH = Math.Max(h - 1, redrawH);
			}

			if (thing.IsOnTop || thing.IsOnBottom || thing.IsGroundBorder || thing.IsGround)
				stopDrawing = true;
			if (stopDrawing)
				continue;

			DrawItem(drawer, thing, count, dest, elevation);
			elevation = AddElevation(elevation, thing);
		}

		for (var ox = -redrawW; ox <= 0; ox++)
		{
			for (var oy = -redrawH; oy <= 0; oy++)
			{
				if (ox == 0 && oy == 0)
					continue;
				var nx = tileX + ox;
				var ny = tileY + oy;
				if (!map.IsInside(new Position(nx, ny, z)))
					continue;
				var visualNx = nx - dz;
				var visualNy = ny - dz;
				if (!IsOnScreen(visualNx, visualNy, camXf, camYf, winW, winH, out var neighborDest))
					continue;

				var nIdx = (ny - minTy) * stride + (nx - minTx);
				if (nIdx >= 0 && nIdx < topCorrection.Length)
				{
					if (nx >= minTx && nx <= maxTx && ny >= minTy && ny <= maxTy)
					{
						var nElevation = GetTileElevation(ref tileCache[nIdx], things);
						DrawCreaturesPass(topCorrection, topDraws, nIdx, new Position(nx, ny, z), neighborDest, drawCreaturesAtTile, nElevation);
						DrawTop(things, drawer, ref tileCache[nIdx], z, topCorrection, topDraws, minTx, minTy, stride, nx, ny, neighborDest, drawCreaturesAtTile, nElevation);
					}
					else
					{
						var nTile = map.GetTile(new Position(nx, ny, z));
						var ct = CacheTileOnFly(nTile);
						var nElevation = GetTileElevation(ref ct, things);
						DrawCreaturesPass(topCorrection, topDraws, nIdx, new Position(nx, ny, z), neighborDest, drawCreaturesAtTile, nElevation);
						DrawTop(things, drawer, ref ct, z, topCorrection, topDraws, minTx, minTy, stride, nx, ny, neighborDest, drawCreaturesAtTile, nElevation);
					}
				}
			}
		}

		return elevation;
	}

	private static void DrawCreaturesPass(
		int[] topCorrection,
		int[] topDraws,
		int tileIndex,
		Position pos,
		Point dest,
		Action<Position, float, float, int> drawCreaturesAtTile,
		int elevation = 0)
	{
		if (topDraws[tileIndex] < topCorrection[tileIndex])
			return;
		DrawCreaturesOnTile(pos, dest, elevation, drawCreaturesAtTile);
	}

	private static unsafe void DrawTop(
		ThingCatalog things,
		AssetDrawer drawer,
		ref CachedTile tile,
		int z,
		int[] topCorrection,
		int[] topDraws,
		int minTx,
		int minTy,
		int stride,
		int tileX,
		int tileY,
		Point dest,
		Action<Position, float, float, int> drawCreaturesAtTile,
		int elevation)
	{
		var idx = (tileY - minTy) * stride + (tileX - minTx);
		if (idx < 0 || idx >= topCorrection.Length) return;
		if (topDraws[idx]++ < topCorrection[idx])
			return;

		DrawCreaturesOnTile(new Position(tileX, tileY, z), dest, elevation, drawCreaturesAtTile);

		for (var i = 0; i < tile.StackCount; i++)
		{
			uint packed = tile.Stack[i];
			ushort datId = (ushort)(packed >> 16);
			ushort count = (ushort)(packed & 0xFFFF);
			if (things.TryGetItem(datId) is not { } thing || !thing.IsOnTop)
				continue;
			DrawItem(drawer, thing, count, dest, 0);
		}
	}

	private static void DrawCreaturesOnTile(
		Position pos,
		Point dest,
		int elevation,
		Action<Position, float, float, int> drawCreaturesAtTile)
	{
		var elevPx = elevation * ElevationPx;
		drawCreaturesAtTile(pos, dest.X, dest.Y, elevPx);
	}

	private static void DrawItem(AssetDrawer drawer, ThingType thing, ushort count, Point dest, int elevation)
	{
		if (thing.FrameGroups.Count == 0)
			return;

		var fg = thing.FrameGroups[0];
		ItemStackPatterns.Resolve(fg, thing.Stackable, count, out var patternX, out var patternY);

		drawer.Items.Draw(new ItemDrawRequest
		{
			Item = thing,
			AnchorX = dest.X - elevation * ElevationPx,
			AnchorY = dest.Y - elevation * ElevationPx,
			PatternX = patternX,
			PatternY = patternY,
		});
	}

	private static unsafe int GetTileElevation(ref CachedTile tile, ThingCatalog things)
	{
		var elevation = 0;
		for (var i = 0; i < tile.StackCount; i++)
		{
			uint packed = tile.Stack[i];
			ushort datId = (ushort)(packed >> 16);
			if (things.TryGetItem(datId) is not { } thing)
				continue;
			elevation = AddElevation(elevation, thing);
		}
		return elevation;
	}

	private static int GetTileElevation(ThingCatalog things, Tile tile)
	{
		Span<TileStackEntry> stack = stackalloc TileStackEntry[64];
		var stackCount = tile.FillStack(stack);
		var elevation = 0;
		for (var i = 0; i < stackCount; i++)
		{
			var entry = stack[i];
			if (things.TryGetItem(entry.DatId) is not { } thing)
				continue;
			elevation = AddElevation(elevation, thing);
		}
		return elevation;
	}

	private static unsafe CachedTile CacheTileOnFly(Tile tile)
	{
		CachedTile ct = default;
		ct.Tile = tile;
		Span<TileStackEntry> tempStack = stackalloc TileStackEntry[64];
		int cnt = tile.FillStack(tempStack);
		ct.StackCount = cnt;
		for (int i = 0; i < cnt; i++)
		{
			ct.Stack[i] = ((uint)tempStack[i].DatId << 16) | tempStack[i].Count;
		}
		return ct;
	}

	private static int AddElevation(int elevation, ThingType thing)
	{
		if (!thing.HasElevation)
			return elevation;
		return Math.Min(elevation + (int)thing.Elevation / ElevationPx, MaxElevation / ElevationPx);
	}

	private static (int Width, int Height) GetThingSize(ThingType thing)
	{
		if (thing.FrameGroups.Count == 0)
			return (1, 1);
		var fg = thing.FrameGroups[0];
		return ((int)(fg.Width == 0 ? 1 : fg.Width), (int)(fg.Height == 0 ? 1 : fg.Height));
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static bool IsOnScreen(int tileX, int tileY, float camXf, float camYf, int winW, int winH)
	{
		var sx = (tileX - camXf) * TilePx;
		var sy = (tileY - camYf) * TilePx;
		return sx < winW && sy < winH && sx + TilePx > 0f && sy + TilePx > 0f;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static bool IsOnScreen(int tileX, int tileY, float camXf, float camYf, int winW, int winH, out Point dest)
	{
		var sx = (tileX - camXf) * TilePx;
		var sy = (tileY - camYf) * TilePx;
		dest = new Point((int)sx, (int)sy);
		return sx < winW && sy < winH && sx + TilePx > 0f && sy + TilePx > 0f;
	}

	/// <summary>
	/// Finds the lowest z that should be rendered. Floors with z &lt; ceilingZ
	/// (physically above the ceiling, closer to sky) are hidden.
	/// e.g. player at z=6, solid ceiling at z=5 → returns 6, so z=5,4,3... are skipped.
	/// </summary>
	private static int FindCeilingZ(NyxGameMap.GameMap map, ThingCatalog things, Position playerPos, int endZ)
	{
		int firstFloor = 0;
		if (playerPos.Z > 7)
		{
			firstFloor = Math.Max(playerPos.Z - 2, 8);
		}
		else
		{
			firstFloor = Math.Max(0, playerPos.Z - MaxFloorsAbove);
		}

		// loop in 3x3 tiles around the player/camera
		for (int ix = -1; ix <= 1 && firstFloor < playerPos.Z; ++ix)
		{
			for (int iy = -1; iy <= 1 && firstFloor < playerPos.Z; ++iy)
			{
				var pos = new Position(playerPos.X + ix, playerPos.Y + iy, playerPos.Z);

				// process tiles that we can look through
				bool isCardinal = Math.Abs(ix) != Math.Abs(iy);
				bool isLookPossiblePos = IsLookPossible(map, things, pos);
				if ((ix == 0 && iy == 0) || (isCardinal && isLookPossiblePos))
				{
					Position upperPos = pos;
					Position coveredPos = pos;

					while (true)
					{
						// coveredUp: z--, x++, y++
						if (coveredPos.Z <= 0) break;
						coveredPos = new Position(coveredPos.X + 1, coveredPos.Y + 1, coveredPos.Z - 1);

						// up: z--
						if (upperPos.Z <= 0) break;
						upperPos = new Position(upperPos.X, upperPos.Y, upperPos.Z - 1);

						// upperPos.z >= firstFloor
						if (upperPos.Z < firstFloor) break;

						// check tiles physically above
						if (map.IsInside(upperPos))
						{
							if (LimitsFloorsView(map, things, upperPos, !isLookPossiblePos))
							{
								firstFloor = upperPos.Z + 1;
								break;
							}
						}

						// check tiles geometrically above
						if (map.IsInside(coveredPos))
						{
							if (LimitsFloorsView(map, things, coveredPos, isLookPossiblePos))
							{
								firstFloor = coveredPos.Z + 1;
								break;
							}
						}
					}
				}
			}
		}

		return Math.Clamp(firstFloor, 0, 15);
	}

	private static bool IsLookPossible(NyxGameMap.GameMap map, ThingCatalog things, Position pos)
	{
		if (!map.IsInside(pos))
			return true;

		var tile = map.GetTile(pos);
		Span<TileStackEntry> stack = stackalloc TileStackEntry[64];
		var stackCount = tile.FillStack(stack);
		for (var i = 0; i < stackCount; i++)
		{
			var entry = stack[i];
			if (things.TryGetItem(entry.DatId) is { } thing && thing.BlockMissile)
				return false;
		}

		return true;
	}

	private static bool LimitsFloorsView(NyxGameMap.GameMap map, ThingCatalog things, Position pos, bool isFreeView)
	{
		if (!map.IsInside(pos))
			return false;

		var tile = map.GetTile(pos);
		ushort firstThingId = GetFirstThingId(tile);
		if (firstThingId == 0)
			return false;

		var firstThing = things.TryGetItem(firstThingId);
		if (firstThing == null)
			return false;

		if (isFreeView)
		{
			if (!firstThing.DontHide && (firstThing.IsGround || firstThing.IsOnBottom))
				return true;
		}
		else
		{
			if (!firstThing.DontHide && (firstThing.IsGround || (firstThing.IsOnBottom && firstThing.BlockMissile)))
				return true;
		}

		return false;
	}

	private static ushort GetFirstThingId(Tile tile)
	{
		if (!tile.Ground.IsEmpty)
			return (ushort)tile.Ground.ItemTypeId;
		if (tile.Items.Count > 0)
			return (ushort)tile.Items[0].ItemTypeId;
		return 0;
	}

	/// <summary>
	/// Checks if the tile at pos is a solid ceiling.
	/// Any non-translucent item or built floor blocks view upward.
	/// Natural ground (grass, dirt) does NOT block — you can see the sky through it.
	/// </summary>
	private static bool IsSolidCeiling(NyxGameMap.GameMap map, ThingCatalog things, Position pos)
	{
		if (!map.IsInside(pos))
			return false;

		var tile = map.GetTile(pos);

		// Ground tiles: built floors (wood, stone, etc.) ARE solid ceilings.
		// Natural ground (grass, dirt) is not — it means open sky.
		if (tile.GroundId != 0)
		{
			if (things.TryGetItem(tile.GroundId) is { } groundThing)
			{
				if (groundThing.DontHide || groundThing.IsTranslucent)
					return false;

				// Built floors block view upward. We detect them by the fact that
				// they are NOT natural ground — natural ground in this demo is
				// grass (100) and dirt (101). Anything else is a built floor.
				if (tile.GroundId != 100 && tile.GroundId != 101)
					return true;
			}
			else
			{
				// Unknown ground — treat as solid to be safe.
				return true;
			}
		}

		// Items: walls, roofs, etc. block view.
		Span<TileStackEntry> stack = stackalloc TileStackEntry[64];
		var stackCount = tile.FillStack(stack);

		for (var i = 0; i < stackCount; i++)
		{
			var entry = stack[i];
			if (things.TryGetItem(entry.DatId) is not { } thing)
				continue;

			if (thing.DontHide || thing.IsTranslucent)
				continue;

			return true;
		}

		return false;
	}

	private readonly struct Point(int x, int y)
	{
		public int X { get; } = x;
		public int Y { get; } = y;
	}

	private unsafe struct CachedTile
	{
		public Tile Tile;
		public int StackCount;
		public fixed uint Stack[64];
	}
}
