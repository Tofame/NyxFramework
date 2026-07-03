using NyxRender;
using NyxRender.Shaders;

namespace Sandbox;

/// <summary>
/// Sandbox-specific effect shader names and asset paths. NyxRender stays agnostic; this wires our GLSL + textures.
/// </summary>
internal static class SandboxEffectShaders
{
    public static void RegisterAll(ShaderRegistry registry, string? shadersDirOverride = null)
    {
        var dir = ResolveShadersDirectory(shadersDirOverride);
        if (!Directory.Exists(dir))
        {
            Console.WriteLine($"⚠ No shader directory at \"{dir}\" — effect draws will miss programs.");
            return;
        }

        var imagesDir = SandboxResources.ImagesDirectory;

        RegisterOutfit(registry, dir, "outfit_default");

        var goldTex = TryLoadTexture(registry, imagesDir, "gold.png");
        RegisterOutfit(registry, dir, "outfit_gold", goldTex);
        RegisterOutfit(registry, dir, "outline_outfit_yellow");
        RegisterOutfit(registry, dir, "outline_outfit_orange");
        RegisterOutfit(registry, dir, "outline_outfit_blue");
        RegisterOutfit(registry, dir, "sprite_selected_outfit");

        RegisterSprite(registry, dir, "sprite_default");
        RegisterSprite(registry, dir, "sprite_selected");
        RegisterSprite(registry, dir, "outline_yellow");
        RegisterSprite(registry, dir, "outline_orange");
        RegisterSprite(registry, dir, "outline_blue");

        RegisterSprite(registry, dir, "map_default");
        var rainbowTex = TryLoadTexture(registry, imagesDir, "rainbow.png") ?? registry.CreateProceduralRainbowStrip();
        RegisterSprite(registry, dir, "map_rainbow", rainbowTex);
        RegisterSprite(registry, dir, "map_snow", TryLoadTexture(registry, imagesDir, "snow.png"));
    }

    /// <summary>Resolves shaders directory, using shadersDirOverride if provided.</summary>
    public static string ResolveShadersDirectory(string? shadersDirOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(shadersDirOverride) && Directory.Exists(shadersDirOverride))
            return shadersDirOverride;

        var nextToExe = Path.Combine(AppContext.BaseDirectory, "shaders");
        if (Directory.Exists(nextToExe))
            return nextToExe;

        var devSandbox = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "shaders"));
        if (Directory.Exists(devSandbox))
            return devSandbox;

        return nextToExe;
    }

    private static void RegisterOutfit(ShaderRegistry registry, string dir, string name, Texture? secondary = null)
    {
        var path = Path.Combine(dir, $"{name}_fragment.frag");
        if (registry.RegisterOutfitShaderFromFragmentFile(name, path, secondary))
            Console.WriteLine($"✓ Outfit effect shader: {name}");
    }

    private static void RegisterSprite(ShaderRegistry registry, string dir, string name, Texture? secondary = null)
    {
        var path = Path.Combine(dir, $"{name}_fragment.frag");
        if (registry.RegisterSpriteShaderFromFragmentFile(name, path, secondary))
            Console.WriteLine($"✓ Sprite effect shader: {name}");
    }

    private static Texture? TryLoadTexture(ShaderRegistry registry, string imagesDir, string fileName)
    {
        var path = Path.Combine(imagesDir, fileName);
        if (!File.Exists(path))
        {
            var alt = Path.Combine(SandboxResources.ImagesShadersDirectory, fileName);
            if (File.Exists(alt))
                path = alt;
            else
            {
                Console.WriteLine($"… Shader texture not found: {fileName} (procedural fallback where applicable)");
                return null;
            }
        }

        try
        {
            var tex = registry.LoadSecondaryTextureFromFile(path);
            if (fileName.Equals("rainbow.png", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("gold.png", StringComparison.OrdinalIgnoreCase))
                tex.SetWrapRepeat();
            return tex;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Failed to load shader texture {path}: {ex.Message}");
            return null;
        }
    }
}
