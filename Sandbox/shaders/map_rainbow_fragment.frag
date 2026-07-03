#version 330 core

in vec2 v_TexCoord;
in vec2 v_TexCoord3;
in vec4 v_Color;

uniform sampler2D uTexture;
uniform sampler2D uEffectTexture;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture, v_TexCoord) * v_Color;
    FragColor += texture(uEffectTexture, v_TexCoord3);
    if (FragColor.a < 0.01)
        discard;
}
