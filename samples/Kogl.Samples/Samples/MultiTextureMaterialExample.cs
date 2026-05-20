using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Graphics;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.FreeType;
using Kogl.Input;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class MultiTextureMaterialExample
{
    private static readonly AppWindow _app = new(800, 600, "KoGL - Multi-Texture Material Example");
    private static readonly Camera _camera = new();
    private static Font _uiFont = null!;

    private static Shader _pbrShader = null!;
    private static Material _brickMaterial = null!;
    private static Material _blueMaterial = null!;
    private static Material _gridMaterial = null!;

    private static Texture _brickAlbedoTex = null!;
    private static Texture _brickNormalTex = null!;
    private static Texture _containerAlbedoTex = null!;

    private static float _yaw = -90f;
    private static float _pitch = 0f;
    private static float _time = 0f;

    public static void Start()
    {
        _app.OnLoad += () =>
        {
            _camera.Position = new Vector3(0, 3, 8);
            _camera.Projection = CameraProjection.Perspective;
            _camera.Fov = 60f;
            _camera.LookAt(new Vector3(0, 0, 0));

            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);

            // TODO: Tangent and Bitangent vectors
            string vs =
                @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;
layout(location = 3) in vec3 aNormal;
layout(location = 4) in vec4 aTangent;

out vec2 fTex;
out vec4 fCol;
out vec3 fNormal;
out vec3 fTangent;
out vec3 fBitangent;
out vec3 fWorldPos;

uniform mat4 uMVP;

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fTex = aTex;
    fCol = aCol;
    fWorldPos = aPos;

    mat3 normalMatrix = mat3(uMVP); // or better: transpose(inverse(mat3(model))) if you have model matrix
    fNormal    = normalize(normalMatrix * aNormal);
    fTangent   = normalize(normalMatrix * aTangent.xyz);
    fBitangent = cross(fNormal, fTangent) * aTangent.w; // handedness
}";

            string fs =
                @"#version 330 core
in vec2 fTex;
in vec4 fCol;
in vec3 fNormal;
in vec3 fTangent;
in vec3 fBitangent;
in vec3 fWorldPos;

out vec4 FragColor;

uniform sampler2D uAlbedoTex;
uniform sampler2D uNormalTex;
uniform vec4 uTint;

void main() {
    vec4 albedo = texture(uAlbedoTex, fTex);
    vec3 normalMap = texture(uNormalTex, fTex).rgb * 2.0 - 1.0;

    // build TBN matrix
    mat3 TBN = mat3(fTangent, fBitangent, fNormal);
    vec3 finalNormal = normalize(TBN * normalMap);

    // lighting
    vec3 sunDirection = normalize(vec3(0.4, 1.0, 0.3));
    vec3 sunColor = vec3(1.1, 1.05, 0.95);
    vec3 ambientColor = vec3(0.22, 0.24, 0.26);

    float diffuseFactor = max(dot(finalNormal, sunDirection), 0.0);
    vec3 lighting = ambientColor + (diffuseFactor * sunColor);

    FragColor = albedo * vec4(lighting, 1.0) * fCol * uTint;
}";
            _pbrShader = Shader.Create(vs, fs);

            _pbrShader.AddProperty("uAlbedoTex", ShaderPropertyType.Texture2D);
            _pbrShader.AddProperty("uNormalTex", ShaderPropertyType.Texture2D);
            _pbrShader.AddProperty("uTint", ShaderPropertyType.Vec4);

            _brickAlbedoTex = ResourceManager.Load<Texture>("assets/brickwall.jpg");
            _brickNormalTex = ResourceManager.Load<Texture>("assets/brickwall_normal.jpg");
            _containerAlbedoTex = ResourceManager.Load<Texture>("assets/container.jpg");

            Material baseMat = new(_pbrShader);
            baseMat.SetTexture("uAlbedoTex", _brickAlbedoTex);
            baseMat.SetTexture("uNormalTex", _brickNormalTex);
            baseMat.SetVector4("uTint", Vector4.One);
            baseMat.DepthTest = true;
            baseMat.Blending = false;

            _brickMaterial = baseMat.CreateInstance();
            _brickMaterial.SetVector4("uTint", new Vector4(1.0f, 0.8f, 0.6f, 1.0f));

            _blueMaterial = baseMat.CreateInstance();
            _blueMaterial.SetTexture("uAlbedoTex", _containerAlbedoTex);
            // btw container doesn't have a specific normal map, it reuses the base brick normal texture slot smoothly
            _blueMaterial.SetVector4("uTint", new Vector4(0.3f, 0.5f, 1.0f, 1.0f));

            _gridMaterial = baseMat.CreateInstance();
            _gridMaterial.SetVector4("uTint", new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
        };

        _app.OnRender += RenderLoop;
        _app.OnResizeEvent += (width, height) =>
        {
            _camera.AspectRatio = height == 0 ? 1f : (float)width / height;
        };
        _app.OnUnload += () =>
        {
            ResourceManager.UnloadAll();
            _uiFont?.Dispose();
        };
        _app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;
        UpdateInput((float)dt);

        KoRender.Clear(0.11f, 0.13f, 0.16f, 1.0f);
        KoRender.EnableDepthTest();

        KoRender.BeginCamera(_camera);
        DrawWorld();
        KoRender.EndCamera();

        KoRender.DisableDepthTest();
        DrawUI();
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

    private static void DrawWorld()
    {
        KoRender.Begin(PrimitiveMode.Lines);
        for (int i = -10; i <= 10; i++)
        {
            KoRender.Vertex3(i, 0, -10);
            KoRender.Vertex3(i, 0, 10);
            KoRender.Vertex3(-10, 0, i);
            KoRender.Vertex3(10, 0, i);
        }
        KoRender.End();

        DrawMaterialCube(new Vector3(-2, 1, 0), _brickMaterial);
        DrawMaterialCube(new Vector3(2, 1, 0), _blueMaterial);

        // This is a test for a material that uses multiple textures.
        // I was genuinely surprised when I saw that all of this was only using 3% of the CPU and 70 MB of RAM.
        for (int i = 0; i < 450; i++)
        {
            DrawMaterialCube(new Vector3(-2, 6, i + 30), _brickMaterial);
            DrawMaterialCube(new Vector3(-4, 6, i - 30), _brickMaterial);
            DrawMaterialCube(new Vector3(i + 30, 8, 1), _blueMaterial);
            DrawMaterialCube(new Vector3(i - 30, 8, 3), _blueMaterial);
        }
    }

    private static void DrawMaterialCube(Vector3 position, Material mat)
    {
        KoRender.PushMatrix();
        KoRender.Translate(position.X, position.Y, position.Z);
        KoRender.ApplyMaterial(mat);

        KoRender.EnableCulling(CullFaceState.Back);
        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 1, 1, 1);

        // Front (+Z)
        KoRender.Normal3(0, 0, 1);
        KoRender.Tangent4(1, 0, 0, 1); // tangent = +X, handedness +1
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, 1);

        // Back (-Z)
        KoRender.Normal3(0, 0, -1);
        KoRender.Tangent4(-1, 0, 0, 1); // tangent = -X
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, 1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);

        // Top (+Y)
        KoRender.Normal3(0, 1, 0);
        KoRender.Tangent4(1, 0, 0, 1); // tangent = +X
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, -1);

        // Bottom (-Y)
        KoRender.Normal3(0, -1, 0);
        KoRender.Tangent4(1, 0, 0, -1); // handedness flipped
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, -1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, -1, 1);

        // Right (+X)
        KoRender.Normal3(1, 0, 0);
        KoRender.Tangent4(0, 0, 1, 1); // tangent = +Z
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, -1, 1);

        // Left (-X)
        KoRender.Normal3(-1, 0, 0);
        KoRender.Tangent4(0, 0, -1, 1); // tangent = -Z
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, 1, -1);

        KoRender.End();
        KoRender.PopMatrix();
    }

    private static void DrawUI()
    {
        KoRender.EnableBlending();

        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, _app.Width, _app.Height, 0, -1, 1);
        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        string pos = $"Pos: {_camera.Position:F1}";
        KoGLText.DrawText(_uiFont, pos, new Vector2(10, 10), new Vector4(0.2f, 0.8f, 0.2f, 1));
    }
}
