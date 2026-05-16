using System.Numerics;
using System.Runtime.InteropServices;

namespace Kogl.Abstractions;

/// <summary>
/// The primitive mode
/// </summary>
public enum PrimitiveMode
{
    Lines,
    LineStrip,
    Triangles,
    TriangleStrip,
    TriangleFan,
    Quads,
}

/// <summary>
/// A texture
/// </summary>
/// <param name="Id">The id</param>
public readonly record struct TextureHandle(uint Id);

/// <summary>
/// A shader
/// </summary>
/// <param name="Id">The id</param>
public readonly record struct ShaderHandle(uint Id);

/// <summary>
/// A render target
/// </summary>
/// <param name="FboId">The fbo id</param>
/// <param name="RboId">The rbo id</param>
/// <param name="Texture">The texture</param>
/// <param name="Width">The width</param>
/// <param name="Height">The height</param>
public readonly record struct RenderTarget(
    uint FboId,
    uint RboId,
    TextureHandle Texture,
    int Width,
    int Height
);

/// <summary>
/// The vertex data
/// </summary>
/// <param name="Position">The position</param>
/// <param name="TexCoord">The texture coordinate</param>
/// <param name="Color">The color</param>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexData(Vector3 pos, Vector2 uv, Vector4 color)
{
    public Vector3 Position = pos;
    public Vector2 TexCoord = uv;
    public Vector4 Color = color;
}

/// <summary>
/// The render batch
/// </summary>
/// <param name="Mode">The mode</param>
/// <param name="Texture">The texture</param>
/// <param name="Shader">The shader</param>
/// <param name="VertexOffset">The vertex offset</param>
/// <param name="VertexCount">The vertex count</param>
/// <param name="IndexOffset">The index offset</param>
/// <param name="IndexCount">The index count</param>
/// <param name="LineWidth">The line width</param>
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
