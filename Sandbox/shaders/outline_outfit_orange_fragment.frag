#version 330 core

in vec2 v_TexCoord;
in vec2 v_TexCoord2;

uniform sampler2D uTexture;
uniform vec4 u_PartColors[4];
uniform float u_PaletteFromMask;
uniform vec2 u_TexStep;
uniform vec2 u_UvMin;
uniform vec2 u_UvMax;

out vec4 FragColor;

float sampleNeighborAlpha(vec2 delta)
{
    vec2 uv = v_TexCoord + delta;
    if (uv.x < u_UvMin.x || uv.x > u_UvMax.x || uv.y < u_UvMin.y || uv.y > u_UvMax.y)
        return 0.0;
    return texture(uTexture, uv).a;
}

void main()
{
    FragColor = texture(uTexture, v_TexCoord);
    vec4 mask = texture(uTexture, v_TexCoord2);
    if (u_PaletteFromMask > 0.5)
    {
        if (mask.r > 0.9)
            FragColor.rgb *= mask.g > 0.9 ? u_PartColors[0].rgb : u_PartColors[1].rgb;
        else if (mask.g > 0.9)
            FragColor.rgb *= u_PartColors[2].rgb;
        else if (mask.b > 0.9)
            FragColor.rgb *= u_PartColors[3].rgb;
    }

    float neighbor = max(
        max(max(sampleNeighborAlpha(vec2(0.0, u_TexStep.y)), sampleNeighborAlpha(vec2(0.0, -u_TexStep.y))),
            max(sampleNeighborAlpha(vec2(-u_TexStep.x, 0.0)), sampleNeighborAlpha(vec2(u_TexStep.x, 0.0)))),
        max(max(sampleNeighborAlpha(vec2(-u_TexStep.x, u_TexStep.y)), sampleNeighborAlpha(vec2(u_TexStep.x, u_TexStep.y))),
            max(sampleNeighborAlpha(vec2(-u_TexStep.x, -u_TexStep.y)), sampleNeighborAlpha(vec2(u_TexStep.x, -u_TexStep.y)))));

    vec3 outline = vec3(1.0, 0.55, 0.0);

    if (FragColor.a <= 0.05)
    {
        if (neighbor > 0.05)
            FragColor = vec4(outline, 1.0);
        else
            discard;
    }
}
