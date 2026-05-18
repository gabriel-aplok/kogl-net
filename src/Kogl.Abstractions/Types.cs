using System.Numerics;
using System.Runtime.InteropServices;
using Kogl.Abstractions.Types;

namespace Kogl.Abstractions;

public readonly record struct TextureHandle(uint Id);

public readonly record struct ShaderHandle(uint Id);

public readonly record struct RenderTarget(
    uint FboId,
    uint RboId,
    TextureHandle[] Textures,
    int Width,
    int Height
)
{
    public TextureHandle Texture => Textures != null && Textures.Length > 0 ? Textures[0] : default;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VertexData(Vector3 pos, Vector2 uv, Vector4 color)
{
    public Vector3 Position = pos;
    public Vector2 TexCoord = uv;
    public Vector4 Color = color;
}

public struct TextureSet : IEquatable<TextureSet>
{
    public TextureHandle Slot0;
    public TextureHandle Slot1;
    public TextureHandle Slot2;
    public TextureHandle Slot3;
    public TextureHandle Slot4;
    public TextureHandle Slot5;
    public TextureHandle Slot6;
    public TextureHandle Slot7;

    public readonly bool Equals(TextureSet other)
    {
        return Slot0.Id == other.Slot0.Id
            && Slot1.Id == other.Slot1.Id
            && Slot2.Id == other.Slot2.Id
            && Slot3.Id == other.Slot3.Id
            && Slot4.Id == other.Slot4.Id
            && Slot5.Id == other.Slot5.Id
            && Slot6.Id == other.Slot6.Id
            && Slot7.Id == other.Slot7.Id;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is TextureSet other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Slot0, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7);
    }

    public static bool operator ==(TextureSet left, TextureSet right) => left.Equals(right);

    public static bool operator !=(TextureSet left, TextureSet right) => !left.Equals(right);
}

public struct RenderBatch
{
    public PrimitiveMode Mode;
    public TextureSet Textures;
    public ShaderHandle Shader;
    public int VertexOffset;
    public int VertexCount;
    public int IndexOffset;
    public int IndexCount;
    public float LineWidth;
}

public enum CullFaceState
{
    Front,
    Back,
    FrontAndBack,
}

public enum PolygonState
{
    Fill,
    Line,
    Point,
}

public enum BlendingFactorState
{
    Zero,
    One,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha,
    SrcColor,
    OneMinusSrcColor,
    DstColor,
    OneMinusDstColor,
}

public enum BlendEquationState
{
    Add,
    Subtract,
    ReverseSubtract,
}

public enum DepthFunctionState
{
    Never,
    Less,
    Equal,
    Lequal,
    Greater,
    NotEqual,
    Gequal,
    Always,
}
