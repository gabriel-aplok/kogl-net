using System.Numerics;

namespace Kogl.Core.Maths;

/// <summary>A bounding box</summary>
public struct BoundingBox(Vector3 min, Vector3 max)
{
    public Vector3 Min = min;
    public Vector3 Max = max;
}
