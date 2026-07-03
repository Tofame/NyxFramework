namespace NyxRender.Shaders;

public enum EffectShaderKind
{
    /// <summary>Single texture × vertex color (optional tint).</summary>
    Sprite,

    /// <summary>Base + mask template coloring (<c>u_PartColors</c>).</summary>
    OutfitLayers,

    /// <summary>Custom fragment; outfit draws still supply mask UVs.</summary>
    CustomOutfit,

    /// <summary>Custom fragment; single texture sprite/item draw.</summary>
    CustomSprite,
}
