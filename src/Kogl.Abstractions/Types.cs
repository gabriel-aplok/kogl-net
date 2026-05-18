using System.Numerics;
using System.Runtime.InteropServices;
using Kogl.Abstractions.Types;

namespace Kogl.Abstractions;

public readonly record struct TextureHandle(uint Id);

public readonly record struct ShaderHandle(uint Id);

public readonly record struct RenderTarget(
    uint FboId,
    uint RboId,
    TextureHandle[] Textures,
    int Width,
    int Height
)
{
    public TextureHandle Texture => Textures != null && Textures.Length > 0 ? Textures[0] : default;
}

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
