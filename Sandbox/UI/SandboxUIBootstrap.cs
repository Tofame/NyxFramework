using NyxGui;
using NyxGui.Definitions;

namespace Sandbox.UI;

/// <summary>
/// Loads UI modules from .nyxui files.
/// </summary>
internal sealed class SandboxUIBootstrap
{
    private SandboxUIBootstrap(SandboxUIConfig config)
    {
        Config = config;
    }

    public SandboxUIConfig Config { get; }

    public static SandboxUIBootstrap? Current { get; private set; }

    public static SandboxUIBootstrap Initialize(
        NyxGuiSettings settings,
        int windowWidth,
        int windowHeight)
    {
        var configPath = SandboxResources.TryGetUiDefinitionPath("sandbox_ui_config.toml");
        var config = LoadConfig(configPath);
        var bootstrap = new SandboxUIBootstrap(config);
        Current = bootstrap;

        Console.WriteLine($"NyxGUI: bootstrap initialized.");

        return bootstrap;
    }

    private static SandboxUIConfig LoadConfig(string? configPath)
    {
        if (configPath is null || !File.Exists(configPath))
            return new SandboxUIConfig(
                new Dictionary<string, SandboxUIToggleConfig>(),
                new Dictionary<string, SandboxUIDockConfig>(),
                Array.Empty<string>());

        // Simple TOML-like parser for toggle/dock config
        var lines = File.ReadAllLines(configPath);
        var toggles = new Dictionary<string, SandboxUIToggleConfig>(StringComparer.OrdinalIgnoreCase);
        var docks = new Dictionary<string, SandboxUIDockConfig>(StringComparer.OrdinalIgnoreCase);
        var shellAdopt = new List<string>();
        string? currentSection = null;
        string? currentModule = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var section = line[1..^1].Trim();
                var dotIdx = section.IndexOf('.');
                if (dotIdx > 0)
                {
                    currentSection = section[..dotIdx].Trim();
                    currentModule = section[(dotIdx + 1)..].Trim();
                }
                else
                {
                    currentSection = section;
                    currentModule = null;
                }
                continue;
            }

            var eqIdx = line.IndexOf('=');
            if (eqIdx < 0) continue;

            var key = line[..eqIdx].Trim();
            var value = line[(eqIdx + 1)..].Trim().Trim('"');

            if (currentSection == "toggles" && currentModule is not null)
            {
                if (key == "key")
                    toggles[currentModule] = new SandboxUIToggleConfig(value);
            }
            else if (currentSection == "docks" && currentModule is not null)
            {
                docks.TryGetValue(currentModule, out var existingDock);
                existingDock ??= new SandboxUIDockConfig("left", 200);
                if (key == "side")
                    docks[currentModule] = existingDock with { Side = value! };
                else if (key == "width" && int.TryParse(value, out var w))
					docks[currentModule] = existingDock with { Width = w };
            }
            else if (currentSection == "shell_adopt")
            {
                shellAdopt.Add(value);
            }
        }

        return new SandboxUIConfig(toggles, docks, shellAdopt);
    }
}

internal sealed record SandboxUIToggleConfig(string Key);

internal sealed record SandboxUIDockConfig(string Side, int Width);

internal sealed record SandboxUIConfig(
    IReadOnlyDictionary<string, SandboxUIToggleConfig> Toggles,
    IReadOnlyDictionary<string, SandboxUIDockConfig> Docks,
    IReadOnlyList<string> ShellAdopt);
