using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class Cube3DExample
{
    private static float _angle = 0f;

    private static readonly bool _useFrustum = true;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - 3D Rotating Cube");
        app.OnRender += RenderLoop;
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _angle += (float)dt * 45f;

        RenderApi.Clear(0.1f, 0.1f, 0.12f, 1.0f);

        // enable depth test
        RenderApi.EnableDepthTest();

        // projection matrix (perspective)
        RenderApi.MatrixMode(MatrixMode.Projection);
        RenderApi.LoadIdentity();

        if (_useFrustum)
        {
            float left = -0.05f;
            float right = 0.15f;
            float bottom = -0.05f;
            float top = 0.05f;

            RenderApi.Frustum(left, right, bottom, top, 0.1f, 100.0f);
        }
        else
        {
            RenderApi.Perspective(45.0f, RenderApi.GetAspectRatio(), 0.1f, 100.0f);
        }

        // modelView matrix (camera & object)
        RenderApi.MatrixMode(MatrixMode.ModelView);
        RenderApi.LoadIdentity();

        // move the camera back 5 units
        RenderApi.Translate(0, 0, -5);

        // rotate the cube on multiple axes
        RenderApi.Rotate(_angle, 1, 1, 0);

        RenderApi.UseDefaultShader();

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

        // final dispatch to GPU
        RenderApi.Flush();

        // disable depth test
        RenderApi.DisableDepthTest();
    }
}
