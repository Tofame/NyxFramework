namespace NyxGui.Definitions;

/// <summary>Options when loading NyxGUI TOML (paths, defaults, optional root id).</summary>
public sealed class NyxGuiLoadOptions
{
    /// <summary>Host settings (drag opacity, etc.). Defaults to <see cref="NyxGuiSettings.Default"/>.</summary>
    public NyxGuiSettings Settings { get; init; } = NyxGuiSettings.Default;

    /// <summary>Directory used to resolve relative <c>image-source</c> paths (e.g. game <c>resources/images/ui</c>).</summary>
    public string? UiImagesDirectory { get; init; }

    /// <summary>Directory for Lua UI modules (e.g. <c>resources/ui</c>) — used by <c>IncludeStyles</c>.</summary>
    public string? UiDefinitionsDirectory { get; init; }

    /// <summary>Directory used to resolve relative <c>font</c> paths (e.g. game <c>resources/fonts</c>).</summary>
    public string? UiFontsDirectory { get; init; }

    /// <summary>When set, this element id becomes <see cref="NyxGuiBuiltDocument.Root"/> (must be a <see cref="NyxContainer"/>).</summary>
    public string? RootId { get; init; }

    /// <summary>If &gt; 0, applied after load so <c>rootWindow.*</c> anchors resolve immediately.</summary>
    public int InitialWindowWidth { get; init; }

    public int InitialWindowHeight { get; init; }

    /// <summary>Maps a logical image path from TOML to a host file path. Default: join with <see cref="UiImagesDirectory"/>.</summary>
    public Func<string, string?>? ResolveImageSource { get; init; }

    /// <summary>Maps a logical font file from TOML to a host file path. Default: join with <see cref="UiFontsDirectory"/>.</summary>
    public Func<string, string?>? ResolveFontSource { get; init; }

    public string ResolveImagePath(string source)
    {
        if (ResolveImageSource is { } custom)
        {
            var resolved = custom(source);
            if (!string.IsNullOrEmpty(resolved))
                return resolved;
        }

        if (string.IsNullOrWhiteSpace(source))
            return source;

        if (Path.IsPathRooted(source) && File.Exists(source))
            return source;

        var trimmed = source.TrimStart('/', '\\');
        if (UiImagesDirectory is not null)
        {
            var combined = Path.Combine(UiImagesDirectory, trimmed);
            if (File.Exists(combined))
                return combined;
        }

        return source;
    }

    public string? ResolveFontFile(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (ResolveFontSource is { } custom)
        {
            var resolved = custom(source);
            if (!string.IsNullOrEmpty(resolved))
                return resolved;
        }

        var trimmed = source.TrimStart('/', '\\');
        if (Path.IsPathRooted(source) && File.Exists(source))
            return source;

        if (UiFontsDirectory is not null)
        {
            var combined = Path.Combine(UiFontsDirectory, trimmed);
            if (File.Exists(combined))
                return combined;
        }

        return File.Exists(trimmed) ? trimmed : null;
    }
}
