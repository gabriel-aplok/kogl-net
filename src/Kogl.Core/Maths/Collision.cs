using System.Numerics;

namespace Kogl.Core.Maths;

/// <summary>A collection of collision functions</summary>
public static class Collision
{
    public static RayCollision GetRayCollisionBox(Ray ray, BoundingBox box)
    {
        RayCollision result = new()
        {
            Hit = false,
            Distance = 0f,
            Point = Vector3.Zero,
            Normal = Vector3.Zero,
        };

        float t0 = 0.0f,
            t1 = float.MaxValue;
        float tmin,
            tmax,
            tymin,
            tymax,
            tzmin,
            tzmax;

        float invDirX = 1.0f / ray.Direction.X;
        if (invDirX >= 0)
        {
            tmin = (box.Min.X - ray.Position.X) * invDirX;
            tmax = (box.Max.X - ray.Position.X) * invDirX;
        }
        else
        {
            tmin = (box.Max.X - ray.Position.X) * invDirX;
            tmax = (box.Min.X - ray.Position.X) * invDirX;
        }

        float invDirY = 1.0f / ray.Direction.Y;
        if (invDirY >= 0)
        {
            tymin = (box.Min.Y - ray.Position.Y) * invDirY;
            tymax = (box.Max.Y - ray.Position.Y) * invDirY;
        }
        else
        {
            tymin = (box.Max.Y - ray.Position.Y) * invDirY;
            tymax = (box.Min.Y - ray.Position.Y) * invDirY;
        }

        if ((tmin > tymax) || (tymin > tmax))
            return result;

        if (tymin > tmin)
            tmin = tymin;
        if (tymax < tmax)
            tmax = tymax;

        float invDirZ = 1.0f / ray.Direction.Z;
        if (invDirZ >= 0)
        {
            tzmin = (box.Min.Z - ray.Position.Z) * invDirZ;
            tzmax = (box.Max.Z - ray.Position.Z) * invDirZ;
        }
        else
        {
            tzmin = (box.Max.Z - ray.Position.Z) * invDirZ;
            tzmax = (box.Min.Z - ray.Position.Z) * invDirZ;
        }

        if ((tmin > tzmax) || (tzmin > tmax))
            return result;

        if (tzmin > tmin)
            tmin = tzmin;
        if (tzmax < tmax)
            tmax = tzmax;

        if (tmin > t1 || tmax < t0)
            return result;

        result.Hit = true;
        result.Distance = tmin > 0 ? tmin : tmax;
        result.Point = ray.Position + (ray.Direction * result.Distance);
        return result;
    }

    public static RayCollision GetRayCollisionQuad(
        Ray ray,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        Vector3 p4
    )
    {
        RayCollision result = GetRayCollisionTriangle(ray, p1, p2, p4);
        if (!result.Hit)
            result = GetRayCollisionTriangle(ray, p2, p3, p4);
        return result;
    }

    public static RayCollision GetRayCollisionTriangle(Ray ray, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        RayCollision result = new() { Hit = false, Distance = 0f };
        const float epsilon = 0.0000001f;

        Vector3 edge1 = p2 - p1;
        Vector3 edge2 = p3 - p1;
        Vector3 h = Vector3.Cross(ray.Direction, edge2);
        float a = Vector3.Dot(edge1, h);

        if (a > -epsilon && a < epsilon)
            return result;

        float f = 1.0f / a;
        Vector3 s = ray.Position - p1;
        float u = f * Vector3.Dot(s, h);

        if (u < 0.0f || u > 1.0f)
            return result;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.Direction, q);

        if (v < 0.0f || u + v > 1.0f)
            return result;

        float t = f * Vector3.Dot(edge2, q);
        if (t > epsilon)
        {
            result.Hit = true;
            result.Distance = t;
            result.Point = ray.Position + (ray.Direction * t);
        }
        return result;
    }

    public static RayCollision GetRayCollisionSphere(Ray ray, Vector3 center, float radius)
    {
        RayCollision result = new() { Hit = false };

        Vector3 oc = ray.Position - center;
        float a = Vector3.Dot(ray.Direction, ray.Direction);
        float b = 2.0f * Vector3.Dot(oc, ray.Direction);
        float c = Vector3.Dot(oc, oc) - (radius * radius);
        float discriminant = (b * b) - (4 * a * c);

        if (discriminant < 0)
            return result;

        float t = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
        if (t < 0)
            t = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
        if (t < 0)
            return result;

        result.Hit = true;
        result.Distance = t;
        result.Point = ray.Position + (ray.Direction * t);
        return result;
    }
}
