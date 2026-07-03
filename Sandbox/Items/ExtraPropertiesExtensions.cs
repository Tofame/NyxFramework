using System;
using System.Collections.Generic;

namespace Sandbox.Items;

/// <summary>
/// Safe parsing helpers for extra properties.
/// </summary>
public static class ExtraPropertiesExtensions
{
	public static string GetString(this Dictionary<string, string> dict, string key, string defaultValue = "")
	{
		return dict.TryGetValue(key, out var val) ? val : defaultValue;
	}

	public static int GetInt(this Dictionary<string, string> dict, string key, int defaultValue = 0)
	{
		return dict.TryGetValue(key, out var val) && int.TryParse(val, out var res) ? res : defaultValue;
	}

	public static float GetFloat(this Dictionary<string, string> dict, string key, float defaultValue = 0f)
	{
		return dict.TryGetValue(key, out var val) && float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var res) ? res : defaultValue;
	}

	public static bool GetBool(this Dictionary<string, string> dict, string key, bool defaultValue = false)
	{
		return dict.TryGetValue(key, out var val) && bool.TryParse(val, out var res) ? res : defaultValue;
	}
}
