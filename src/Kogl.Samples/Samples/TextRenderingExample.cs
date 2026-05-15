using System.Numerics;
using Kogl.Core;
using Kogl.FreeType;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class TextRenderingExample
{
    private static Font _uiFont = null!;
    private static Font _titleFont = null!;
    private static float _time;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - FreeType Text Rendering");

        app.OnLoad += () =>
        {
            _uiFont = Font.Load("assets/fonts/arial.ttf", 24);
            _titleFont = Font.Load("assets/fonts/arial.ttf", 48);
        };

        app.OnRender += RenderLoop;

        app.OnUnload += () =>
        {
            _uiFont?.Dispose();
            _titleFont?.Dispose();
        };

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        RenderApi.Clear(0.1f, 0.1f, 0.15f, 1.0f);
        RenderApi.DisableDepthTest(); // 2d text mode

        RenderApi.MatrixMode(MatrixMode.Projection);
        RenderApi.LoadIdentity();
        RenderApi.Ortho(0, 800, 600, 0, -1, 1);

        RenderApi.MatrixMode(MatrixMode.ModelView);
        RenderApi.LoadIdentity();

        // standard drawing using the default shader
        RenderApi.UseDefaultShader();

        // basic Text
        KoGLText.DrawText(
            _titleFont,
            "KoGL FreeType Pipeline",
            new Vector2(50, 50),
            new Vector4(1, 1, 1, 1)
        );

        // multiline & unicode support
        string paragraph =
            "High-Performance Text Batching\nUnicode Supported: áéíóú 漢字\nDynamic Texture Atlas Generation";
        KoGLText.DrawText(
            _uiFont,
            paragraph,
            new Vector2(50, 150),
            new Vector4(0.7f, 0.7f, 0.8f, 1.0f)
        );

        // transformation & alignment (floating animation)
        float bounceY = MathF.Sin(_time * 5f) * 10f;
        KoGLText.DrawText(
            _uiFont,
            "Center Aligned & Animated",
            new Vector2(400, 300 + bounceY),
            new Vector4(1, 0.5f, 0.2f, 1),
            TextAlignment.Center
        );

        RenderApi.Flush();
    }
}
