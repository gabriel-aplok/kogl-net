namespace Kogl.Abstractions.Types;

public readonly record struct TextureHandle(uint Id);

public enum TextureFormat
{
    None,
    R8,
    Rg8,
    Rgb8,
    Rgba8,
    Rgba16F,
    Rgba32F,
    Depth16,
    Depth24,
    Depth32F,
    Depth24Stencil8,
}

public enum TextureFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear,
}

public enum TextureWrap
{
    Repeat,
    MirroredRepeat,
    ClampToEdge,
    ClampToBorder,
}

public enum TextureCompare
{
    None,
    CompareRefToTexture,
}
