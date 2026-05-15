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
        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        RenderApi.MatrixMode(MatrixMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);

        RenderApi.MatrixMode(MatrixMode.ModelView);
        RenderApi.LoadIdentity();

        // draw a standard quad using the default shader
        RenderApi.UseDefaultShader();
        RenderApi.UseDefaultTexture();

        RenderApi.PushMatrix();
        RenderApi.Translate(100, 100, 0);

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

        // draw the logo
        if (_logo != null)
        {
            RenderApi.UseTexture(_logo.Handle);

            RenderApi.PushMatrix();
            RenderApi.Translate(400, 100, 0);

            RenderApi.Begin(PrimitiveMode.Quads);
            RenderApi.Color4(1, 1, 1, 1);
            RenderApi.TexCoord2(0, 0);
            RenderApi.Vertex2(0, 0);
            RenderApi.TexCoord2(1, 0);
            RenderApi.Vertex2(200, 0);
            RenderApi.TexCoord2(1, 1);
            RenderApi.Vertex2(200, 200);
            RenderApi.TexCoord2(0, 1);
            RenderApi.Vertex2(0, 200);
            RenderApi.End();

            RenderApi.PopMatrix();
        }

        // final dispatch to GPU
        RenderApi.Flush();
    }
}
