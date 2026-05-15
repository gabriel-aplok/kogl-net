using System.Numerics;

namespace Kogl.Abstractions;

public interface IGraphicsBackend : IDisposable
{
    // Initialization
    public void Initialize();
    public void SetViewport(int x, int y, int width, int height);
    public void Clear(float r, float g, float b, float a);

    // Textures
    public TextureHandle CreateTexture(
        ReadOnlySpan<byte> pixelData,
        int width,
        int height,
        int channels
    );
    public void UpdateTexture(
        TextureHandle texture,
        int xOffset,
        int yOffset,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        int channels
    );
    public void BindTexture(TextureHandle texture);
    public void DeleteTexture(TextureHandle texture);

    // RenderTargets
    public RenderTarget CreateRenderTarget(int width, int height);
    public void SetRenderTarget(RenderTarget? target);
    public void DeleteRenderTarget(RenderTarget target);

    // Shaders
    public ShaderHandle CreateShader(string vertexSrc, string fragmentSrc);
    public void BindShader(ShaderHandle shader);

    // Uniforms
    public void SetUniformInt(ShaderHandle shader, string name, int value);
    public void SetUniformFloat(ShaderHandle shader, string name, float value);
    public void SetUniformVec2(ShaderHandle shader, string name, in Vector2 value);
    public void SetUniformVec3(ShaderHandle shader, string name, in Vector3 value);
    public void SetUniformVec4(ShaderHandle shader, string name, in Vector4 value);
    public void SetUniformMatrix4(ShaderHandle shader, string name, in Matrix4x4 matrix);

    // States
    public void SetDepthTest(bool enabled);
    public void SetBlending(bool enabled);

    // Scissors
    public void SetScissor(int x, int y, int width, int height);
    public void SetScissorEnabled(bool enabled);

    // Buffers
    public void UpdateVertexBuffer(ReadOnlySpan<VertexData> vertices);
    public void UpdateIndexBuffer(ReadOnlySpan<ushort> indices);
    public void DrawBatch(in RenderBatch batch);
}
