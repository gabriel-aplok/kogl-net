using System.Numerics;
using System.Runtime.InteropServices;

namespace Kogl.Abstractions;

public enum PrimitiveMode
{
    Lines,
    LineStrip,
    Triangles,
    TriangleStrip,
    TriangleFan,
    Quads, // will be triangulated by the batcher
}

public readonly record struct TextureHandle(uint Id);

public readonly record struct ShaderHandle(uint Id);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexData(Vector3 pos, Vector2 uv, Vector4 color)
{
    public Vector3 Position = pos;
    public Vector2 TexCoord = uv;
    public Vector4 Color = color;
}

public struct RenderBatch
{
    public PrimitiveMode Mode;
    public TextureHandle Texture;
    public ShaderHandle Shader;
    public int VertexOffset;
    public int VertexCount;
    public int IndexOffset;
    public int IndexCount;
    public float LineWidth;
}
