using NyxGui;

namespace Sandbox;

/// <summary>Loads NyxGUI settings from a simple config file.</summary>
internal static class SandboxNyxGUISettingsLoader
{
    public const string DefaultFileName = "nyx_gui_settings.txt";

    public static NyxGuiSettings LoadOrDefault(string? path)
    {
        if (path is null || !File.Exists(path))
            return new NyxGuiSettings();

        try
        {
            var settings = new NyxGuiSettings();
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var eqIdx = line.IndexOf('=');
                if (eqIdx < 0) continue;

                var key = line[..eqIdx].Trim();
                var value = line[(eqIdx + 1)..].Trim();

                if (key == "panel_drag_opacity" && float.TryParse(value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var opacity))
                    settings.PanelDragOpacity = Math.Clamp(opacity, 0.05f, 1f);
            }

            return settings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NyxGUI: failed to load settings \"{path}\" — {ex.Message}; using defaults.");
            return new NyxGuiSettings();
        }
    }
}
