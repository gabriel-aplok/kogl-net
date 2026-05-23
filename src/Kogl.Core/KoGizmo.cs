using System.Numerics;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core.Maths;

namespace Kogl.Core;

[Flags]
public enum GizmoFlags
{
    Disabled = 0,
    Translate = 1 << 0,
    Rotate = 1 << 1,
    Scale = 1 << 2,
    All = Translate | Rotate | Scale,
    Local = 1 << 3,
    View = 1 << 4,
    ConstantScreenSize = 1 << 5,
    RenderOnTop = 1 << 6,
}

[Flags]
internal enum GizmoActiveAxis
{
    None = 0,
    X = 1 << 0,
    Y = 1 << 1,
    Z = 1 << 2,
    XYZ = X | Y | Z,
}

internal enum GizmoAction
{
    None = 0,
    Translate,
    Scale,
    Rotate,
}

public static class KoGizmo
{
    private struct GizmoAxis
    {
        public Vector3 Normal;
        public Vector4 Color;
    }

    private struct GizmoData
    {
        public Matrix4x4 InvViewProj;
        public Vector3 CamPos;
        public Vector3 Right,
            Up,
            Forward;
        public Vector3[] Axis;
        public float GizmoSize;
        public GizmoFlags Flags;
        public Transform CurTransform;
    }

    private const int _axisX = 0;
    private const int _axisY = 1;
    private const int _axisZ = 2;

    private static float _gizmoSize = 1.5f;
    private static float _lineWidth = 2.5f;

    private static readonly float _trArrowWidthFactor = 0.1f;
    private static readonly float _trArrowLengthFactor = 0.15f;
    private static readonly float _trPlaneOffsetFactor = 0.3f;
    private static readonly float _trPlaneSizeFactor = 0.15f;
    private static readonly float _trCircleRadiusFactor = 0.1f;
    private static Vector4 _trCircleColor = new(1f, 1f, 1f, 200f / 255f);

    private static readonly GizmoAxis[] _axisCfg =
    [
        new GizmoAxis
        {
            Normal = Vector3.UnitX,
            Color = new Vector4(229 / 255f, 72 / 255f, 91 / 255f, 1f),
        },
        new GizmoAxis
        {
            Normal = Vector3.UnitY,
            Color = new Vector4(131 / 255f, 205 / 255f, 56 / 255f, 1f),
        },
        new GizmoAxis
        {
            Normal = Vector3.UnitZ,
            Color = new Vector4(69 / 255f, 138 / 255f, 242 / 255f, 1f),
        },
    ];

    private static GizmoAction _curAction = GizmoAction.None;
    private static GizmoActiveAxis _activeAxis = GizmoActiveAxis.None;
    private static int _activeGizmoId = -1;
    private static Transform _startTransform;
    private static Vector3 _startWorldMouse;

    public static void SetGizmoSize(float size)
    {
        _gizmoSize = MathF.Max(0, size);
    }

    public static void SetGizmoLineWidth(float width)
    {
        _lineWidth = MathF.Max(0, width);
    }

    public static void SetGizmoColors(Vector4 x, Vector4 y, Vector4 z, Vector4 center)
    {
        _axisCfg[_axisX].Color = x;
        _axisCfg[_axisY].Color = y;
        _axisCfg[_axisZ].Color = z;
        _trCircleColor = center;
    }

    public static void SetGizmoGlobalAxis(Vector3 right, Vector3 up, Vector3 forward)
    {
        _axisCfg[_axisX].Normal = Vector3.Normalize(right);
        _axisCfg[_axisY].Normal = Vector3.Normalize(up);
        _axisCfg[_axisZ].Normal = Vector3.Normalize(forward);
    }

    /// <summary>
    /// Evaluates and draws a 3D gizmo.
    /// Returns true if the referenced transform is being mutated this frame.
    /// </summary>
    public static bool DrawGizmo3D(int id, GizmoFlags flags, ref Transform transform)
    {
        if (flags == GizmoFlags.Disabled)
            return false;

        Matrix4x4 matProj = KoRender.GetProjectionMatrix();
        Matrix4x4 matView = KoRender.GetModelViewMatrix();

        Matrix4x4.Invert(matView, out Matrix4x4 invMat);
        Matrix4x4.Invert(matView * matProj, out Matrix4x4 invViewProj);

        GizmoData data = new()
        {
            InvViewProj = invViewProj,
            CamPos = new Vector3(invMat.M41, invMat.M42, invMat.M43),
            Right = new Vector3(invMat.M11, invMat.M12, invMat.M13),
            Up = new Vector3(invMat.M21, invMat.M22, invMat.M23),
            CurTransform = transform,
            Flags = flags,
            Axis = new Vector3[3],
        };

        data.Forward = Vector3.Normalize(transform.Translation - data.CamPos);

        if (flags.HasFlag(GizmoFlags.ConstantScreenSize))
        {
            data.GizmoSize =
                _gizmoSize * Vector3.Distance(data.CamPos, transform.Translation) * 0.1f;
        }
        else
        {
            data.GizmoSize = _gizmoSize;
        }

        ComputeAxisOrientation(ref data);

        // push explicit batch states ensuring non-conflicting visual outputs
        KoRender.Flush();
        KoRender.DisableCulling();

        if (flags.HasFlag(GizmoFlags.RenderOnTop))
        {
            KoRender.DisableDepthTest();
            KoRender.DepthMask(false);
        }

        KoRender.LineWidth(_lineWidth);

        for (int i = 0; i < 3; ++i)
        {
            if (data.Flags.HasFlag(GizmoFlags.Translate))
                DrawGizmoArrow(id, in data, i);
            if (data.Flags.HasFlag(GizmoFlags.Scale))
                DrawGizmoCube(id, in data, i);
            if ((data.Flags & (GizmoFlags.Scale | GizmoFlags.Translate)) != 0)
                DrawGizmoPlane(id, in data, i);
            if (data.Flags.HasFlag(GizmoFlags.Rotate))
                DrawGizmoCircle(id, in data, i);
        }

        if ((data.Flags & (GizmoFlags.Scale | GizmoFlags.Translate)) != 0)
            DrawGizmoCenter(id, in data);

        // restore batch defaults
        KoRender.Flush();
        KoRender.LineWidth(1.0f);
        KoRender.EnableCulling(CullFaceState.Back);

        if (flags.HasFlag(GizmoFlags.RenderOnTop))
        {
            KoRender.EnableDepthTest();
            KoRender.DepthMask(true);
        }

        if (!IsGizmoTransforming() || id == _activeGizmoId)
        {
            GizmoHandleInput(id, in data, ref transform);
        }

        return IsThisGizmoTransforming(id);
    }

    private static void ComputeAxisOrientation(ref GizmoData data)
    {
        if (data.Flags.HasFlag(GizmoFlags.Scale))
        {
            data.Flags &= ~GizmoFlags.View;
            data.Flags |= GizmoFlags.Local;
        }

        if (data.Flags.HasFlag(GizmoFlags.View))
        {
            data.Axis[_axisX] = data.Right;
            data.Axis[_axisY] = data.Up;
            data.Axis[_axisZ] = data.Forward;
        }
        else
        {
            data.Axis[_axisX] = _axisCfg[_axisX].Normal;
            data.Axis[_axisY] = _axisCfg[_axisY].Normal;
            data.Axis[_axisZ] = _axisCfg[_axisZ].Normal;

            if (data.Flags.HasFlag(GizmoFlags.Local))
            {
                for (int i = 0; i < 3; ++i)
                    data.Axis[i] = Vector3.Normalize(
                        Vector3.Transform(data.Axis[i], data.CurTransform.Rotation)
                    );
            }
        }
    }

    private static bool IsGizmoTransforming()
    {
        return _curAction != GizmoAction.None;
    }

    private static bool IsThisGizmoTransforming(int id)
    {
        return IsGizmoTransforming() && id == _activeGizmoId;
    }

    private static bool IsGizmoScaling()
    {
        return _curAction == GizmoAction.Scale;
    }

    private static bool IsGizmoTranslating()
    {
        return _curAction == GizmoAction.Translate;
    }

    private static bool IsGizmoRotating()
    {
        return _curAction == GizmoAction.Rotate;
    }

    private static bool IsGizmoAxisActive(int axis)
    {
        return (axis == _axisX && _activeAxis.HasFlag(GizmoActiveAxis.X))
            || (axis == _axisY && _activeAxis.HasFlag(GizmoActiveAxis.Y))
            || (axis == _axisZ && _activeAxis.HasFlag(GizmoActiveAxis.Z));
    }

    private static Ray Vec3ScreenToWorldRay(Vector2 position, in Matrix4x4 invViewProj)
    {
        float w = KoRender.ViewportWidth;
        float h = KoRender.ViewportHeight;

        Vector2 deviceCoords = new((2.0f * position.X / w) - 1.0f, 1.0f - (2.0f * position.Y / h));

        Vector4 nearPt = Vector4.Transform(new Vector4(deviceCoords, 0.0f, 1.0f), invViewProj);
        Vector3 near = new Vector3(nearPt.X, nearPt.Y, nearPt.Z) / nearPt.W;

        Vector4 farPt = Vector4.Transform(new Vector4(deviceCoords, 1.0f, 1.0f), invViewProj);
        Vector3 far = new Vector3(farPt.X, farPt.Y, farPt.Z) / farPt.W;

        Vector4 camPt = Vector4.Transform(new Vector4(deviceCoords, -1.0f, 1.0f), invViewProj);
        Vector3 cameraPlane = new Vector3(camPt.X, camPt.Y, camPt.Z) / camPt.W;

        return new Ray(cameraPlane, Vector3.Normalize(far - near));
    }

    #region Drawing

    private static void DrawGizmoCube(int id, in GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(id) && (!IsGizmoAxisActive(axis) || !IsGizmoScaling()))
            return;

        float gizmoSize =
            (data.Flags & (GizmoFlags.Scale | GizmoFlags.Translate)) != 0
                ? data.GizmoSize * 0.5f
                : data.GizmoSize;
        Vector3 endPos =
            data.CurTransform.Translation
            + (data.Axis[axis] * (gizmoSize * (1.0f - _trArrowWidthFactor)));

        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color4(
            _axisCfg[axis].Color.X,
            _axisCfg[axis].Color.Y,
            _axisCfg[axis].Color.Z,
            _axisCfg[axis].Color.W
        );
        KoRender.Vertex3(
            data.CurTransform.Translation.X,
            data.CurTransform.Translation.Y,
            data.CurTransform.Translation.Z
        );
        KoRender.Vertex3(endPos.X, endPos.Y, endPos.Z);
        KoRender.End();

        float boxSize = data.GizmoSize * _trArrowWidthFactor;
        Vector3 dim1 = data.Axis[(axis + 1) % 3] * boxSize;
        Vector3 dim2 = data.Axis[(axis + 2) % 3] * boxSize;
        Vector3 depth = data.Axis[axis] * boxSize;

        Vector3 a = endPos - (dim1 * 0.5f) - (dim2 * 0.5f);
        Vector3 b = a + dim1;
        Vector3 c = b + dim2;
        Vector3 d = a + dim2;

        Vector3 e = a + depth;
        Vector3 f = b + depth;
        Vector3 g = c + depth;
        Vector3 h = d + depth;

        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(
            _axisCfg[axis].Color.X,
            _axisCfg[axis].Color.Y,
            _axisCfg[axis].Color.Z,
            _axisCfg[axis].Color.W
        );

        PushQuad(a, b, c, d);
        PushQuad(e, f, g, h);
        PushQuad(a, e, f, d); // wait, winding order might be tricky on generic quads without culling, but I disabled it
        PushQuad(b, f, g, c);
        PushQuad(a, b, f, e);
        PushQuad(c, g, h, d);

        KoRender.End();
    }

    private static void DrawGizmoPlane(int id, in GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(id))
            return;

        Vector3 dir1 = data.Axis[(axis + 1) % 3];
        Vector3 dir2 = data.Axis[(axis + 2) % 3];
        Vector4 col = _axisCfg[axis].Color;

        float offset = _trPlaneOffsetFactor * data.GizmoSize;
        float size = _trPlaneSizeFactor * data.GizmoSize;

        Vector3 a = data.CurTransform.Translation + (dir1 * offset) + (dir2 * offset);
        Vector3 b = a + (dir1 * size);
        Vector3 c = b + (dir2 * size);
        Vector3 d = a + (dir2 * size);

        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(col.X, col.Y, col.Z, col.W * 0.5f);
        PushQuad(a, b, c, d);
        KoRender.End();

        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color4(col.X, col.Y, col.Z, col.W);
        KoRender.Vertex3(a.X, a.Y, a.Z);
        KoRender.Vertex3(b.X, b.Y, b.Z);
        KoRender.Vertex3(b.X, b.Y, b.Z);
        KoRender.Vertex3(c.X, c.Y, c.Z);
        KoRender.Vertex3(c.X, c.Y, c.Z);
        KoRender.Vertex3(d.X, d.Y, d.Z);
        KoRender.Vertex3(d.X, d.Y, d.Z);
        KoRender.Vertex3(a.X, a.Y, a.Z);
        KoRender.End();
    }

    private static void DrawGizmoArrow(int id, in GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(id) && (!IsGizmoAxisActive(axis) || !IsGizmoTranslating()))
            return;

        Vector3 endPos =
            data.CurTransform.Translation
            + (data.Axis[axis] * (data.GizmoSize * (1.0f - _trArrowLengthFactor)));

        if (!data.Flags.HasFlag(GizmoFlags.Scale))
        {
            KoRender.Begin(PrimitiveMode.Lines);
            KoRender.Color4(
                _axisCfg[axis].Color.X,
                _axisCfg[axis].Color.Y,
                _axisCfg[axis].Color.Z,
                _axisCfg[axis].Color.W
            );
            KoRender.Vertex3(
                data.CurTransform.Translation.X,
                data.CurTransform.Translation.Y,
                data.CurTransform.Translation.Z
            );
            KoRender.Vertex3(endPos.X, endPos.Y, endPos.Z);
            KoRender.End();
        }

        float arrowLen = data.GizmoSize * _trArrowLengthFactor;
        float arrowWid = data.GizmoSize * _trArrowWidthFactor;

        Vector3 dim1 = data.Axis[(axis + 1) % 3] * arrowWid;
        Vector3 dim2 = data.Axis[(axis + 2) % 3] * arrowWid;
        Vector3 v = endPos + (data.Axis[axis] * arrowLen);

        Vector3 a = endPos - (dim1 * 0.5f) - (dim2 * 0.5f);
        Vector3 b = a + dim1;
        Vector3 c = b + dim2;
        Vector3 d = a + dim2;

        KoRender.Begin(PrimitiveMode.Triangles);
        KoRender.Color4(
            _axisCfg[axis].Color.X,
            _axisCfg[axis].Color.Y,
            _axisCfg[axis].Color.Z,
            _axisCfg[axis].Color.W
        );
        PushTri(a, b, c);
        PushTri(a, c, d);
        PushTri(a, v, b);
        PushTri(b, v, c);
        PushTri(c, v, d);
        PushTri(d, v, a);
        KoRender.End();
    }

    private static void DrawGizmoCenter(int id, in GizmoData data)
    {
        float radius = data.GizmoSize * _trCircleRadiusFactor;
        DrawCircleLines(data.CurTransform.Translation, data.Right, data.Up, radius, _trCircleColor);
    }

    private static void DrawGizmoCircle(int id, in GizmoData data, int axis)
    {
        if (IsThisGizmoTransforming(id) && (!IsGizmoAxisActive(axis) || !IsGizmoRotating()))
            return;
        DrawCircleLines(
            data.CurTransform.Translation,
            data.Axis[(axis + 1) % 3],
            data.Axis[(axis + 2) % 3],
            data.GizmoSize,
            _axisCfg[axis].Color
        );
    }

    private static void PushQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        KoRender.Vertex3(a.X, a.Y, a.Z);
        KoRender.Vertex3(b.X, b.Y, b.Z);
        KoRender.Vertex3(c.X, c.Y, c.Z);
        KoRender.Vertex3(d.X, d.Y, d.Z);
    }

    private static void PushTri(Vector3 a, Vector3 b, Vector3 c)
    {
        KoRender.Vertex3(a.X, a.Y, a.Z);
        KoRender.Vertex3(b.X, b.Y, b.Z);
        KoRender.Vertex3(c.X, c.Y, c.Z);
    }

    private static void DrawCircleLines(
        Vector3 center,
        Vector3 dir1,
        Vector3 dir2,
        float radius,
        Vector4 color
    )
    {
        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color4(color.X, color.Y, color.Z, color.W);
        const int step = 15;
        for (int i = 0; i < 360; i += step)
        {
            float a1 = i * (MathF.PI / 180f);
            float a2 = (i + step) * (MathF.PI / 180f);
            Vector3 p1 = center + (dir1 * MathF.Sin(a1) * radius) + (dir2 * MathF.Cos(a1) * radius);
            Vector3 p2 = center + (dir1 * MathF.Sin(a2) * radius) + (dir2 * MathF.Cos(a2) * radius);
            KoRender.Vertex3(p1.X, p1.Y, p1.Z);
            KoRender.Vertex3(p2.X, p2.Y, p2.Z);
        }
        KoRender.End();
    }

    #endregion
    #region Interceptions

    private static bool CheckGizmoAxis(in GizmoData data, int axis, Ray ray, GizmoFlags type)
    {
        float[] halfDim = new float[3];
        halfDim[axis] = data.GizmoSize * 0.5f;
        halfDim[(axis + 1) % 3] = data.GizmoSize * _trArrowWidthFactor * 0.5f;
        halfDim[(axis + 2) % 3] = halfDim[(axis + 1) % 3];

        if (
            type == GizmoFlags.Scale
            && (data.Flags & (GizmoFlags.Translate | GizmoFlags.Scale))
                == (GizmoFlags.Translate | GizmoFlags.Scale)
        )
            halfDim[axis] *= 0.5f;

        Vector3 obbCenter = data.CurTransform.Translation + (data.Axis[axis] * halfDim[axis]);

        Vector3 oLocal = ray.Position - obbCenter;
        Ray localRay = new(
            new Vector3(
                Vector3.Dot(oLocal, data.Axis[_axisX]),
                Vector3.Dot(oLocal, data.Axis[_axisY]),
                Vector3.Dot(oLocal, data.Axis[_axisZ])
            ),
            new Vector3(
                Vector3.Dot(ray.Direction, data.Axis[_axisX]),
                Vector3.Dot(ray.Direction, data.Axis[_axisY]),
                Vector3.Dot(ray.Direction, data.Axis[_axisZ])
            )
        );

        BoundingBox box = new(
            new Vector3(-halfDim[0], -halfDim[1], -halfDim[2]),
            new Vector3(halfDim[0], halfDim[1], halfDim[2])
        );
        return Collision.GetRayCollisionBox(localRay, box).Hit;
    }

    private static bool CheckGizmoPlane(in GizmoData data, int axis, Ray ray)
    {
        Vector3 dir1 = data.Axis[(axis + 1) % 3];
        Vector3 dir2 = data.Axis[(axis + 2) % 3];
        float offset = _trPlaneOffsetFactor * data.GizmoSize;
        float size = _trPlaneSizeFactor * data.GizmoSize;

        Vector3 a = data.CurTransform.Translation + (dir1 * offset) + (dir2 * offset);
        Vector3 b = a + (dir1 * size);
        Vector3 c = b + (dir2 * size);
        Vector3 d = a + (dir2 * size);

        return Collision.GetRayCollisionQuad(ray, a, b, c, d).Hit;
    }

    private static bool CheckGizmoCircle(in GizmoData data, int index, Ray ray)
    {
        Vector3 dir1 = data.Axis[(index + 1) % 3];
        Vector3 dir2 = data.Axis[(index + 2) % 3];
        float sphereRadius = data.GizmoSize * MathF.Sin(10f * (MathF.PI / 180f) / 2.0f);

        for (int i = 0; i < 360; i += 10)
        {
            float angle = i * (MathF.PI / 180f);
            Vector3 p =
                data.CurTransform.Translation
                + (dir1 * MathF.Sin(angle) * data.GizmoSize)
                + (dir2 * MathF.Cos(angle) * data.GizmoSize);
            if (Collision.GetRayCollisionSphere(ray, p, sphereRadius).Hit)
                return true;
        }
        return false;
    }

    private static bool CheckGizmoCenter(in GizmoData data, Ray ray)
    {
        return Collision
            .GetRayCollisionSphere(
                ray,
                data.CurTransform.Translation,
                data.GizmoSize * _trCircleRadiusFactor
            )
            .Hit;
    }

    #endregion
    #region Input

    private static Vector3 GetWorldMouse(in GizmoData data)
    {
        float dist = Vector3.Distance(data.CamPos, data.CurTransform.Translation);
        Ray mouseRay = Vec3ScreenToWorldRay(InputManager.MousePosition, data.InvViewProj);
        return mouseRay.Position + (mouseRay.Direction * dist);
    }

    private static Vector3 Project(Vector3 v, Vector3 normal)
    {
        return Vector3.Dot(v, normal) * normal;
    }

    private static void GizmoHandleInput(int id, in GizmoData data, ref Transform transform)
    {
        GizmoAction action = _curAction;

        if (action != GizmoAction.None)
        {
            if (!InputManager.IsMouseButtonDown(MouseButton.Left))
            {
                action = GizmoAction.None;
                _activeAxis = GizmoActiveAxis.None;
                _activeGizmoId = -1;
            }
            else
            {
                Vector3 endWorldMouse = GetWorldMouse(data);
                Vector3 pVec = endWorldMouse - _startWorldMouse;

                switch (action)
                {
                    case GizmoAction.Translate:
                        transform.Translation = _startTransform.Translation;
                        if (_activeAxis == GizmoActiveAxis.XYZ)
                        {
                            transform.Translation += Project(pVec, data.Right);
                            transform.Translation += Project(pVec, data.Up);
                        }
                        else
                        {
                            if (_activeAxis.HasFlag(GizmoActiveAxis.X))
                                transform.Translation += Project(pVec, data.Axis[_axisX]);
                            if (_activeAxis.HasFlag(GizmoActiveAxis.Y))
                                transform.Translation += Project(pVec, data.Axis[_axisY]);
                            if (_activeAxis.HasFlag(GizmoActiveAxis.Z))
                                transform.Translation += Project(pVec, data.Axis[_axisZ]);
                        }
                        break;
                    case GizmoAction.Scale:
                        transform.Scale = _startTransform.Scale;
                        if (_activeAxis == GizmoActiveAxis.XYZ)
                        {
                            float delta = Vector3.Dot(pVec, _axisCfg[_axisX].Normal);
                            transform.Scale += new Vector3(delta);
                        }
                        else
                        {
                            if (_activeAxis.HasFlag(GizmoActiveAxis.X))
                                transform.Scale += Project(pVec, _axisCfg[_axisX].Normal);
                            if (_activeAxis.HasFlag(GizmoActiveAxis.Y))
                                transform.Scale += Project(pVec, _axisCfg[_axisY].Normal);
                            if (_activeAxis.HasFlag(GizmoActiveAxis.Z))
                                transform.Scale += Project(pVec, _axisCfg[_axisZ].Normal);
                        }
                        break;
                    case GizmoAction.Rotate:
                        transform.Rotation = _startTransform.Rotation;
                        float deltaRot = Math.Clamp(
                            Vector3.Dot(pVec, data.Right + data.Up),
                            -2 * MathF.PI,
                            2 * MathF.PI
                        );
                        if (_activeAxis.HasFlag(GizmoActiveAxis.X))
                            transform.Rotation =
                                Quaternion.CreateFromAxisAngle(data.Axis[_axisX], deltaRot)
                                * transform.Rotation;
                        if (_activeAxis.HasFlag(GizmoActiveAxis.Y))
                            transform.Rotation =
                                Quaternion.CreateFromAxisAngle(data.Axis[_axisY], deltaRot)
                                * transform.Rotation;
                        if (_activeAxis.HasFlag(GizmoActiveAxis.Z))
                            transform.Rotation =
                                Quaternion.CreateFromAxisAngle(data.Axis[_axisZ], deltaRot)
                                * transform.Rotation;

                        _startTransform = transform;
                        _startWorldMouse = endWorldMouse;
                        break;
                }
            }
        }
        else
        {
            if (InputManager.IsMouseButtonPressed(MouseButton.Left))
            {
                Ray mouseRay = Vec3ScreenToWorldRay(InputManager.MousePosition, data.InvViewProj);
                int hit = -1;
                action = GizmoAction.None;

                for (int k = 0; hit == -1 && k < 2; ++k)
                {
                    GizmoFlags gizmoFlag = k == 0 ? GizmoFlags.Scale : GizmoFlags.Translate;
                    GizmoAction gizmoAction = k == 0 ? GizmoAction.Scale : GizmoAction.Translate;

                    if (data.Flags.HasFlag(gizmoFlag))
                    {
                        if (CheckGizmoCenter(data, mouseRay))
                        {
                            action = gizmoAction;
                            hit = 6;
                            break;
                        }
                        for (int i = 0; i < 3; ++i)
                        {
                            if (CheckGizmoAxis(data, i, mouseRay, gizmoFlag))
                            {
                                action = gizmoAction;
                                hit = i;
                                break;
                            }
                            if (CheckGizmoPlane(data, i, mouseRay))
                            {
                                action =
                                    (data.Flags & (GizmoFlags.Scale | GizmoFlags.Translate)) != 0
                                        ? GizmoAction.Translate
                                        : gizmoAction;
                                hit = 3 + i;
                                break;
                            }
                        }
                    }
                }

                if (hit == -1 && data.Flags.HasFlag(GizmoFlags.Rotate))
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        if (CheckGizmoCircle(data, i, mouseRay))
                        {
                            action = GizmoAction.Rotate;
                            hit = i;
                            break;
                        }
                    }
                }

                _activeAxis = GizmoActiveAxis.None;
                if (hit >= 0)
                {
                    _activeAxis = hit switch
                    {
                        0 => GizmoActiveAxis.X,
                        1 => GizmoActiveAxis.Y,
                        2 => GizmoActiveAxis.Z,
                        3 => GizmoActiveAxis.Y | GizmoActiveAxis.Z,
                        4 => GizmoActiveAxis.X | GizmoActiveAxis.Z,
                        5 => GizmoActiveAxis.X | GizmoActiveAxis.Y,
                        6 => GizmoActiveAxis.XYZ,
                        _ => GizmoActiveAxis.None,
                    };

                    _activeGizmoId = id;
                    _startTransform = transform;
                    _startWorldMouse = GetWorldMouse(data);
                }
            }
        }
        _curAction = action;
    }

    #endregion
}
