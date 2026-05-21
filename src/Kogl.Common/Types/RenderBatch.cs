namespace Kogl.Common.Types;

/// <summary>Render batch containing all data needed for a single draw call</summary>
public struct RenderBatch
{
    /// <summary>Primitive drawing mode</summary>
    public PrimitiveMode Mode;

    /// <summary>Textures used by this batch</summary>
    public TextureSet Textures;

    /// <summary>Shader program to use</summary>
    public ShaderHandle Shader;

    /// <summary>Starting vertex index in the vertex buffer</summary>
    public int VertexOffset;

    /// <summary>Number of vertices to draw</summary>
    public int VertexCount;

    /// <summary>Starting index in the index buffer (if indexed drawing)</summary>
    public int IndexOffset;

    /// <summary>Number of indices to draw (0 = use VertexCount)</summary>
    public int IndexCount;

    /// <summary>Line width for line-based primitives</summary>
    public float LineWidth;
}
