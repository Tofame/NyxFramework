using Xunit;
using System;
using System.IO;
using System.Linq;
using NyxAssets.Things;
using NyxAssets.Things.Exchange;

namespace NyxAssets.Tests;

public class ObdFixtureCollectionTests
{
	private static string GetFixturePath(params string[] paths)
	{
		var baseDir = AppContext.BaseDirectory;
		return Path.Combine(baseDir, "Fixtures", Path.Combine(paths));
	}

	[Theory]
	[InlineData("860", 860u)]
	[InlineData("1098", 1098u)]
	public void Read_WithAllObdFiles_RoundtripsSuccessfully(string versionFolder, uint clientVersion)
	{
		var obdDir = GetFixturePath("Obd", versionFolder);
		var files = Directory.GetFiles(obdDir, "*.obd");
		Assert.NotEmpty(files);

		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(clientVersion),
			TransparentSprites = true
		};

		foreach (var file in files)
		{
			var bytes = File.ReadAllBytes(file);
			var doc = ObdThingCodec.Read(bytes, options);

			Assert.NotNull(doc);
			Assert.NotNull(doc.Thing);

			// Roundtrip write
			var writtenBytes = ObdThingCodec.Write(doc, options, doc.ObdVersion);
			var docRead = ObdThingCodec.Read(writtenBytes, options);

			// Assert identity integrity
			Assert.Equal(doc.Kind, docRead.Kind);
			Assert.Equal(doc.Thing.Id, docRead.Thing.Id);
			Assert.Equal(doc.Thing.FrameGroups.Count, docRead.Thing.FrameGroups.Count);

			for (int g = 0; g < doc.Thing.FrameGroups.Count; g++)
			{
				var groupOrig = doc.Thing.FrameGroups[g];
				var groupRead = docRead.Thing.FrameGroups[g];

				Assert.Equal(groupOrig.Frames, groupRead.Frames);
				Assert.Equal(groupOrig.Width, groupRead.Width);
				Assert.Equal(groupOrig.Height, groupRead.Height);
				Assert.Equal(groupOrig.Layers, groupRead.Layers);
				Assert.Equal(groupOrig.SpriteIds, groupRead.SpriteIds);
			}

			// Validate sprite count equality
			if (doc.SpritesRgba != null)
			{
				Assert.NotNull(docRead.SpritesRgba);
				Assert.Equal(doc.SpritesRgba.Count, docRead.SpritesRgba.Count);
				foreach (var key in doc.SpritesRgba.Keys)
				{
					Assert.True(docRead.SpritesRgba.ContainsKey(key));
					Assert.Equal(doc.SpritesRgba[key], docRead.SpritesRgba[key]);
				}
			}
		}
	}

	[Fact]
	public void Read_WithMalformedObdBytes_ThrowsInvalidDataException()
	{
		var options = new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(1098),
			TransparentSprites = true
		};

		// Obd expects a valid header and LZMA payload. Corrupt bytes should cause failure.
		var corruptBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
		Assert.ThrowsAny<Exception>(() => ObdThingCodec.Read(corruptBytes, options));
	}
}
