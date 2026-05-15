namespace Kogl.FreeType;

public interface IFontGlyph
{
    public int Width { get; }
    public int Height { get; }
    public int BearingX { get; }
    public int BearingY { get; }
    public int Advance { get; }

    // yv coords in the atlas
    public float U0 { get; }
    public float V0 { get; }
    public float U1 { get; }
    public float V1 { get; }
}

public readonly struct FontGlyph(
    int width,
    int height,
    int bearingX,
    int bearingY,
    int advance,
    float u0,
    float v0,
    float u1,
    float v1
) : IFontGlyph
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int BearingX { get; } = bearingX;
    public int BearingY { get; } = bearingY;
    public int Advance { get; } = advance;
    public float U0 { get; } = u0;
    public float V0 { get; } = v0;
    public float U1 { get; } = u1;
    public float V1 { get; } = v1;
}
