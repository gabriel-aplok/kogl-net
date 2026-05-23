using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class CustomShadersExample
{
    private static float _time = 0f;
    private static Shader _customShader = null!;

    public static void Start()
    {
        AppWindow app = new(800, 600, "Kolpa - Custom Shaders And Textures");

        // Hook the OnLoad event properly so we don't have to load on frame 1 anymore lol
        app.OnLoad += static () =>
        {
            _customShader = AssetManager.Load<Shader>("res://shaders/custom_wave.glsl");
            // _customShader = ResourceManager.Load<Shader>("assets/shaders/custom_wave.glsl");
        };

        app.OnRender += RenderLoop;

        app.OnUnload += static () =>
        {
            AssetManager.Unload("res://shaders/custom_wave.glsl");
            // ResourceManager.Unload("assets/shaders/custom_wave.glsl");
        };

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        KoRender.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, 800, 600, 0, -1, 1);

        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        // draw a standard quad using the default shader
        KoRender.UseDefaultShader();
        KoRender.PushMatrix();
        KoRender.Translate(150, 200, 0);

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

        // draw an animated quad using the custom shader loaded from the asset pipeline
        if (_customShader != null && _customShader.Handle.Id != 0)
        {
            KoRender.UseShader(_customShader);

            // setting a uniform automatically flushes the batcher
            KoRender.SetUniform("uTime", _time);
            KoRender.SetUniform("uTint", new Vector3(0.5f, 0.8f, 1.0f));

            KoRender.PushMatrix();
            KoRender.Translate(450, 200, 0);

            KoRender.Begin(PrimitiveMode.Quads);
            KoRender.Color4(1, 1, 1, 1);
            KoRender.TexCoord2(0, 0);
            KoRender.Vertex2(0, 0);
            KoRender.Color4(1, 1, 1, 1);
            KoRender.TexCoord2(1, 0);
            KoRender.Vertex2(200, 0);
            KoRender.Color4(1, 1, 1, 1);
            KoRender.TexCoord2(1, 1);
            KoRender.Vertex2(200, 200);
            KoRender.Color4(1, 1, 1, 1);
            KoRender.TexCoord2(0, 1);
            KoRender.Vertex2(0, 200);
            KoRender.End();

            KoRender.PopMatrix();
        }

        // final dispatch to GPU
        KoRender.Flush();
    }
}
