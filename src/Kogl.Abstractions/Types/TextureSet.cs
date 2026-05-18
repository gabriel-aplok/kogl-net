namespace Kogl.Abstractions.Types;

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
