using NyxGui;
using NyxGui.Definitions;

namespace Sandbox;

/// <summary>Loads Sandbox UI modules from .nyxui files.</summary>
internal static class SandboxUIDefinitions
{
    public sealed record LoadResult(
        NyxGuiBuiltDocument Document,
        string SourcePath);

    public static NyxGuiLoadOptions CreateLoadOptions(
        NyxGuiSettings? settings = null,
        int windowWidth = 0,
        int windowHeight = 0) => new()
    {
        Settings = settings ?? NyxGuiSettings.Default,
        UiImagesDirectory = SandboxResources.ImagesUiDirectory,
        UiDefinitionsDirectory = SandboxResources.UiDefinitionsDirectory,
        ResolveImageSource = SandboxResources.TryGetUiImagePath,
        UiFontsDirectory = SandboxResources.FontsDirectory,
        ResolveFontSource = SandboxResources.FindFontFile,
        InitialWindowWidth = windowWidth > 0 ? windowWidth : SandboxDefaults.WindowWidth,
        InitialWindowHeight = windowHeight > 0 ? windowHeight : SandboxDefaults.WindowHeight,
    };

    public static LoadResult? TryLoad(
        string baseName,
        NyxGuiLoadOptions options,
        int windowWidth = 0,
        int windowHeight = 0)
    {
        var nyxuiPath = SandboxResources.TryGetUiDefinitionPath($"{baseName}.nyxui");
        if (nyxuiPath is null)
            return null;

        var source = File.ReadAllText(nyxuiPath);
        var document = NyxuiParser.ParseAndBuild(source, options, nyxuiPath);
        return new LoadResult(document, nyxuiPath);
    }
}
