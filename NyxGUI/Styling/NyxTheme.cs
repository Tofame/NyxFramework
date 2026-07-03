namespace NyxGui;

/// <summary>
/// Theme definition: color palette, type styles, class styles, and pseudo-state deltas.
/// Loaded from .nyxtheme files (indentation-based format).
/// </summary>
public sealed class NyxTheme
{
    /// <summary>Named color palette (e.g. "panel-bg", "accent").</summary>
    public Dictionary<string, NyxColor> Colors { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Named font references (e.g. "default" → "ARIAL.TTF").</summary>
    public Dictionary<string, string> Fonts { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Type styles keyed by widget type name (e.g. "NyxLabel", "NyxButton").</summary>
    public Dictionary<string, NyxStyle> TypeStyles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Class styles keyed by class name (e.g. "primary", "inventory-slot").</summary>
    public Dictionary<string, NyxStyle> ClassStyles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Pseudo-state styles keyed by "Type:state" or ".Class:state" (e.g. "NyxButton:hover").</summary>
    public Dictionary<string, NyxStyle> StateStyles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Resolves a color by name from the palette. Throws if not found.</summary>
    public NyxColor GetColor(string name)
    {
        if (Colors.TryGetValue(name, out var color)) return color;
        throw new KeyNotFoundException($"Theme color \"{name}\" not found.");
    }

    /// <summary>Tries to resolve a color by name. Returns false if not found.</summary>
    public bool TryGetColor(string name, out NyxColor color) => Colors.TryGetValue(name, out color);

    /// <summary>Resolves a font file path by name. Throws if not found.</summary>
    public string GetFont(string name)
    {
        if (Fonts.TryGetValue(name, out var file)) return file;
        throw new KeyNotFoundException($"Theme font \"{name}\" not found.");
    }

    /// <summary>Tries to resolve a font file path by name. Returns false if not found.</summary>
    public bool TryGetFont(string name, out string file) => Fonts.TryGetValue(name, out file!);

    /// <summary>Sets a color at runtime (for overrides or dynamic themes).</summary>
    public void SetColor(string name, NyxColor color) => Colors[name] = color;

    /// <summary>Sets a font reference at runtime.</summary>
    public void SetFont(string name, string file) => Fonts[name] = file;

    /// <summary>Loads a theme from a .nyxtheme file.</summary>
    public static NyxTheme Load(string path)
    {
        var theme = new NyxTheme();
        NyxThemeParser.Parse(File.ReadAllText(path), path, theme);
        return theme;
    }

    /// <summary>Loads a theme from a string (for embedded or generated themes).</summary>
    public static NyxTheme Parse(string content, string sourceName = "<string>")
    {
        var theme = new NyxTheme();
        NyxThemeParser.Parse(content, sourceName, theme);
        return theme;
    }

    /// <summary>Creates an empty theme with default dark palette.</summary>
    public static NyxTheme CreateDefault()
    {
        var theme = new NyxTheme();
        theme.Colors["panel-bg"] = NyxColor.FromRgb(45, 45, 48);
        theme.Colors["panel-border"] = NyxColor.FromRgb(80, 80, 88);
        theme.Colors["button-face"] = NyxColor.FromRgb(70, 70, 78);
        theme.Colors["button-face-hover"] = NyxColor.FromRgb(90, 90, 100);
        theme.Colors["button-face-pressed"] = NyxColor.FromRgb(55, 55, 62);
        theme.Colors["button-border"] = NyxColor.FromRgb(120, 120, 130);
        theme.Colors["text-primary"] = NyxColor.FromRgb(220, 220, 225);
        theme.Colors["text-muted"] = NyxColor.FromRgb(160, 160, 170);
        theme.Colors["accent"] = NyxColor.FromRgb(74, 158, 255);
        return theme;
    }
}
