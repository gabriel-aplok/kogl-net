using System.Numerics;

namespace Kogl.Core.Maths;

public struct Ray(Vector3 position, Vector3 direction)
{
    public Vector3 Position = position;
    public Vector3 Direction = direction;
}

public struct RayCollision
{
    public bool Hit;
    public float Distance;
    public Vector3 Point;
    public Vector3 Normal;
}

public struct BoundingBox(Vector3 min, Vector3 max)
{
    public Vector3 Min = min;
    public Vector3 Max = max;
}
