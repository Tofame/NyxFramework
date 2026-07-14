using System.Text.Json;
using Xunit;
using NyxAssets.Sprites;
using NyxAssets.Things;
using NyxAssets.Things.Exchange;

namespace NyxAssets.Tests;

public class ThingDocumentExchangeTests
{
	private static ThingType CreateSampleEffect(uint id = 7)
	{
		var effect = new ThingType
		{
			Id = id,
			Kind = ThingKind.Effect,
			AnimateAlways = true,
		};
		effect.FrameGroups.Add(new ThingFrameGroup
		{
			Width = 1,
			Height = 1,
			ExactSize = 32,
			Layers = 1,
			PatternX = 1,
			PatternY = 1,
			PatternZ = 1,
			Frames = 2,
			IsAnimation = true,
			AnimationMode = 0,
			LoopCount = 0,
			StartFrame = 0,
			FrameTimings =
			[
				new AnimationFrameTiming(100, 100),
				new AnimationFrameTiming(150, 150),
			],
			SpriteIds = [501, 502],
		});
		return effect;
	}

	private static byte[] CreateSampleRgba(byte red, byte green, byte blue, byte alpha = 255)
	{
		var rgba = new byte[SpritePixelCodec.RgbaBufferLength];
		var o = (16 * 32 + 16) * 4;
		rgba[o] = red;
		rgba[o + 1] = green;
		rgba[o + 2] = blue;
		rgba[o + 3] = alpha;
		return rgba;
	}

	[Fact]
	public void JsonDocument_Roundtrip_PreservesTypeAndFields()
	{
		var effect = CreateSampleEffect();
		effect.ExtraProperties["name"] = "fire";

		var original = new ThingDocument
		{
			Thing = effect,
			ClientVersion = 1098,
			SpritesRgba = new Dictionary<uint, byte[]>
			{
				[501] = CreateSampleRgba(255, 0, 0),
				[502] = CreateSampleRgba(0, 255, 0),
			},
		};

		using var ms = new MemoryStream();
		ThingDocumentJsonCodec.Write(original, ms);

		var loaded = ThingDocumentJsonCodec.Read(ms.ToArray().AsMemory());
		Assert.Equal(ThingKind.Effect, loaded.Kind);
		Assert.Equal("effect", ThingKindNames.ToName(loaded.Kind));
		Assert.Equal(1098u, loaded.ClientVersion);
		Assert.Equal(7u, loaded.Thing.Id);
		Assert.True(loaded.Thing.AnimateAlways);
		Assert.Equal("fire", loaded.Thing.ExtraProperties["name"]);
		Assert.Equal(new uint[] { 501, 502 }, loaded.Thing.FrameGroups[0].SpriteIds);
		Assert.NotNull(loaded.SpritesRgba);
		Assert.Equal(501u, loaded.SpritesRgba!.Keys.Min());
		Assert.Equal(255, loaded.SpritesRgba[501][(16 * 32 + 16) * 4]);
	}

	[Fact]
	public void JsonDocument_RequiresTypeField()
	{
		var json = """{"format":"nyx-thing","id":1,"frameGroups":[{"spriteIds":[1]}]}""";
		Assert.Throws<InvalidDataException>(() => ThingDocumentJsonCodec.Read(System.Text.Encoding.UTF8.GetBytes(json).AsMemory()));
	}

	[Fact]
	public void ObdV3_Roundtrip_PreservesThingAndSprites()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var effect = CreateSampleEffect();
		var document = new ThingDocument
		{
			Thing = effect,
			ClientVersion = 1098,
			SpritesRgba = new Dictionary<uint, byte[]>
			{
				[501] = CreateSampleRgba(10, 20, 30),
				[502] = CreateSampleRgba(40, 50, 60),
			},
		};

		var obdBytes = ObdThingCodec.Write(document, options, ObdVersions.Version3);
		var loaded = ObdThingCodec.Read(obdBytes, options);

		Assert.Equal(ObdVersions.Version3, loaded.ObdVersion);
		Assert.Equal(ThingKind.Effect, loaded.Kind);
		Assert.True(loaded.Thing.AnimateAlways);
		Assert.Equal(new uint[] { 501, 502 }, loaded.Thing.FrameGroups[0].SpriteIds);
		Assert.Equal(100u, loaded.Thing.FrameGroups[0].FrameTimings![0].MinimumMilliseconds);
		Assert.NotNull(loaded.SpritesRgba);
		var pixel = (16 * 32 + 16) * 4;
		Assert.Equal(10, loaded.SpritesRgba![501][pixel]);
		Assert.Equal(60, loaded.SpritesRgba[502][pixel + 2]);
	}

	[Fact]
	public void ObdV2_Roundtrip_PreservesSprites()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var missile = new ThingType
		{
			Id = 3,
			Kind = ThingKind.Missile,
		};
		missile.FrameGroups.Add(new ThingFrameGroup
		{
			SpriteIds = [900],
		});

		var document = new ThingDocument
		{
			Thing = missile,
			ClientVersion = 1098,
			SpritesRgba = new Dictionary<uint, byte[]>
			{
				[900] = CreateSampleRgba(1, 2, 3),
			},
		};

		var obdBytes = ObdThingCodec.Write(document, options, ObdVersions.Version2);
		var loaded = ObdThingCodec.Read(obdBytes, options);

		Assert.Equal(ObdVersions.Version2, loaded.ObdVersion);
		Assert.Equal(ThingKind.Missile, loaded.Kind);
		Assert.Equal(1, loaded.SpritesRgba![900][(16 * 32 + 16) * 4]);
	}

	[Fact]
	public void ImportInto_PutsThingInCatalog()
	{
		var catalog = new ThingCatalog();
		var outfit = new ThingType
		{
			Id = 1,
			Kind = ThingKind.Outfit,
			Cloth = true,
			ClothSlot = 1,
		};
		outfit.FrameGroups.Add(new ThingFrameGroup { SpriteIds = [42] });

		var document = ThingDocument.FromThing(outfit);
		document.ImportInto(catalog);

		var loaded = catalog.GetOutfit(1);
		Assert.True(loaded.Cloth);
		Assert.Equal(1u, loaded.ClothSlot);
	}

	[Fact]
	public void JsonFromObd_Roundtrip()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
		};

		var document = new ThingDocument
		{
			Thing = CreateSampleEffect(9),
			ClientVersion = 1098,
			SpritesRgba = new Dictionary<uint, byte[]>
			{
				[501] = CreateSampleRgba(7, 8, 9),
				[502] = CreateSampleRgba(1, 2, 3),
			},
		};

		var obdBytes = ObdThingCodec.Write(document, options, ObdVersions.Version3);
		var fromObd = ThingDocumentJsonCodec.FromObd(obdBytes, options);

		using var ms = new MemoryStream();
		ThingDocumentJsonCodec.Write(fromObd, ms);
		var fromJson = ThingDocumentJsonCodec.Read(ms.ToArray().AsMemory());

		Assert.Equal(ThingKind.Effect, fromJson.Kind);
		Assert.Equal(0u, fromJson.Thing.Id);
		Assert.Equal(7, fromJson.SpritesRgba![501][(16 * 32 + 16) * 4]);
	}

	[Fact]
	public void JsonDocument_WithNullSpritesRgba_OmitsAndRestoresAsNull()
	{
		var document = new ThingDocument
		{
			Thing = CreateSampleEffect(),
			ClientVersion = 1098,
			SpritesRgba = null,
		};

		using var ms = new MemoryStream();
		ThingDocumentJsonCodec.Write(document, ms);

		var loaded = ThingDocumentJsonCodec.Read(ms.ToArray().AsMemory());
		Assert.Null(loaded.SpritesRgba);
		Assert.Equal(ThingKind.Effect, loaded.Kind);
	}

	[Theory]
	[InlineData(ThingKind.Item, "item")]
	[InlineData(ThingKind.Outfit, "outfit")]
	[InlineData(ThingKind.Effect, "effect")]
	[InlineData(ThingKind.Missile, "missile")]
	public void ThingKindNames_ToName_ReturnsCorrectStringForAllKinds(ThingKind kind, string expected)
	{
		Assert.Equal(expected, ThingKindNames.ToName(kind));
	}

	[Fact]
	public void ImportInto_Item_PutsItemInCatalog()
	{
		var catalog = new ThingCatalog();
		var item = new ThingType
		{
			Id = 100,
			Kind = ThingKind.Item,
			IsGround = true,
			GroundSpeed = 150,
		};
		item.FrameGroups.Add(new ThingFrameGroup { SpriteIds = [10] });

		var document = ThingDocument.FromThing(item);
		document.ImportInto(catalog);

		var loaded = catalog.GetItem(100);
		Assert.True(loaded.IsGround);
		Assert.Equal(150u, loaded.GroundSpeed);
	}
}
