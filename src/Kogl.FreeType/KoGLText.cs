using System.Numerics;
using System.Text;
using Kogl.Abstractions;
using Kogl.Core;

namespace Kogl.FreeType;

public static class KoGLText
{
    private static ShaderHandle _sdfShader;
    private static bool _sdfShaderInitialized;

    private static void EnsureSdfShader()
    {
        if (_sdfShaderInitialized)
            return;

        string vs =
            @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

out vec2 fTex;
out vec4 fCol;

uniform mat4 uMVP;

void main() {
    fTex = aTex;
    fCol = aCol;
    gl_Position = uMVP * vec4(aPos, 1.0);
}";

        string fs =
            @"#version 330 core
in vec2 fTex;
in vec4 fCol;
out vec4 FragColor;

uniform sampler2D uTex;
uniform float uSmoothing;

void main() {
    // SDF data is stored in the alpha channel
    float distance = texture(uTex, fTex).a;

    // smoothstep creates the sharp edge based on the 0.5 threshold
    float alpha = smoothstep(0.5 - uSmoothing, 0.5 + uSmoothing, distance);

    FragColor = vec4(fCol.rgb, fCol.a * alpha);

    if (FragColor.a < 0.01) discard;
}";

        _sdfShader = RenderApi.CreateShader(vs, fs);
        _sdfShaderInitialized = true;
    }

    /// <summary>
    /// Draws a string to the screen.
    /// </summary>
    public static void DrawText(
        Font font,
        string text,
        Vector2 position,
        Vector4 color,
        TextAlignment alignment = TextAlignment.Left,
        float scale = 1.0f
    )
    {
        if (string.IsNullOrEmpty(text))
            return;

        RenderApi.UseTexture(font.AtlasTexture);

        if (font.IsSdf)
        {
            EnsureSdfShader();
            RenderApi.UseShader(_sdfShader);

            Matrix4x4 mvp = RenderApi.GetModelViewMatrix() * RenderApi.GetProjectionMatrix();
            RenderApi.SetUniform("uMVP", mvp);

            // smoothing should scale with the font size/zoom to keep edges crisp
            // standard value is ~0.25 / (font_size * scale)
            float smoothing = 0.125f / (font.Size * scale);
            RenderApi.SetUniform("uSmoothing", smoothing);
        }
        else
        {
            RenderApi.UseDefaultShader();
        }

        // push to the stack to respect parent transforms easily
        RenderApi.PushMatrix();
        RenderApi.Translate(position.X, position.Y, 0);
        RenderApi.Scale(scale, scale, 1.0f);

        // handle alignment offset
        ReadOnlySpan<char> span = text.AsSpan();
        if (alignment != TextAlignment.Left)
        {
            Vector2 size = Measure(font, span);
            if (alignment == TextAlignment.Center)
                position.X -= size.X * 0.5f;
            if (alignment == TextAlignment.Right)
                position.X -= size.X;
        }

        // batch rendering
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
