using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class SimpleExample
{
    // private static Font _font = null!;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Hello World");

        // app.OnLoad += () => _font = Font.Load("assets/fonts/arial.ttf", 24);
        app.OnRender += RenderLoop;
        // app.OnUnload += () => _font?.Dispose();

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        RenderApi.MatrixMode(MatrixStackMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);

        RenderApi.MatrixMode(MatrixStackMode.ModelView);
        RenderApi.LoadIdentity();

        // draw a standard quad using the default shader
        RenderApi.UseDefaultShader();

        // text
        // KoGLText.DrawText(
        //     _font,
        //     "Close this window to see another example",
        //     new System.Numerics.Vector2(50, 50),
        //     new System.Numerics.Vector4(1, 1, 1, 1)
        // );

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
