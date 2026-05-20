using System.Numerics;
using Kogl.Common.Types;

namespace Kogl.Common;

public interface IGraphicsBackend : IDisposable
{
    // Initialization
    public void Initialize();
    public void SetViewport(int x, int y, int width, int height);
    public void Clear(float r, float g, float b, float a);

    // States
    public void SetDepthTest(bool enabled);
    public void SetBlending(bool enabled);

    public void SetCulling(bool enabled, CullFaceState mode = CullFaceState.Back);
    public void SetFrontFace(FrontFaceState mode);
    public void SetPolygonMode(PolygonState mode);
    public void SetPolygonOffset(float factor, float units);
    public void SetDither(bool enabled);
    public void SetLineWidth(float width);
    public void SetPointSize(float size);

    public void SetDepthMask(bool write);
    public void SetColorMask(bool r, bool g, bool b, bool a);

    public void SetBlendFunc(BlendingFactorState src, BlendingFactorState dst);
    public void SetBlendEquation(BlendEquationState mode);
    public void SetDepthFunc(DepthFunctionState func);

    public void SetStencilFunc(StencilFunctionState func, int reference, uint mask);
    public void SetStencilOp(StencilOpState sfail, StencilOpState dpfail, StencilOpState dppass);
    public void SetStencilMask(uint mask);
    public void SetLogicOp(LogicOpState op);
    public void SetStencilTest(bool enabled);

    public void SetClearDepth(float depth);
    public void SetClearStencil(int stencil);

    // Scissors
    public void SetScissor(int x, int y, int width, int height);
    public void SetScissorEnabled(bool enabled);

    // Textures
    public TextureHandle CreateTexture(
        int width,
        int height,
        TextureFormat format,
        TextureFilter minFilter,
        TextureFilter magFilter,
        TextureWrap wrapS,
        TextureWrap wrapT,
        ReadOnlySpan<byte> pixelData
    );
    public void UpdateTexture(
        TextureHandle texture,
        int xOffset,
        int yOffset,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        TextureFormat format
    );

    public void BindTexture(TextureHandle texture, int slot);
    public void DeleteTexture(TextureHandle texture);
    public void SetTextureCompareMode(TextureHandle texture, TextureCompare mode);
    public void SetTextureBorderColor(TextureHandle texture, Vector4 color);

    // RenderTargets
    public RenderTarget CreateRenderTarget(
        int width,
        int height,
        ReadOnlySpan<TextureFormat> colorFormats,
        TextureFormat depthFormat,
        bool depthAsTexture
    );

    public void SetRenderTarget(RenderTarget? target);
    public void DeleteRenderTarget(RenderTarget target);

    // Shaders
    public ShaderHandle CreateShader(string vertexSrc, string fragmentSrc);
    public void BindShader(ShaderHandle shader);

    // Uniforms
    public void SetUniformInt(ShaderHandle shader, string name, int value);
    public void SetUniformFloat(ShaderHandle shader, string name, float value);
    public void SetUniformBool(ShaderHandle shader, string name, bool value);
    public void SetUniformVec2(ShaderHandle shader, string name, in Vector2 value);
    public void SetUniformVec3(ShaderHandle shader, string name, in Vector3 value);
    public void SetUniformVec4(ShaderHandle shader, string name, in Vector4 value);
    public void SetUniformMatrix4x4(ShaderHandle shader, string name, in Matrix4x4 matrix);

    // Buffers
    public void UpdateVertexBuffer(ReadOnlySpan<VertexData> vertices);
    public void UpdateIndexBuffer(ReadOnlySpan<ushort> indices);
    public void DrawBatch(in RenderBatch batch);
}
