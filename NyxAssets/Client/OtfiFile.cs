using System;
using System.IO;
using NyxAssets.Things;

namespace NyxAssets.Client;

public sealed class OtfiFile
{
	public bool? Extended { get; set; }
	public bool? Transparency { get; set; }
	public bool? FrameDurations { get; set; }
	public bool? FrameGroups { get; set; }
	public string? MetadataFile { get; set; }
	public string? SpritesFile { get; set; }
	public int? SpriteSize { get; set; }
	public int? SpriteDataSize { get; set; }

	public static OtfiFile Parse(string content)
	{
		var otfi = new OtfiFile();
		using var reader = new StringReader(content);
		string? line;
		while ((line = reader.ReadLine()) != null)
		{
			// Remove comments
			int commentIdx = line.IndexOf("//");
			if (commentIdx >= 0)
			{
				line = line.Substring(0, commentIdx);
			}
			commentIdx = line.IndexOf('#');
			if (commentIdx >= 0)
			{
				line = line.Substring(0, commentIdx);
			}

			line = line.Trim();
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}

			int colonIdx = line.IndexOf(':');
			if (colonIdx <= 0)
			{
				continue;
			}

			string key = line.Substring(0, colonIdx).Trim().ToLowerInvariant();
			string value = line.Substring(colonIdx + 1).Trim();

			// Strip quotes if present
			if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
			{
				if (value.Length >= 2)
				{
					value = value.Substring(1, value.Length - 2);
				}
			}

			switch (key)
			{
				case "extended":
					otfi.Extended = ParseBool(value);
					break;
				case "transparency":
					otfi.Transparency = ParseBool(value);
					break;
				case "frame-durations":
					otfi.FrameDurations = ParseBool(value);
					break;
				case "frame-groups":
					otfi.FrameGroups = ParseBool(value);
					break;
				case "metadata-file":
					otfi.MetadataFile = value;
					break;
				case "sprites-file":
					otfi.SpritesFile = value;
					break;
				case "sprite-size":
					if (int.TryParse(value, out int size))
						otfi.SpriteSize = size;
					break;
				case "sprite-data-size":
					if (int.TryParse(value, out int dataSize))
						otfi.SpriteDataSize = dataSize;
					break;
			}
		}
		return otfi;
	}

	public static OtfiFile Load(string filePath)
	{
		return Parse(File.ReadAllText(filePath));
	}

	public uint InferClientVersion()
	{
		if (FrameGroups == true)
			return 1098;
		if (FrameDurations == true)
			return 1050;
		if (Extended == true)
			return 960;
		return 860;
	}

	public ClientDataReadOptions ToReadOptions(uint? clientVersion = null)
	{
		uint version = clientVersion ?? InferClientVersion();
		return new ClientDataReadOptions
		{
			ClientVersion = new ClientDataVersion(version),
			ExtendedSpriteIds = Extended,
			TransparentSprites = Transparency ?? false,
			ImprovedAnimations = FrameDurations,
			OutfitFrameGroups = FrameGroups
		};
	}

	private static bool? ParseBool(string value)
	{
		if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1"))
			return true;
		if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("0"))
			return false;
		return null;
	}
}
