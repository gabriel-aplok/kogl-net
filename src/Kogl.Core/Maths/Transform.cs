using System.Numerics;

namespace Kogl.Core.Maths;

public struct Transform
{
    public Vector3 Translation;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static Transform Identity =>
        new()
        {
            Translation = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One,
        };

    public readonly Matrix4x4 ToMatrix()
    {
        return Matrix4x4.CreateScale(Scale)
            * Matrix4x4.CreateFromQuaternion(Rotation)
            * Matrix4x4.CreateTranslation(Translation);
    }
}
