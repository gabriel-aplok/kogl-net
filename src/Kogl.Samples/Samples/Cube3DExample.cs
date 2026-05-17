using Kogl.Abstractions;
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

        KoGL.Clear(0.1f, 0.1f, 0.12f, 1.0f);

        // enable depth test
        KoGL.EnableDepthTest();

        // projection matrix (perspective)
        KoGL.MatrixMode(MatrixStackMode.Projection);
        KoGL.LoadIdentity();

        if (_useFrustum)
        {
            float left = -0.05f;
            float right = 0.15f;
            float bottom = -0.05f;
            float top = 0.05f;

            KoGL.Frustum(left, right, bottom, top, 0.1f, 100.0f);
        }
        else
        {
            KoGL.Perspective(45.0f, KoGL.GetAspectRatio(), 0.1f, 100.0f);
        }

        // modelView matrix (camera & object)
        KoGL.MatrixMode(MatrixStackMode.ModelView);
        KoGL.LoadIdentity();

        // move the camera back 5 units
        KoGL.Translate(0, -1, -10);

        // rotate the cube on multiple axes
        KoGL.Rotate(_angle, 1, 1, 0);

        KoGL.UseDefaultShader();

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

        // final dispatch to GPU
        KoGL.Flush();

        // disable depth test
        KoGL.DisableDepthTest();
    }
}
