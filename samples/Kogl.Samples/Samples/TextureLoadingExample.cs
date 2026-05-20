using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class TextureLoadingExample
{
    private static Texture? _logo;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Texture Loading");

        app.OnLoad += static () =>
        {
            // _logo = ResourceManager.Load<Texture>("assets/container.jpg");
            _logo = ResourceManager.Load<Texture>("assets/logo.png");
        };
        app.OnRender += RenderLoop;
        app.OnUnload += static () =>
        {
            ResourceManager.UnloadAll();
        };

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
        KoRender.UseDefaultTexture();

        KoRender.PushMatrix();
        KoRender.Translate(100, 100, 0);

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

        // draw the logo
        if (_logo != null)
        {
            KoRender.UseTexture(_logo.Handle);

            KoRender.PushMatrix();
            KoRender.Translate(400, 100, 0);

            KoRender.Begin(PrimitiveMode.Quads);
            KoRender.Color4(1, 1, 1, 1);

            // Top-Left corner: Map to U=0, V=1
            KoRender.TexCoord2(0, 1);
            KoRender.Vertex2(0, 0);

            // Top-Right corner: Map to U=1, V=1
            KoRender.TexCoord2(1, 1);
            KoRender.Vertex2(200, 0);

            // Bottom-Right corner: Map to U=1, V=0
            KoRender.TexCoord2(1, 0);
            KoRender.Vertex2(200, 200);

            // Bottom-Left corner: Map to U=0, V=0
            KoRender.TexCoord2(0, 0);
            KoRender.Vertex2(0, 200);

            KoRender.End();

            KoRender.PopMatrix();
        }

        // final dispatch to GPU
        KoRender.Flush();
    }
}
