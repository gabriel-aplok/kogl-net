namespace Kogl.Abstractions.Types;

public readonly record struct RenderTarget(
    uint FboId,
    uint RboId,
    TextureHandle[] ColorTextures,
    TextureHandle DepthTexture,
    int Width,
    int Height
)
{
    public TextureHandle Texture =>
        ColorTextures != null && ColorTextures.Length > 0 ? ColorTextures[0] : DepthTexture;
}
