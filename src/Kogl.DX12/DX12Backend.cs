using System.Numerics;
using Kogl.Abstractions;

namespace Kogl.DX12;

public sealed class DX12Backend : IGraphicsBackend
{
    public void BindShader(ShaderHandle shader)
    {
        throw new NotImplementedException();
    }

    public void BindTexture(TextureHandle texture)
    {
        throw new NotImplementedException();
    }

    public void Clear(float r, float g, float b, float a)
    {
        throw new NotImplementedException();
    }

    public RenderTarget CreateRenderTarget(int width, int height)
    {
        throw new NotImplementedException();
    }

    public void SetRenderTarget(RenderTarget? target)
    {
        throw new NotImplementedException();
    }

    public void DeleteRenderTarget(RenderTarget target)
    {
        throw new NotImplementedException();
    }

    public ShaderHandle CreateShader(string vertexSrc, string fragmentSrc)
    {
        throw new NotImplementedException();
    }

    public TextureHandle CreateTexture(
        ReadOnlySpan<byte> pixelData,
        int width,
        int height,
        int channels
    )
    {
        throw new NotImplementedException();
    }

    public void DeleteTexture(TextureHandle texture)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void DrawBatch(in RenderBatch batch)
    {
        throw new NotImplementedException();
    }

    public void Initialize()
    {
        throw new NotImplementedException();
    }

    public void SetUniformFloat(ShaderHandle shader, string name, float value)
    {
        throw new NotImplementedException();
    }

    public void SetUniformInt(ShaderHandle shader, string name, int value)
    {
        throw new NotImplementedException();
    }

    public void SetUniformMatrix4(ShaderHandle shader, string name, in Matrix4x4 matrix)
    {
        throw new NotImplementedException();
    }

    public void SetUniformVec2(ShaderHandle shader, string name, in Vector2 value)
    {
        throw new NotImplementedException();
    }

    public void SetUniformVec3(ShaderHandle shader, string name, in Vector3 value)
    {
        throw new NotImplementedException();
    }

    public void SetUniformVec4(ShaderHandle shader, string name, in Vector4 value)
    {
        throw new NotImplementedException();
    }

    public void SetViewport(int x, int y, int width, int height)
    {
        throw new NotImplementedException();
    }

    public void UpdateIndexBuffer(ReadOnlySpan<ushort> indices)
    {
        throw new NotImplementedException();
    }

    public void UpdateVertexBuffer(ReadOnlySpan<VertexData> vertices)
    {
        throw new NotImplementedException();
    }

    public void SetScissor(int x, int y, int width, int height)
    {
        throw new NotImplementedException();
    }

    public void SetScissorEnabled(bool enabled)
    {
        throw new NotImplementedException();
    }
}
