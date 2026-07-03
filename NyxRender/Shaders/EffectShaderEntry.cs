namespace NyxRender.Shaders;

public sealed class EffectShaderEntry : IDisposable
{
    internal EffectShaderEntry(
        string name,
        EffectShaderKind kind,
        Shader program,
        Texture? secondaryTexture = null)
    {
        Name = name;
        Kind = kind;
        Program = program;
        SecondaryTexture = secondaryTexture;
    }

    public string Name { get; }
    public EffectShaderKind Kind { get; }
    public Shader Program { get; }
    /// <summary>Bound to texture unit 1 when present (e.g. map snow, outfit gold).</summary>
    public Texture? SecondaryTexture { get; }

    public void Dispose() => Program.Dispose();
}
