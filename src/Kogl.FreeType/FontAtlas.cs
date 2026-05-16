using Kogl.Abstractions;
using Kogl.Core;

namespace Kogl.FreeType;

internal class FontAtlas : IDisposable
{
    public TextureHandle Texture { get; private set; }
    public int Size { get; }

    private int _currentX = 0;
    private int _currentY = 0;
    private int _currentRowHeight = 0;
    private const int _padding = 1;

    public FontAtlas(int size = 2048)
    {
        Size = size;

        // initialize an empty RGBA texture
        byte[] emptyData = new byte[size * size * 4];
        Texture = KoGL.GetBackend().CreateTexture(emptyData, size, size, 4);
    }

    public bool TryAddGlyph(
        ReadOnlySpan<byte> grayscaleBitmap,
        int width,
        int height,
        out float u0,
        out float v0,
        out float u1,
        out float v1
    )
    {
        if (_currentX + width + _padding > Size)
        {
            _currentX = 0;
            _currentY += _currentRowHeight + _padding;
            _currentRowHeight = 0;
        }

        if (_currentY + height + _padding > Size)
        {
            u0 = v0 = u1 = v1 = 0;
            return false;
        }

        // convert freeType's 8-bit grayscale to 32-bit RGBA for the kogl backend
        // set RGB to white, and use the grayscale value as the Alpha channel
        byte[] rgbaData = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            byte alpha = grayscaleBitmap[i];
            rgbaData[(i * 4) + 0] = 255;
            rgbaData[(i * 4) + 1] = 255;
            rgbaData[(i * 4) + 2] = 255;
            rgbaData[(i * 4) + 3] = alpha;
        }

        // upload glyph to gpu
        KoGL.UpdateTexture(Texture, _currentX, _currentY, width, height, rgbaData, 4);

        // calculate UVs
        u0 = (float)_currentX / Size;
        v0 = (float)_currentY / Size;
        u1 = (float)(_currentX + width) / Size;
        v1 = (float)(_currentY + height) / Size;

        _currentX += width + _padding;
        if (height > _currentRowHeight)
            _currentRowHeight = height;

        return true;
    }

    public void Dispose()
    {
        KoGL.DeleteTexture(Texture);
    }
}
