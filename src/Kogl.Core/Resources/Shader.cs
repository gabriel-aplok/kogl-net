using Kogl.Common.Types;

namespace Kogl.Core.Resources;

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

    public static Shader Create(string vertexSrc, string fragmentSrc)
    {
        ShaderHandle handle = KoRender.GetBackend().CreateShader(vertexSrc, fragmentSrc);
        return new Shader(handle);
    }

    public void AddProperty(string name, ShaderPropertyType type)
    {
        _properties.Add(new ShaderProperty(name, type));
    }

    public void RemoveProperty(string name)
    {
        _properties.RemoveAll(p => p.Name == name);
    }

    protected override void DisposeManaged() { }
}
