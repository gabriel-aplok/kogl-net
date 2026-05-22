using System.Numerics;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class RaymarchedHillsExample
{
    private static Shader _raymarchShader = null!;
    private static float _time = 0f;

    private static Vector2 _mousePos = Vector2.Zero;

    private static int _width = 800;
    private static int _height = 600;

    public static void Start()
    {
        AppWindow app = new(_width, _height, "Kolpa - Raymarched Hills Example");
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

        if (_raymarchShader.Handle.Id == 0)
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

            // Adpted from https://www.shadertoy.com/view/Xsf3zX
            string fsRaymarch =
                @"#version 330 core

in vec2 fTex;
out vec4 FragColor;

uniform float uTime;
uniform vec2 uResolution;
uniform vec2 uMouse;

#define THRESHOLD .003
#define MOD2 vec2(3.07965, 7.4235)

float PI = 4.0 * atan(1.0);
vec3 sunLight = normalize(vec3(0.35, 0.2, 0.3));
vec3 cameraPos;
vec3 sunColour = vec3(1.0, .75, .6);
const mat2 rotate2D = mat2(1.932, 1.623, -1.623, 1.952);

float Hash(float p) {
    vec2 p2 = fract(vec2(p) / MOD2);
    p2 += dot(p2.yx, p2.xy + 19.19);
    return fract(p2.x * p2.y);
}

float Hash(vec2 p) {
    p = fract(p / MOD2);
    p += dot(p.xy, p.yx + 19.19);
    return fract(p.x * p.y);
}

float Noise(in vec2 x) {
    vec2 p = floor(x);
    vec2 f = fract(x);
    f = f * f * (3.0 - 2.0 * f);
    float n = p.x + p.y * 57.0;
    return mix(mix(Hash(vec2(n)), Hash(vec2(n + 1.0)), f.x),
               mix(Hash(vec2(n + 57.0)), Hash(vec2(n + 58.0)), f.x), f.y);
}

vec2 hash22(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * vec3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.xx + p3.yz) * p3.zy);
}

vec2 Voronoi(in vec2 x)
{
    vec2 p = floor(x);
    vec2 f = fract(x);
    float res = 100.0;
    vec2 id;
    for(int j = -1; j <= 1; j++)
    for(int i = -1; i <= 1; i++)
    {
        vec2 b = vec2(float(i), float(j));
        vec2 r = b - f + hash22(p + b);
        float d = dot(r, r);
        if(d < res)
        {
            res = d;
            id.x = Hash(p + b);
        }
    }
    return vec2(max(.4 - sqrt(res), 0.0), id.x);
}

vec2 Terrain(in vec2 p) {
    vec2 pos = p * 0.002;
    float h = 0.0;
    float amp = 30.0;
    for (int i = 0; i < 5; i++) {
        h += Noise(pos) * amp;
        pos *= 2.1;
        amp *= 0.5;
    }
    return vec2(h, 0.0);
}

vec2 Map(in vec3 p) {
    vec2 h = Terrain(p.xz);
    return vec2(p.y - h.x, h.y);
}

float FractalNoise(in vec2 xy)
{
    float w = .7;
    float f = 0.0;
    for (int i = 0; i < 3; i++)
    {
        f += Noise(xy) * w;
        w = w * 0.6;
        xy = 2.0 * xy;
    }
    return f;
}

vec3 GetSky(vec3 rd) {
    float sun = clamp(dot(rd, normalize(vec3(0.5, 0.3, 0.5))), 0.0, 1.0);
    vec3 sky = mix(vec3(0.3, 0.5, 0.8), vec3(0.1, 0.1, 0.2), rd.y);
    sky += vec3(1.0, 0.9, 0.7) * pow(sun, 50.0); // Sun disc
    return sky;
}

vec3 ApplyFog(in vec3 rgb, in float dis, in vec3 dir)
{
    float fogAmount = clamp(dis * dis * 0.0000012, 0.0, 1.0);
    return mix(rgb, GetSky(dir), fogAmount);
}

vec3 DE(vec3 p)
{
    float base = Terrain(p.xz).x - 1.9;
    float height = Noise(p.xz * 2.0) * .75 + Noise(p.xz) * .35 + Noise(p.xz * .5) * .2;
    float y = p.y - base - height;
    y = y * y;
    vec2 ret = Voronoi((p.xz * 2.5 + sin(y * 2.0 + p.zx * 12.3) * .12 + vec2(sin(uTime * 1.3 + 1.5 * p.z), sin(uTime * 2.6 + 1.5 * p.x)) * y * .5));
    float f = ret.x * .65 + y * .5;
    return vec3(y - f * 1.4, clamp(f * 1.1, 0.0, 1.0), ret.y);
}

float CircleOfConfusion(float t)
{
    return max(t * .04, (2.0 / uResolution.y) * (1.0 + t));
}

float Linstep(float a, float b, float t)
{
    return clamp((t - a) / (b - a), 0., 1.);
}

vec3 GrassBlades(in vec3 rO, in vec3 rD, in vec3 mat, in float dist)
{
    float d = 0.0;
    float rCoC = CircleOfConfusion(dist * .3);
    float alpha = 0.0;
    vec4 col = vec4(mat * 0.15, 0.0);

    for (int i = 0; i < 15; i++)
    {
        if (col.w > .99) break;
        vec3 p = rO + rD * d;
        vec3 ret = DE(p);
        ret.x += .5 * rCoC;

        if (ret.x < rCoC)
        {
            alpha = (1.0 - col.y) * Linstep(-rCoC, rCoC, -ret.x);
            vec3 gra = mix(mat, vec3(.35, .35, min(pow(ret.z, 4.0) * 35.0, .35)), pow(ret.y, 9.0) * .7) * ret.y;
            col += vec4(gra * alpha, alpha);
        }
        d += max(ret.x * .7, .1);
    }
    if(col.w < .2)
        col.xyz = vec3(0.1, .15, 0.05);
    return col.xyz;
}

void DoLighting(inout vec3 mat, in vec3 pos, in vec3 normal, in vec3 eyeDir, in float dis)
{
    float h = dot(sunLight, normal);
    mat = mat * sunColour * (max(h, 0.0) + .2);
}

vec3 TerrainColour(vec3 pos, vec3 dir, vec3 normal, float dis, float type)
{
    vec3 mat = vec3(0.0);

    if (type == 0.0)
    {
        // Base color layer
        mat = mix(vec3(.0, .3, .0), vec3(.2, .3, .0), Noise(pos.xz * .025));

        // Add grass details if the terrain is relatively flat (slope factor)
        float slope = 1.0 - normal.y;
        if(slope < 0.5)
        {
            float t = FractalNoise(pos.xz * .1) + .5;
            mat = GrassBlades(pos, dir, mat, dis) * t;
        }

        // Apply Lighting
        DoLighting(mat, pos, normal, dir, dis);
    }

    // Apply Fog
    return ApplyFog(mat, dis, dir);
}

float BinarySubdivision(in vec3 rO, in vec3 rD, float t, float oldT)
{
    float halfwayT = 0.0;
    for (int n = 0; n < 5; n++)
    {
        halfwayT = (oldT + t) * .5;
        float h = Map(rO + halfwayT * rD).x;
        if (h < THRESHOLD) t = halfwayT; else oldT = halfwayT;
    }
    return t;
}

bool Scene(in vec3 rO, in vec3 rD, out float resT, out float type)
{
    float t = 5.;
    float oldT = 0.0;
    float h = 0.0;
    bool hit = false;
    for(int j = 0; j < 60; j++)
    {
        vec3 p = rO + t * rD;
        h = Map(p).x;
        if(h < THRESHOLD)
        {
            hit = true;
            break;
        }
        oldT = t;
        t += h + (t * 0.04);
    }
    type = 0.0;
    resT = BinarySubdivision(rO, rD, t, oldT);
    return hit;
}

vec3 CameraPath(float t)
{
    vec2 p = vec2(200.0 * sin(3.54 * t), 200.0 * cos(2.0 * t));
    return vec3(p.x + 55.0, 12.0 + sin(t * .3) * 6.5, -94.0 + p.y);
}

vec3 PostEffects(vec3 rgb, vec2 xy)
{
    rgb = pow(rgb, vec3(0.45));
    #define CONTRAST 1.1
    #define SATURATION 1.3
    #define BRIGHTNESS 1.3
    rgb = mix(vec3(.5), mix(vec3(dot(vec3(.2125, .7154, .0721), rgb * BRIGHTNESS)), rgb * BRIGHTNESS, SATURATION), CONTRAST);
    rgb *= .4 + 0.5 * pow(40.0 * xy.x * xy.y * (1.0 - xy.x) * (1.0 - xy.y), 0.2);
    return rgb;
}

void main()
{
    // Setup Coordinates
    vec2 uv = (-1.0 + 2.0 * fTex) * vec2(uResolution.x / uResolution.y, 1.0);

    if (fTex.y < .13 || fTex.y >= .87)
    {
        FragColor = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }

    // Camera & View
    float m = (uMouse.x / uResolution.x) * 300.0;
    float gTime = (uTime * 5.0 + m + 2352.0) * .006;

    cameraPos = CameraPath(gTime + 0.0);
    cameraPos.x -= 3.0;
    vec3 camTar = CameraPath(gTime + .009);
    cameraPos.y += Terrain(CameraPath(gTime + .009).xz).x;
    camTar.y = cameraPos.y;

    float roll = .4 * sin(gTime + .5);
    vec3 cw = normalize(camTar - cameraPos);
    vec3 cp = vec3(sin(roll), cos(roll), 0.0);
    vec3 cu = cross(cw, cp);
    vec3 cv = cross(cu, cw);
    vec3 dir = normalize(uv.x * cu + uv.y * cv + 1.3 * cw);

    // Raymarch
    vec3 col = vec3(0.0);
    float distance, type;
    if(!Scene(cameraPos, dir, distance, type))
    {
        col = GetSky(dir);
    }
    else
    {
        vec3 pos = cameraPos + distance * dir;
        vec2 p = vec2(0.01, 0.0);
        vec3 nor = normalize(vec3(Map(pos + p.xyy).x - Map(pos - p.xyy).x,
                                  Map(pos + p.yxy).x - Map(pos - p.yxy).x,
                                  Map(pos + p.yyx).x - Map(pos - p.yyx).x));
        col = TerrainColour(pos, dir, nor, distance, type);
    }

    // Lens Flare & Glare
    float bri = dot(cw, sunLight) * .75;
    if (bri > 0.0)
    {
        vec2 sunPos = vec2(dot(sunLight, cu), dot(sunLight, cv));
        vec2 uvT = uv - sunPos;
        uvT = uvT * (length(uvT));
        bri = pow(bri, 6.0) * .8;

        float glare1 = max(dot(normalize(vec3(dir.x, dir.y + .3, dir.z)), sunLight), 0.0) * 1.4;
        float glare2 = max(1.0 - length(uvT + sunPos * .5) * 4.0, 0.0);
        uvT = mix(uvT, uv, -2.3);
        float glare3 = max(1.0 - length(uvT + sunPos * 5.0) * 1.2, 0.0);

        col += bri * vec3(1.0, .0, .0) * pow(glare1, 12.5) * .05;
        col += bri * vec3(1.0, 1.0, 0.2) * pow(glare2, 2.0) * 2.5;
        col += bri * sunColour * pow(glare3, 2.0) * 3.0;
    }

    // Final Post
    col = PostEffects(col, fTex);
    FragColor = vec4(col, 1.0);
}";

            _raymarchShader = Shader.Create(vs, fsRaymarch);
        }

        // target backbuffer screen directly
        KoRender.SetRenderTarget(null);
        KoRender.Clear(0.0f, 0.0f, 0.0f, 1.0f);

        // ortho camera
        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, _width, _height, 0, -1, 1);

        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        // bind procedural landscape shader
        KoRender.UseShader(_raymarchShader);

        // pass uniforms required by procedural raymarching
        KoRender.SetUniform("uTime", _time);
        KoRender.SetUniform("uResolution", new Vector2(_width, _height));
        KoRender.SetUniform("uMouse", _mousePos);

        // draw a full-screen quad to execute the fragment shader across every pixel
        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 1, 1, 1);

        KoRender.TexCoord2(0, 1);
        KoRender.Vertex2(0, 0);

        KoRender.TexCoord2(1, 1);
        KoRender.Vertex2(_width, 0);

        KoRender.TexCoord2(1, 0);
        KoRender.Vertex2(_width, _height);

        KoRender.TexCoord2(0, 0);
        KoRender.Vertex2(0, _height);
        KoRender.End();

        KoRender.Flush();
    }
}
