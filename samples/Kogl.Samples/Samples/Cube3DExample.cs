using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class Cube3DExample
{
    private static float _angle = 0f;

    private static readonly bool _useFrustum = false;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - 3D Rotating Cube");
        app.OnRender += RenderLoop;
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _angle += (float)dt * 45f;

        KoRender.Clear(0.1f, 0.1f, 0.12f, 1.0f);

        // enable depth test
        KoRender.EnableDepthTest();

        // projection matrix (perspective)
        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();

        if (_useFrustum)
        {
            float left = -0.05f;
            float right = 0.15f;
            float bottom = -0.05f;
            float top = 0.05f;

            KoRender.Frustum(left, right, bottom, top, 0.1f, 100.0f);
        }
        else
        {
            KoRender.Perspective(45.0f, KoRender.GetAspectRatio(), 0.1f, 100.0f);
        }

        // modelView matrix (camera & object)
        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        // move the camera back 5 units
        KoRender.Translate(0, -1, -10);

        // rotate the cube on multiple axes
        KoRender.Rotate(_angle, 1, 1, 0);

        KoRender.UseDefaultShader();

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

        // final dispatch to GPU
        KoRender.Flush();

        // disable depth test
        KoRender.DisableDepthTest();
    }
}
