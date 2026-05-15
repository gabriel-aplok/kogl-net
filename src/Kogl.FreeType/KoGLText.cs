using System.Numerics;
using System.Text;
using Kogl.Abstractions;
using Kogl.Core;

namespace Kogl.FreeType;

public static class KoGLText
{
    /// <summary>
    /// Draws a string to the screen.
    /// </summary>
    public static void DrawText(
        Font font,
        string text,
        Vector2 position,
        Vector4 color,
        TextAlignment alignment = TextAlignment.Left
    )
    {
        if (string.IsNullOrEmpty(text))
            return;

        ReadOnlySpan<char> span = text.AsSpan();

        // handle alignment offset
        if (alignment != TextAlignment.Left)
        {
            Vector2 size = Measure(font, span);
            if (alignment == TextAlignment.Center)
                position.X -= size.X * 0.5f;
            if (alignment == TextAlignment.Right)
                position.X -= size.X;
        }

        RenderApi.UseTexture(font.AtlasTexture);

        // push to the stack to respect parent transforms easily
        RenderApi.PushMatrix();
        RenderApi.Translate(position.X, position.Y, 0);

        RenderApi.Color4(color.X, color.Y, color.Z, color.W);
        RenderApi.Begin(PrimitiveMode.Quads);

        float cursorX = 0;
        float cursorY = font.Size; // base alignment

        while (span.Length > 0)
        {
            Rune.DecodeFromUtf16(span, out Rune rune, out int charsConsumed);
            span = span[charsConsumed..];

            if (rune.Value == '\n')
            {
                cursorX = 0;
                cursorY += font.LineHeight;
                continue;
            }

            FontGlyph glyph = font.GetGlyph((uint)rune.Value);

            if (glyph.Width > 0 && glyph.Height > 0)
            {
                float xpos = cursorX + glyph.BearingX;
                float ypos = cursorY - glyph.BearingY;
                float w = glyph.Width;
                float h = glyph.Height;

                // push quad vertices directly into the batcher
                RenderApi.TexCoord2(glyph.U0, glyph.V0);
                RenderApi.Vertex2(xpos, ypos);
                RenderApi.TexCoord2(glyph.U1, glyph.V0);
                RenderApi.Vertex2(xpos + w, ypos);
                RenderApi.TexCoord2(glyph.U1, glyph.V1);
                RenderApi.Vertex2(xpos + w, ypos + h);
                RenderApi.TexCoord2(glyph.U0, glyph.V1);
                RenderApi.Vertex2(xpos, ypos + h);
            }

            cursorX += glyph.Advance;
        }

        RenderApi.End();
        RenderApi.PopMatrix();
    }

    /// <summary>
    /// Measures the pixel dimensions of a string without allocating arrays.
    /// </summary>
    public static Vector2 Measure(Font font, ReadOnlySpan<char> text)
    {
        float maxWidth = 0;
        float cursorX = 0;
        float cursorY = font.LineHeight;

        while (text.Length > 0)
        {
            Rune.DecodeFromUtf16(text, out Rune rune, out int charsConsumed);
            text = text[charsConsumed..];

            if (rune.Value == '\n')
            {
                if (cursorX > maxWidth)
                    maxWidth = cursorX;
                cursorX = 0;
                cursorY += font.LineHeight;
                continue;
            }

            FontGlyph glyph = font.GetGlyph((uint)rune.Value);
            cursorX += glyph.Advance;
        }

        if (cursorX > maxWidth)
            maxWidth = cursorX;

        return new Vector2(maxWidth, cursorY);
    }
}
