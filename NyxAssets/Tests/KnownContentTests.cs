using Xunit;
using System.IO;
using NyxAssets.Things;
using NyxAssets.Sprites;
using NyxAssets.Client;

namespace NyxAssets.Tests;

/// <summary>
/// Content-specific tests that assert known, hand-authored values baked into the
/// fixture .spr and .dat files.  These act as golden-file checks: if parsing or
/// codec changes accidentally corrupt a value, these tests catch it immediately.
/// </summary>
public class KnownContentTests
{
	private static string GetFixturePath(params string[] paths)
	{
		var baseDir = AppContext.BaseDirectory;
		return Path.Combine(baseDir, "Fixtures", Path.Combine(paths));
	}

	/// <summary>
	/// Verifies every pixel of a decoded 32×32 RGBA buffer matches the expected color.
	/// </summary>
	private static bool IsFullSolidSquare(byte[] rgba, byte r, byte g, byte b, byte a = 255)
	{
		for (var i = 0; i < rgba.Length; i += 4)
		{
			if (rgba[i] != r || rgba[i + 1] != g || rgba[i + 2] != b || rgba[i + 3] != a)
				return false;
		}
		return true;
	}

	private static (ThingCatalog catalog, string label) LoadCatalog(string version)
	{
		var datPath = GetFixturePath("ClientAssets", version, $"Test_{version}.dat");
		var otfiPath = GetFixturePath("ClientAssets", version, $"Test_{version}.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		return (ThingCatalog.Load(File.ReadAllBytes(datPath), otfi.ToReadOptions()), version);
	}

	// ── SPR content ──────────────────────────────────────────────────────────

	/// <summary>
	/// Sprites 8, 9, and 10 in both archives are fully-filled 32×32 solid-colour squares.
	/// They correspond to the three reference items 109, 110, 111 in the DAT files.
	/// </summary>
	[Theory]
	[InlineData("860",  8u,  0xFF, 0xC8, 0x00)]  // sprite 8  → #FFC800 (gold)
	[InlineData("860",  9u,  0x00, 0xC0, 0xFF)]  // sprite 9  → #00C0FF (sky-blue)
	[InlineData("860",  10u, 0xFF, 0x00, 0x00)]  // sprite 10 → #FF0000 (red)
	[InlineData("1098", 8u,  0xFF, 0xC8, 0x00)]
	[InlineData("1098", 9u,  0x00, 0xC0, 0xFF)]
	[InlineData("1098", 10u, 0xFF, 0x00, 0x00)]
	public void Sprite_IsFullSolidColorSquare(string version, uint spriteId, byte r, byte g, byte b)
	{
		var sprPath = GetFixturePath("ClientAssets", version, $"Test_{version}.spr");
		using var archive = SpriteArchive.Load(
			File.ReadAllBytes(sprPath),
			extendedSpriteIds: true,
			transparentPixels: true);

		var dest = new byte[SpritePixelCodec.RgbaBufferLength];
		Assert.True(
			archive.TryDecodeSpriteById(spriteId, dest),
			$"[{version}] Sprite {spriteId} is empty or out of range.");

		Assert.True(
			IsFullSolidSquare(dest, r, g, b),
			$"[{version}] Sprite {spriteId} is not a solid #{r:X2}{g:X2}{b:X2} square. " +
			$"First pixel: R={dest[0]} G={dest[1]} B={dest[2]} A={dest[3]}");
	}

	// ── DAT item → sprite-id mapping ─────────────────────────────────────────

	/// <summary>
	/// Items 109–111 are 1×1 reference tiles whose single sprite IDs map directly
	/// to the three colour sprites (8 = gold, 9 = sky-blue, 10 = red).
	/// </summary>
	[Theory]
	[InlineData("860",  109u, 8u)]
	[InlineData("860",  110u, 9u)]
	[InlineData("860",  111u, 10u)]
	[InlineData("1098", 109u, 8u)]
	[InlineData("1098", 110u, 9u)]
	[InlineData("1098", 111u, 10u)]
	public void Item_HasExpectedSolidColorSpriteId(string version, uint itemId, uint expectedSpriteId)
	{
		var (catalog, _) = LoadCatalog(version);
		var item = catalog.GetItem(itemId);

		Assert.Single(item.FrameGroups);
		Assert.Single(item.FrameGroups[0].SpriteIds);
		Assert.Equal(expectedSpriteId, item.FrameGroups[0].SpriteIds[0]);
	}

	// ── DAT item property assertions ─────────────────────────────────────────

	/// <summary>Item 102 is an unpassable, immovable automap object (minimap color 114).</summary>
	[Theory]
	[InlineData("860")]
	[InlineData("1098")]
	public void Item102_IsUnpassable_IsUnmoveable_HasAutomap114(string version)
	{
		var (catalog, _) = LoadCatalog(version);
		var item = catalog.GetItem(102);

		Assert.True(item.IsUnpassable, $"[{version}] Item 102 should be unpassable.");
		Assert.True(item.IsUnmoveable, $"[{version}] Item 102 should be unmoveable.");
		Assert.True(item.MiniMap, $"[{version}] Item 102 should have a minimap marker.");
		Assert.Equal(114u, item.MiniMapColor);
	}

	/// <summary>Item 104 (5th item) is a market item: King's Boots, vocation 2, level 40.</summary>
	[Fact]
	public void Item104_IsMarketItem_KingsBoots_Vocation2_Level40()
	{
		// Market data is a 1098-era feature; test only the 1098 fixture
		var (catalog, _) = LoadCatalog("1098");
		var item = catalog.GetItem(104);

		Assert.True(item.IsMarketItem);
		Assert.Equal("kings boots", item.MarketName, ignoreCase: true);
		Assert.Equal(2u, item.MarketRestrictProfession);  // vocation 2
		Assert.Equal(40u, item.MarketRestrictLevel);      // level 40
		// MarketCategory 3 = Boots category as stored in this fixture
		Assert.Equal(3u, item.MarketCategory);
	}

	// ── DAT outfit layout assertions ──────────────────────────────────────────

	/// <summary>
	/// 860 catalog contains exactly 2 outfits.
	/// The 2nd outfit has PatternX=4 (four directions) and 1 animation frame (idle only).
	/// 860 protocol has no OutfitFrameGroups so there is exactly one frame group.
	/// </summary>
	[Fact]
	public void Outfits_860_Count2_SecondOutfit_PatternX4_OneFrame()
	{
		var (catalog, _) = LoadCatalog("860");

		Assert.Equal(2u, catalog.OutfitCount);

		var outfit2 = catalog.GetOutfit(2);
		Assert.Single(outfit2.FrameGroups);
		Assert.Equal(4u, outfit2.FrameGroups[0].PatternX);
		Assert.Equal(1u, outfit2.FrameGroups[0].Frames);
	}

	/// <summary>
	/// 1098 catalog contains exactly 2 outfits.
	/// The 2nd outfit has two frame groups (idle + walk).
	/// Frame group 0 (idle) has 1 animation frame.
	/// Frame group 1 (walk) has 2 animation frames.
	/// </summary>
	[Fact]
	public void Outfits_1098_Count2_SecondOutfit_TwoFrameGroups_CorrectFrameCounts()
	{
		var (catalog, _) = LoadCatalog("1098");

		Assert.Equal(2u, catalog.OutfitCount);

		var outfit2 = catalog.GetOutfit(2);
		Assert.Equal(2, outfit2.FrameGroups.Count);
		Assert.Equal(1u, outfit2.FrameGroups[0].Frames);  // idle: 1 frame
		Assert.Equal(2u, outfit2.FrameGroups[1].Frames);  // walk: 2 frames
	}

	// ── DAT effects and missiles assertions ──────────────────────────────────

	[Theory]
	[InlineData("860")]
	[InlineData("1098")]
	public void Effects_VerifySecondEffect_SpriteIdsAndTimings(string version)
	{
		var (catalog, _) = LoadCatalog(version);

		Assert.Equal(2u, catalog.EffectCount);

		var effect2 = catalog.GetEffect(2);
		Assert.Single(effect2.FrameGroups);

		var fg = effect2.FrameGroups[0];
		Assert.Equal(4u, fg.Frames);
		Assert.Equal(new uint[] { 26, 27, 28, 29 }, fg.SpriteIds);

		if (version == "1098" && fg.FrameTimings != null && fg.FrameTimings.Length >= 4)
		{
			// 2nd animation frame timing (index 1)
			Assert.Equal(200u, fg.FrameTimings[1].MinimumMilliseconds);
			Assert.Equal(200u, fg.FrameTimings[1].MaximumMilliseconds);

			// 3rd animation frame timing (index 2)
			Assert.Equal(300u, fg.FrameTimings[2].MinimumMilliseconds);
			Assert.Equal(300u, fg.FrameTimings[2].MaximumMilliseconds);

			// 4th animation frame timing (index 3)
			Assert.Equal(300u, fg.FrameTimings[3].MinimumMilliseconds);
			Assert.Equal(300u, fg.FrameTimings[3].MaximumMilliseconds);
		}
	}

	[Theory]
	[InlineData("860")]
	[InlineData("1098")]
	public void Missiles_VerifyFirstMissile_PropertiesAndSprites(string version)
	{
		var (catalog, _) = LoadCatalog(version);

		Assert.Equal(1u, catalog.MissileCount);

		var missile1 = catalog.GetMissile(1);
		Assert.Single(missile1.FrameGroups);

		var fg = missile1.FrameGroups[0];
		Assert.Equal(3u, fg.PatternX);
		Assert.Equal(3u, fg.PatternY);

		// Missile uses sprite IDs 30-37. Filter out 0 (empty slots).
		var nonZeroSprites = fg.SpriteIds.Where(id => id != 0).OrderBy(id => id).ToArray();
		var expectedSprites = Enumerable.Range(30, 8).Select(id => (uint)id).ToArray();
		Assert.Equal(expectedSprites, nonZeroSprites);
	}

	// ── Binary Compilation Round-trips ───────────────────────────────────────

	[Theory]
	[InlineData("860")]
	[InlineData("1098")]
	public void DatFile_Compilation_Roundtrip(string version)
	{
		// 1. Load original fixture catalog
		var (originalCatalog, _) = LoadCatalog(version);
		var otfiPath = GetFixturePath("ClientAssets", version, $"Test_{version}.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		// 2. Compile to bytes
		using var ms = new MemoryStream();
		originalCatalog.WriteDatTo(ms, options);
		var compiledBytes = ms.ToArray();

		// 3. Reload compiled bytes
		var reloadedCatalog = ThingCatalog.Load(compiledBytes, options);

		// 4. Verify identity integrity
		Assert.Equal(originalCatalog.DatSignature, reloadedCatalog.DatSignature);
		Assert.Equal(originalCatalog.ItemCount, reloadedCatalog.ItemCount);
		Assert.Equal(originalCatalog.OutfitCount, reloadedCatalog.OutfitCount);
		Assert.Equal(originalCatalog.EffectCount, reloadedCatalog.EffectCount);
		Assert.Equal(originalCatalog.MissileCount, reloadedCatalog.MissileCount);

		// Verify specific content survives compilation
		var origItem = originalCatalog.GetItem(102);
		var reloadItem = reloadedCatalog.GetItem(102);
		Assert.Equal(origItem.IsUnpassable, reloadItem.IsUnpassable);
		Assert.Equal(origItem.IsUnmoveable, reloadItem.IsUnmoveable);
		Assert.Equal(origItem.MiniMapColor, reloadItem.MiniMapColor);
	}

	[Theory]
	[InlineData("860")]
	[InlineData("1098")]
	public void SprFile_Compilation_Roundtrip(string version)
	{
		// 1. Load original fixture spr with proper OTFI options
		var otfiPath = GetFixturePath("ClientAssets", version, $"Test_{version}.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		var sprPath = GetFixturePath("ClientAssets", version, $"Test_{version}.spr");
		var originalBytes = File.ReadAllBytes(sprPath);
		using var originalArchive = SpriteArchive.Load(
			originalBytes,
			options.ResolveExtendedSpriteIds(),
			options.TransparentSprites);

		// 2. Extract all sprites
		var count = originalArchive.SpriteCount;
		var spriteList = new byte[]?[count + 1]; // index 0 is unused (null)
		for (uint i = 1; i <= count; i++)
		{
			if (originalArchive.IsEmptySprite(i))
			{
				spriteList[i] = null;
			}
			else
			{
				var buffer = new byte[SpritePixelCodec.RgbaBufferLength];
				Assert.True(originalArchive.TryDecodeSpriteById(i, buffer));
				spriteList[i] = buffer;
			}
		}

		// 3. Compile sprites back into new .spr file bytes
		using var ms = new MemoryStream();
		SpriteSheetCompiler.WriteToStream(
			ms,
			originalArchive.Signature,
			originalArchive.UsesExtendedSpriteIds,
			originalArchive.TransparentPixels,
			spriteList);
		var compiledBytes = ms.ToArray();

		// 4. Reload compiled bytes
		using var reloadedArchive = SpriteArchive.Load(
			compiledBytes,
			originalArchive.UsesExtendedSpriteIds,
			originalArchive.TransparentPixels);

		// 5. Assert equality
		Assert.Equal(originalArchive.Signature, reloadedArchive.Signature);
		Assert.Equal(originalArchive.SpriteCount, reloadedArchive.SpriteCount);

		var origDest = new byte[SpritePixelCodec.RgbaBufferLength];
		var reloadDest = new byte[SpritePixelCodec.RgbaBufferLength];
		for (uint i = 1; i <= count; i++)
		{
			var origEmpty = originalArchive.IsEmptySprite(i);
			var reloadEmpty = reloadedArchive.IsEmptySprite(i);
			Assert.Equal(origEmpty, reloadEmpty);

			if (!origEmpty)
			{
				System.Array.Clear(origDest);
				System.Array.Clear(reloadDest);

				Assert.True(originalArchive.TryDecodeSpriteById(i, origDest));
				Assert.True(reloadedArchive.TryDecodeSpriteById(i, reloadDest));
				for (int p = 0; p < origDest.Length; p++)
				{
					if (origDest[p] != reloadDest[p])
					{
						Assert.Fail($"Mismatch at sprite {i}, byte index {p}. Expected {origDest[p]}, got {reloadDest[p]}.");
					}
				}
			}
		}
	}
}
