namespace NyxGui;

/// <summary>
/// Parser for .nyxtheme files (indentation-based theme definition).
/// Format: [colors], [fonts], type styles, class styles (.name), and pseudo-states (Type:state).
/// </summary>
internal static class NyxThemeParser
{
    public static void Parse(string content, string sourceName, NyxTheme theme)
    {
        var lines = content.Split('\n');
        string? section = null;
        NyxStyle? currentStyle = null;
        string? currentStyleKey = null;
        bool isStateStyle = false;
        var indentStack = new List<int> { -1 };

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;

            var indent = line.Length - line.TrimStart().Length;

            // Section headers
            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                section = trimmed[1..^1].Trim();
                indentStack.Clear();
                indentStack.Add(indent);
                continue;
            }

            // Style selector (type name, .class, or Type:state / .Class:state)
            if (indent == 0 && !trimmed.Contains('=') && section is null)
            {
                // Could be a type style, class style, or state style
                currentStyleKey = trimmed;
                isStateStyle = trimmed.Contains(':');

                if (isStateStyle)
                {
                    if (!theme.StateStyles.TryGetValue(currentStyleKey, out currentStyle))
                    {
                        currentStyle = new NyxStyle();
                        theme.StateStyles[currentStyleKey] = currentStyle;
                    }
                }
                else if (trimmed.StartsWith('.'))
                {
                    var className = trimmed[1..];
                    if (!theme.ClassStyles.TryGetValue(className, out currentStyle))
                    {
                        currentStyle = new NyxStyle();
                        theme.ClassStyles[className] = currentStyle;
                    }
                }
                else
                {
                    if (!theme.TypeStyles.TryGetValue(trimmed, out currentStyle))
                    {
                        currentStyle = new NyxStyle();
                        theme.TypeStyles[trimmed] = currentStyle;
                    }
                }
                continue;
            }

            // Key-value pairs
            if (trimmed.Contains('='))
            {
                var eq = trimmed.IndexOf('=');
                var key = trimmed[..eq].Trim();
                var value = trimmed[(eq + 1)..].Trim();

                if (section == "colors")
                {
                    if (NyxColor.TryParseHex(value, out var color))
                        theme.Colors[key] = color;
                }
                else if (section == "fonts")
                {
                    theme.Fonts[key] = value.Trim('"');
                }
                else if (currentStyle is not null)
                {
                    ApplyStyleProperty(currentStyle, key, value);
                }
                continue;
            }
        }
    }

    private static void ApplyStyleProperty(NyxStyle style, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "background":
                if (NyxColor.TryParseHex(value, out var c)) style.Background = c;
                break;
            case "foreground":
            case "color":
                if (NyxColor.TryParseHex(value, out var c2)) style.Foreground = c2;
                break;
            case "border-color":
                if (NyxColor.TryParseHex(value, out var c3)) style.BorderColor = c3;
                break;
            case "padding":
                style.Padding = ParseThickness(value);
                break;
            case "margin":
                style.Margin = ParseThickness(value);
                break;
            case "font":
                style.FontFile = value.Trim('"');
                break;
            case "font-size":
                if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var fs))
                    style.FontSize = fs;
                break;
            case "font-bold":
                style.FontBold = bool.Parse(value);
                break;
            case "text-outline":
            case "font-outlined":
                style.FontOutlined = bool.Parse(value);
                break;
            case "border-width":
                if (int.TryParse(value, out var bw)) style.BorderWidth = bw;
                break;
            case "border-radius":
                if (int.TryParse(value, out var br)) style.BorderRadius = br;
                break;
            case "opacity":
                if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var o))
                    style.Opacity = o;
                break;
        }
    }

    private static NyxThickness ParseThickness(string value)
    {
        value = value.Trim('"');
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            1 => NyxThickness.Uniform(int.Parse(parts[0])),
            2 => new NyxThickness(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[0]), int.Parse(parts[1])),
            4 => new NyxThickness(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3])),
            _ => NyxThickness.Uniform(0),
        };
    }
}
