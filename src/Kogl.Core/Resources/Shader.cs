using Kogl.Abstractions;

namespace Kogl.Core.Resources;

/// <summary>The type of a shader property</summary>
public enum ShaderPropertyType
{
    Int,
    Float,
    Vec2,
    Vec3,
    Vec4,
    Mat4,
    Texture2D,
}

/// <summary>A shader property</summary>
public sealed class ShaderProperty(string name, ShaderPropertyType type)
{
    public string Name { get; init; } = name;
    public ShaderPropertyType Type { get; init; } = type;
}

/// <summary>A shader program</summary>
public sealed class Shader(ShaderHandle handle) : Resource
{
    public ShaderHandle Handle { get; } = handle;
    public IReadOnlyList<ShaderProperty> Properties => _properties;

    private readonly List<ShaderProperty> _properties = [];

    /// <summary>Adds a shader property</summary>
    public void AddProperty(string name, ShaderPropertyType type)
    {
        _properties.Add(new ShaderProperty(name, type));
    }

    protected override void Dispose(bool disposing)
    {
        Log.Info("Shader Disposed");
    }

    /// <summary>Creates a new shader</summary>
    public static Shader Create(string vertexSrc, string fragmentSrc)
    {
        ShaderHandle handle = KoGL.GetBackend().CreateShader(vertexSrc, fragmentSrc);
        return new Shader(handle);
    }
}
