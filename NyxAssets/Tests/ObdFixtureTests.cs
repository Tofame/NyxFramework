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

	[Fact]
	public void ObdV3_Export_UsesObjectBuilderArgbSprites()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var effect = new ThingType { Id = 1, Kind = ThingKind.Effect };
		effect.FrameGroups.Add(new ThingFrameGroup
		{
			Frames = 1,
			SpriteIds = [42],
		});

		var rgba = new byte[SpritePixelCodec.RgbaBufferLength];
		rgba[0] = 255;
		var document = new ThingDocument
		{
			Thing = effect,
			ClientVersion = 1098,
			SpritesRgba = new Dictionary<uint, byte[]> { [42] = rgba },
		};

		var obdBytes = ObdThingCodec.Write(document, options, ObdVersions.Version3);
		var loaded = ObdThingCodec.Read(obdBytes, options);
		Assert.Equal(42u, loaded.Thing.FrameGroups[0].SpriteIds[0]);
		Assert.Equal(255, loaded.SpritesRgba![42][0]);
	}

	[Fact]
	public void ObdV2_AnimatedEffect_Export_Roundtrips()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var effect = new ThingType { Id = 1, Kind = ThingKind.Effect };
		effect.FrameGroups.Add(new ThingFrameGroup
		{
			Frames = 4,
			// IsAnimation unset — export must still write animation block for Object Builder
			SpriteIds = [1, 2, 3, 4],
		});

		var sprites = new Dictionary<uint, byte[]>();
		for (uint i = 1; i <= 4; i++)
			sprites[i] = new byte[SpritePixelCodec.RgbaBufferLength];

		var document = new ThingDocument
		{
			Thing = effect,
			ClientVersion = 1098,
			SpritesRgba = sprites,
		};

		var obdBytes = ObdThingCodec.Write(document, options, ObdVersions.Version2);
		var loaded = ObdThingCodec.Read(obdBytes, options);
		Assert.Equal(4u, loaded.Thing.FrameGroups[0].Frames);
		Assert.True(loaded.Thing.FrameGroups[0].IsAnimation);
		Assert.Equal(4, loaded.Thing.FrameGroups[0].FrameTimings!.Length);
	}

	[Fact]
	public void ItemTestObd_Export_ProducesObjectBuilderCompatiblePayload()
	{
		var path = FixturePath("item_test.obd");
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var originalBytes = File.ReadAllBytes(path);
		var doc = ObdThingCodec.Read(originalBytes, options);
		var exportedBytes = ObdThingCodec.Write(doc, options, doc.ObdVersion);

		var origPayload = FlashLzmaCodec.Decompress(originalBytes);
		var expPayload = FlashLzmaCodec.Decompress(exportedBytes);
		Assert.Equal(origPayload.Length, expPayload.Length);
		Assert.Equal(origPayload, expPayload);

		// Recompress original payload — isolates LZMA compatibility for Object Builder
		var recompressed = FlashLzmaCodec.Compress(origPayload);
		ObdThingCodec.Read(recompressed, options);
	}
}
