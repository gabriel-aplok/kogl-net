using System.Numerics;
using Kogl.Common;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class ObjLoaderTestExample
{
    private static Camera _camera = null!;
    private static Model _testModel = null!;
    private static Material _modelMaterial = null!;
    private static Texture _modelTexture = null!;
    private static Shader _modelShader = null!;

    private static float _yaw = -90f;
    private static float _pitch = 0f;
    private static bool _isMouseCaptured = true;

    public static void Start()
    {
        AppWindow app = new(1280, 720, "Kolpa - Model Loader Example");

        app.OnLoad += () =>
        {
            LogCat.Info("SAMPLE", "Initializing...");

            _camera = new Camera
            {
                Position = new Vector3(0f, 2f, 5f),
                Projection = CameraProjection.Perspective,
                Fov = 60f,
                Near = 0.1f,
                Far = 1000f,
            };

            InputManager.CursorMode = CursorMode.Locked;

            _testModel = Assets.Load<Model>("res://models/suzanne.obj");
            _modelTexture = Assets.Load<Texture>("res://models/crate.png");
            _modelShader = Assets.Load<Shader>("res://shaders/model_shader.glsl");

            _modelMaterial = new Material(_modelShader);
            _modelMaterial.SetTexture("uMainTex", _modelTexture);
            // _modelMaterial.SetVector4("uTint", new Vector4(1f, 0f, 0f, 1f));
        };

        app.OnRender += (dt) =>
        {
            float deltaTime = (float)dt;

            HandleControls(deltaTime);

            KoRender.Clear(0.15f, 0.17f, 0.2f, 1.0f);
            KoRender.EnableDepthTest();

            KoRender.BeginCamera(_camera);

            DrawReferenceGrid();

            // KoRender.ApplyMaterial(_modelMaterial);
            // _modelMaterial.Apply();
            // _modelMaterial.BInd();

            Vector3 modelPos = new(0f, 0f, 0f);
            float modelScale = 1.0f;
            KoRender.DrawModel(_testModel, modelPos, modelScale);

            KoRender.EndCamera();
            KoRender.DisableDepthTest();
        };

        app.OnUnload += () =>
        {
            LogCat.Info("SAMPLE", "Shutting down...");
            Assets.Unload("res://models/suzanne.obj");
            Assets.Unload("res://models/crate.png");
            Assets.Unload("res://shaders/model_shader.glsl");
        };

        app.Run();
    }

    private static void HandleControls(float dt)
    {
        if (InputManager.IsKeyPressed(Key.Escape))
        {
            _isMouseCaptured = !_isMouseCaptured;
            InputManager.CursorMode = _isMouseCaptured ? CursorMode.Locked : CursorMode.Normal;
        }

        if (!_isMouseCaptured)
            return;

        Vector2 mouseDelta = InputManager.MouseDelta;
        float sensitivity = 0.15f;

        _yaw += mouseDelta.X * sensitivity;
        _pitch -= mouseDelta.Y * sensitivity;

        _pitch = MathFloat.Clamp(_pitch, -89f, 89f);

        Vector3 front;
        front.X =
            MathF.Cos(MathFloat.DegreesToRadians(_yaw))
            * MathF.Cos(MathFloat.DegreesToRadians(_pitch));
        front.Y = MathF.Sin(MathFloat.DegreesToRadians(_pitch));
        front.Z =
            MathF.Sin(MathFloat.DegreesToRadians(_yaw))
            * MathF.Cos(MathFloat.DegreesToRadians(_pitch));

        Vector3 targetDirection = Vector3.Normalize(front);
        _camera.LookAt(_camera.Position + targetDirection);

        float moveSpeed = 6.0f * dt;

        Vector3 forward = Vector3.Normalize(new Vector3(targetDirection.X, 0f, targetDirection.Z));
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

        if (InputManager.IsKeyDown(Key.W))
            _camera.Position += forward * moveSpeed;
        if (InputManager.IsKeyDown(Key.S))
            _camera.Position -= forward * moveSpeed;
        if (InputManager.IsKeyDown(Key.A))
            _camera.Position -= right * moveSpeed;
        if (InputManager.IsKeyDown(Key.D))
            _camera.Position += right * moveSpeed;

        if (InputManager.IsKeyDown(Key.Space))
            _camera.Position += Vector3.UnitY * moveSpeed;
        if (InputManager.IsKeyDown(Key.ShiftLeft))
            _camera.Position -= Vector3.UnitY * moveSpeed;
    }

    private static void DrawReferenceGrid()
    {
        KoRender.UseDefaultShader();
        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color4(0.4f, 0.4f, 0.4f, 1.0f);

        int extents = 20;
        for (int i = -extents; i <= extents; i++)
        {
            // Grid along X axis
            KoRender.Vertex3(i, 0f, -extents);
            KoRender.Vertex3(i, 0f, extents);

            // Grid along Z axis
            KoRender.Vertex3(-extents, 0f, i);
            KoRender.Vertex3(extents, 0f, i);
        }
        KoRender.End();
    }
}

internal static class MathFloat
{
    public static float Clamp(float val, float min, float max)
    {
        return val < min ? min : (val > max ? max : val);
    }

    public static float DegreesToRadians(float degrees)
    {
        return degrees * (MathF.PI / 180f);
    }
}
