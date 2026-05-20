namespace Kogl.Common.Types;

public struct RenderBatch
{
    public PrimitiveMode Mode;
    public TextureSet Textures;
    public ShaderHandle Shader;
    public int VertexOffset;
    public int VertexCount;
    public int IndexOffset;
    public int IndexCount;
    public float LineWidth;
}
