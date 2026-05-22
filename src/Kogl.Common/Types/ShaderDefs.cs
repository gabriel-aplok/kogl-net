namespace Kogl.Common.Types;

/// <summary>Immutable handle to a compiled GPU shader program</summary>
public readonly record struct ShaderHandle(uint Id);

/// <summary>The type of a shader property</summary>
public enum ShaderPropertyType
{
    Int,
    Float,
    Bool,
    Vec2,
    Vec3,
    Vec4,
    Mat4,
    Texture2D,
}
