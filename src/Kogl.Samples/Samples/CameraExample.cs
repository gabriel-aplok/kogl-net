using System.Numerics;
using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class CameraExample
{
    private static readonly Camera _camera = new();
    private static float _time;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Camera Example");

        _camera.Position = new Vector3(0, 3, 8);
        _camera.Projection = CameraProjection.Perspective;
        _camera.Fov = 60f;

        app.OnRender += RenderLoop;
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        KoGL.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        KoGL.EnableDepthTest();

        // orbit the camera in a circle, constantly looking at the center (0,0,0)
        _camera.Position.X = MathF.Sin(_time) * 8f;
        _camera.Position.Z = MathF.Cos(_time) * 8f;
        _camera.LookAt(Vector3.Zero);

        // apply projection and view matrices
        KoGL.BeginCamera(_camera);
        KoGL.UseDefaultShader();

        // draw a reference grid (lines)
        KoGL.Begin(PrimitiveMode.Lines);
        KoGL.Color4(0.3f, 0.3f, 0.3f, 1.0f);
        for (int i = -5; i <= 5; i++)
        {
            KoGL.Vertex3(i, 0, -5);
            KoGL.Vertex3(i, 0, 5);
            KoGL.Vertex3(-5, 0, i);
            KoGL.Vertex3(5, 0, i);
        }
        KoGL.End();

        // draw a central object (cube)
        KoGL.PushMatrix();
        KoGL.Translate(0, 0.5f, 0);
        DrawCube();
        KoGL.PopMatrix();

        // flush and reset matrices back to Identity
        KoGL.EndCamera();

        KoGL.DisableDepthTest();
    }

    private static void DrawCube()
    {
        // draw the cube using 6 quads
        KoGL.Begin(PrimitiveMode.Quads);

        // ff (red)
        KoGL.Color4(1, 0, 0, 1);
        KoGL.Vertex3(-1, -1, 1);
        KoGL.Vertex3(1, -1, 1);
        KoGL.Vertex3(1, 1, 1);
        KoGL.Vertex3(-1, 1, 1);

        // bf (orange)
        KoGL.Color4(1, 0.5f, 0, 1);
        KoGL.Vertex3(-1, -1, -1);
        KoGL.Vertex3(-1, 1, -1);
        KoGL.Vertex3(1, 1, -1);
        KoGL.Vertex3(1, -1, -1);

        // tf (blue)
        KoGL.Color4(0, 0, 1, 1);
        KoGL.Vertex3(-1, 1, -1);
        KoGL.Vertex3(-1, 1, 1);
        KoGL.Vertex3(1, 1, 1);
        KoGL.Vertex3(1, 1, -1);

        // bf (yellow)
        KoGL.Color4(1, 1, 0, 1);
        KoGL.Vertex3(-1, -1, -1);
        KoGL.Vertex3(1, -1, -1);
        KoGL.Vertex3(1, -1, 1);
        KoGL.Vertex3(-1, -1, 1);

        // rf (green)
        KoGL.Color4(0, 1, 0, 1);
        KoGL.Vertex3(1, -1, -1);
        KoGL.Vertex3(1, 1, -1);
        KoGL.Vertex3(1, 1, 1);
        KoGL.Vertex3(1, -1, 1);

        // lf (purple)
        KoGL.Color4(1, 0, 1, 1);
        KoGL.Vertex3(-1, -1, -1);
        KoGL.Vertex3(-1, -1, 1);
        KoGL.Vertex3(-1, 1, 1);
        KoGL.Vertex3(-1, 1, -1);

        KoGL.End();
    }
}
