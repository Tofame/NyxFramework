#version 330 core

in vec2 v_TexCoord;
in vec4 v_Color;

uniform sampler2D uTexture;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture, v_TexCoord) * v_Color;
    if (FragColor.a < 0.01)
        discard;
}
