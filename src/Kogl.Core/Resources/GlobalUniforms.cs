using System.Numerics;
using Kogl.Common.Agnostics;

namespace Kogl.Core.Resources;

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

    /// <summary>Sets a global bool uniform</summary>
    public static void SetBool(string name, bool value)
    {
        _globals[name] = value;
    }

    /// <summary>Sets a global vector uniform</summary>
    public static void SetVector2(string name, Vector4 value)
    {
        _globals[name] = value;
    }

    /// <summary>Sets a global vector uniform</summary>
    public static void SetVector3(string name, Vector4 value)
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
    public static void ApplyTo(Shader shader)
    {
        IGraphicsBackend backend = KoRender.GetBackend();
        foreach (KeyValuePair<string, object> kvp in _globals)
        {
            if (kvp.Value is int i)
            {
                backend.SetUniformInt(shader.Handle, kvp.Key, i);
            }
            else if (kvp.Value is float f)
            {
                backend.SetUniformFloat(shader.Handle, kvp.Key, f);
            }
            else if (kvp.Value is bool b)
            {
                backend.SetUniformBool(shader.Handle, kvp.Key, b);
            }
            else if (kvp.Value is Vector2 v2)
            {
                backend.SetUniformVec2(shader.Handle, kvp.Key, v2);
            }
            else if (kvp.Value is Vector3 v3)
            {
                backend.SetUniformVec3(shader.Handle, kvp.Key, v3);
            }
            else if (kvp.Value is Vector4 v4)
            {
                backend.SetUniformVec4(shader.Handle, kvp.Key, v4);
            }
            else if (kvp.Value is Matrix4x4 m44)
            {
                backend.SetUniformMatrix4x4(shader.Handle, kvp.Key, m44);
            }
        }
    }
}
