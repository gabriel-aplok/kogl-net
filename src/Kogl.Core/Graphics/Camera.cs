using System.Numerics;

namespace Kogl.Core.Graphics;

/// <summary>The projection mode</summary>
public enum CameraProjection
{
    Perspective,
    Orthographic,
    Frustum,
}

/// <summary>A camera</summary>
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

    public float AspectRatio { get; set; } = 800f / 600f;

    public float FrustumLeft = -1f;
    public float FrustumRight = 1f;
    public float FrustumBottom = -1f;
    public float FrustumTop = 1f;

    /// <summary>Looks at a target.</summary>
    public void LookAt(Vector3 target)
    {
        Vector3 direction = target - Position;
        if (direction == Vector3.Zero)
            return;

        direction = Vector3.Normalize(direction);

        Rotation.X = MathF.Asin(direction.Y) * (180f / MathF.PI);
        Rotation.Y = MathF.Atan2(direction.X, direction.Z) * (180f / MathF.PI);

        UpdateVectors();
    }

    /// <summary>Updates the camera directional vectors based on the current Rotation angles.</summary>
    private void UpdateVectors()
    {
        float pitch = Rotation.X * (MathF.PI / 180f);
        float yaw = Rotation.Y * (MathF.PI / 180f);

        Vector3 direction;
        direction.X = MathF.Cos(pitch) * MathF.Sin(yaw);
        direction.Y = MathF.Sin(pitch);
        direction.Z = MathF.Cos(pitch) * MathF.Cos(yaw);

        Front = Vector3.Normalize(direction);
        Right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, Front));
    }

    /// <summary>Gets the view matrix</summary>
    public Matrix4x4 GetViewMatrix()
    {
        UpdateVectors();
        return Matrix4x4.CreateLookAt(Position, Position + Front, Vector3.UnitY);
    }

    /// <summary>Gets the projection matrix</summary>
    public Matrix4x4 GetProjectionMatrix(float? aspectRatio = null)
    {
        float actualAspect = aspectRatio ?? AspectRatio;

        return Projection switch
        {
            CameraProjection.Perspective => Matrix4x4.CreatePerspectiveFieldOfView(
                Fov * (MathF.PI / 180f),
                actualAspect,
                Near,
                Far
            ),
            CameraProjection.Orthographic => Matrix4x4.CreateOrthographic(
                OrthoSize * actualAspect,
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

    /// <summary>Lerps the camera</summary>
    public void Lerp(Vector3 targetPos, Vector3 targetRot, float alpha)
    {
        Position = Vector3.Lerp(Position, targetPos, alpha);
        Rotation = Vector3.Lerp(Rotation, targetRot, alpha);
    }
}
