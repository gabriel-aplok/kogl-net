using Kogl.Abstractions;

namespace Kogl.Core.Resources;

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
