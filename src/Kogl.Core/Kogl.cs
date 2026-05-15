using System.Numerics;
using Kogl.Abstractions;
using StbImageSharp;

namespace Kogl.Core;

/// <summary>
/// static frontend simulating the immediate mode OpenGL API.
/// </summary>
public static class RenderApi
{
    private static IGraphicsBackend _backend = null!;
    private static DynamicBatcher _batcher = null!;
    private static readonly MatrixStack _matrices = new();

    private static TextureHandle _defaultTexture;
    private static ShaderHandle _defaultShader;

    // current state
    private static Vector2 _currentTexCoord = Vector2.Zero;
    private static Vector4 _currentColor = Vector4.One;
    private static TextureHandle _currentTextureHandle;
    private static ShaderHandle _currentShaderHandle;

    private static uint _cachedFboId = 0;
    private static int _screenWidth = 800;
    private static int _screenHeight = 600;

    public static void Initialize(IGraphicsBackend backend)
    {
        _backend = backend;
        _backend.Initialize();
        _batcher = new DynamicBatcher(_backend);

        // generate 1x1 white texture
        ReadOnlySpan<byte> whitePixels = [255, 255, 255, 255];
        _defaultTexture = _backend.CreateTexture(whitePixels, 1, 1, 4);
        _currentTextureHandle = _defaultTexture;

        // setup glsl fallback shaders matching structural CPU transform properties
        string vertexSource =
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

        string fragmentSource =
            @"#version 330 core
in vec2 fTex;
in vec4 fCol;
out vec4 FragColor;
uniform sampler2D uTex;
void main() {
    FragColor = texture(uTex, fTex) * fCol;
}";

        _defaultShader = _backend.CreateShader(vertexSource, fragmentSource);
        _currentShaderHandle = _defaultShader;
    }

    // ==========================================
    // Matrix
    // ==========================================

    public static void MatrixMode(MatrixMode mode)
    {
        _matrices.CurrentMode = mode;
    }

    public static void PushMatrix()
    {
        _matrices.Push();
    }

    public static void PopMatrix()
    {
        _matrices.Pop();
    }

    public static void LoadIdentity()
    {
        _matrices.LoadIdentity();
    }

    public static void Translate(float x, float y, float z)
    {
        _matrices.Translate(x, y, z);
    }

    public static void Rotate(float angle, float x, float y, float z)
    {
        _matrices.Rotate(angle, x, y, z);
    }

    public static void Ortho(float l, float r, float b, float t, float n, float f)
    {
        _matrices.Ortho(l, r, b, t, n, f);
    }

    internal static Matrix4x4 GetProjectionMatrix()
    {
        return _matrices.Projection;
    }

    // ==========================================
    // Rendering
    // ==========================================

    public static void Begin(PrimitiveMode mode)
    {
        _batcher.Begin(mode, _currentTextureHandle, _currentShaderHandle);
    }

    public static void End()
    {
        _batcher.End();
    }

    public static void Flush()
    {
        _batcher.Flush(_matrices.Projection);
    }

    // ==========================================
    // Rendering / Post-Processing
    // ==========================================

    public static RenderTarget CreateRenderTarget(int width, int height)
    {
        return _backend.CreateRenderTarget(width, height);
    }

    public static void SetRenderTarget(RenderTarget? target)
    {
        // dispatch all pending geometry before switching the destination!
        Flush();

        _backend.SetRenderTarget(target);
        _cachedFboId = target?.FboId ?? 0;

        // restore the window viewport if we just unbound the fbo
        if (target == null)
        {
            _backend.SetViewport(0, 0, _screenWidth, _screenHeight);
        }
    }

    // ==========================================
    // Vertex
    // ==========================================

    public static void TexCoord2(float x, float y)
    {
        _currentTexCoord = new Vector2(x, y);
    }

    public static void Color4(float r, float g, float b, float a)
    {
        _currentColor = new Vector4(r, g, b, a);
    }

    public static void Vertex2(float x, float y)
    {
        Vertex3(x, y, 0);
    }

    public static void Vertex3(float x, float y, float z)
    {
        _batcher.AddVertex(
            new Vector3(x, y, z),
            _currentTexCoord,
            _currentColor,
            _matrices.ModelView
        );
    }

    // ==========================================
    // Resources / States
    // ==========================================

    public static ShaderHandle CreateShader(string vsCode, string fsCode)
    {
        return _backend.CreateShader(vsCode, fsCode);
    }

    public static void UseShader(ShaderHandle shader)
    {
        _currentShaderHandle = shader.Id == 0 ? _defaultShader : shader;
    }

    public static void UseDefaultShader()
    {
        _currentShaderHandle = _defaultShader;
    }

    public static void UseTexture(TextureHandle texture)
    {
        _currentTextureHandle = texture.Id == 0 ? _defaultTexture : texture;
    }

    public static void UseDefaultTexture()
    {
        _currentTextureHandle = _defaultTexture;
    }

    public static TextureHandle LoadTexture(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Texture file not found: {path}");
        }

        StbImage.stbi_set_flip_vertically_on_load(1);
        using FileStream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        return _backend.CreateTexture(image.Data, image.Width, image.Height, 4);
    }

    public static void DeleteTexture(TextureHandle handle)
    {
        if (_currentTextureHandle.Id == handle.Id)
        {
            UseDefaultTexture();
        }
        _backend.DeleteTexture(handle);
    }

    public static void Clear(float r, float g, float b, float a)
    {
        _backend.Clear(r, g, b, a);
    }

    public static void SetViewport(int x, int y, int width, int height)
    {
        if (_cachedFboId == 0)
        {
            _screenWidth = width;
            _screenHeight = height;
        }
        _backend.SetViewport(x, y, width, height);
    }

    public static void BeginScissor(int x, int y, int width, int height)
    {
        // flush any pending geometry before changing the clipping region
        Flush();

        // enable the test
        _backend.SetScissorEnabled(true);

        // flip y for Top-left coordinate system
        int flippedY = _screenHeight - (y + height);

        _backend.SetScissor(x, flippedY, width, height);
    }

    public static void EndScissor()
    {
        // flush pending geometry drawn inside the scissor box
        Flush();

        _backend.SetScissorEnabled(false);
    }

    // ==========================================
    // Uniforms
    // ==========================================

    public static void SetUniform(string name, int value)
    {
        Flush();
        _backend.SetUniformInt(_currentShaderHandle, name, value);
    }

    public static void SetUniform(string name, float value)
    {
        Flush();
        _backend.SetUniformFloat(_currentShaderHandle, name, value);
    }

    public static void SetUniform(string name, in Vector2 value)
    {
        Flush();
        _backend.SetUniformVec2(_currentShaderHandle, name, value);
    }

    public static void SetUniform(string name, in Vector3 value)
    {
        Flush();
        _backend.SetUniformVec3(_currentShaderHandle, name, value);
    }

    public static void SetUniform(string name, in Vector4 value)
    {
        Flush();
        _backend.SetUniformVec4(_currentShaderHandle, name, value);
    }

    public static void SetUniform(string name, in Matrix4x4 value)
    {
        Flush();
        _backend.SetUniformMatrix4(_currentShaderHandle, name, value);
    }

    internal static IGraphicsBackend GetBackend()
    {
        return _backend;
    }
}
