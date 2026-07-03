namespace NyxRender.Shaders;

public static class OutfitShaderUtil
{
    /// <summary>Palette-only pass (<c>outfit_default</c>). Effect shaders (gold, outline, …) also run on single-layer outfits.</summary>
    public static bool IsEffectShader(string? shaderName) =>
        !string.IsNullOrEmpty(shaderName) &&
        !shaderName.Equals("outfit_default", StringComparison.OrdinalIgnoreCase);
}
