// FILE: src/Kogl.Core/Rendering/SpriteRenderer.cs
using System.Numerics;
using Kogl.Abstractions.Types;
using Kogl.Core.Resources;

namespace Kogl.Core.Rendering;

/// <summary>2D batched sprite component utilizing the underlying KoGL matrix stack and Batcher.</summary>
public static class SpriteRenderer
{
    /// <summary>Draws a texture.</summary>
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

        KoGL.UseTexture(texture, 0);
        KoGL.Begin(PrimitiveMode.Triangles);

        KoGL.PushMatrix();
        KoGL.Translate(position.X, position.Y, 0.0f);

        if (rotationRadians != 0.0f)
        {
            KoGL.Rotate(rotationRadians, 0.0f, 0.0f, 1.0f);
        }

        Vector2 minPos = -actualPivot;
        Vector2 maxPos = minPos + size;

        KoGL.Color4(color.X, color.Y, color.Z, color.W);
        KoGL.TexCoord2(0.0f, 0.0f);
        KoGL.Vertex2(minPos.X, minPos.Y);
        KoGL.TexCoord2(1.0f, 0.0f);
        KoGL.Vertex2(maxPos.X, minPos.Y);
        KoGL.TexCoord2(1.0f, 1.0f);
        KoGL.Vertex2(maxPos.X, maxPos.Y);

        KoGL.Color4(color.X, color.Y, color.Z, color.W);
        KoGL.TexCoord2(0.0f, 0.0f);
        KoGL.Vertex2(minPos.X, minPos.Y);
        KoGL.TexCoord2(1.0f, 1.0f);
        KoGL.Vertex2(maxPos.X, maxPos.Y);
        KoGL.TexCoord2(0.0f, 1.0f);
        KoGL.Vertex2(minPos.X, maxPos.Y);

        KoGL.PopMatrix();
        KoGL.End();
    }

    /// <summary>Draws a sprite region.</summary>
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

        KoGL.UseTexture(sprite.Texture, 0);
        KoGL.Begin(PrimitiveMode.Triangles);

        float u0 = flipX ? sprite.UVMax.X : sprite.UVMin.X;
        float u1 = flipX ? sprite.UVMin.X : sprite.UVMax.X;
        float v0 = flipY ? sprite.UVMax.Y : sprite.UVMin.Y;
        float v1 = flipY ? sprite.UVMin.Y : sprite.UVMax.Y;

        KoGL.PushMatrix();
        KoGL.Translate(position.X, position.Y, 0.0f);

        if (rotationRadians != 0.0f)
        {
            KoGL.Rotate(rotationRadians, 0.0f, 0.0f, 1.0f);
        }

        Vector2 minPos = -actualPivot;
        Vector2 maxPos = minPos + actualSize;

        KoGL.Color4(color.X, color.Y, color.Z, color.W);
        KoGL.TexCoord2(u0, v0);
        KoGL.Vertex2(minPos.X, minPos.Y);
        KoGL.TexCoord2(u1, v0);
        KoGL.Vertex2(maxPos.X, minPos.Y);
        KoGL.TexCoord2(u1, v1);
        KoGL.Vertex2(maxPos.X, maxPos.Y);

        KoGL.Color4(color.X, color.Y, color.Z, color.W);
        KoGL.TexCoord2(u0, v0);
        KoGL.Vertex2(minPos.X, minPos.Y);
        KoGL.TexCoord2(u1, v1);
        KoGL.Vertex2(maxPos.X, maxPos.Y);
        KoGL.TexCoord2(u0, v1);
        KoGL.Vertex2(minPos.X, maxPos.Y);

        KoGL.PopMatrix();
        KoGL.End();
    }
}
