using Kogl.Abstractions;

namespace Kogl.Core.Resources;

/// <summary>
///  A texture
/// </summary>
/// <param name="handle">The handle of the texture</param>
/// <param name="width">The width</param>
/// <param name="height">The height</param>
public class Texture(TextureHandle handle, int width, int height) : Resource
{
    public TextureHandle Handle { get; } = handle;
    public int Width { get; } = width;
    public int Height { get; } = height;

    protected override void Dispose(bool disposing)
    {
        RenderApi.DeleteTexture(Handle);
    }
}
