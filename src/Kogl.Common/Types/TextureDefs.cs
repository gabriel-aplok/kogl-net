namespace Kogl.Common.Types;

/// <summary>Immutable handle to a GPU texture</summary>
public readonly record struct TextureHandle(uint Id);

/// <summary>Texture internal format</summary>
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

/// <summary>Texture sampling filter</summary>
public enum TextureFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear,
}

/// <summary>Texture wrapping mode</summary>
public enum TextureWrap
{
    Repeat,
    MirroredRepeat,
    ClampToEdge,
    ClampToBorder,
}

/// <summary>Depth texture comparison mode</summary>
public enum TextureCompare
{
    None,
    CompareRefToTexture,
}
