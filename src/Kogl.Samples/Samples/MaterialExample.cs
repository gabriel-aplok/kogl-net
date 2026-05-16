using System.Numerics;
using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Core.Resources;
using Kogl.FreeType;
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

        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        RenderApi.EnableDepthTest();

        _camera.Position.X = MathF.Sin(_time) * 8f;
        _camera.Position.Z = MathF.Cos(_time) * 8f;
        _camera.LookAt(Vector3.Zero);

        RenderApi.BeginCamera(_camera);

        GlobalUniforms.SetFloat("uTime", _time);

        DrawWorld();

        RenderApi.EndCamera();
        RenderApi.DisableDepthTest();
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
}
