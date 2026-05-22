using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class SimpleExample
{
    // private static Font _font = null!;

    public static void Start()
    {
        AppWindow app = new(800, 600, "Kolpa - Hello World");

        // app.OnLoad += () => _font = Font.Load("assets/fonts/arial.ttf", 24);
        app.OnRender += RenderLoop;
        // app.OnUnload += () => _font?.Dispose();

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, 800, 600, 0, -1, 1);

        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        // draw a standard quad using the default shader
        KoRender.UseDefaultShader();

        // text
        // KoGLText.DrawText(
        //     _font,
        //     "Close this window to see another example",
        //     new System.Numerics.Vector2(50, 50),
        //     new System.Numerics.Vector4(1, 1, 1, 1)
        // );

        KoRender.PushMatrix();
        KoRender.Translate(200, 200, 0);

        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 0, 0, 1);
        KoRender.Vertex2(0, 0);
        KoRender.Color4(1, 1, 0, 1);
        KoRender.Vertex2(200, 0);
        KoRender.Color4(0, 1, 0, 1);
        KoRender.Vertex2(200, 200);
        KoRender.Color4(0, 0, 1, 1);
        KoRender.Vertex2(0, 200);
        KoRender.End();

        KoRender.PopMatrix();

        // final dispatch to GPU
        KoRender.Flush();
    }
}
