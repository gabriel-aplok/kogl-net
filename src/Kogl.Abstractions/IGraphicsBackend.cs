namespace Kogl.Abstractions;

public interface IGraphicsBackend : IDisposable
{
    public void Initialize();
    public void SetViewport(int x, int y, int width, int height);
    public void Clear(float r, float g, float b, float a);

    public TextureHandle CreateTexture(
        ReadOnlySpan<byte> pixelData,
        int width,
        int height,
        int channels
    );
    public void BindTexture(TextureHandle texture);
    public void DeleteTexture(TextureHandle texture);

    public ShaderHandle CreateShader(string vertexSrc, string fragmentSrc);
    public void BindShader(ShaderHandle shader);
    public void SetUniformMatrix4(
        ShaderHandle shader,
        string name,
        in System.Numerics.Matrix4x4 matrix
    );

    public void UpdateVertexBuffer(ReadOnlySpan<VertexData> vertices);
    public void UpdateIndexBuffer(ReadOnlySpan<ushort> indices);
    public void DrawBatch(in RenderBatch batch);
}
