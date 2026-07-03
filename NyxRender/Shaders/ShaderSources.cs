namespace NyxRender.Shaders;

/// <summary>GLSL 330 core vertex shaders for <see cref="EffectDrawBatch"/> (matches layout: position, UV, mask UV, tint).</summary>
internal static class ShaderSources
{
    /// <summary>Default: base UV, mask UV, and <c>v_TexCoord3</c> for scrolling secondary sampling.</summary>
    public const string EffectVertex = """
        #version 330 core
        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aTexCoord;
        layout (location = 2) in vec2 aMaskTexCoord;
        layout (location = 3) in vec4 aColor;

        uniform mat4 uProjection;
        uniform float u_Time;

        out vec2 v_TexCoord;
        out vec2 v_TexCoord2;
        out vec2 v_TexCoord3;

        void main()
        {
            gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
            v_TexCoord = aTexCoord;
            v_TexCoord2 = aMaskTexCoord;
            v_TexCoord3 = fract(aTexCoord + vec2(u_Time * 0.15, u_Time * 0.08));
        }
        """;
}
