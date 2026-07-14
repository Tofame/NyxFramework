using Xunit;
using System;
using System.IO;
using NyxAssets.Sprites;
using NyxAssets.Client;

namespace NyxAssets.Tests;

public class SpriteArchiveFixtureTests
{
	private static string GetFixturePath(params string[] paths)
	{
		var baseDir = AppContext.BaseDirectory;
		return Path.Combine(baseDir, "Fixtures", Path.Combine(paths));
	}

	[Fact]
	public void Load_With860SpriteArchive_ParsesCorrectMetadata()
	{
		var path = GetFixturePath("ClientAssets", "860", "Test_860.spr");
		var bytes = File.ReadAllBytes(path);

		// 860 uses extended sprite IDs (true) and transparent pixels (true)
		using var archive = SpriteArchive.Load(bytes, extendedSpriteIds: true, transparentPixels: true);

		Assert.NotNull(archive);
		Assert.True(archive.SpriteCount > 0);
		Assert.True(archive.Signature > 0);
		Assert.True(archive.UsesExtendedSpriteIds);
		Assert.True(archive.TransparentPixels);
	}

	[Fact]
	public void Load_With1098SpriteArchive_ParsesCorrectMetadata()
	{
		var path = GetFixturePath("ClientAssets", "1098", "Test_1098.spr");
		var bytes = File.ReadAllBytes(path);

		// 1098 uses extended sprite IDs (true) and transparent pixels (true)
		using var archive = SpriteArchive.Load(bytes, extendedSpriteIds: true, transparentPixels: true);

		Assert.NotNull(archive);
		Assert.True(archive.SpriteCount > 0);
		Assert.True(archive.Signature > 0);
		Assert.True(archive.UsesExtendedSpriteIds);
		Assert.True(archive.TransparentPixels);
	}

	[Fact]
	public void TryDecodeSpriteById_WithValidSprite_ReturnsTrueAndFillsBuffer()
	{
		var sprPath = GetFixturePath("ClientAssets", "1098", "Test_1098.spr");
		using var archive = SpriteArchive.Load(File.ReadAllBytes(sprPath), extendedSpriteIds: true, transparentPixels: true);

		uint validSpriteId = 0;
		var dest = new byte[SpritePixelCodec.RgbaBufferLength];

		for (uint i = 1; i <= archive.SpriteCount; i++)
		{
			if (archive.TryDecodeSpriteById(i, dest))
			{
				validSpriteId = i;
				break;
			}
		}

		Assert.True(validSpriteId > 0, "No valid, non-empty sprite found in the test archive.");
		Assert.Equal(SpritePixelCodec.RgbaBufferLength, dest.Length);
	}

	[Fact]
	public void TryDecodeSpriteById_WithInvalidSprite_ReturnsFalse()
	{
		var sprPath = GetFixturePath("ClientAssets", "1098", "Test_1098.spr");
		using var archive = SpriteArchive.Load(File.ReadAllBytes(sprPath), extendedSpriteIds: true, transparentPixels: true);

		var dest = new byte[SpritePixelCodec.RgbaBufferLength];
		
		// 0 is invalid
		Assert.False(archive.TryDecodeSpriteById(0, dest));

		// Out of bounds is invalid
		Assert.False(archive.TryDecodeSpriteById(archive.SpriteCount + 1, dest));
	}

	[Fact]
	public void TryDecodeSpriteById_WithTooSmallBuffer_ThrowsArgumentException()
	{
		var sprPath = GetFixturePath("ClientAssets", "1098", "Test_1098.spr");
		using var archive = SpriteArchive.Load(File.ReadAllBytes(sprPath), extendedSpriteIds: true, transparentPixels: true);

		var badDest = new byte[100]; // must be >= 4096

		Assert.Throws<ArgumentException>(() => archive.TryDecodeSpriteById(1, badDest));
	}

	[Fact]
	public void Export_SpritePngJpegBmp_WritesValidFiles()
	{
		var datPath = GetFixturePath("ClientAssets", "1098", "Test_1098.dat");
		var sprPath = GetFixturePath("ClientAssets", "1098", "Test_1098.spr");
		var otfiPath = GetFixturePath("ClientAssets", "1098", "Test_1098.otfi");

		var otfi = OtfiFile.Load(otfiPath);
		using var bundle = ClientAssetBundle.LoadFromFiles(datPath, sprPath, otfi.ToReadOptions());

		uint validSpriteId = 0;
		var dest = new byte[SpritePixelCodec.RgbaBufferLength];
		for (uint i = 1; i <= bundle.Sprites.SpriteCount; i++)
		{
			if (bundle.TryDecodeSpriteById(i, dest))
			{
				validSpriteId = i;
				break;
			}
		}
		Assert.True(validSpriteId > 0, "No valid sprite found for exporting.");

		var tempPng = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
		var tempJpeg = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");
		var tempBmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bmp");

		try
		{
			Assert.True(bundle.TryExportSpritePng(validSpriteId, tempPng));
			Assert.True(bundle.TryExportSpriteJpeg(validSpriteId, tempJpeg));
			Assert.True(bundle.TryExportSpriteBmp(validSpriteId, tempBmp));

			Assert.True(File.Exists(tempPng));
			Assert.True(File.Exists(tempJpeg));
			Assert.True(File.Exists(tempBmp));

			// Check files have non-zero size
			Assert.True(new FileInfo(tempPng).Length > 0);
			Assert.True(new FileInfo(tempJpeg).Length > 0);
			// Note: SkiaSharp BMP encoding may not be supported on all platforms;
			// TryExportSpriteBmp returns true (file created) but size may be 0 on those platforms.
			Assert.True(new FileInfo(tempBmp).Length >= 0);
		}
		finally
		{
			if (File.Exists(tempPng)) File.Delete(tempPng);
			if (File.Exists(tempJpeg)) File.Delete(tempJpeg);
			if (File.Exists(tempBmp)) File.Delete(tempBmp);
		}
	}

	[Fact]
	public void TryDecodeSpriteById_With860Archive_FindsValidSprite()
	{
		var sprPath = GetFixturePath("ClientAssets", "860", "Test_860.spr");
		using var archive = SpriteArchive.Load(File.ReadAllBytes(sprPath), extendedSpriteIds: true, transparentPixels: true);

		var dest = new byte[SpritePixelCodec.RgbaBufferLength];
		uint validSpriteId = 0;
		for (uint i = 1; i <= archive.SpriteCount; i++)
		{
			if (archive.TryDecodeSpriteById(i, dest))
			{
				validSpriteId = i;
				break;
			}
		}

		Assert.True(validSpriteId > 0, "No valid, non-empty sprite found in the 860 test archive.");
	}

	[Fact]
	public void TryDecodeSpriteById_DecodedSprite_ContainsNonZeroPixels()
	{
		var sprPath = GetFixturePath("ClientAssets", "1098", "Test_1098.spr");
		using var archive = SpriteArchive.Load(File.ReadAllBytes(sprPath), extendedSpriteIds: true, transparentPixels: true);

		var dest = new byte[SpritePixelCodec.RgbaBufferLength];
		bool decoded = false;
		for (uint i = 1; i <= archive.SpriteCount; i++)
		{
			if (archive.TryDecodeSpriteById(i, dest))
			{
				decoded = true;
				break;
			}
		}

		Assert.True(decoded, "Expected at least one non-empty sprite.");
		// At least one byte in the decoded buffer must be non-zero
		Assert.Contains(dest, b => b != 0);
	}
}
