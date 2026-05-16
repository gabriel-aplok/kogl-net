using Kogl.Abstractions;
using Kogl.Core;
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
            _logo = ResourceManager.Load<Texture>("assets/container.jpg");
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
        KoGL.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        KoGL.MatrixMode(MatrixStackMode.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, 800, 600, 0, -1, 1);

        KoGL.MatrixMode(MatrixStackMode.ModelView);
        KoGL.LoadIdentity();

        // draw a standard quad using the default shader
        KoGL.UseDefaultShader();
        KoGL.UseDefaultTexture();

        KoGL.PushMatrix();
        KoGL.Translate(100, 100, 0);

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

        // draw the logo
        if (_logo != null)
        {
            KoGL.UseTexture(_logo.Handle);

            KoGL.PushMatrix();
            KoGL.Translate(400, 100, 0);

            KoGL.Begin(PrimitiveMode.Quads);
            KoGL.Color4(1, 1, 1, 1);
            KoGL.TexCoord2(0, 0);
            KoGL.Vertex2(0, 0);
            KoGL.TexCoord2(1, 0);
            KoGL.Vertex2(200, 0);
            KoGL.TexCoord2(1, 1);
            KoGL.Vertex2(200, 200);
            KoGL.TexCoord2(0, 1);
            KoGL.Vertex2(0, 200);
            KoGL.End();

            KoGL.PopMatrix();
        }

        // final dispatch to GPU
        KoGL.Flush();
    }
}
