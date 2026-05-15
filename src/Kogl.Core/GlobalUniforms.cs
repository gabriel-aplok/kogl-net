using System.Numerics;
using Kogl.Abstractions;

namespace Kogl.Core;

public static class GlobalUniforms
{
    private static readonly Dictionary<string, object> _globals = [];

    public static void SetInt(string name, int value)
    {
        _globals[name] = value;
    }

    public static void SetFloat(string name, float value)
    {
        _globals[name] = value;
    }

    public static void SetVector4(string name, Vector4 value)
    {
        _globals[name] = value;
    }

    public static void SetMatrix4(string name, Matrix4x4 value)
    {
        _globals[name] = value;
    }

    public static void ApplyTo(Resources.Shader shader)
    {
        IGraphicsBackend backend = RenderApi.GetBackend();
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
