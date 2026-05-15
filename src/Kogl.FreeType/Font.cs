using System.Runtime.InteropServices;
using FreeTypeSharp;
using Kogl.Abstractions;
using Kogl.Core.Resources;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;
using static FreeTypeSharp.FT_Render_Mode_;

namespace Kogl.FreeType;

public unsafe class Font : Resource
{
    private static FT_LibraryRec_* _library;
    private static bool _libraryInitialized;

    private readonly FT_FaceRec_* _face;
    private readonly FontAtlas _atlas;
    private readonly Dictionary<uint, FontGlyph> _glyphs = [];

    public int Size { get; }
    public TextureHandle AtlasTexture => _atlas.Texture;

    public int LineHeight => (int)((long)_face->size->metrics.height >> 6);

    public static Font Load(string path, uint size)
    {
        if (!_libraryInitialized)
        {
            FT_LibraryRec_* lib;
            FT_Error error = FT_Init_FreeType(&lib);
            if (error != FT_Error.FT_Err_Ok)
                throw new Exception($"FreeType: Could not initialize library. Error: {error}");

            _library = lib;
            _libraryInitialized = true;
        }

        return new Font(path, size);
    }

    private Font(string path, uint size)
    {
        IntPtr pathPtr = Marshal.StringToHGlobalAnsi(path);
        try
        {
            FT_FaceRec_* face;
            FT_Error error = FT_New_Face(_library, (byte*)pathPtr, 0, &face);
            if (error != FT_Error.FT_Err_Ok)
                throw new Exception($"FreeType: Failed to load font at {path}. Error: {error}");

            _face = face;
        }
        finally
        {
            Marshal.FreeHGlobal(pathPtr);
        }

        FT_Set_Pixel_Sizes(_face, 0, size);

        Size = (int)size;
        _atlas = new FontAtlas();
    }

    public FontGlyph GetGlyph(uint codepoint)
    {
        if (_glyphs.TryGetValue(codepoint, out FontGlyph cachedGlyph))
            return cachedGlyph;

        uint glyphIndex = FT_Get_Char_Index(_face, codepoint);

        FT_Load_Glyph(_face, glyphIndex, FT_LOAD_DEFAULT);
        FT_Render_Glyph(_face->glyph, FT_RENDER_MODE_NORMAL);

        FT_GlyphSlotRec_* slot = _face->glyph;
        FT_Bitmap_ bitmap = slot->bitmap;

        int width = (int)bitmap.width;
        int height = (int)bitmap.rows;

        float u0 = 0,
            v0 = 0,
            u1 = 0,
            v1 = 0;
        if (width > 0 && height > 0)
        {
            ReadOnlySpan<byte> bitmapData = new(bitmap.buffer, width * height);
            _atlas.TryAddGlyph(bitmapData, width, height, out u0, out v0, out u1, out v1);
        }

        FontGlyph glyph = new(
            width,
            height,
            slot->bitmap_left,
            slot->bitmap_top,
            (int)((long)slot->advance.x >> 6),
            u0,
            v0,
            u1,
            v1
        );

        _glyphs[codepoint] = glyph;
        return glyph;
    }

    protected override void Dispose(bool disposing)
    {
        if (_face != null)
        {
            FT_Done_Face(_face);
        }

        _atlas.Dispose();
    }
}
