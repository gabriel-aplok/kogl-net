namespace Kogl.Common.Types;

/// <summary>Fixed set of texture handles for the first 8 texture slots/units</summary>
public struct TextureSet : IEquatable<TextureSet>
{
    /// <summary>Texture in slot 0</summary>
    public TextureHandle Slot0;

    /// <summary>Texture in slot 1</summary>
    public TextureHandle Slot1;

    /// <summary>Texture in slot 2</summary>
    public TextureHandle Slot2;

    /// <summary>Texture in slot 3</summary>
    public TextureHandle Slot3;

    /// <summary>Texture in slot 4</summary>
    public TextureHandle Slot4;

    /// <summary>Texture in slot 5</summary>
    public TextureHandle Slot5;

    /// <summary>Texture in slot 6</summary>
    public TextureHandle Slot6;

    /// <summary>Texture in slot 7</summary>
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
