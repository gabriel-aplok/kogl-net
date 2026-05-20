using System.Numerics;
using Kogl.Common;

namespace Kogl.Core.Resources;

/// <summary>A material</summary>
public class Material : Resource
{
    public Shader Shader { get; }

    public bool DepthTest { get; set; } = true;
    public bool Blending { get; set; } = true;

    private readonly Dictionary<string, object> _parameters = [];
    private readonly Material? _parent;

    public Material(Shader shader)
    {
        Shader = shader;
    }

    protected Material(Material parent)
    {
        _parent = parent;
        Shader = parent.Shader;
        DepthTest = parent.DepthTest;
        Blending = parent.Blending;
    }

    public Material CreateInstance()
    {
        return new Material(this);
    }

    public void SetInt(string name, int value)
    {
        _parameters[name] = value;
    }

    public void SetFloat(string name, float value)
    {
        _parameters[name] = value;
    }

    public void SetBool(string name, bool value)
    {
        _parameters[name] = value;
    }

    public void SetVector2(string name, Vector2 value)
    {
        _parameters[name] = value;
    }

    public void SetVector3(string name, Vector3 value)
    {
        _parameters[name] = value;
    }

    public void SetVector4(string name, Vector4 value)
    {
        _parameters[name] = value;
    }

    public void SetMatrix4x4(string name, Matrix4x4 value)
    {
        _parameters[name] = value;
    }

    public void SetTexture(string name, Texture texture)
    {
        _parameters[name] = texture;
    }

    public object? GetParameter(string name)
    {
        return _parameters.TryGetValue(name, out object? value)
            ? value
            : _parent?.GetParameter(name);
    }

    public void Apply()
    {
        IGraphicsBackend backend = KoRender.GetBackend();
        backend.BindShader(Shader.Handle);
        backend.SetDepthTest(DepthTest);
        backend.SetBlending(Blending);

        int textureSlot = 0;

        foreach (ShaderProperty prop in Shader.Properties)
        {
            object? val = GetParameter(prop.Name);
            if (val == null)
                continue;

            switch (prop.Type)
            {
                case ShaderPropertyType.Int:
                    backend.SetUniformInt(Shader.Handle, prop.Name, (int)val);
                    break;
                case ShaderPropertyType.Float:
                    backend.SetUniformFloat(Shader.Handle, prop.Name, (float)val);
                    break;
                case ShaderPropertyType.Bool:
                    backend.SetUniformBool(Shader.Handle, prop.Name, (bool)val);
                    break;
                case ShaderPropertyType.Vec2:
                    backend.SetUniformVec2(Shader.Handle, prop.Name, (Vector2)val);
                    break;
                case ShaderPropertyType.Vec3:
                    backend.SetUniformVec3(Shader.Handle, prop.Name, (Vector3)val);
                    break;
                case ShaderPropertyType.Vec4:
                    backend.SetUniformVec4(Shader.Handle, prop.Name, (Vector4)val);
                    break;
                case ShaderPropertyType.Mat4:
                    backend.SetUniformMatrix4x4(Shader.Handle, prop.Name, (Matrix4x4)val);
                    break;
                case ShaderPropertyType.Texture2D:
                    Texture tex = (Texture)val;
                    KoRender.UseTexture(tex.Handle, textureSlot);
                    backend.SetUniformInt(Shader.Handle, prop.Name, textureSlot);
                    textureSlot++;
                    break;
            }
        }
    }

    protected override void DisposeManaged()
    {
        Log.Info("Material Disposed");
        _parameters.Clear();
    }
}
