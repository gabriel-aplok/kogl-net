// FILE: src/Kogl.Core/Rendering/SpriteRenderer.cs
using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core.Resources;

namespace Kogl.Core.Rendering;

/// <summary>2D batched sprite component utilizing the underlying KoGL matrix stack and Batcher</summary>
public static class SpriteRenderer
{
    /// <summary>Draws a texture</summary>
    public static void DrawTexture(
        TextureHandle texture,
        Vector2 position,
        Vector2 size,
        Vector2? pivot = null,
        float rotationRadians = 0.0f,
        Vector4? tint = null
    )
    {
        if (texture.Id == 0)
            return;

        Vector2 actualPivot = pivot ?? Vector2.Zero;
        Vector4 color = tint ?? Vector4.One;

        KoRender.UseTexture(texture, 0);
        KoRender.Begin(PrimitiveMode.Triangles);

        KoRender.PushMatrix();
        KoRender.Translate(position.X, position.Y, 0.0f);

        if (rotationRadians != 0.0f)
        {
            KoRender.Rotate(rotationRadians, 0.0f, 0.0f, 1.0f);
        }

        Vector2 minPos = -actualPivot;
        Vector2 maxPos = minPos + size;

        KoRender.Color4(color.X, color.Y, color.Z, color.W);
        KoRender.TexCoord2(0.0f, 0.0f);
        KoRender.Vertex2(minPos.X, minPos.Y);
        KoRender.TexCoord2(1.0f, 0.0f);
        KoRender.Vertex2(maxPos.X, minPos.Y);
        KoRender.TexCoord2(1.0f, 1.0f);
        KoRender.Vertex2(maxPos.X, maxPos.Y);

        KoRender.Color4(color.X, color.Y, color.Z, color.W);
        KoRender.TexCoord2(0.0f, 0.0f);
        KoRender.Vertex2(minPos.X, minPos.Y);
        KoRender.TexCoord2(1.0f, 1.0f);
        KoRender.Vertex2(maxPos.X, maxPos.Y);
        KoRender.TexCoord2(0.0f, 1.0f);
        KoRender.Vertex2(minPos.X, maxPos.Y);

        KoRender.PopMatrix();
        KoRender.End();
    }

    /// <summary>Draws a sprite region</summary>
    public static void Draw(
        SpriteRegion sprite,
        Vector2 position,
        Vector2? size = null,
        Vector2? pivot = null,
        float rotationRadians = 0.0f,
        Vector4? tint = null,
        bool flipX = false,
        bool flipY = false
    )
    {
        if (sprite.Texture.Id == 0)
            return;

        Vector2 actualSize = size ?? sprite.Size;
        Vector2 actualPivot = pivot ?? Vector2.Zero;
        Vector4 color = tint ?? Vector4.One;

        KoRender.UseTexture(sprite.Texture, 0);
        KoRender.Begin(PrimitiveMode.Triangles);

        float u0 = flipX ? sprite.UVMax.X : sprite.UVMin.X;
        float u1 = flipX ? sprite.UVMin.X : sprite.UVMax.X;
        float v0 = flipY ? sprite.UVMax.Y : sprite.UVMin.Y;
        float v1 = flipY ? sprite.UVMin.Y : sprite.UVMax.Y;

        KoRender.PushMatrix();
        KoRender.Translate(position.X, position.Y, 0.0f);

        if (rotationRadians != 0.0f)
        {
            KoRender.Rotate(rotationRadians, 0.0f, 0.0f, 1.0f);
        }

        Vector2 minPos = -actualPivot;
        Vector2 maxPos = minPos + actualSize;

        KoRender.Color4(color.X, color.Y, color.Z, color.W);
        KoRender.TexCoord2(u0, v0);
        KoRender.Vertex2(minPos.X, minPos.Y);
        KoRender.TexCoord2(u1, v0);
        KoRender.Vertex2(maxPos.X, minPos.Y);
        KoRender.TexCoord2(u1, v1);
        KoRender.Vertex2(maxPos.X, maxPos.Y);

        KoRender.Color4(color.X, color.Y, color.Z, color.W);
        KoRender.TexCoord2(u0, v0);
        KoRender.Vertex2(minPos.X, minPos.Y);
        KoRender.TexCoord2(u1, v1);
        KoRender.Vertex2(maxPos.X, maxPos.Y);
        KoRender.TexCoord2(u0, v1);
        KoRender.Vertex2(minPos.X, maxPos.Y);

        KoRender.PopMatrix();
        KoRender.End();
    }
}
