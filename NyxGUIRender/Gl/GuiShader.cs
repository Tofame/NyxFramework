using System.Numerics;
using Silk.NET.OpenGL;

namespace NyxGuiRender.Gl;

/// <summary>
/// Compiles and wraps the NyxGUIRender GLSL shader (textured quad with projection).
/// Uniform locations for <c>uProjection</c> and <c>uTexture</c> are cached at construction
/// time and reused for all draw calls.
/// </summary>
internal sealed class GuiShader : IDisposable
{
    private readonly GL _gl;
    private readonly uint _handle;
    private int _projectionLoc = -1;
    private int _textureLoc    = -1;

    public GuiShader(GL gl)
    {
        _gl = gl;
        _handle = CreateProgram(
            """
            #version 330 core
            layout (location = 0) in vec2 aPosition;
            layout (location = 1) in vec2 aTexCoord;
            layout (location = 2) in vec4 aColor;
            uniform mat4 uProjection;
            out vec2 TexCoord;
            out vec4 Color;
            void main() {
                gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
                TexCoord = aTexCoord;
                Color = aColor;
            }
            """,
            """
            #version 330 core
            in vec2 TexCoord;
            in vec4 Color;
            out vec4 FragColor;
            uniform sampler2D uTexture;
            void main() {
                FragColor = texture(uTexture, TexCoord) * Color;
            }
            """);

        _projectionLoc = _gl.GetUniformLocation(_handle, "uProjection");
        _textureLoc    = _gl.GetUniformLocation(_handle, "uTexture");
    }

    public void Use() => _gl.UseProgram(_handle);

    public void SetProjection(Matrix4x4 matrix)
    {
        if (_projectionLoc == -1) return;
        unsafe
        {
            _gl.UniformMatrix4(_projectionLoc, 1, false, (float*)&matrix);
        }
    }

    public void SetTextureUnit(int unit)
    {
        if (_textureLoc != -1)
            _gl.Uniform1(_textureLoc, unit);
    }

    public void Dispose()
    {
        if (_handle != 0)
            _gl.DeleteProgram(_handle);
    }

    private uint CreateProgram(string vertex, string fragment)
    {
        var program = _gl.CreateProgram();
        var vs = Compile(ShaderType.VertexShader, vertex);
        var fs = Compile(ShaderType.FragmentShader, fragment);
        _gl.AttachShader(program, vs);
        _gl.AttachShader(program, fs);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var ok);
        if (ok == 0)
            throw new InvalidOperationException($"GUI shader link failed: {_gl.GetProgramInfoLog(program)}");
        _gl.DetachShader(program, vs);
        _gl.DetachShader(program, fs);
        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);
        return program;
    }

    private uint Compile(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var ok);
        if (ok == 0)
            throw new InvalidOperationException($"GUI shader compile failed: {_gl.GetShaderInfoLog(shader)}");
        return shader;
    }
}
