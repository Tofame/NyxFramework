using Silk.NET.OpenGL;
using NyxRender;

namespace NyxRender.Shaders;

/// <summary>Hosts named fragment programs paired with <see cref="ShaderSources"/> vertex layout for <see cref="EffectDrawBatch"/>.</summary>
public sealed class ShaderRegistry : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, EffectShaderEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Texture> _ownedSecondaryTextures = new();

    public ShaderRegistry(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
    }

    public IReadOnlyCollection<string> Names => _entries.Keys;

    public bool TryGet(string name, out EffectShaderEntry entry) => _entries.TryGetValue(name, out entry!);

    public EffectShaderEntry Get(string name) =>
        _entries.TryGetValue(name, out var e) ? e : throw new KeyNotFoundException($"Shader '{name}' is not registered.");

    /// <summary>Loads GLSL from disk (paired with <see cref="ShaderSources.EffectVertex"/>).</summary>
    public bool RegisterOutfitShaderFromFragmentFile(
        string name,
        string fragmentFilePath,
        Texture? secondaryTexture = null)
    {
        if (!File.Exists(fragmentFilePath))
        {
            Console.WriteLine($"⚠ Outfit shader skipped (missing file): {fragmentFilePath}");
            return false;
        }

        try
        {
            var frag = File.ReadAllText(fragmentFilePath);
            return RegisterOutfitShaderFromSource(name, frag, secondaryTexture);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Outfit shader '{name}' failed reading {fragmentFilePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Loads a sprite/map-style GLSL fragment from disk (paired with <see cref="ShaderSources.EffectVertex"/>).</summary>
    public bool RegisterSpriteShaderFromFragmentFile(string name, string fragmentFilePath, Texture? secondaryTexture = null)
    {
        if (!File.Exists(fragmentFilePath))
        {
            Console.WriteLine($"⚠ Sprite shader skipped (missing file): {fragmentFilePath}");
            return false;
        }

        try
        {
            var frag = File.ReadAllText(fragmentFilePath);
            return RegisterSpriteShaderFromSource(name, frag, ShaderSources.EffectVertex, secondaryTexture);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Sprite shader '{name}' failed reading {fragmentFilePath}: {ex.Message}");
            return false;
        }
    }

    public bool RegisterOutfitShaderFromSource(
        string name,
        string fragmentShaderSource,
        Texture? secondaryTexture = null,
        string? vertexShaderSource = null)
    {
        try
        {
            var program = new Shader(_gl, vertexShaderSource ?? ShaderSources.EffectVertex, fragmentShaderSource);
            RegisterEntry(name, EffectShaderKind.CustomOutfit, program, secondaryTexture);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Outfit shader '{name}' failed: {ex.Message}");
            return false;
        }
    }

    public bool RegisterSpriteShaderFromSource(
        string name,
        string fragmentShaderSource,
        string? vertexShaderSource = null,
        Texture? secondaryTexture = null)
    {
        try
        {
            var program = new Shader(_gl, vertexShaderSource ?? ShaderSources.EffectVertex, fragmentShaderSource);
            var kind = secondaryTexture is not null ? EffectShaderKind.CustomSprite : EffectShaderKind.Sprite;
            RegisterEntry(name, kind, program, secondaryTexture);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Sprite shader '{name}' failed: {ex.Message}");
            return false;
        }
    }

    public Texture LoadSecondaryTextureFromFile(string imagePath)
    {
        var tex = new Texture(_gl, imagePath);
        _ownedSecondaryTextures.Add(tex);
        return tex;
    }

    public Texture CreateProceduralRainbowStrip(int width = 512, int height = 32)
    {
        var pixels = new byte[width * height * 4];
        for (var x = 0; x < width; x++)
        {
            RgbFromHue(x / (float)width, out var r, out var g, out var b);
            for (var y = 0; y < height; y++)
            {
                var o = (y * width + x) * 4;
                pixels[o] = r;
                pixels[o + 1] = g;
                pixels[o + 2] = b;
                pixels[o + 3] = 255;
            }
        }

        var tex = new Texture(_gl, width, height, pixels);
        tex.SetWrapRepeat();
        _ownedSecondaryTextures.Add(tex);
        return tex;
    }

    private void RegisterEntry(
        string name,
        EffectShaderKind kind,
        Shader program,
        Texture? secondaryTexture = null)
    {
        if (_entries.TryGetValue(name, out var old))
            old.Dispose();
        if (secondaryTexture is not null && !_ownedSecondaryTextures.Contains(secondaryTexture))
            _ownedSecondaryTextures.Add(secondaryTexture);
        _entries[name] = new EffectShaderEntry(name, kind, program, secondaryTexture);
    }

    private static void RgbFromHue(float h, out byte r, out byte g, out byte b)
    {
        var hi = (int)(h * 6f) % 6;
        var f = h * 6f - (int)(h * 6f);
        var q = (byte)(255 * (1 - f));
        var t = (byte)(255 * f);
        switch (hi)
        {
            case 0: r = 255; g = t; b = 0; break;
            case 1: r = q; g = 255; b = 0; break;
            case 2: r = 0; g = 255; b = t; break;
            case 3: r = 0; g = q; b = 255; break;
            case 4: r = t; g = 0; b = 255; break;
            default: r = 255; g = 0; b = q; break;
        }
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
            entry.Dispose();
        _entries.Clear();
        foreach (var tex in _ownedSecondaryTextures)
            tex.Dispose();
        _ownedSecondaryTextures.Clear();
    }
}
