using System.Runtime.InteropServices;
using FreeTypeSharp;
using Kogl.Common.Types;
using Kogl.Core.Resources;
using static FreeTypeSharp.FT;
using static FreeTypeSharp.FT_LOAD;
using static FreeTypeSharp.FT_Render_Mode_;

namespace Kogl.FreeType;

/// <summary>A font</summary>
public unsafe class Font : Resource
{
    private static FT_LibraryRec_* _library;
    private static bool _libraryInitialized;

    private readonly FT_FaceRec_* _face;
    private readonly FontAtlas _atlas;
    private readonly Dictionary<uint, FontGlyph> _glyphs = [];

    public int Size { get; }
    public TextureHandle AtlasTexture => _atlas.Texture;
    public bool IsSdf { get; private set; }

    public int LineHeight => (int)((long)_face->size->metrics.height >> 6);

    private Font(string path, uint size)
    {
        _face = LoadFace(path);
        FT_Set_Pixel_Sizes(_face, 0, size);

        Size = (int)size;
        _atlas = new FontAtlas();
    }

    private static void InitializeLibrary()
    {
        FT_LibraryRec_* lib;
        FT_Error error = FT_Init_FreeType(&lib);
        if (error != FT_Error.FT_Err_Ok)
            throw new Exception($"FreeType: Could not initialize library. Error: {error}");

        _library = lib;
        _libraryInitialized = true;
    }

    private static FT_FaceRec_* LoadFace(string path)
    {
        IntPtr pathPtr = Marshal.StringToHGlobalAnsi(path);
        try
        {
            FT_FaceRec_* face;
            FT_Error error = FT_New_Face(_library, (byte*)pathPtr, 0, &face);
            if (error != FT_Error.FT_Err_Ok)
                throw new Exception($"FreeType: Failed to load font at {path}. Error: {error}");

            return face;
        }
        finally
        {
            Marshal.FreeHGlobal(pathPtr);
        }
    }

    /// <summary>Loads a font from a file</summary>
    public static Font Load(string path, uint size)
    {
        if (!_libraryInitialized)
            InitializeLibrary();

        return new Font(path, size);
    }

    /// <summary>Loads an SDF font from a file</summary>
    public static Font LoadSdf(string path, uint size)
    {
        Font font = Load(path, size);
        font.IsSdf = true;
        return font;
    }

    /// <summary>Retrieves the glyph for the given codepoint</summary>
    public FontGlyph GetGlyph(uint codepoint)
    {
        if (_glyphs.TryGetValue(codepoint, out FontGlyph cached))
            return cached;

        uint glyphIndex = FT_Get_Char_Index(_face, codepoint);
        FT_Load_Glyph(_face, glyphIndex, FT_LOAD_DEFAULT);
        FT_Render_Glyph(_face->glyph, IsSdf ? FT_RENDER_MODE_SDF : FT_RENDER_MODE_NORMAL);

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
            ReadOnlySpan<byte> data = new(bitmap.buffer, width * height);
            _atlas.TryAddGlyph(data, width, height, out u0, out v0, out u1, out v1);
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

    protected override void DisposeManaged()
    {
        if (_face != null)
        {
            FT_Done_Face(_face);
        }

        _atlas.Dispose();
        _glyphs.Clear();
    }
}
