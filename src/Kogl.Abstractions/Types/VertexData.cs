using System.Numerics;
using System.Runtime.InteropServices;

namespace Kogl.Abstractions.Types;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexData(Vector3 pos, Vector2 uv, Vector4 color)
{
    public Vector3 Position = pos;
    public Vector2 TexCoord = uv;
    public Vector4 Color = color;
}
