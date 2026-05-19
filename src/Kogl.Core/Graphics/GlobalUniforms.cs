using System.Numerics;
using Kogl.Abstractions;

namespace Kogl.Core.Graphics;

/// <summary>A collection of global uniforms</summary>
public static class GlobalUniforms
{
    private static readonly Dictionary<string, object> _globals = [];

    /// <summary>Sets a global int uniform</summary>
    public static void SetInt(string name, int value)
    {
        _globals[name] = value;
    }

    /// <summary>Sets a global float uniform</summary>
    public static void SetFloat(string name, float value)
    {
        _globals[name] = value;
    }

    /// <summary>Sets a global vector uniform</summary>
    public static void SetVector4(string name, Vector4 value)
    {
        _globals[name] = value;
    }

    /// <summary>Sets a global matrix uniform</summary>
    public static void SetMatrix4(string name, Matrix4x4 value)
    {
        _globals[name] = value;
    }

    /// <summary>Applies the global uniforms to a shader</summary>
    public static void ApplyTo(Resources.Shader shader)
    {
        IGraphicsBackend backend = KoGL.GetBackend();
        foreach (KeyValuePair<string, object> kvp in _globals)
        {
            if (kvp.Value is float f)
                backend.SetUniformFloat(shader.Handle, kvp.Key, f);
            else if (kvp.Value is int i)
                backend.SetUniformInt(shader.Handle, kvp.Key, i);
            else if (kvp.Value is Vector4 v)
                backend.SetUniformVec4(shader.Handle, kvp.Key, v);
            else if (kvp.Value is Matrix4x4 m)
                backend.SetUniformMatrix4(shader.Handle, kvp.Key, m);
        }
    }
}
