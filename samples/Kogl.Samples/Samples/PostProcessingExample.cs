using System.Numerics;
using Kogl.Abstractions.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class PostProcessingExample
{
    private static float _time = 0f;
    private static ShaderHandle _sceneShader;
    private static ShaderHandle _postProcessShader;
    private static RenderTarget _renderTarget;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL - Post-Processing");
        app.OnRender += RenderLoop;

        // wait for initialize to complete before creating shader
        // TODO: hook an OnLoad event. I will load it on frame 1 here to test lol

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        if (_sceneShader.Id == 0)
        {
            // create the render target (match window size for 1:1 pixel mapping)
            _renderTarget = KoGL.CreateRenderTarget(800, 600);

            // scene shader
            string vs =
                "#version 330 core\nlayout(location=0) in vec3 aPos; layout(location=1) in vec2 aTex; layout(location=2) in vec4 aCol; out vec2 fTex; out vec4 fCol; uniform mat4 uMVP; void main() { gl_Position = uMVP * vec4(aPos, 1.0); fTex = aTex; fCol = aCol; }";
            string fsScene =
                "#version 330 core\nin vec2 fTex; in vec4 fCol; out vec4 FragColor; uniform float uTime; void main() { float wave = sin(fTex.x * 10.0 + uTime * 3.0) * 0.5 + 0.5; vec3 colorA = vec3(0.1, 0.5, 0.8); vec3 colorB = vec3(0.8, 0.2, 0.1); FragColor = vec4(mix(colorA, colorB, wave), 1.0) * fCol; }";
            _sceneShader = KoGL.CreateShader(vs, fsScene);

            // post-process shader (crt / vignette effect)
            string fsPost =
                @"#version 330 core
            in vec2 fTex;
            out vec4 FragColor;
            uniform sampler2D uTex;
            uniform float uTime;

            void main() {
                // Screen curvature
                vec2 crtUv = fTex * 2.0 - 1.0;
                vec2 offset = crtUv.yx / 5.0;
                crtUv = crtUv + crtUv * offset * offset;
                crtUv = crtUv * 0.5 + 0.5;

                // Sample the FBO texture
                vec4 texColor = texture(uTex, crtUv);

                // Scanlines
                float scanline = sin(crtUv.y * 800.0) * 0.04;
                texColor.rgb -= scanline;

                // Vignette (Darken edges)
                float vignette = length(fTex - 0.5) * 1.5;
                texColor.rgb -= vignette;

                // Black out borders caused by curvature curve
                if (crtUv.x < 0.0 || crtUv.x > 1.0 || crtUv.y < 0.0 || crtUv.y > 1.0) {
                    texColor = vec4(0.0, 0.0, 0.0, 1.0);
                }

                FragColor = texColor;
            }";
            _postProcessShader = KoGL.CreateShader(vs, fsPost);
        }

        // ==========================================
        // draw the fbo
        // ==========================================
        KoGL.SetRenderTarget(_renderTarget);
        KoGL.Clear(0.1f, 0.1f, 0.15f, 1.0f); // clear the render target

        KoGL.MatrixMode(MatrixState.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, 800, 600, 0, -1, 1);

        KoGL.MatrixMode(MatrixState.ModelView);
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
        KoGL.UseShader(_sceneShader);

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

        // ==========================================
        // draw the fbo to the screen
        // ==========================================
        KoGL.SetRenderTarget(null); // unbind fbo, go back to screen
        KoGL.Clear(0.0f, 0.0f, 0.0f, 1.0f); // clear the screen

        // setup a basic orthographic camera for a fullscreen quad
        KoGL.MatrixMode(MatrixState.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, 800, 600, 0, -1, 1);
        KoGL.MatrixMode(MatrixState.ModelView);
        KoGL.LoadIdentity();

        // bind the post-process shader and feed it our renderTarget texture
        KoGL.UseShader(_postProcessShader);
        KoGL.SetUniform("uTime", _time);
        KoGL.UseTexture(_renderTarget.Texture);

        // draw a giant quad covering the entire screen
        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(0, 1);
        KoGL.Vertex2(0, 0); // notice Y coords are flipped
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(1, 1);
        KoGL.Vertex2(800, 0); // opengl textures load bottom-up
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(1, 0);
        KoGL.Vertex2(800, 600);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(0, 0);
        KoGL.Vertex2(0, 600);
        KoGL.End();

        // final dispatch to GPU
        KoGL.Flush();
    }
}
