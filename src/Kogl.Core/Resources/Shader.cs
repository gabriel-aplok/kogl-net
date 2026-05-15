using Kogl.Abstractions;

namespace Kogl.Core.Resources;

public sealed class Shader(ShaderHandle handle) : Resource
{
    public ShaderHandle Handle { get; } = handle;
    private readonly List<ShaderProperty> _properties = [];
    public IReadOnlyList<ShaderProperty> Properties => _properties;

    public void AddProperty(string name, ShaderPropertyType type)
    {
        _properties.Add(new ShaderProperty(name, type));
    }

    protected override void Dispose(bool disposing)
    {
        Console.WriteLine("Shader Disposed");
    }

    public static Shader Create(string vertexSrc, string fragmentSrc)
    {
        ShaderHandle handle = RenderApi.GetBackend().CreateShader(vertexSrc, fragmentSrc);
        return new Shader(handle);
    }
}
