using System.Numerics;

namespace Kogl.Core;

public enum CameraProjection
{
    Perspective,
    Orthographic,
    Frustum,
}

public struct Ray(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin = origin;
    public Vector3 Direction = direction;
}

public class Camera
{
    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;

    public CameraProjection Projection = CameraProjection.Perspective;
    public float Fov = 45f;
    public float OrthoSize = 10f;
    public float Near = 0.1f;
    public float Far = 1000f;

    public Vector3 Front { get; private set; } = -Vector3.UnitZ;
    public Vector3 Up { get; private set; } = Vector3.UnitY;
    public Vector3 Right { get; private set; } = Vector3.UnitX;

    public float ViewportWidth { get; set; } = 800;
    public float ViewportHeight { get; set; } = 600;

    public float FrustumLeft = -1f;
    public float FrustumRight = 1f;
    public float FrustumBottom = -1f;
    public float FrustumTop = 1f;

    private Vector3? _target;

    public void LookAt(Vector3 target, Vector3 up = default)
    {
        _target = target;
    }

    public void ClearLookAt()
    {
        _target = null;
    }

    private void UpdateVectors()
    {
        if (_target.HasValue)
        {
            // target camera
            Front = Vector3.Normalize(_target.Value - Position);

            Rotation.X = MathF.Asin(Front.Y) * (180f / MathF.PI);
            Rotation.Y = MathF.Atan2(Front.X, Front.Z) * (180f / MathF.PI);
        }
        else
        {
            // fps camera
            float pitch = Rotation.X * (MathF.PI / 180f);
            float yaw = Rotation.Y * (MathF.PI / 180f);

            Vector3 direction;
            direction.X = MathF.Cos(pitch) * MathF.Sin(yaw);
            direction.Y = MathF.Sin(pitch);
            direction.Z = MathF.Cos(pitch) * MathF.Cos(yaw);

            Front = Vector3.Normalize(direction);
        }

        Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));
    }

    public Matrix4x4 GetViewMatrix()
    {
        UpdateVectors();

        Vector3 finalTarget = _target ?? (Position + Front);

        return Matrix4x4.CreateLookAt(Position, finalTarget, Vector3.UnitY);
    }

    public Matrix4x4 GetProjectionMatrix(float aspectRatio)
    {
        float aspect = aspectRatio <= 0.0001f ? 1.0f : aspectRatio;

        return Projection switch
        {
            CameraProjection.Perspective => Matrix4x4.CreatePerspectiveFieldOfView(
                Fov * (MathF.PI / 180f),
                aspect,
                Near,
                Far
            ),
            CameraProjection.Orthographic => Matrix4x4.CreateOrthographic(
                OrthoSize * aspect,
                OrthoSize,
                Near,
                Far
            ),
            CameraProjection.Frustum => Matrix4x4.CreatePerspectiveOffCenter(
                FrustumLeft,
                FrustumRight,
                FrustumBottom,
                FrustumTop,
                Near,
                Far
            ),
            _ => Matrix4x4.Identity,
        };
    }

    public void Lerp(Vector3 targetPos, Vector3 targetRot, float alpha)
    {
        Position = Vector3.Lerp(Position, targetPos, alpha);
        Rotation = Vector3.Lerp(Rotation, targetRot, alpha);
    }

    public Ray GetScreenRay(Vector2 mousePosition, float aspectRatio)
    {
        Matrix4x4 view = GetViewMatrix();
        Matrix4x4 proj = GetProjectionMatrix(aspectRatio);
        Matrix4x4.Invert(view * proj, out Matrix4x4 invViewProj);

        // map mouse to normalized device coords (-1 to 1)
        float x = (2.0f * mousePosition.X / ViewportWidth) - 1.0f;
        float y = 1.0f - (2.0f * mousePosition.Y / ViewportHeight);

        Vector4 nearSource = new(x, y, 0f, 1f);
        Vector4 farSource = new(x, y, 1f, 1f);

        Vector4 nearPoint = Vector4.Transform(nearSource, invViewProj);
        Vector4 farPoint = Vector4.Transform(farSource, invViewProj);

        Vector3 rayStart = new(
            nearPoint.X / nearPoint.W,
            nearPoint.Y / nearPoint.W,
            nearPoint.Z / nearPoint.W
        );
        Vector3 rayEnd = new(
            farPoint.X / farPoint.W,
            farPoint.Y / farPoint.W,
            farPoint.Z / farPoint.W
        );

        return new Ray(rayStart, Vector3.Normalize(rayEnd - rayStart));
    }

    public bool IsInView(Vector3 point, float radius)
    {
        // simple distance check against far plane as a baseline
        float dist = Vector3.Distance(Position, point);
        if (dist > Far + radius)
            return false;

        // plane-based culling
        Vector3 toPoint = Vector3.Normalize(point - Position);
        float dot = Vector3.Dot(Front, toPoint);

        // check if point is roughly in front of the camera based on FOV
        return dot > MathF.Cos((Fov + 10f) * (MathF.PI / 180f));
    }
}
