using System.Numerics;
using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Core.Resources;
using Kogl.FreeType;
using Kogl.Input;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class MaterialExample
{
    private static readonly Camera _camera = new();
    private static Font _uiFont = null!;

    private static Shader _standardShader = null!;
    private static Material _redMaterial = null!;
    private static Material _bluePulseMaterial = null!;
    private static Material _gridMaterial = null!;
    private static Texture _logoTex = null!;

    private static float _yaw = -90f;
    private static float _pitch = 0f;
    private static float _time = 0f;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Material System");

        app.OnLoad += () =>
        {
            _camera.Position = new Vector3(0, 3, 8);
            _camera.Projection = CameraProjection.Perspective;
            _camera.Fov = 60f;

            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);

            string vs =
                @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

out vec2 fTex;
out vec4 fCol;

uniform mat4 uMVP;

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fTex = aTex;
    fCol = aCol;
}";

            string fs =
                @"#version 330 core
in vec2 fTex;
in vec4 fCol;
out vec4 FragColor;

uniform sampler2D uTex;
uniform vec4 uTint;
uniform float uTime;

void main() {
    vec4 tex = texture(uTex, fTex);
    float pulse = (sin(uTime * 4.0) * 0.15) + 0.85;
    FragColor = tex * fCol * uTint * vec4(pulse, pulse, pulse, 1.0);
}";

            _standardShader = Shader.Create(vs, fs);
            _standardShader.AddProperty("uTex", ShaderPropertyType.Texture2D);
            _standardShader.AddProperty("uTint", ShaderPropertyType.Vec4);

            _logoTex = ResourceManager.Load<Texture>("assets/logo.png");

            Material baseMat = new(_standardShader);
            baseMat.SetTexture("uTex", _logoTex);
            baseMat.SetVector4("uTint", Vector4.One);
            baseMat.DepthTest = true;
            baseMat.Blending = false;

            _redMaterial = baseMat.CreateInstance();
            _redMaterial.SetVector4("uTint", new Vector4(1.0f, 0.3f, 0.3f, 1.0f));

            _bluePulseMaterial = baseMat.CreateInstance();
            _bluePulseMaterial.SetVector4("uTint", new Vector4(0.3f, 0.5f, 1.0f, 1.0f));

            _gridMaterial = baseMat.CreateInstance();
            _gridMaterial.SetVector4("uTint", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        };
        app.OnRender += RenderLoop;
        app.OnUnload += () =>
        {
            ResourceManager.UnloadAll();
            _uiFont?.Dispose();
        };
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;
        UpdateInput((float)dt);

        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        RenderApi.EnableDepthTest();

        RenderApi.BeginCamera(_camera);

        GlobalUniforms.SetFloat("uTime", _time);

        DrawWorld();

        RenderApi.EndCamera();
        RenderApi.DisableDepthTest();

        DrawUI();
    }

    private static void UpdateInput(float dt)
    {
        // right mouse button
        if (InputManager.IsMouseButtonDown(MouseButton.Right))
        {
            InputManager.CursorMode = CursorMode.Locked;

            // mouse
            Vector2 delta = InputManager.MouseDelta;
            _yaw -= delta.X * 0.15f;
            _pitch -= delta.Y * 0.15f;
            _pitch = Math.Clamp(_pitch, -89f, 89f);
            _camera.Rotation = new Vector3(_pitch, _yaw, 0);

            // move
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
        else
        {
            InputManager.CursorMode = CursorMode.Normal;
        }
    }

    private static void DrawWorld()
    {
        RenderApi.ApplyMaterial(_gridMaterial);

        RenderApi.Begin(PrimitiveMode.Lines);
        for (int i = -10; i <= 10; i++)
        {
            RenderApi.Vertex3(i, 0, -10);
            RenderApi.Vertex3(i, 0, 10);
            RenderApi.Vertex3(-10, 0, i);
            RenderApi.Vertex3(10, 0, i);
        }
        RenderApi.End();

        DrawMaterialCube(new Vector3(-2, 1, 0), _redMaterial);
        DrawMaterialCube(new Vector3(2, 1, 0), _bluePulseMaterial);
    }

    private static void DrawMaterialCube(Vector3 position, Material mat)
    {
        RenderApi.PushMatrix();
        RenderApi.Translate(position.X, position.Y, position.Z);

        RenderApi.ApplyMaterial(mat);
        RenderApi.UseTexture(_logoTex.Handle);

        RenderApi.Begin(PrimitiveMode.Quads);
        RenderApi.Color4(1, 1, 1, 1);

        // Front
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(-1, -1, 1);
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(1, -1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(-1, 1, 1);

        // Back
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(-1, 1, -1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(1, 1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(1, -1, -1);

        // Top
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(-1, 1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(-1, 1, 1);
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(1, 1, -1);

        // Bottom
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(1, -1, -1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(1, -1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(-1, -1, 1);

        // Right
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(1, -1, -1);
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(1, 1, -1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(1, -1, 1);

        // Left
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(-1, -1, 1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(-1, 1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(-1, 1, -1);

        RenderApi.End();
        RenderApi.PopMatrix();
    }

    private static void DrawUI()
    {
        RenderApi.DisableDepthTest();
        RenderApi.EnableBlending();

        RenderApi.MatrixMode(MatrixStackMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);
        RenderApi.MatrixMode(MatrixStackMode.ModelView);
        RenderApi.LoadIdentity();

        KoGLText.DrawText(
            _uiFont,
            "WASD = Move | Right Mouse = Look | ESC = Toggle Cursor",
            new Vector2(10, 10),
            Vector4.One
        );

        string pos = $"Pos: {_camera.Position:F1}";
        KoGLText.DrawText(_uiFont, pos, new Vector2(10, 40), new Vector4(0.2f, 0.8f, 0.2f, 1));
    }
}
