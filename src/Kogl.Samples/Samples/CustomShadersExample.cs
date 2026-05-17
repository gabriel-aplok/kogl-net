using System.Numerics;
using Kogl.Abstractions;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class CustomShadersExample
{
    private static float _time = 0f;
    private static ShaderHandle _customShader;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Custom Shaders");
        app.OnRender += RenderLoop;

        // wait for initialize to complete before creating shader
        // TODO: hook an OnLoad event. I will load it on frame 1 here to test lol

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        if (_customShader.Id == 0)
        {
            string vs =
                @"#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

out vec2 fTex;
out vec4 fCol;

uniform mat4 uMVP;

void main() {
    gl_Position = uMVP * vec4(aPos, 1.0);
    fTex = aTex;
    fCol = aCol;
}";

            string fs =
                @"#version 330 core
in vec2 fTex;
in vec4 fCol;
out vec4 FragColor;

uniform float uTime;

void main() {
    // Procedural animation that doesn't rely on texture details
    float wave = sin(fTex.x * 10.0 + uTime * 3.0) * 0.5 + 0.5;
    vec3 colorA = vec3(0.1, 0.5, 0.8); // Blue
    vec3 colorB = vec3(0.8, 0.2, 0.1); // Red

    vec3 finalRGB = mix(colorA, colorB, wave);
    FragColor = vec4(finalRGB, 1.0) * fCol;
}";

            _customShader = KoGL.CreateShader(vs, fs);
        }

        KoGL.Clear(0.1f, 0.1f, 0.15f, 1.0f);

        KoGL.MatrixMode(MatrixStackMode.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, 800, 600, 0, -1, 1);

        KoGL.MatrixMode(MatrixStackMode.ModelView);
        KoGL.LoadIdentity();

        // draw a standard quad using the default shader
        KoGL.UseDefaultShader();
        KoGL.PushMatrix();
        KoGL.Translate(150, 200, 0);

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

        // draw an animated quad using the custom shader
        KoGL.UseShader(_customShader);

        // setting a uniform automatically flushes the batcher
        KoGL.SetUniform("uTime", _time);
        KoGL.SetUniform("uTint", new Vector3(0.5f, 0.8f, 1.0f));

        KoGL.PushMatrix();
        KoGL.Translate(450, 200, 0);

        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(0, 0);
        KoGL.Vertex2(0, 0);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(1, 0);
        KoGL.Vertex2(200, 0);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(1, 1);
        KoGL.Vertex2(200, 200);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(0, 1);
        KoGL.Vertex2(0, 200);
        KoGL.End();

        KoGL.PopMatrix();

        // final dispatch to GPU
        KoGL.Flush();
    }
}
