using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class CustomShaders2Example
{
    private static float _time = 0f;
    private static Shader _customShader = null!;

    public static void Start()
    {
        AppWindow app = new(800, 600, "Kolpa - Custom 2 Shaders");

        app.OnLoad += static () =>
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

            _customShader = Shader.Create(vs, fs);
        };
        app.OnRender += RenderLoop;

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

        // draw an animated quad using the custom shader
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

        // final dispatch to GPU
        KoRender.Flush();
    }
}
