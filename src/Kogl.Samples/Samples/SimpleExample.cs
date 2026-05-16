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
        KoGL.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        KoGL.MatrixMode(MatrixStackMode.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, 800, 600, 0, -1, 1);

        KoGL.MatrixMode(MatrixStackMode.ModelView);
        KoGL.LoadIdentity();

        // draw a standard quad using the default shader
        KoGL.UseDefaultShader();

        // text
        // KoGLText.DrawText(
        //     _font,
        //     "Close this window to see another example",
        //     new System.Numerics.Vector2(50, 50),
        //     new System.Numerics.Vector4(1, 1, 1, 1)
        // );

        KoGL.PushMatrix();
        KoGL.Translate(200, 200, 0);

        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(1, 0, 0, 1);
        KoGL.Vertex2(0, 0);
        KoGL.Color4(1, 1, 0, 1);
        KoGL.Vertex2(200, 0);
        KoGL.Color4(0, 1, 0, 1);
        KoGL.Vertex2(200, 200);
        KoGL.Color4(0, 0, 1, 1);
        KoGL.Vertex2(0, 200);
        KoGL.End();

        KoGL.PopMatrix();

        // final dispatch to GPU
        KoGL.Flush();
    }
}
