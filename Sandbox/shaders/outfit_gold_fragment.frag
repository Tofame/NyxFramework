#version 330 core

in vec2 v_TexCoord;
in vec2 v_TexCoord2;

uniform sampler2D uTexture;
uniform sampler2D uEffectTexture;
uniform vec4 u_PartColors[4];
uniform float u_PaletteFromMask;

out vec4 FragColor;

void main()
{
    vec4 base = texture(uTexture, v_TexCoord);
    vec4 mask = texture(uTexture, v_TexCoord2);

    float cover = u_PaletteFromMask > 0.5 ? mask.a : base.a;
    if (base.a < 0.01)
        discard;

    if (cover < 0.08)
    {
        FragColor = base;
        return;
    }

    // Folds/shadows from the template — do not multiply dye colors (that + gold = flat neon).
    float shade = dot(base.rgb, vec3(0.299, 0.587, 0.114));
    shade = clamp(shade * 1.1 + 0.18, 0.22, 1.0);

    // Tile foil so 32×32 cells show texture detail, not one flat texel.
    vec3 foil = texture(uEffectTexture, fract(v_TexCoord * 5.0)).rgb;
    float foilL = dot(foil, vec3(0.25, 0.65, 0.10));

    vec3 gold = foil * vec3(0.92, 0.78, 0.42) * shade;
    gold += vec3(smoothstep(0.55, 0.92, foilL) * 0.10);

    FragColor = base;
    FragColor.rgb = mix(base.rgb * 0.35, gold, cover * 0.78);
    FragColor.rgb = clamp(FragColor.rgb, 0.0, 1.0);
}
