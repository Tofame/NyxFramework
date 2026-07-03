namespace Sandbox;

/// <summary>Runtime paths for <c>resources/images</c> and <c>resources/fonts</c> next to the executable (or dev tree).</summary>
internal static class SandboxResources
{
    public const string DefaultUiFontFile = "ARIAL.TTF";

    public static string ResolveRoot()
    {
        var nextToExe = Path.Combine(AppContext.BaseDirectory, "resources");
        if (Directory.Exists(nextToExe))
            return nextToExe;

        var dev = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "resources"));
        if (Directory.Exists(dev))
            return dev;

        return nextToExe;
    }

    public static string ImagesDirectory => Path.Combine(ResolveRoot(), "images");

    public static string ImagesUiDirectory => Path.Combine(ImagesDirectory, "ui");

    public static string ImagesShadersDirectory => Path.Combine(ImagesDirectory, "shaders");

    /// <summary>Returns a path under <see cref="ImagesUiDirectory"/> if the file exists.</summary>
    public static string UiDefinitionsDirectory => Path.Combine(ResolveRoot(), "ui");

    public static string AssetsDirectory => Path.Combine(ResolveRoot(), "assets");

    public static string? TryGetThingsPath()
    {
        var path = Path.Combine(ResolveRoot(), "things.json");
        if (File.Exists(path))
            return path;

        var subPath = Path.Combine(AssetsDirectory, "things.json");
        return File.Exists(subPath) ? subPath : null;
    }

    public static string? TryGetAssetsPath(string fileName)
    {
        var path = Path.Combine(ResolveRoot(), fileName);
        if (File.Exists(path))
            return path;

        var subPath = Path.Combine(AssetsDirectory, fileName);
        return File.Exists(subPath) ? subPath : null;
    }

    public static string? TryGetUiImagePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var trimmed = fileName.TrimStart('/', '\\');
        var path = Path.Combine(ImagesUiDirectory, trimmed);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Returns a path under <see cref="UiDefinitionsDirectory"/> if the file exists.</summary>
    public static string? TryGetUiDefinitionPath(string fileName)
    {
        var path = Path.Combine(UiDefinitionsDirectory, fileName);
        return File.Exists(path) ? path : null;
    }

    public static string FontsDirectory => Path.Combine(ResolveRoot(), "fonts");

    /// <summary>
    /// Resolves <paramref name="preferredFileName"/> (default <see cref="DefaultUiFontFile"/>) under
    /// <see cref="FontsDirectory"/>, then any <c>.ttf</c>/<c>.otf</c> there, then Windows Fonts folder.
    /// </summary>
    public static string? FindFontFile(string? preferredFileName = null)
    {
        preferredFileName ??= DefaultUiFontFile;

        var fontsDir = FontsDirectory;
        if (Directory.Exists(fontsDir))
        {
            var exact = Path.Combine(fontsDir, preferredFileName);
            if (File.Exists(exact))
                return exact;

            foreach (var path in Directory.EnumerateFiles(fontsDir, "*.ttf"))
            {
                if (string.Equals(Path.GetFileName(path), preferredFileName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
        }

        var systemArial = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            "arial.ttf");
        return File.Exists(systemArial) ? systemArial : null;
    }
}
