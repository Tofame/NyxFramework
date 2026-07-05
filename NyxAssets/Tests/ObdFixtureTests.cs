using Xunit;
using NyxAssets.Sprites;
using NyxAssets.Things;
using NyxAssets.Things.Exchange;

namespace NyxAssets.Tests;

public class ObdFixtureTests
{
	private static string FixturePath(string name)
	{
		var baseDir = AppContext.BaseDirectory;
		return Path.Combine(baseDir, "Fixtures", "Obd", name);
	}

	[Fact]
	public void ItemTestObd_RealFile_LoadsItemWithSprites()
	{
		var path = FixturePath("item_test.obd");
		Assert.True(File.Exists(path), $"Missing fixture: {path}");

		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var doc = ObdThingCodec.Read(path, options);

		Assert.Equal(ThingKind.Item, doc.Kind);
		Assert.True(doc.ObdVersion is ObdVersions.Version1 or ObdVersions.Version2 or ObdVersions.Version3);
		Assert.NotEmpty(doc.Thing.FrameGroups);
		Assert.NotEmpty(doc.Thing.FrameGroups[0].SpriteIds);
		Assert.NotNull(doc.SpritesRgba);
		Assert.Equal(doc.Thing.FrameGroups[0].SpriteIds.Length, doc.SpritesRgba!.Count);

		foreach (var rgba in doc.SpritesRgba.Values)
			Assert.Equal(SpritePixelCodec.RgbaBufferLength, rgba.Length);

		// Round-trip through our encoder (preserve detected version when possible)
		var roundtripBytes = ObdThingCodec.Write(doc, options, doc.ObdVersion >= ObdVersions.Version2 ? doc.ObdVersion : ObdVersions.Version3);
		var roundtrip = ObdThingCodec.Read(roundtripBytes, options);
		Assert.Equal(doc.Kind, roundtrip.Kind);
		Assert.Equal(doc.Thing.FrameGroups[0].SpriteIds, roundtrip.Thing.FrameGroups[0].SpriteIds);
	}
}
