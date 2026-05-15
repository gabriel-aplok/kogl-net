using System.Numerics;

namespace Kogl.Core;

public enum CameraProjection
{
    Perspective,
    Orthographic,
    Frustum,
}

public class Camera
{
    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero; // euler angles in degrees (pitch, yaw, roll)

    public CameraProjection Projection = CameraProjection.Perspective;
    public float Fov = 45f;
    public float OrthoSize = 10f;
    public float Near = 0.1f;
    public float Far = 1000f;

    // frustum specifics
    public float Left = -1f;
    public float Right = 1f;
    public float Bottom = -1f;
    public float Top = 1f;

    private Vector3? _target;
    private Vector3 _up = Vector3.UnitY;

    public void LookAt(Vector3 target, Vector3 up = default)
    {
        _target = target;
        _up = up == default ? Vector3.UnitY : up;
    }

    public void ClearLookAt()
    {
        _target = null;
    }

    public Matrix4x4 GetViewMatrix()
    {
        if (_target.HasValue)
        {
            return Matrix4x4.CreateLookAt(Position, _target.Value, _up);
        }

        // view matrix is the inverse of the camera's transform.
        // translation is inverted, and rotations are inverted and applied in reverse order.
        return Matrix4x4.CreateTranslation(-Position)
            * Matrix4x4.CreateRotationZ(-Rotation.Z * (MathF.PI / 180f))
            * Matrix4x4.CreateRotationX(-Rotation.X * (MathF.PI / 180f))
            * Matrix4x4.CreateRotationY(-Rotation.Y * (MathF.PI / 180f));
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
                Left,
                Right,
                Bottom,
                Top,
                Near,
                Far
            ),
            _ => Matrix4x4.Identity,
        };
    }
}
