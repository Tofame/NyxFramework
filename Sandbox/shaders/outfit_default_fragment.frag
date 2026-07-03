#version 330 core

in vec2 v_TexCoord;
in vec2 v_TexCoord2;

uniform sampler2D uTexture;
uniform vec4 u_PartColors[4];
uniform float u_PaletteFromMask;

out vec4 FragColor;

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

    if (FragColor.a < 0.01)
        discard;
}
