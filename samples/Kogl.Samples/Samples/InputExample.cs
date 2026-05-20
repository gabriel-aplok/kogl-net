using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Graphics;
using Kogl.Core.Rendering;
using Kogl.FreeType;
using Kogl.Input;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class InputExample
{
    private static readonly AppWindow _app = new(800, 600, "KoGL - Input & Action Mapping");
    private static readonly Camera _camera = new();
    private static readonly Camera _uiCamera = new();
    private static Font _uiFont = null!;

    private static float _yaw = 0f;
    private static float _pitch = 0f;

    public static void Start()
    {
        _app.OnLoad += () =>
        {
            // scene camera
            _camera.Position = new Vector3(0, 1, 5);

            // ui camera
            _uiCamera.Projection = CameraProjection.Orthographic;
            _uiCamera.OrthoSize = 600;
            _uiCamera.Near = -1;
            _uiCamera.Far = 1;

            // font
            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            // input mapping
            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);

            InputMap.Bind("Jump", Key.Space);
            InputMap.Bind("Shoot", MouseButton.Left);
            InputMap.Bind("ToggleMouse", Key.Escape);

            // lock mouse
            InputManager.CursorMode = CursorMode.Locked;
        };

        _app.OnRender += RenderLoop;
        _app.OnResizeEvent += (width, height) =>
        {
            _camera.AspectRatio = height == 0 ? 1f : (float)width / height;
        };
        _app.OnUnload += () => _uiFont?.Dispose();
        _app.Run();
    }

    private static void RenderLoop(double dt)
    {
        UpdateInput((float)dt);

        // render world
        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        KoRender.EnableDepthTest();
        KoRender.DisableBlending();

        KoRender.BeginCamera(_camera);
        DrawWorld();
        KoRender.EndCamera();

        // render ui
        KoRender.DisableDepthTest();
        KoRender.EnableBlending();

        // match screen coords (0,0 is top-left)
        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, _app.Width, _app.Height, 0, -1, 1);
        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        DrawUI();

        KoRender.Flush();
    }

    private static void UpdateInput(float dt)
    {
        if (InputMap.IsActionPressed("ToggleMouse"))
        {
            InputManager.CursorMode =
                InputManager.CursorMode == CursorMode.Locked
                    ? CursorMode.Normal
                    : CursorMode.Locked;
        }

        if (InputManager.CursorMode == CursorMode.Locked)
        {
            Vector2 mouseDelta = InputManager.MouseDelta;
            _yaw -= mouseDelta.X * 0.1f;
            _pitch -= mouseDelta.Y * 0.1f;
            _pitch = Math.Clamp(_pitch, -89f, 89f);
            _camera.Rotation = new Vector3(_pitch, _yaw, 0);
        }

        Vector2 inputDir = InputMap.GetVector(
            "MoveLeft",
            "MoveRight",
            "MoveBackward",
            "MoveForward"
        );
        _camera.GetViewMatrix();

        Vector3 horizontalFront = Vector3.Normalize(
            new Vector3(_camera.Front.X, 0, _camera.Front.Z)
        );
        _camera.Position += horizontalFront * inputDir.Y * 5.0f * dt;
        _camera.Position += _camera.Right * inputDir.X * 5.0f * dt;

        if (InputMap.IsActionPressed("Jump") && _camera.Position.Y <= 1.0f)
            _camera.Position.Y = 3.0f;
        if (_camera.Position.Y > 1.0f)
        {
            _camera.Position.Y -= 5.0f * dt;
            if (_camera.Position.Y < 1.0f)
                _camera.Position.Y = 1.0f;
        }
    }

    private static void DrawWorld()
    {
        // draw reference grid
        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color4(0.5f, 0.5f, 0.5f, 1.0f);
        for (int i = -10; i <= 10; i++)
        {
            KoRender.Vertex3(i, 0, -10);
            KoRender.Vertex3(i, 0, 10);
            KoRender.Vertex3(-10, 0, i);
            KoRender.Vertex3(10, 0, i);
        }
        KoRender.End();

        // draw floor
        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(0.3f, 0.3f, 0.3f, 1.0f);
        KoRender.Vertex3(-10, 0, -10);
        KoRender.Vertex3(10, 0, -10);
        KoRender.Vertex3(10, 0, 10);
        KoRender.Vertex3(-10, 0, 10);
        KoRender.End();
    }

    private static void DrawUI()
    {
        // draw main text
        KoGLText.DrawText(
            _uiFont,
            "CONTROLS: WASD to Move | ESC to Toggle Mouse",
            new Vector2(10, 10),
            Vector4.One
        );

        // camera info
        string posText =
            $"XYZ: {_camera.Position.X:F2}, {_camera.Position.Y:F2}, {_camera.Position.Z:F2}";
        KoGLText.DrawText(_uiFont, posText, new Vector2(10, 40), new Vector4(0.2f, 0.8f, 0.2f, 1f));

        // crosshair
        KoGLText.DrawText(_uiFont, "+", new Vector2(400, 300), Vector4.One, TextAlignment.Center);

        if (InputMap.IsActionDown("Shoot"))
        {
            KoGLText.DrawText(
                _uiFont,
                "FIRING!",
                new Vector2(400, 340),
                new Vector4(1, 0, 0, 1),
                TextAlignment.Center
            );
        }
    }
}
