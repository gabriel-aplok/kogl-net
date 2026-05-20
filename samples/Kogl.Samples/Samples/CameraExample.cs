using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Graphics;
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

        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        KoRender.EnableDepthTest();

        // orbit the camera in a circle, constantly looking at the center (0,0,0)
        _camera.Position.X = MathF.Sin(_time) * 8f;
        _camera.Position.Z = MathF.Cos(_time) * 8f;
        _camera.LookAt(Vector3.Zero);

        // apply projection and view matrices
        KoRender.BeginCamera(_camera);
        KoRender.UseDefaultShader();

        // draw a reference grid (lines)
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

        // draw a central object (cube)
        KoRender.PushMatrix();
        KoRender.Translate(0, 0.5f, 0);
        DrawCube();
        KoRender.PopMatrix();

        // flush and reset matrices back to Identity
        KoRender.EndCamera();

        KoRender.DisableDepthTest();
    }

    private static void DrawCube()
    {
        // draw the cube using 6 quads
        KoRender.Begin(PrimitiveMode.Quads);

        // ff (red)
        KoRender.Color4(1, 0, 0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.Vertex3(-1, 1, 1);

        // bf (orange)
        KoRender.Color4(1, 0.5f, 0, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.Vertex3(1, -1, -1);

        // tf (blue)
        KoRender.Color4(0, 0, 1, 1);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.Vertex3(1, 1, -1);

        // bf (yellow)
        KoRender.Color4(1, 1, 0, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.Vertex3(-1, -1, 1);

        // rf (green)
        KoRender.Color4(0, 1, 0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.Vertex3(1, -1, 1);

        // lf (purple)
        KoRender.Color4(1, 0, 1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.Vertex3(-1, 1, -1);

        KoRender.End();
    }
}
