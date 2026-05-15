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

    private static float _yaw = 0f;
    private static float _pitch = 0f;
    private static float _time = 0f;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Material System");

        app.OnLoad += () =>
        {
            // scene camera
            _camera.Position = new Vector3(0, 1, 5);

            // font
            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            string vs =
                @"#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aColor;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uView;
uniform mat4 uProjection;
uniform mat4 uModel;

out vec4 vColor;
out vec2 vTexCoord;

void main() {
    gl_Position = uProjection * uView * uModel * vec4(aPos, 1.0);
    vColor = aColor;
    vTexCoord = aTexCoord;
}";

            string fs =
                @"#version 330 core
in vec4 vColor;
in vec2 vTexCoord;

uniform sampler2D uTexture;
uniform vec4 uTint;
uniform float uTime;

out vec4 FragColor;

void main() {
    vec4 tex = texture(uTexture, vTexCoord);
    float pulse = (sin(uTime * 4.0) * 0.15) + 0.85;
    FragColor = tex * vColor * uTint * vec4(pulse, pulse, pulse, 1.0);
}";

            // shader
            _standardShader = Shader.Create(vs, fs);
            _standardShader.AddProperty("uTexture", ShaderPropertyType.Texture2D);
            _standardShader.AddProperty("uTint", ShaderPropertyType.Vec4);

            // texture
            _logoTex = ResourceManager.Load<Texture>("assets/logo.png");

            // material
            Material baseMat = new(_standardShader);
            baseMat.SetTexture("uTexture", _logoTex);
            baseMat.SetVector4("uTint", Vector4.One);
            baseMat.DepthTest = true;
            baseMat.Blending = false;

            // deriving material instances via overrides
            _redMaterial = baseMat.CreateInstance();
            _redMaterial.SetVector4("uTint", new Vector4(1.0f, 0.3f, 0.3f, 1.0f));

            _bluePulseMaterial = baseMat.CreateInstance();
            _bluePulseMaterial.SetVector4("uTint", new Vector4(0.3f, 0.5f, 1.0f, 1.0f));

            _gridMaterial = baseMat.CreateInstance();
            _gridMaterial.SetVector4("uTint", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

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

        // render world
        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        RenderApi.EnableDepthTest();
        RenderApi.DisableBlending();

        RenderApi.BeginCamera(_camera);

        float aspect = 800f / 600f;

        GlobalUniforms.SetMatrix4("uView", _camera.GetViewMatrix());
        GlobalUniforms.SetMatrix4("uProjection", _camera.GetProjectionMatrix(aspect));
        GlobalUniforms.SetMatrix4("uModel", Matrix4x4.Identity);
        GlobalUniforms.SetFloat("uTime", _time);

        DrawWorld();

        RenderApi.EndCamera();

        // render ui
        RenderApi.DisableDepthTest();
        RenderApi.EnableBlending();

        // match screen coords (0,0 is top-left)
        RenderApi.MatrixMode(MatrixMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);
        RenderApi.MatrixMode(MatrixMode.ModelView);
        RenderApi.LoadIdentity();

        DrawUI();

        RenderApi.Flush();
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
        // render environment grid
        RenderApi.ApplyMaterial(_gridMaterial);

        // identity model matrix for grid floor
        GlobalUniforms.SetMatrix4("uModel", Matrix4x4.Identity);

        RenderApi.Begin(PrimitiveMode.Lines);
        for (int i = -10; i <= 10; i++)
        {
            RenderApi.Vertex3(i, 0, -10);
            RenderApi.Vertex3(i, 0, 10);
            RenderApi.Vertex3(-10, 0, i);
            RenderApi.Vertex3(10, 0, i);
        }
        RenderApi.End();

        // render left primitive object
        DrawMaterialCube(new Vector3(-2, 1, 0), _redMaterial);

        // render right primitive object
        DrawMaterialCube(new Vector3(2, 1, 0), _bluePulseMaterial);
    }

    private static void DrawMaterialCube(Vector3 position, Material mat)
    {
        // bind material
        RenderApi.ApplyMaterial(mat);

        // push model transform matrix down to current material vertex uniform
        Matrix4x4 modelMatrix = Matrix4x4.CreateTranslation(position);
        GlobalUniforms.SetMatrix4("uModel", modelMatrix);

        RenderApi.Begin(PrimitiveMode.Quads);

        // Front Face
        RenderApi.Color4(1, 1, 1, 1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(-1, -1, 1);
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(1, -1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(-1, 1, 1);

        // Back Face
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(-1, 1, -1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(1, 1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(1, -1, -1);

        // Top Face
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(-1, 1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(-1, 1, 1);
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(1, 1, -1);

        // Bottom Face
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(1, -1, -1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(1, -1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(-1, -1, 1);

        // Right Face
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(1, -1, -1);
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(1, 1, -1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(1, -1, 1);

        // Left Face
        RenderApi.TexCoord2(1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.TexCoord2(0, 1);
        RenderApi.Vertex3(-1, -1, 1);
        RenderApi.TexCoord2(0, 0);
        RenderApi.Vertex3(-1, 1, 1);
        RenderApi.TexCoord2(1, 0);
        RenderApi.Vertex3(-1, 1, -1);

        RenderApi.End();
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
