using Xunit;
using System;
using System.IO;
using NyxAssets.Sprites;

namespace NyxAssets.Tests;

public class SpriteArchiveTests
{
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(false, false)]
	public void TestSprArchiveRoundtrip_DifferentOptions(bool extendedIds, bool transparent)
	{
		// 1. Arrange: Create mock sprite RGBA data (4096 bytes each for 32x32 pixels)
		var sprite1 = new byte[SpritePixelCodec.RgbaBufferLength];
		// Fill with test values
		for (int i = 0; i < sprite1.Length; i += 4)
		{
			sprite1[i] = 200;
			sprite1[i + 1] = 100;
			sprite1[i + 2] = 50;
			sprite1[i + 3] = transparent ? (byte)128 : (byte)255;
		}

		var sprite2 = new byte[SpritePixelCodec.RgbaBufferLength]; // empty

		var rgbaList = new byte[]?[] { null, sprite1, sprite2 };

		// 2. Act: Compile spr file in memory
		using var ms = new MemoryStream();
		SpriteSheetCompiler.WriteToStream(ms, 0x55AA, extendedIds, transparent, rgbaList);
		var sprBytes = ms.ToArray();

		// 3. Act & Assert: Load and decode
		using var archive = SpriteArchive.Load(sprBytes, extendedIds, transparent, preloadSprites: true);
		Assert.Equal(2u, archive.SpriteCount);
		Assert.Equal(0x55AAu, archive.Signature);

		var decoded1 = archive.DecodeSpriteById(1);
		// Check color values. Since RLE compression / decompression is lossy for alpha if transparent is false,
		// let's check exact pixel matching.
		for (int i = 0; i < sprite1.Length; i += 4)
		{
			Assert.Equal(sprite1[i], decoded1[i]);
			Assert.Equal(sprite1[i + 1], decoded1[i + 1]);
			Assert.Equal(sprite1[i + 2], decoded1[i + 2]);
			if (transparent)
			{
				Assert.Equal(sprite1[i + 3], decoded1[i + 3]);
			}
			else
			{
				Assert.Equal(255, decoded1[i + 3]);
			}
		}

		Assert.True(archive.IsEmptySprite(2));
	}

	[Fact]
	public void TestSprArchive_CanAppendAndRemoveSprites()
	{
		var sprite1 = new byte[SpritePixelCodec.RgbaBufferLength];
		var sprite2 = new byte[SpritePixelCodec.RgbaBufferLength];
		for (var i = 0; i < sprite1.Length; i += 4)
		{
			sprite1[i] = 10;
			sprite1[i + 1] = 20;
			sprite1[i + 2] = 30;
			sprite1[i + 3] = 255;
			sprite2[i] = 40;
			sprite2[i + 1] = 50;
			sprite2[i + 2] = 60;
			sprite2[i + 3] = 255;
		}

		using var ms = new MemoryStream();
		SpriteSheetCompiler.WriteToStream(ms, 0x55AA, extendedSpriteIds: false, transparentPixels: true, new byte[]?[] { null, sprite1, sprite2 });
		using var archive = SpriteArchive.Load(ms.ToArray(), extendedSpriteIds: false, transparentPixels: true, preloadSprites: true);

		var sprite3 = new byte[SpritePixelCodec.RgbaBufferLength];
		for (var i = 0; i < sprite3.Length; i += 4)
		{
			sprite3[i] = 70;
			sprite3[i + 1] = 80;
			sprite3[i + 2] = 90;
			sprite3[i + 3] = 255;
		}

		archive.PutSprite(3, sprite3);
		Assert.Equal(sprite3, archive.DecodeSpriteById(3));

		Assert.True(archive.RemoveSprite(2));
		Assert.True(archive.IsEmptySprite(2));
		Assert.Equal(3u, archive.SpriteCount);
	}

	[Fact]
	public void TestAssetArchive_CanAppendAndRemoveSprites()
	{
		var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.assets");
		try
		{
			var writer = new AssetArchiveWriter();
			var sprite1 = new byte[SpritePixelCodec.RgbaBufferLength];
			var sprite2 = new byte[SpritePixelCodec.RgbaBufferLength];
			for (var i = 0; i < sprite1.Length; i += 4)
			{
				sprite1[i] = 10;
				sprite1[i + 1] = 20;
				sprite1[i + 2] = 30;
				sprite1[i + 3] = 255;
				sprite2[i] = 40;
				sprite2[i + 1] = 50;
				sprite2[i + 2] = 60;
				sprite2[i + 3] = 255;
			}

			writer.AddSprite(32, 32, sprite1);
			writer.AddSprite(0, 0, ReadOnlySpan<byte>.Empty);
			writer.Save(tempPath);

			using var archive = AssetArchive.Load(File.ReadAllBytes(tempPath).AsMemory(), preloadPages: true);
			Assert.Equal(2u, archive.SpriteCount);

			var sprite3 = new byte[SpritePixelCodec.RgbaBufferLength];
			for (var i = 0; i < sprite3.Length; i += 4)
			{
				sprite3[i] = 70;
				sprite3[i + 1] = 80;
				sprite3[i + 2] = 90;
				sprite3[i + 3] = 255;
			}

			archive.PutSprite(3, sprite3);
			Assert.Equal(sprite3, archive.DecodeSpriteById(3));

			Assert.True(archive.RemoveSprite(2));
			Assert.True(archive.IsEmptySprite(2));
			Assert.Equal(3u, archive.SpriteCount);
		}
		finally
		{
			if (File.Exists(tempPath))
				File.Delete(tempPath);
		}
	}

	[Fact]
	public void TestSprArchive_NegativeScenarios()
	{
		// Empty / too small byte array
		var emptyBytes = Array.Empty<byte>();
		Assert.Throws<InvalidDataException>(() => SpriteArchive.Load(emptyBytes, extendedSpriteIds: true, transparentPixels: true));

		var tooSmallBytes = new byte[4];
		Assert.Throws<InvalidDataException>(() => SpriteArchive.Load(tooSmallBytes, extendedSpriteIds: true, transparentPixels: true));

		// Mock spr file with 1 empty sprite
		using var ms = new MemoryStream();
		SpriteSheetCompiler.WriteToStream(ms, 0x1234, extendedSpriteIds: true, transparentPixels: true, new byte[]?[] { null, null });
		var sprBytes = ms.ToArray();

		using var archive = SpriteArchive.Load(sprBytes, extendedSpriteIds: true, transparentPixels: true);
		Assert.Equal(1u, archive.SpriteCount);

		// Out of bounds accesses
		Assert.False(archive.TryDecodeSpriteById(0, new byte[SpritePixelCodec.RgbaBufferLength]));
		Assert.False(archive.TryDecodeSpriteById(2, new byte[SpritePixelCodec.RgbaBufferLength]));
		Assert.True(archive.IsEmptySprite(0));
		Assert.True(archive.IsEmptySprite(2));
		Assert.Throws<InvalidDataException>(() => archive.DecodeSpriteById(2));
	}

	[Fact]
	public void TestAssetArchive_NegativeScenarios()
	{
		// Truncated/Corrupt AssetArchive
		var corruptBytes = new byte[8];
		Assert.Throws<InvalidDataException>(() => AssetArchive.Load(corruptBytes, preloadPages: false));

		// Check magic signature verification
		var badSigBytes = new byte[20];
		badSigBytes[0] = 0x00; // Expected AssetArchive.MagicSignature (0x54535341)
		Assert.Throws<InvalidDataException>(() => AssetArchive.Load(badSigBytes, preloadPages: false));
	}
}
