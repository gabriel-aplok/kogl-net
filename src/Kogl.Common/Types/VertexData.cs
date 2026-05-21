namespace Kogl.Common.Types;

using System.Numerics;
using System.Runtime.InteropServices;

/// <summary>Vertex data struct for rendering</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexData(
    Vector3 pos,
    Vector2 uv,
    Vector4 color,
    Vector3 normal = default,
    Vector4 tangent = default
)
{
    /// <summary>Vertex position</summary>
    public Vector3 Position = pos;

    /// <summary>Texture coordinates</summary>
    public Vector2 TexCoord = uv;

    /// <summary>Vertex color (RGBA)</summary>
    public Vector4 Color = color;

    /// <summary>Vertex normal</summary>
    public Vector3 Normal = normal;

    /// <summary>Tangent vector (for normal mapping)</summary>
    public Vector4 Tangent = tangent;
}
