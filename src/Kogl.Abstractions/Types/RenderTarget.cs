namespace Kogl.Abstractions.Types;

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
