using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Maths;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class CameraExample
{
    private static readonly Camera _camera = new();
    private static Shader _shader = null!;
    private static Transform _myCubeTransform;

    private static float _time;

    public static void Start()
    {
        AppWindow app = new(800, 600, "Kolpa - Camera Example");

        _camera.Position = new Vector3(0, 3, 8);
        _camera.Projection = CameraProjection.Perspective;
        _camera.Fov = 60f;

        app.OnLoad += static () =>
        {
            //             string vs =
            //                 @"#version 330 core
            // layout(location = 0) in vec3 aPos;
            // layout(location = 1) in vec2 aTex;
            // layout(location = 2) in vec4 aCol;

            // out vec2 fTex;
            // out vec4 fCol;

            // uniform mat4 uMVP;

            // void main() {
            //     gl_Position = uMVP * vec4(aPos, 1.0);
            //     fTex = aTex;
            //     fCol = aCol;
            // }";

            //             string fs =
            //                 @"#version 330 core
            // in vec2 fTex;
            // in vec4 fCol;
            // out vec4 FragColor;

            // uniform float uTime;

            // void main() {
            //     // Procedural animation that doesn't rely on texture details
            //     float wave = sin(fTex.x * 10.0 + uTime * 3.0) * 0.5 + 0.5;
            //     vec3 colorA = vec3(0.1, 0.5, 0.8); // Blue
            //     vec3 colorB = vec3(0.8, 0.2, 0.1); // Red

            //     vec3 finalRGB = mix(colorA, colorB, wave);
            //     FragColor = vec4(finalRGB, 1.0) * fCol;
            // }";

            // _shader = Shader.Create(vs, fs);
            _shader = AssetManager.Load<Shader>("res://shaders/std.glsl");

            _myCubeTransform = Transform.Identity;
            _myCubeTransform.Translation = new Vector3(0, 1, 0);
        };
        app.OnRender += RenderLoop;
        app.OnResizeEvent += (width, height) =>
        {
            _camera.AspectRatio = height == 0 ? 1f : (float)width / height;
        };
        app.OnUnload += static () =>
        {
            AssetManager.UnloadAll();
            ResourceManager.UnloadAll();
        };
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        KoRender.EnableDepthTest();

        // _camera.Position.X = MathF.Sin(_time) * 8f;
        // _camera.Position.Z = MathF.Cos(_time) * 8f;
        _camera.LookAt(Vector3.Zero);

        KoRender.BeginCamera(_camera);

        // draw a reference grid (lines)
        KoRender.UseDefaultShader();

        KoRender.PushMatrix();
        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color4(0.3f, 0.3f, 0.3f, 1.0f);
        for (int i = -5; i <= 5; i++)
        {
            KoRender.Vertex3(i, 0, -5);
            KoRender.Vertex3(i, 0, 5);
            KoRender.Vertex3(-5, 0, i);
            KoRender.Vertex3(5, 0, i);
        }
        KoRender.End();
        KoRender.PopMatrix();

        KoRender.UseDefaultShader();
        KoGizmo.DrawGizmo3D(100, GizmoFlags.All, ref _myCubeTransform);

        // draw a cube
        KoRender.UseShader(_shader);
        KoRender.SetUniform("uTime", _time);
        KoRender.SetUniform("uTint", new Vector3(0.5f, 0.8f, 1.0f));

        KoRender.PushMatrix();
        // KoRender.Translate(0, 0.5f, 0);
        KoRender.Multiply(_myCubeTransform.ToMatrix());
        DrawCube();
        KoRender.PopMatrix();

        KoRender.EndCamera();
    }

    private static void DrawCube()
    {
        KoRender.Begin(PrimitiveMode.LineStrip);
        KoRender.Color4(1, 1, 1, 1);

        // ff (red)
        // KoRender.Color4(1, 0, 0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.Vertex3(-1, 1, 1);

        // bf (orange)
        // KoRender.Color4(1, 0.5f, 0, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.Vertex3(1, -1, -1);

        // tf (blue)
        // KoRender.Color4(0, 0, 1, 1);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.Vertex3(1, 1, -1);

        // bf (yellow)
        // KoRender.Color4(1, 1, 0, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.Vertex3(-1, -1, 1);

        // rf (green)
        // KoRender.Color4(0, 1, 0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.Vertex3(1, -1, 1);

        // lf (purple)
        // KoRender.Color4(1, 0, 1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.Vertex3(-1, 1, -1);

        KoRender.End();
    }
}
