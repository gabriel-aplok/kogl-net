using Kogl.Abstractions;

namespace Kogl.Core.Resources;

/// <summary>
/// The type of a shader property
/// </summary>
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

/// <summary>
/// A shader property
/// </summary>
/// <param name="name">The name of the property</param>
/// <param name="type">The type of the property</param>
public sealed class ShaderProperty(string name, ShaderPropertyType type)
{
    public string Name { get; init; } = name;
    public ShaderPropertyType Type { get; init; } = type;
}

/// <summary>
/// A shader program
/// </summary>
/// <param name="handle">The handle of the shader</param>
public sealed class Shader(ShaderHandle handle) : Resource
{
    public ShaderHandle Handle { get; } = handle;
    private readonly List<ShaderProperty> _properties = [];
    public IReadOnlyList<ShaderProperty> Properties => _properties;

    /// <summary>
    /// Adds a shader property
    /// </summary>
    /// <param name="name">The name of the property</param>
    /// <param name="type">The type of the property</param>
    public void AddProperty(string name, ShaderPropertyType type)
    {
        _properties.Add(new ShaderProperty(name, type));
    }

    protected override void Dispose(bool disposing)
    {
        Console.WriteLine("Shader Disposed");
    }

    /// <summary>
    /// Creates a new shader
    /// </summary>
    /// <param name="vertexSrc">Code for the vertex shader</param>
    /// <param name="fragmentSrc">Code for the fragment shader</param>
    /// <returns></returns>
    public static Shader Create(string vertexSrc, string fragmentSrc)
    {
        ShaderHandle handle = KoGL.GetBackend().CreateShader(vertexSrc, fragmentSrc);
        return new Shader(handle);
    }
}
