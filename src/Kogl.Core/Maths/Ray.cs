using System.Numerics;

namespace Kogl.Core.Maths;

/// <summary>A ray</summary>
public struct Ray(Vector3 position, Vector3 direction)
{
    public Vector3 Position = position;
    public Vector3 Direction = direction;
}

/// <summary>A ray collision</summary>
public struct RayCollision
{
    public bool Hit;
    public float Distance;
    public Vector3 Point;
    public Vector3 Normal;
}
