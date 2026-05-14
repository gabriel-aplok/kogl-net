using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Windowing;

namespace Kogl.Samples;

internal class Program
{
    private static float _rotation = 0f;

    private static void Main()
    {
        AppWindow app = new(800, 600, "KOGL Sample");
        app.OnRender += RenderLoop;
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        RenderApi.Clear(0.1f, 0.1f, 0.1f, 1.0f);

        // setup projection
        RenderApi.MatrixMode(MatrixMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);

        // setup modelview
        RenderApi.MatrixMode(MatrixMode.ModelView);
        RenderApi.LoadIdentity();

        // push matrix to isolate rotation
        RenderApi.PushMatrix();
        RenderApi.Translate(400, 300, 0); // center
        RenderApi.Rotate(_rotation, 0, 0, 1); // spin
        RenderApi.Translate(-100, -100, 0); // offset for drawing

        // draw a quad
        RenderApi.Begin(PrimitiveMode.Quads);

        RenderApi.Color4(1f, 0f, 0f, 1f);
        RenderApi.TexCoord2(0f, 0f);
        RenderApi.Vertex2(0, 0);

        RenderApi.Color4(0f, 1f, 0f, 1f);
        RenderApi.TexCoord2(1f, 0f);
        RenderApi.Vertex2(200, 0);

        RenderApi.Color4(0f, 0f, 1f, 1f);
        RenderApi.TexCoord2(1f, 1f);
        RenderApi.Vertex2(200, 200);

        RenderApi.Color4(1f, 1f, 0f, 1f);
        RenderApi.TexCoord2(0f, 1f);
        RenderApi.Vertex2(0, 200);

        RenderApi.End();

        RenderApi.PopMatrix();

        // flush all batched vertices to the GPU backend
        RenderApi.Flush();

        _rotation += (float)(100 * dt);
    }
}
