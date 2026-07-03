#version 330 core

in vec2 v_TexCoord;
in vec4 v_Color;

uniform sampler2D uTexture;
uniform sampler2D uEffectTexture;
uniform float u_Time;
uniform vec2 u_WalkOffset;

out vec4 FragColor;

const vec2 kSnowDir = vec2(1.0, 0.2);
const float kSnowSpeed = 0.08;
const float kSnowPressure = 0.4;
const float kSnowZoom = 0.6;

void main()
{
    FragColor = texture(uTexture, v_TexCoord) * v_Color;

    vec2 snowCoord = (v_TexCoord + u_WalkOffset + kSnowDir * u_Time * kSnowSpeed) / kSnowZoom;
    FragColor += texture(uEffectTexture, snowCoord) * kSnowPressure;

    if (FragColor.a < 0.01)
        discard;
}
