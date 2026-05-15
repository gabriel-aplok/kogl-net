using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class SimpleExample
{
    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Hello World");
        app.OnRender += RenderLoop;

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        RenderApi.MatrixMode(MatrixMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);

        RenderApi.MatrixMode(MatrixMode.ModelView);
        RenderApi.LoadIdentity();

        // draw a standard quad using the default shader
        RenderApi.UseDefaultShader();
        RenderApi.PushMatrix();
        RenderApi.Translate(200, 200, 0);

        RenderApi.Begin(PrimitiveMode.Quads);
        RenderApi.Color4(1, 0, 0, 1);
        RenderApi.Vertex2(0, 0);
        RenderApi.Color4(1, 1, 0, 1);
        RenderApi.Vertex2(200, 0);
        RenderApi.Color4(0, 1, 0, 1);
        RenderApi.Vertex2(200, 200);
        RenderApi.Color4(0, 0, 1, 1);
        RenderApi.Vertex2(0, 200);
        RenderApi.End();

        RenderApi.PopMatrix();

        // final dispatch to GPU
        RenderApi.Flush();
    }
}
