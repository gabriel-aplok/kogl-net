using Kogl.Common.Types;

namespace Kogl.Core.Resources;

/// <summary>A texture</summary>
public class Texture(TextureHandle handle, int width, int height) : Resource
{
    public TextureHandle Handle { get; } = handle;
    public int Width { get; } = width;
    public int Height { get; } = height;

    protected override void DisposeManaged()
    {
        KoRender.DeleteTexture(Handle);
    }
}
