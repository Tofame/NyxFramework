using Xunit;
using System;
using System.IO;
using System.Collections.Generic;
using NyxAssets.Things;
using NyxAssets.Client;

namespace NyxAssets.Tests;

public class DatFixtureTests
{
	private static string GetFixturePath(params string[] paths)
	{
		var baseDir = AppContext.BaseDirectory;
		return Path.Combine(baseDir, "Fixtures", Path.Combine(paths));
	}

	[Fact]
	public void Load_With860Dat_ParsesCatalogAndMetadata()
	{
		var datPath = GetFixturePath("ClientAssets", "860", "Test_860.dat");
		var bytes = File.ReadAllBytes(datPath);

		var otfiPath = GetFixturePath("ClientAssets", "860", "Test_860.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		var catalog = ThingCatalog.Load(bytes, options);

		Assert.NotNull(catalog);
		Assert.True(catalog.DatSignature > 0);
		Assert.True(catalog.ItemCount > 0);
	}

	[Fact]
	public void Load_With1098Dat_ParsesCatalogAndMetadata()
	{
		var datPath = GetFixturePath("ClientAssets", "1098", "Test_1098.dat");
		var bytes = File.ReadAllBytes(datPath);

		var otfiPath = GetFixturePath("ClientAssets", "1098", "Test_1098.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		var catalog = ThingCatalog.Load(bytes, options);

		Assert.NotNull(catalog);
		Assert.True(catalog.DatSignature > 0);
		Assert.True(catalog.ItemCount > 0);
	}

	[Fact]
	public void GetItem_WithValidId_ReturnsItemWithCorrectKind()
	{
		var datPath = GetFixturePath("ClientAssets", "1098", "Test_1098.dat");
		var otfiPath = GetFixturePath("ClientAssets", "1098", "Test_1098.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		var catalog = ThingCatalog.Load(File.ReadAllBytes(datPath), options);

		// The minimum valid item ID in DAT is 100
		var item = catalog.GetItem(100);
		Assert.NotNull(item);
		Assert.Equal(ThingKind.Item, item.Kind);
		Assert.Equal(100u, item.Id);
	}

	[Fact]
	public void GetOutfit_WithValidId_ReturnsOutfit()
	{
		var datPath = GetFixturePath("ClientAssets", "1098", "Test_1098.dat");
		var otfiPath = GetFixturePath("ClientAssets", "1098", "Test_1098.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		var catalog = ThingCatalog.Load(File.ReadAllBytes(datPath), options);

		if (catalog.OutfitCount > 0)
		{
			var outfit = catalog.GetOutfit(1);
			Assert.NotNull(outfit);
			Assert.Equal(ThingKind.Outfit, outfit.Kind);
			Assert.Equal(1u, outfit.Id);
		}
	}

	[Fact]
	public void Load_WithMismatchingVersion_ThrowsOrFailsToParse()
	{
		var dat860Path = GetFixturePath("ClientAssets", "860", "Test_860.dat");
		var bytes = File.ReadAllBytes(dat860Path);

		// Attempt to load 8.60 DAT using 10.98 options (which parses different flag offsets)
		var mismatchOptions = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true,
			OutfitFrameGroups = true
		};

		Assert.ThrowsAny<Exception>(() => ThingCatalog.Load(bytes, mismatchOptions));
	}

	[Fact]
	public void Load_WithTruncatedData_ThrowsInvalidDataException()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true
		};

		// 4 bytes is too small even for signature
		var emptyBytes = new byte[4];
		Assert.Throws<InvalidDataException>(() => ThingCatalog.Load(emptyBytes, options));
	}

	[Fact]
	public void GetItem_OutOfBounds_ThrowsKeyNotFoundException()
	{
		var datPath = GetFixturePath("ClientAssets", "1098", "Test_1098.dat");
		var otfiPath = GetFixturePath("ClientAssets", "1098", "Test_1098.otfi");
		var otfi = OtfiFile.Load(otfiPath);
		var options = otfi.ToReadOptions();

		var catalog = ThingCatalog.Load(File.ReadAllBytes(datPath), options);

		// Request item index greater than ItemCount + 100
		uint invalidId = catalog.ItemCount + 200;
		Assert.Throws<KeyNotFoundException>(() => catalog.GetItem(invalidId));
	}
}
