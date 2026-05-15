namespace Kogl.Core.Resources;

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

public sealed class ShaderProperty(string name, ShaderPropertyType type)
{
    public string Name { get; init; } = name;
    public ShaderPropertyType Type { get; init; } = type;
}
