using System.Numerics;
using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Input;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class InputExample
{
    private static readonly Camera _camera = new();
    private static float _yaw = 0f;
    private static float _pitch = 0f;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Input & Action Mapping");

        app.OnLoad += () =>
        {
            _camera.Position = new Vector3(0, 1, 5);

            // register input map actions
            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);

            InputMap.Bind("Jump", Key.Space);
            InputMap.Bind("Shoot", MouseButton.Left);
            InputMap.Bind("ToggleMouse", Key.Escape);

            // lock cursor into raw windowed context
            Input.Input.CursorMode = CursorMode.Locked;
        };

        app.OnRender += RenderLoop;
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        // state toggling
        if (InputMap.IsActionPressed("ToggleMouse"))
        {
            Input.Input.CursorMode =
                Input.Input.CursorMode == CursorMode.Locked ? CursorMode.Normal : CursorMode.Locked;
        }

        // raw delta mouse look
        if (Input.Input.CursorMode == CursorMode.Locked)
        {
            Vector2 mouseDelta = Input.Input.MouseDelta;
            float sensitivity = 0.1f;
            _yaw -= mouseDelta.X * sensitivity;
            _pitch -= mouseDelta.Y * sensitivity;

            _pitch = Math.Clamp(_pitch, -89f, 89f);
            _camera.Rotation = new Vector3(_pitch, _yaw, 0);
        }

        // input vector normalization
        Vector2 movement = InputMap.GetVector(
            "MoveLeft",
            "MoveRight",
            "MoveBackward",
            "MoveForward"
        );
        float speed = 5.0f * (float)dt;

        // matrix calculation for forward/right vectors based on yaw
        float yawRad = _yaw * (MathF.PI / 180f);
        Vector3 forward = new(MathF.Sin(yawRad), 0, MathF.Cos(yawRad));
        Vector3 right = new(MathF.Cos(yawRad), 0, -MathF.Sin(yawRad));

        _camera.Position += forward * -movement.Y * speed;
        _camera.Position += right * movement.X * speed;

        // input pressed checks
        if (InputMap.IsActionPressed("Jump") && _camera.Position.Y <= 1.0f)
        {
            _camera.Position.Y = 3.0f; // jump
        }

        if (_camera.Position.Y > 1.0f)
        {
            _camera.Position.Y -= 5.0f * (float)dt; // gravity
            if (_camera.Position.Y < 1.0f)
                _camera.Position.Y = 1.0f;
        }

        // output display
        if (InputMap.IsActionDown("Shoot"))
            RenderApi.Clear(0.8f, 0.2f, 0.2f, 1.0f); // fire indicator
        else
            RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        RenderApi.EnableDepthTest();
        RenderApi.BeginCamera(_camera);

        // draw floor
        RenderApi.UseDefaultShader();
        RenderApi.Begin(PrimitiveMode.Quads);
        RenderApi.Color4(0.3f, 0.3f, 0.3f, 1.0f);
        RenderApi.Vertex3(-10, 0, -10);
        RenderApi.Vertex3(10, 0, -10);
        RenderApi.Vertex3(10, 0, 10);
        RenderApi.Vertex3(-10, 0, 10);
        RenderApi.End();

        RenderApi.EndCamera();
        RenderApi.Flush();
    }
}
