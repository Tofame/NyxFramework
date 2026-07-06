using Xunit;
using System;
using System.IO;
using System.Text;
using NyxAssets.Things;
using System.Collections.Generic;

namespace NyxAssets.Tests;

public class ThingCatalogTests
{
	public enum DatVersionTestType
	{
		V1 = 710,
		V2 = 740,
		V3 = 760,
		V4 = 800,
		V5 = 960,
		V6 = 1056
	}

	[Fact]
	public void TestDatFormatsRoundtrip_V1_To_V6()
	{
		foreach (DatVersionTestType verVal in Enum.GetValues(typeof(DatVersionTestType)))
		{
			var clientVer = (uint)verVal;
			var format = DatThingFormatRules.SelectFromClientVersion(new ClientDataVersion(clientVer));
			
			var originalCatalog = new ThingCatalog(123456, 99, 0, 0, 0, format);
			var options = new ClientDataReadOptions
			{
				ClientVersion = new ClientDataVersion(clientVer),
				TransparentSprites = true
			};

			var item = new ThingType { Id = 100, Kind = ThingKind.Item };
			if (format >= DatThingFormat.V3_7_55__7_72)
			{
				item.IsGroundBorder = true;
			}
			else
			{
				item.IsGround = true;
				item.GroundSpeed = 120;
			}
			if (format == DatThingFormat.V4_7_80__8_54)
			{
				item.HasCharges = true;
			}
			if (format >= DatThingFormat.V5_8_60__9_86)
			{
				item.IsTranslucent = true;
			}
			if (format >= DatThingFormat.V6_10_10__10_56)
			{
				item.NoMoveAnimation = true;
			}

			item.FrameGroups.Add(new ThingFrameGroup
			{
				GroupTypeId = 0,
				Width = 1,
				Height = 1,
				ExactSize = 32,
				Layers = 1,
				PatternX = 1,
				PatternY = 1,
				PatternZ = 1,
				Frames = 1,
				SpriteIds = new uint[] { 10 }
			});

			originalCatalog.PutItem(item);

			// Write to memory stream
			using var ms = new MemoryStream();
			originalCatalog.WriteDatTo(ms, options);
			var bytes = ms.ToArray();

			// Read back
			var readCatalog = ThingCatalog.Load(bytes, options);

			// Assert
			Assert.Equal(123456u, readCatalog.DatSignature);
			var readItem = readCatalog.GetItem(100);
			if (format >= DatThingFormat.V3_7_55__7_72)
			{
				Assert.True(readItem.IsGroundBorder);
				Assert.False(readItem.IsGround);
			}
			else
			{
				Assert.True(readItem.IsGround);
				Assert.Equal(120u, readItem.GroundSpeed);
				Assert.False(readItem.IsGroundBorder);
			}
			if (format == DatThingFormat.V4_7_80__8_54)
			{
				Assert.True(readItem.HasCharges);
			}
			else
			{
				Assert.False(readItem.HasCharges);
			}
			if (format >= DatThingFormat.V5_8_60__9_86)
			{
				Assert.True(readItem.IsTranslucent);
			}
			if (format >= DatThingFormat.V6_10_10__10_56)
			{
				Assert.True(readItem.NoMoveAnimation);
			}
		}
	}

	[Fact]
	public void TestLoadDat_TruncatedStream_ThrowsException()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1056),
			TransparentSprites = true
		};

		// Too small DAT (header is 12 bytes minimum)
		var truncatedBytes = new byte[8];
		Assert.Throws<InvalidDataException>(() => ThingCatalog.Load(truncatedBytes, options));

		// DAT header okay but no item payloads
		var headerBytes = new byte[12];
		// Put some count of items so it tries to read them
		headerBytes[4] = 100; // items count = 100
		Assert.ThrowsAny<Exception>(() => ThingCatalog.Load(headerBytes, options));
	}

	[Fact]
	public void TestJsonCatalogRoundtrip_PreservesThingProperties()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1056),
			TransparentSprites = true
		};

		var catalog = new ThingCatalog();
		var item = new ThingType
		{
			Id = 100,
			Kind = ThingKind.Item,
			IsGround = true,
			GroundSpeed = 120,
			HasLight = true,
			LightLevel = 7,
			LightColor = 215,
			OffsetX = -1,
			OffsetY = 2,
			MarketName = "magic sword",
			MarketCategory = 19,
		};
		item.ExtraProperties["name"] = "magic sword";
		item.ExtraProperties["weight"] = "4200";
		item.ExtraProperties["attack"] = "48";
		item.ExtraProperties["abilities"] = "true";
		item.ExtraProperties["abilities.regeneration"] = "50";
		item.FrameGroups.Add(new ThingFrameGroup
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
			AnimationMode = 1,
			LoopCount = -1,
			StartFrame = 0,
			FrameTimings = new[]
			{
				new AnimationFrameTiming(100, 100),
				new AnimationFrameTiming(200, 200),
			},
			SpriteIds = new uint[] { 10, 11 },
		});
		catalog.PutItem(item);

		using var ms = new MemoryStream();
		catalog.ExportJson(ms, options);
		var loaded = ThingCatalog.LoadJson(ms.ToArray().AsMemory(), options);
		var readItem = loaded.GetItem(100);

		Assert.True(readItem.IsGround);
		Assert.Equal(120u, readItem.GroundSpeed);
		Assert.True(readItem.HasLight);
		Assert.Equal(7u, readItem.LightLevel);
		Assert.Equal(215u, readItem.LightColor);
		Assert.Equal(-1, readItem.OffsetX);
		Assert.Equal(2, readItem.OffsetY);
		Assert.Equal("magic sword", readItem.MarketName);
		Assert.Equal(19u, readItem.MarketCategory);
		Assert.Equal("magic sword", readItem.ExtraProperties["name"]);
		Assert.Equal("4200", readItem.ExtraProperties["weight"]);
		Assert.Equal("48", readItem.ExtraProperties["attack"]);
		Assert.Equal("true", readItem.ExtraProperties["abilities"]);
		Assert.Equal("50", readItem.ExtraProperties["abilities.regeneration"]);

		Assert.Single(readItem.FrameGroups);
		var fg = readItem.FrameGroups[0];
		Assert.Equal(2u, fg.Frames);
		Assert.True(fg.IsAnimation);
		Assert.Equal(2, fg.FrameTimings!.Length);
		Assert.Equal(100u, fg.FrameTimings[0].MinimumMilliseconds);
		Assert.Equal(200u, fg.FrameTimings[1].MaximumMilliseconds);
		Assert.Equal(new uint[] { 10, 11 }, fg.SpriteIds);
	}

	[Fact]
	public void TestThingCatalog_CanRemoveThings()
	{
		var catalog = new ThingCatalog();
		var item = new ThingType { Id = 100, Kind = ThingKind.Item };
		item.FrameGroups.Add(new ThingFrameGroup
		{
			Width = 1,
			Height = 1,
			ExactSize = 32,
			Layers = 1,
			PatternX = 1,
			PatternY = 1,
			PatternZ = 1,
			Frames = 1,
			SpriteIds = new uint[] { 1 }
		});

		catalog.PutItem(item);
		Assert.True(catalog.RemoveItem(100));
		Assert.False(catalog.RemoveItem(100));
		Assert.Throws<KeyNotFoundException>(() => catalog.GetItem(100));
	}

	[Fact]
	public void TestGetItem_OutOfBounds_ThrowsKeyNotFoundException()
	{
		var catalog = new ThingCatalog(123, 99, 0, 0, 0, DatThingFormat.V6_10_10__10_56);
		catalog.InitializeFastArrays();

		Assert.Throws<KeyNotFoundException>(() => catalog.GetItem(99));
		Assert.Throws<KeyNotFoundException>(() => catalog.GetOutfit(1));
		Assert.Throws<KeyNotFoundException>(() => catalog.GetEffect(1));
		Assert.Throws<KeyNotFoundException>(() => catalog.GetMissile(1));
	}
}
