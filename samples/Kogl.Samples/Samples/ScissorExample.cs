using Kogl.Abstractions;
using Kogl.Abstractions.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class ScissorExample
{
    private static float _rotation = 0f;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Scissor Test");
        app.OnRender += RenderLoop;

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _rotation += (float)dt * 50f;

        // clear the whole screen
        KoGL.Clear(0.05f, 0.05f, 0.08f, 1.0f);

        KoGL.MatrixMode(MatrixStackMode.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, 800, 600, 0, -1, 1);

        KoGL.MatrixMode(MatrixStackMode.ModelView);
        KoGL.LoadIdentity();

        // draw a background "cntainer" to show where the scissor box is
        KoGL.UseDefaultShader();
        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(0.15f, 0.15f, 0.2f, 1.0f); // lighter gray-blue
        KoGL.Vertex2(200, 150);
        KoGL.Vertex2(600, 150);
        KoGL.Vertex2(600, 450);
        KoGL.Vertex2(200, 450);
        KoGL.End();

        // begin scissor
        // clip everything to the bounds of the container we just drew
        KoGL.BeginScissor(200, 150, 400, 300);

        // draw a large rotating quad that would normally overflow the container
        KoGL.PushMatrix();
        KoGL.Translate(400, 300, 0); // move to center of container
        KoGL.Rotate(_rotation, 0, 0, 1);
        KoGL.Translate(-150, -150, 0); // center the quad on its own axis

        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(1, 0, 0, 1); // red
        KoGL.Vertex2(0, 0);

        KoGL.Color4(0, 1, 0, 1); // green
        KoGL.Vertex2(300, 0);

        KoGL.Color4(0, 0, 1, 1); // blue
        KoGL.Vertex2(300, 300);

        KoGL.Color4(1, 1, 0, 1); // yellow
        KoGL.Vertex2(0, 300);
        KoGL.End();
        KoGL.PopMatrix();

        // end scissor
        KoGL.EndScissor();

        // draw something outside the scissor (to prove the test is off)
        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(1, 1, 1, 0.5f); // semi-transparent white hint
        KoGL.Vertex2(10, 10);
        KoGL.Vertex2(50, 10);
        KoGL.Vertex2(50, 50);
        KoGL.Vertex2(10, 50);
        KoGL.End();

        // final dispatch to GPU
        KoGL.Flush();
    }
}
