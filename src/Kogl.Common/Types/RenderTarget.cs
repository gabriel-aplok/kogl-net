namespace Kogl.Common.Types;

/// <summary>Render target (framebuffer) with color and depth attachments</summary>
public readonly record struct RenderTarget(
    uint FboId,
    uint RboId,
    TextureHandle[] ColorTextures,
    TextureHandle DepthTexture,
    int Width,
    int Height
)
{
    /// <summary>
    /// Returns the main texture (first color texture if available, otherwise depth texture).
    /// </summary>
    public TextureHandle Texture =>
        ColorTextures != null && ColorTextures.Length > 0 ? ColorTextures[0] : DepthTexture;
}
