using System.Numerics;
using Kogl.Abstractions.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Input;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class RaymarchedPrimitivesExample
{
    private static float _time = 0f;
    private static ShaderHandle _primitivesShader;
    private static Vector2 _mousePos = Vector2.Zero;

    private static int _width = 800;
    private static int _height = 600;

    public static void Start()
    {
        AppWindow app = new(_width, _height, "KoGL - Transparent Raymarched Primitives");
        app.OnLoad += () =>
        {
            InputMap.Bind("ToggleMouse", Key.Escape);
            InputManager.CursorMode = CursorMode.Locked;
        };
        app.OnRender += RenderLoop;
        app.OnResizeEvent += (width, height) =>
        {
            _width = width;
            _height = height;
        };
        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;
        _mousePos = InputManager.MousePosition;

        if (InputMap.IsActionPressed("ToggleMouse"))
        {
            InputManager.CursorMode =
                InputManager.CursorMode == CursorMode.Locked
                    ? CursorMode.Normal
                    : CursorMode.Locked;
        }

        if (_primitivesShader.Id == 0)
        {
            string vs =
                @"#version 330 core
                layout(location=0) in vec3 aPos;
                layout(location=1) in vec2 aTex;
                layout(location=2) in vec4 aCol;
                out vec2 fTex;
                out vec4 fCol;
                uniform mat4 uMVP;
                void main() {
                    gl_Position = uMVP * vec4(aPos, 1.0);
                    fTex = aTex;
                    fCol = aCol;
                }";

            string fsPrimitives =
                @"#version 330 core

in vec2 fTex;
out vec4 FragColor;

uniform float uTime;
uniform vec2 uResolution;
uniform vec2 uMouse;

#define LESSON 1
#define MARCHSTEPS 60
#define MAX_DIST 10.0
#define PI 3.14159265359

// Generates a dynamic, scrolling rainbow spectrum mapped to a 3D ray direction
vec3 SampleProceduralBackground(vec3 rd) {
    // Standard spherical coordinates projection
    float phi = atan(rd.z, rd.x);
    float theta = acos(rd.y);
    vec2 uv = vec2(phi / (2.0 * PI) + 0.5, theta / PI);

    // Dynamic wave functions to twist and animate the rainbow space
    float wave1 = sin(uv.x * 4.0 + uTime * 0.5) * 0.5 + 0.5;
    float wave2 = cos(uv.y * 3.0 - uTime * 0.3) * 0.5 + 0.5;
    float dynamicFactor = uv.x + wave1 * 0.3 + wave2 * 0.2;

    // Fast cosine spectrum generator (Inigo Quilez method)
    vec3 colorA = vec3(0.5, 0.5, 0.5);
    vec3 colorB = vec3(0.5, 0.5, 0.5);
    vec3 frequency = vec3(1.0, 1.0, 1.0);
    vec3 phaseOffset = vec3(0.0, 0.33, 0.67); // Splits RGB into clean rainbow cycles

    vec3 rainbow = colorA + colorB * cos(2.0 * PI * (frequency * dynamicFactor * 2.0 + phaseOffset));

    // Vignette/darken the bottom slightly to give the panorama structural depth
    return rainbow * smoothstep(-0.2, 0.8, rd.y + 0.5);
}

float maxcomp(in vec3 p) {
    return max(p.x, max(p.y, p.z));
}

float sdSphere(vec3 p, float r) {
    return length(p) - r;
}

float sdBox(vec3 p, vec3 b, float r) {
    vec3 d = abs(p) - b;
    return min(maxcomp(d), 0.0) - r + length(max(d, 0.0));
}

float DrawScene(vec3 p) {
    float d = MAX_DIST;
    #if LESSON == 1
        d = sdSphere(p, 0.65);
    #elif LESSON == 2
        d = sdBox(p, vec3(0.5, 0.5, 0.5), 0.05);
    #endif
    return d;
}

vec3 GetNormal(vec3 p) {
    vec2 e = vec2(0.001, 0.0);
    return normalize(vec3(
        DrawScene(p + e.xyy) - DrawScene(p - e.xyy),
        DrawScene(p + e.yxy) - DrawScene(p - e.yxy),
        DrawScene(p + e.yyx) - DrawScene(p - e.yyx)
    ));
}

void main()
{
    vec2 uv = (-1.0 + 2.0 * fTex) * vec2(uResolution.x / uResolution.y, 1.0);

    float angle = uTime * 0.15 + (uMouse.x / uResolution.x) * PI * 2.0;
    vec3 ro = vec3(2.2 * sin(angle), 0.5, 2.2 * cos(angle));
    vec3 ta = vec3(0.0, 0.0, 0.0);

    vec3 cw = normalize(ta - ro);
    vec3 cp = vec3(0.0, 1.0, 0.0);
    vec3 cu = normalize(cross(cw, cp));
    vec3 cv = normalize(cross(cu, cw));
    vec3 rd = normalize(uv.x * cu + uv.y * cv + 1.6 * cw);

    float t = 0.0;
    bool hit = false;

    for(int i = 0; i < MARCHSTEPS; i++) {
        vec3 p = ro + rd * t;
        float d = DrawScene(p);
        if(d < 0.001) {
            hit = true;
            break;
        }
        t += d;
        if(t > MAX_DIST) break;
    }

    // Default backdrop color configuration
    vec3 col = SampleProceduralBackground(rd);

    if(hit) {
        vec3 p = ro + rd * t;
        vec3 n = GetNormal(p);

        // --- 1. REFLECTION ---
        vec3 refVec = reflect(rd, n);
        vec3 reflectionColor = SampleProceduralBackground(refVec);

        // --- 2. REFRACTION (TRANSPARENCY) ---
        // Glass Index of Refraction (IoR) is roughly 1.5. Air to Glass ratio is 1.0 / 1.5 = 0.66
        float iorRatio = 0.66;
        vec3 refrVec = refract(rd, n, iorRatio);

        // Secondary trace inside the glass block to sample where it exits back out
        vec3 exitColor = SampleProceduralBackground(refrVec);

        // Optional: Add a slight internal tint to the glass body (crystalline amber/teal cast)
        vec3 glassTint = vec3(0.9, 0.95, 1.0);
        vec3 refractionColor = exitColor * glassTint;

        // --- 3. FRESNEL COMPOSITION ---
        // Calculates how reflective vs transparent the glass pixel should be based on incident angle
        float fresnel = pow(clamp(1.0 + dot(rd, n), 0.0, 1.0), 5.0);

        // Combine refraction and specular reflection together via the Fresnel factor
        col = mix(refractionColor, reflectionColor, 0.2 + fresnel * 0.8);

        // --- 4. SPECULAR HIGHLIGHT ---
        vec3 lightDir = normalize(vec3(1.5, 3.0, 1.0));
        float spec = pow(clamp(dot(refVec, lightDir), 0.0, 1.0), 32.0);
        col += vec3(1.0) * spec * 0.6;
    }

    // Gamma correction pipeline pass
    col = pow(col, vec3(0.4545));
    FragColor = vec4(col, 1.0);
}";

            _primitivesShader = KoGL.CreateShader(vs, fsPrimitives);
        }

        KoGL.SetRenderTarget(null);
        KoGL.Clear(0.0f, 0.0f, 0.0f, 1.0f);

        KoGL.MatrixMode(MatrixState.Projection);
        KoGL.LoadIdentity();
        KoGL.Ortho(0, _width, _height, 0, -1, 1);

        KoGL.MatrixMode(MatrixState.ModelView);
        KoGL.LoadIdentity();

        KoGL.UseShader(_primitivesShader);
        KoGL.UseDefaultTexture();

        KoGL.SetUniform("uTime", _time);
        KoGL.SetUniform("uResolution", new Vector2(_width, _height));
        KoGL.SetUniform("uMouse", _mousePos);

        KoGL.PushMatrix();
        KoGL.Begin(PrimitiveMode.Quads);
        KoGL.Color4(1, 1, 1, 1);
        KoGL.TexCoord2(0, 1);
        KoGL.Vertex2(0, 0);
        KoGL.TexCoord2(1, 1);
        KoGL.Vertex2(_width, 0);
        KoGL.TexCoord2(1, 0);
        KoGL.Vertex2(_width, _height);
        KoGL.TexCoord2(0, 0);
        KoGL.Vertex2(0, _height);
        KoGL.End();
        KoGL.PopMatrix();

        KoGL.Flush();
    }
}
