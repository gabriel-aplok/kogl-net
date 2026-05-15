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

        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        RenderApi.EnableDepthTest();

        // orbit the camera in a circle, constantly looking at the center (0,0,0)
        _camera.Position.X = MathF.Sin(_time) * 8f;
        _camera.Position.Z = MathF.Cos(_time) * 8f;
        _camera.LookAt(Vector3.Zero);

        // apply projection and view matrices
        RenderApi.BeginCamera(_camera);
        RenderApi.UseDefaultShader();

        // draw a reference grid (lines)
        RenderApi.Begin(PrimitiveMode.Lines);
        RenderApi.Color4(0.3f, 0.3f, 0.3f, 1.0f);
        for (int i = -5; i <= 5; i++)
        {
            RenderApi.Vertex3(i, 0, -5);
            RenderApi.Vertex3(i, 0, 5);
            RenderApi.Vertex3(-5, 0, i);
            RenderApi.Vertex3(5, 0, i);
        }
        RenderApi.End();

        // draw a central object (cube)
        RenderApi.PushMatrix();
        RenderApi.Translate(0, 0.5f, 0);
        DrawCube();
        RenderApi.PopMatrix();

        // flush and reset matrices back to Identity
        RenderApi.EndCamera();

        RenderApi.DisableDepthTest();
    }

    private static void DrawCube()
    {
        // draw the cube using 6 quads
        RenderApi.Begin(PrimitiveMode.Quads);

        // ff (red)
        RenderApi.Color4(1, 0, 0, 1);
        RenderApi.Vertex3(-1, -1, 1);
        RenderApi.Vertex3(1, -1, 1);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.Vertex3(-1, 1, 1);

        // bf (orange)
        RenderApi.Color4(1, 0.5f, 0, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.Vertex3(-1, 1, -1);
        RenderApi.Vertex3(1, 1, -1);
        RenderApi.Vertex3(1, -1, -1);

        // tf (blue)
        RenderApi.Color4(0, 0, 1, 1);
        RenderApi.Vertex3(-1, 1, -1);
        RenderApi.Vertex3(-1, 1, 1);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.Vertex3(1, 1, -1);

        // bf (yellow)
        RenderApi.Color4(1, 1, 0, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.Vertex3(1, -1, -1);
        RenderApi.Vertex3(1, -1, 1);
        RenderApi.Vertex3(-1, -1, 1);

        // rf (green)
        RenderApi.Color4(0, 1, 0, 1);
        RenderApi.Vertex3(1, -1, -1);
        RenderApi.Vertex3(1, 1, -1);
        RenderApi.Vertex3(1, 1, 1);
        RenderApi.Vertex3(1, -1, 1);

        // lf (purple)
        RenderApi.Color4(1, 0, 1, 1);
        RenderApi.Vertex3(-1, -1, -1);
        RenderApi.Vertex3(-1, -1, 1);
        RenderApi.Vertex3(-1, 1, 1);
        RenderApi.Vertex3(-1, 1, -1);

        RenderApi.End();
    }
}
