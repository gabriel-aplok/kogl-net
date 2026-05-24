using System.Numerics;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.FreeType;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class BasicLightingExample
{
    private static readonly AppWindow _app = new(800, 600, "Kolpa - Basic Lighting System");
    private static readonly Camera _camera = new();

    private static Font _uiFont = null!;

    private static Shader _lightingShader = null!;
    private static Material _lightingMaterial = null!;
    private static Material _gridMaterial = null!;
    private static Texture _texelChecerTex = null!;

    private static readonly Light[] _lights = new Light[4];
    private static float _yaw = -90f;
    private static float _pitch = -25f;

    public static void Start()
    {
        _app.OnLoad += () =>
        {
            _camera.Position = new Vector3(2.0f, 4.0f, 6.0f);
            _camera.Projection = CameraProjection.Perspective;
            _camera.Fov = 45f;
            _camera.LookAt(new Vector3(0.0f, 0.5f, 0.0f));

            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);
            InputMap.Bind("ToggleMouse", Key.Escape);

            _lightingShader = AssetManager.Load<Shader>("res://shaders/lighting.glsl");
            _lightingShader.AddProperty("uTex", ShaderPropertyType.Texture2D);
            _lightingShader.AddProperty("colDiffuse", ShaderPropertyType.Vec4);

            _texelChecerTex = AssetManager.Load<Texture>("res://textures/texel_checker.png");

            _lightingMaterial = new Material(_lightingShader);
            _lightingMaterial.SetTexture("uTex", _texelChecerTex);
            _lightingMaterial.SetVector4("colDiffuse", Vector4.One);
            _lightingMaterial.DepthTest = true;
            _lightingMaterial.Blending = false;

            _gridMaterial = new Material(KoRender.DefaultShader);
            _gridMaterial.SetTexture("uTex", new Texture(KoRender.DefaultTexture, 1, 1));
            _gridMaterial.SetVector4("uTint", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            _gridMaterial.DepthTest = true;
            _gridMaterial.Blending = false;

            _lights[0] = Light.Create(
                LightType.Point,
                new Vector3(-2, 1, -2),
                Vector3.Zero,
                new Vector4(1f, 0.9f, 0f, 1f)
            );
            _lights[1] = Light.Create(
                LightType.Point,
                new Vector3(2, 1, 2),
                Vector3.Zero,
                new Vector4(1f, 0f, 0f, 1f)
            );
            _lights[2] = Light.Create(
                LightType.Point,
                new Vector3(-2, 1, 2),
                Vector3.Zero,
                new Vector4(0f, 1f, 0f, 1f)
            );
            _lights[3] = Light.Create(
                LightType.Point,
                new Vector3(2, 1, -2),
                Vector3.Zero,
                new Vector4(0f, 0f, 1f, 1f)
            );

            InputManager.CursorMode = CursorMode.Locked;
        };

        _app.OnRender += RenderLoop;
        _app.OnResizeEvent += (width, height) =>
        {
            _camera.UpdateViewport(width, height);
        };

        _app.OnUnload += () =>
        {
            _uiFont?.Dispose();
            AssetManager.Unload("res://shaders/lighting.glsl");
            AssetManager.Unload("res://textures/texel_checker.png");
        };

        _app.Run();
    }

    private static void RenderLoop(double dt)
    {
        UpdateInput((float)dt);

        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        KoRender.EnableDepthTest();
        KoRender.DisableBlending();

        KoRender.BeginCamera(_camera);

        KoRender.ApplyMaterial(_lightingMaterial);
        KoRender.SetUniform("viewPos", _camera.Position);
        KoRender.SetUniform("ambient", new Vector4(0.1f, 0.1f, 0.1f, 1.0f));

        for (int i = 0; i < 4; i++)
        {
            _lights[i].UpdateValues(_lightingShader, i);
        }

        DrawPlane(10.0f, 10.0f);

        KoRender.PushMatrix();
        KoRender.Translate(0.0f, 1.0f, 0.0f);
        DrawCube(2.0f, 2.0f, 2.0f);
        KoRender.PopMatrix();

        for (int i = 0; i < 4; i++)
        {
            DrawSphere(_lights[i].Position, 0.2f, 8, 8, _lights[i].Color, !_lights[i].Enabled);
        }

        DrawGrid(10, 1.0f);

        KoRender.EndCamera();

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
        }

        if (InputManager.IsKeyPressed(Key.Y))
            _lights[0].Enabled = !_lights[0].Enabled;
        if (InputManager.IsKeyPressed(Key.R))
            _lights[1].Enabled = !_lights[1].Enabled;
        if (InputManager.IsKeyPressed(Key.G))
            _lights[2].Enabled = !_lights[2].Enabled;
        if (InputManager.IsKeyPressed(Key.B))
            _lights[3].Enabled = !_lights[3].Enabled;
    }

    private static void DrawPlane(float width, float length)
    {
        float hw = width / 2.0f;
        float hl = length / 2.0f;

        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 1, 1, 1);
        KoRender.Normal3(0.0f, 1.0f, 0.0f);

        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-hw, 0, -hl);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-hw, 0, hl);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(hw, 0, hl);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(hw, 0, -hl);
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

    private static void DrawSphere(
        Vector3 pos,
        float radius,
        int rings,
        int slices,
        Vector4 color,
        bool wireframe
    )
    {
        KoRender.PushMatrix();
        KoRender.Translate(pos.X, pos.Y, pos.Z);

        if (wireframe)
        {
            KoRender.ApplyMaterial(_gridMaterial);
            KoRender.Begin(PrimitiveMode.Lines);
            for (int i = 0; i <= rings; i++)
            {
                float lat0 = MathF.PI * (-0.5f + ((float)(i - 1) / rings));
                float lat1 = MathF.PI * (-0.5f + ((float)i / rings));
                float z0 = MathF.Sin(lat0) * radius;
                float zr0 = MathF.Cos(lat0) * radius;
                float z1 = MathF.Sin(lat1) * radius;
                float zr1 = MathF.Cos(lat1) * radius;

                for (int j = 0; j <= slices; j++)
                {
                    float lng0 = 2 * MathF.PI * (j - 1) / slices;
                    float lng1 = 2 * MathF.PI * j / slices;

                    float x0_0 = MathF.Cos(lng0) * zr0;
                    float y0_0 = MathF.Sin(lng0) * zr0;
                    float x1_0 = MathF.Cos(lng1) * zr0;
                    float y1_0 = MathF.Sin(lng1) * zr0;

                    float x0_1 = MathF.Cos(lng0) * zr1;
                    float y0_1 = MathF.Sin(lng0) * zr1;
                    float x1_1 = MathF.Cos(lng1) * zr1;
                    float y1_1 = MathF.Sin(lng1) * zr1;

                    // wireframe color with alpha
                    KoRender.Color4(color.X, color.Y, color.Z, 0.3f);

                    KoRender.Vertex3(x0_0, y0_0, z0);
                    KoRender.Vertex3(x1_0, y1_0, z0);

                    KoRender.Vertex3(x0_0, y0_0, z0);
                    KoRender.Vertex3(x0_1, y0_1, z1);
                }
            }
            KoRender.End();
        }
        else
        {
            KoRender.ApplyMaterial(_lightingMaterial);
            KoRender.Begin(PrimitiveMode.Quads);
            for (int i = 0; i < rings; i++)
            {
                float lat0 = MathF.PI * (-0.5f + ((float)i / rings));
                float lat1 = MathF.PI * (-0.5f + ((float)(i + 1) / rings));
                float z0 = MathF.Sin(lat0) * radius;
                float zr0 = MathF.Cos(lat0) * radius;
                float z1 = MathF.Sin(lat1) * radius;
                float zr1 = MathF.Cos(lat1) * radius;

                for (int j = 0; j < slices; j++)
                {
                    float lng0 = 2 * MathF.PI * j / slices;
                    float lng1 = 2 * MathF.PI * (j + 1) / slices;

                    float x0 = MathF.Cos(lng0);
                    float y0 = MathF.Sin(lng0);
                    float x1 = MathF.Cos(lng1);
                    float y1 = MathF.Sin(lng1);

                    KoRender.Color4(color.X, color.Y, color.Z, color.W);

                    KoRender.Normal3(x0 * MathF.Cos(lat0), y0 * MathF.Cos(lat0), MathF.Sin(lat0));
                    KoRender.Vertex3(x0 * zr0, y0 * zr0, z0);

                    KoRender.Normal3(x1 * MathF.Cos(lat0), y1 * MathF.Cos(lat0), MathF.Sin(lat0));
                    KoRender.Vertex3(x1 * zr0, y1 * zr0, z0);

                    KoRender.Normal3(x1 * MathF.Cos(lat1), y1 * MathF.Cos(lat1), MathF.Sin(lat1));
                    KoRender.Vertex3(x1 * zr1, y1 * zr1, z1);

                    KoRender.Normal3(x0 * MathF.Cos(lat1), y0 * MathF.Cos(lat1), MathF.Sin(lat1));
                    KoRender.Vertex3(x0 * zr1, y0 * zr1, z1);
                }
            }
            KoRender.End();
        }

        KoRender.PopMatrix();
    }

    private static void DrawGrid(int size, float spacing)
    {
        KoRender.ApplyMaterial(_gridMaterial);
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

    private static void DrawUI()
    {
        KoRender.DisableDepthTest();
        KoRender.EnableBlending();

        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, _app.Width, _app.Height, 0, -1, 1);

        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        KoGLText.DrawText(
            _uiFont,
            "Use keys [Y][R][G][B] to toggle lights",
            new Vector2(10, 10),
            Vector4.One
        );

        KoGLText.DrawText(
            _uiFont,
            "WASD = Move | Right Mouse (hold) = Rotate camera | ESC = Toggle Mouse lock",
            new Vector2(10, 40),
            new Vector4(0.8f, 0.8f, 0.8f, 1f)
        );

        string statusText =
            $"Lights: [Y] Yellow: {(_lights[0].Enabled ? "ON" : "OFF")} | "
            + $"[R] Red: {(_lights[1].Enabled ? "ON" : "OFF")} | "
            + $"[G] Green: {(_lights[2].Enabled ? "ON" : "OFF")} | "
            + $"[B] Blue: {(_lights[3].Enabled ? "ON" : "OFF")}";

        KoGLText.DrawText(
            _uiFont,
            statusText,
            new Vector2(10, 70),
            new Vector4(0.2f, 0.8f, 1f, 1f)
        );
    }
}
