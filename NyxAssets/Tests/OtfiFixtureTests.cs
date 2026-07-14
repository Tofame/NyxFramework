using Xunit;
using System;
using System.IO;
using NyxAssets.Client;

namespace NyxAssets.Tests;

public class OtfiFixtureTests
{
	private static string GetFixturePath(params string[] paths)
	{
		var baseDir = AppContext.BaseDirectory;
		return Path.Combine(baseDir, "Fixtures", Path.Combine(paths));
	}

	[Fact]
	public void Load_WithValid860Otfi_ParsesCorrectProperties()
	{
		var path = GetFixturePath("ClientAssets", "860", "Test_860.otfi");
		Assert.True(File.Exists(path), $"OTFI file missing: {path}");

		var otfi = OtfiFile.Load(path);

		Assert.True(otfi.Extended);
		Assert.True(otfi.Transparency);
		Assert.False(otfi.FrameDurations);
		Assert.False(otfi.FrameGroups);
		Assert.Equal("Test_860.dat", otfi.MetadataFile);
		Assert.Equal("Test_860.spr", otfi.SpritesFile);
		Assert.Equal(32, otfi.SpriteSize);
		Assert.Equal(4096, otfi.SpriteDataSize);
	}

	[Fact]
	public void Load_WithValid1098Otfi_ParsesCorrectProperties()
	{
		var path = GetFixturePath("ClientAssets", "1098", "Test_1098.otfi");
		Assert.True(File.Exists(path), $"OTFI file missing: {path}");

		var otfi = OtfiFile.Load(path);

		Assert.True(otfi.Extended);
		Assert.True(otfi.Transparency);
		Assert.True(otfi.FrameDurations);
		Assert.True(otfi.FrameGroups);
		Assert.Equal("Test_1098.dat", otfi.MetadataFile);
		Assert.Equal("Test_1098.spr", otfi.SpritesFile);
		Assert.Equal(32, otfi.SpriteSize);
		Assert.Equal(4096, otfi.SpriteDataSize);
	}

	[Fact]
	public void InferClientVersion_WithDifferentOptions_ReturnsExpectedProtocol()
	{
		// Test version inference based on flags
		var otfiFrameGroups = new OtfiFile { FrameGroups = true };
		Assert.Equal(1098u, otfiFrameGroups.InferClientVersion());

		var otfiFrameDurations = new OtfiFile { FrameGroups = false, FrameDurations = true };
		Assert.Equal(1050u, otfiFrameDurations.InferClientVersion());

		var otfiExtended = new OtfiFile { FrameGroups = false, FrameDurations = false, Extended = true };
		Assert.Equal(960u, otfiExtended.InferClientVersion());

		var otfiLegacy = new OtfiFile { FrameGroups = false, FrameDurations = false, Extended = false };
		Assert.Equal(860u, otfiLegacy.InferClientVersion());
	}

	[Fact]
	public void ToReadOptions_WithValidFile_MapsCorrectly()
	{
		var otfi = new OtfiFile
		{
			Extended = true,
			Transparency = true,
			FrameDurations = true,
			FrameGroups = true
		};

		var options = otfi.ToReadOptions();

		Assert.Equal(1098u, options.ClientVersion.Value);
		Assert.True(options.ExtendedSpriteIds);
		Assert.True(options.TransparentSprites);
		Assert.True(options.ImprovedAnimations);
		Assert.True(options.OutfitFrameGroups);
	}

	[Fact]
	public void Parse_WithMalformedContent_GracefullyIgnoresBadLines()
	{
		var content = @"DatSpr
invalid line here without colon
extended: true
transparency: false
// comments should be ignored
# another comment
sprites-file: ""Tibia.spr""";

		var otfi = OtfiFile.Parse(content);

		Assert.True(otfi.Extended);
		Assert.False(otfi.Transparency);
		Assert.Equal("Tibia.spr", otfi.SpritesFile);
	}

	[Fact]
	public void ToReadOptions_With860StyleFlags_MapsCorrectly()
	{
		var otfi = new OtfiFile
		{
			Extended = true,
			Transparency = true,
			FrameDurations = false,
			FrameGroups = false,
		};

		var options = otfi.ToReadOptions();

		// 860-style: no frame durations, no frame groups → version ~960
		Assert.False(options.ImprovedAnimations);
		Assert.False(options.OutfitFrameGroups);
		Assert.True(options.ExtendedSpriteIds);
		Assert.True(options.TransparentSprites);
	}

	[Fact]
	public void OtfiFile_SpriteDataSize_IsPreserved()
	{
		var otfi = new OtfiFile
		{
			Extended = true,
			Transparency = true,
			FrameDurations = true,
			FrameGroups = true,
			SpriteSize = 32,
			SpriteDataSize = 4096,
		};

		// SpriteSize and SpriteDataSize are informational fields on OtfiFile itself
		Assert.Equal(32, otfi.SpriteSize);
		Assert.Equal(4096, otfi.SpriteDataSize);

		// ToReadOptions still succeeds (no throw)
		var options = otfi.ToReadOptions();
		Assert.NotNull(options);
	}
}
