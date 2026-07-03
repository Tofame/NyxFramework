using Silk.NET.OpenGL;
using System.Numerics;

namespace NyxRender
{
    /// <summary>
    /// Compiles and wraps an OpenGL shader program with uniform-setting helpers.
    /// Uniform locations are lazily resolved and cached in a dictionary for the
    /// program's lifetime (they are stable once linking succeeds).
    ///
    /// <see cref="SetMatrix4"/> is a convenience alias for <see cref="SetUniform(string, Matrix4x4)"/>
    /// kept for backwards compatibility with older call sites.
    /// </summary>
    public sealed class Shader : IDisposable
    {
        private GL _gl;
        private uint _handle;
        private bool _disposed = false;
        private readonly Dictionary<string, int> _locationCache = new();

        /// <summary>The OpenGL program object handle.</summary>
        public uint Handle => _handle;

        public Shader(GL gl, string vertexShaderSource, string fragmentShaderSource)
        {
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));
            _handle = CreateProgram(vertexShaderSource, fragmentShaderSource);
        }

        private uint CreateProgram(string vertexShaderSource, string fragmentShaderSource)
        {
            uint program = _gl.CreateProgram();

            uint vertexShader = CompileShader(vertexShaderSource, ShaderType.VertexShader);
            uint fragmentShader = CompileShader(fragmentShaderSource, ShaderType.FragmentShader);

            _gl.AttachShader(program, vertexShader);
            _gl.AttachShader(program, fragmentShader);
            _gl.LinkProgram(program);

            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = _gl.GetProgramInfoLog(program);
                throw new Exception($"Shader program linking failed: {infoLog}");
            }

            // Shader objects are no longer needed after linking.
            _gl.DetachShader(program, vertexShader);
            _gl.DetachShader(program, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            return program;
        }

        private uint CompileShader(string source, ShaderType type)
        {
            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                throw new Exception($"Shader compilation failed ({type}): {infoLog}");
            }

            return shader;
        }

        /// <summary>
        /// Returns the uniform location for <paramref name="name"/>, resolving and
        /// caching it on first access.  Locations are stable for the program's lifetime.
        /// </summary>
        private int GetLocation(string name)
        {
            if (!_locationCache.TryGetValue(name, out var loc))
            {
                loc = _gl.GetUniformLocation(_handle, name);
                _locationCache[name] = loc;
            }
            return loc;
        }

        /// <summary>Activates this shader program for subsequent draws.</summary>
        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        public void SetUniform(string name, int value)
        {
            int location = GetLocation(name);
            if (location != -1)
                _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, float value)
        {
            int location = GetLocation(name);
            if (location != -1)
                _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, Vector2 value)
        {
            int location = GetLocation(name);
            if (location != -1)
                _gl.Uniform2(location, value.X, value.Y);
        }

        public void SetUniform(string name, Vector3 value)
        {
            int location = GetLocation(name);
            if (location != -1)
                _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Vector4 value)
        {
            int location = GetLocation(name);
            if (location != -1)
                _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
        }

        public void SetUniform(string name, Matrix4x4 value)
        {
            int location = GetLocation(name);
            if (location != -1)
            {
                unsafe
                {
                    _gl.UniformMatrix4(location, 1, false, (float*)&value);
                }
            }
        }

        /// <summary>Convenience alias for <see cref="SetUniform(string, Matrix4x4)"/>.</summary>
        public void SetMatrix4(string name, Matrix4x4 value)
        {
            SetUniform(name, value);
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed)
                return;

            _locationCache.Clear();

            if (_handle != 0)
            {
                _gl.DeleteProgram(_handle);
                _handle = 0;
            }

            _disposed = true;
        }

        #endregion
    }
}
