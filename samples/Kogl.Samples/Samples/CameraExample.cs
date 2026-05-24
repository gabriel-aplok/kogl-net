using System.Numerics;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Maths;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class CameraExample
{
    private static Camera _camera = null!;
    private static Shader _shader = null!;
    private static Material _mat = null!;
    private static Texture _texture = null!;
    private static Transform _transform = Transform.Identity;

    private static float _yaw = 0f;
    private static float _pitch = 0f;

    public static void Start()
    {
        AppWindow app = new(800, 600, "Kolpa - Camera Example");

        _camera = new Camera
        {
            Position = new Vector3(0f, 4f, 8f),
            Projection = CameraProjection.Perspective,
            Fov = 60f,
            Near = 0.1f,
            Far = 1000f,
        };
        _camera.LookAt(new Vector3(0f, 1f, 0f));

        app.OnLoad += static () =>
        {
            _texture = AssetManager.Load<Texture>("res://textures/texel_checker.png");
            _shader = AssetManager.Load<Shader>("res://shaders/std_textured.glsl");
            _shader.AddProperty("uTex", ShaderPropertyType.Texture2D);

            _mat = new Material(_shader);
            _mat.SetTexture("uTex", _texture);

            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);
        };
        app.OnRender += RenderLoop;
        app.OnResizeEvent += (width, height) =>
        {
            _camera.UpdateViewport(width, height);
        };
        app.OnUnload += static () =>
        {
            AssetManager.UnloadAll();
        };
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        UpdateInput((float)dt);

        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        KoRender.EnableDepthTest();
        KoRender.DisableBlending();

        KoRender.BeginCamera(_camera);

        // draw a reference grid
        DrawGrid(10, 1.0f);

        // draw a cube
        KoRender.ApplyMaterial(_mat);

        KoRender.PushMatrix();
        KoRender.Multiply(_transform.ToMatrix());
        DrawCube(2.0f, 2.0f, 2.0f);
        KoRender.PopMatrix();

        KoRender.UseDefaultShader();
        KoGizmo.DrawGizmo3D(
            100,
            GizmoFlags.Translate
                | GizmoFlags.Scale
                | GizmoFlags.ConstantScreenSize
                | GizmoFlags.RenderOnTop,
            ref _transform
        );

        KoRender.EndCamera();
    }

    private static void UpdateInput(float dt)
    {
        if (InputManager.IsMouseButtonDown(MouseButton.Right))
        {
            InputManager.CursorMode = CursorMode.Locked;

            Vector2 delta = InputManager.MouseDelta;
            _yaw -= delta.X * 0.15f;
            _pitch -= delta.Y * 0.15f;
            _pitch = Math.Clamp(_pitch, -89f, 89f);
            _camera.Rotation = new Vector3(_pitch, _yaw, 0);

            Vector2 inputDir = InputMap.GetVector(
                "MoveLeft",
                "MoveRight",
                "MoveBackward",
                "MoveForward"
            );

            Vector3 horizontalFront = Vector3.Normalize(
                new Vector3(_camera.Front.X, 0, _camera.Front.Z)
            );

            _camera.Position += horizontalFront * inputDir.Y * 8.0f * dt;
            _camera.Position += _camera.Right * inputDir.X * 8.0f * dt;

            if (InputManager.IsKeyDown(Key.Space))
            {
                _camera.Position += Vector3.UnitY * 8.0f * dt;
            }
            if (InputManager.IsKeyDown(Key.ShiftLeft))
            {
                _camera.Position -= Vector3.UnitY * 8.0f * dt;
            }
        }
        else
        {
            InputManager.CursorMode = CursorMode.Normal;
        }
    }

    private static void DrawGrid(int size, float spacing)
    {
        KoRender.UseDefaultShader();
        KoRender.Begin(PrimitiveMode.Lines);
        float halfSize = size * spacing / 2.0f;
        for (int i = 0; i <= size; i++)
        {
            float coord = -halfSize + (i * spacing);

            KoRender.Vertex3(coord, 0.0f, -halfSize);
            KoRender.Vertex3(coord, 0.0f, halfSize);

            KoRender.Vertex3(-halfSize, 0.0f, coord);
            KoRender.Vertex3(halfSize, 0.0f, coord);
        }
        KoRender.End();
    }

    private static void DrawCube(float w, float h, float d)
    {
        float x = w / 2.0f;
        float y = h / 2.0f;
        float z = d / 2.0f;

        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 1, 1, 1);

        // Front Face (Normal +Z)
        KoRender.Normal3(0.0f, 0.0f, 1.0f);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-x, -y, z);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(x, -y, z);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(x, y, z);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-x, y, z);

        // Back Face (Normal -Z)
        KoRender.Normal3(0.0f, 0.0f, -1.0f);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-x, -y, -z);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-x, y, -z);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(x, y, -z);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(x, -y, -z);

        // Top Face (Normal +Y)
        KoRender.Normal3(0.0f, 1.0f, 0.0f);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-x, y, -z);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-x, y, z);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(x, y, z);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(x, y, -z);

        // Bottom Face (Normal -Y)
        KoRender.Normal3(0.0f, -1.0f, 0.0f);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-x, -y, -z);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(x, -y, -z);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(x, -y, z);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-x, -y, z);

        // Right Face (Normal +X)
        KoRender.Normal3(1.0f, 0.0f, 0.0f);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(x, -y, -z);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(x, y, -z);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(x, y, z);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(x, -y, z);

        // Left Face (Normal -X)
        KoRender.Normal3(-1.0f, 0.0f, 0.0f);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-x, -y, -z);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-x, -y, z);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-x, y, z);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-x, y, -z);

        KoRender.End();
    }
}
