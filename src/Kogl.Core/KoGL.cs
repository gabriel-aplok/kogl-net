using System.Numerics;
using Kogl.Abstractions;
using Kogl.Abstractions.Types;
using Kogl.Core.Graphics;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;

namespace Kogl.Core;

/// <summary>
/// This is the heart of the library.
/// Old name was RenderAPI.
/// </summary>
public static class KoGL
{
    public const int MaxTextureSlots = 8;

    private static IGraphicsBackend _backend = null!;
    private static Batcher _batcher = null!;
    private static readonly MatrixStack _matrices = new();

    private static TextureHandle _defaultTexture;
    private static ShaderHandle _defaultShader;

    private static Material? _currentMaterial;
    private static Vector2 _currentTexCoord = Vector2.Zero;
    private static Vector4 _currentColor = Vector4.One;
    private static Vector3 _currentNormal = Vector3.UnitZ;

    private static readonly TextureHandle[] _currentTextures = new TextureHandle[MaxTextureSlots];
    private static ShaderHandle _currentShaderHandle;

    private static uint _cachedFboId = 0;
    private static int _screenWidth = 800;
    private static int _screenHeight = 600;

    #region Init

    /// <summary>
    /// Initializes the render api
    /// </summary>
    /// <param name="backend">The graphics backend</param>
    public static void Initialize(IGraphicsBackend backend)
    {
        // initialize the graphics backend
        _backend = backend;
        _backend.Initialize();
        _batcher = new Batcher(_backend);

        // create default texture
        ReadOnlySpan<byte> whitePixels = [255, 255, 255, 255];
        _defaultTexture = _backend.CreateTexture(
            1,
            1,
            TextureFormat.Rgba8,
            TextureFilter.Nearest,
            TextureFilter.Nearest,
            TextureWrap.Repeat,
            TextureWrap.Repeat,
            whitePixels
        );

        for (int i = 0; i < MaxTextureSlots; i++)
        {
            _currentTextures[i] = _defaultTexture;
        }

        // create default shader
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
uniform sampler2D uTex;
void main() {
    FragColor = texture(uTex, fTex) * fCol;
}";

        // create default shader
        _defaultShader = _backend.CreateShader(vs, fs);
        _currentShaderHandle = _defaultShader;
    }

    #endregion

    #region Matrix

    /// <summary>
    /// Sets the matrix mode
    /// </summary>
    /// <param name="mode">The matrix mode</param>
    public static void MatrixMode(MatrixState mode)
    {
        _matrices.CurrentMode = mode;
    }

    /// <summary>
    /// Pushes the current matrix onto the stack
    /// </summary>
    public static void PushMatrix()
    {
        _matrices.Push();
    }

    /// <summary>
    /// Pops the current matrix off the stack
    /// </summary>
    public static void PopMatrix()
    {
        _matrices.Pop();
    }

    /// <summary>
    /// Sets the current matrix to the identity
    /// </summary>
    public static void LoadIdentity()
    {
        _matrices.LoadIdentity();
    }

    /// <summary>
    /// Sets the current matrix
    /// </summary>
    /// <param name="matrix">The matrix</param>
    public static void LoadMatrix(Matrix4x4 matrix)
    {
        _matrices.LoadMatrix(matrix);
    }

    /// <summary>
    /// Multiplies the current matrix
    /// </summary>
    public static void Multiply(Matrix4x4 matrix)
    {
        _matrices.Multiply(matrix);
    }

    /// <summary>
    /// Scales the current matrix
    /// </summary>
    /// <param name="x">The x scale</param>
    /// <param name="y">The y scale</param>
    /// <param name="z">The z scale</param>
    public static void Scale(float x, float y, float z)
    {
        _matrices.Scale(x, y, z);
    }

    /// <summary>
    /// Scales the current matrix
    /// </summary>
    /// <param name="x">The x scale</param>
    /// <param name="y">The y scale</param>
    public static void Scale(float x, float y)
    {
        _matrices.Scale(x, y, 1);
    }

    /// <summary>
    /// Translates the current matrix
    /// </summary>
    /// <param name="x">The x translation</param>
    /// <param name="y">The y translation</param>
    /// <param name="z">The z translation</param>
    public static void Translate(float x, float y, float z)
    {
        _matrices.Translate(x, y, z);
    }

    /// <summary>
    /// Translates the current matrix
    /// </summary>
    /// <param name="x">The x translation</param>
    /// <param name="y">The y translation</param>
    public static void Translate(float x, float y)
    {
        _matrices.Translate(x, y, 0);
    }

    /// <summary>
    /// Rotates the current matrix
    /// </summary>
    /// <param name="angle">The angle in degrees</param>
    /// <param name="x">The x axis</param>
    /// <param name="y">The y axis</param>
    /// <param name="z">The z axis</param>
    public static void Rotate(float angle, float x, float y, float z)
    {
        _matrices.Rotate(angle, x, y, z);
    }

    /// <summary>
    /// Rotates the current matrix
    /// </summary>
    /// <param name="angle">The angle in degrees</param>
    /// <param name="x">The x axis</param>
    /// <param name="y">The y axis</param>
    public static void Rotate(float angle, float x, float y)
    {
        _matrices.Rotate(angle, x, y, 0);
    }

    /// <summary>
    /// Sets the current matrix to an orthographic projection
    /// </summary>
    /// <param name="left">The left plane</param>
    /// <param name="right">The right plane</param>
    /// <param name="bottom">The bottom plane</param>
    /// <param name="top">The top plane</param>
    /// <param name="zNear">The near plane</param>
    /// <param name="zFar">The far plane</param>
    public static void Ortho(float l, float r, float b, float t, float zn, float zf)
    {
        _matrices.Ortho(l, r, b, t, zn, zf);
    }

    /// <summary>
    /// Sets the current matrix to a perspective projection
    /// </summary>
    /// <param name="fovy">The field of view in y</param>
    /// <param name="aspect">The aspect ratio</param>
    /// <param name="zNear">The near plane</param>
    /// <param name="zFar">The far plane</param>
    public static void Perspective(float fovy, float aspect, float zNear, float zFar)
    {
        float fovRad = fovy * (MathF.PI / 180.0f);
        _matrices.Perspective(fovRad, aspect, zNear, zFar);
    }

    /// <summary>
    /// Sets the current matrix to a perspective projection
    /// </summary>
    /// <param name="left">The left plane</param>
    /// <param name="right">The right plane</param>
    /// <param name="bottom">The bottom plane</param>
    /// <param name="top">The top plane</param>
    /// <param name="zNear">The near plane</param>
    /// <param name="zFar">The far plane</param>
    public static void Frustum(float l, float r, float b, float t, float zn, float zf)
    {
        _matrices.Frustum(l, r, b, t, zn, zf);
    }

    /// <summary>
    /// Returns the current projection matrix
    /// </summary>
    /// <returns>The projection matrix</returns>
    public static Matrix4x4 GetProjectionMatrix()
    {
        return _matrices.Projection;
    }

    /// <summary>
    /// Returns the current model view matrix
    /// </summary>
    /// <returns>The model view matrix</returns>
    public static Matrix4x4 GetModelViewMatrix()
    {
        return _matrices.ModelView;
    }

    /// <summary>
    /// Begins a camera
    /// </summary>
    /// <param name="camera">The camera</param>
    public static void BeginCamera(Camera camera)
    {
        Flush();

        MatrixMode(MatrixState.Projection);
        LoadIdentity();
        Multiply(camera.GetProjectionMatrix(GetAspectRatio()));

        MatrixMode(MatrixState.ModelView);
        LoadIdentity();
        Multiply(camera.GetViewMatrix());
    }

    /// <summary>
    /// Ends a camera
    /// </summary>
    public static void EndCamera()
    {
        Flush();

        MatrixMode(MatrixState.Projection);
        LoadIdentity();

        MatrixMode(MatrixState.ModelView);
        LoadIdentity();
    }

    #endregion

    #region Viewport

    /// <summary>
    /// Clears the screen
    /// </summary>
    /// <param name="r">The red component</param>
    /// <param name="g">The green component</param>
    /// <param name="b">The blue component</param>
    /// <param name="a">The alpha component</param>
    public static void Clear(float r, float g, float b, float a)
    {
        ResetStates();
        _backend.Clear(r, g, b, a);
    }

    /// <summary>
    /// Sets the viewport
    /// </summary>
    /// <param name="x">The x position</param>
    /// <param name="y">The y position</param>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    public static void SetViewport(int x, int y, int width, int height)
    {
        if (_cachedFboId == 0)
        {
            _screenWidth = width;
            _screenHeight = height;
        }
        _backend.SetViewport(x, y, width, height);
    }

    /// <summary>
    /// Returns the aspect ratio
    /// </summary>
    /// <returns>The aspect ratio</returns>
    public static float GetAspectRatio()
    {
        return (float)_screenWidth / _screenHeight;
    }

    /// <summary>
    /// Begins a scissor
    /// </summary>
    /// <param name="x">The x position</param>
    /// <param name="y">The y position</param>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    public static void BeginScissor(int x, int y, int width, int height)
    {
        Flush();
        _backend.SetScissorEnabled(true);

        int flippedY = _screenHeight - (y + height);
        _backend.SetScissor(x, flippedY, width, height);
    }

    /// <summary>
    /// Ends a scissor
    /// </summary>
    public static void EndScissor()
    {
        Flush();
        _backend.SetScissorEnabled(false);
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Begins rendering
    /// </summary>
    /// <param name="mode">The primitive mode</param>
    public static void Begin(PrimitiveMode mode)
    {
        TextureSet set = new()
        {
            Slot0 = _currentTextures[0],
            Slot1 = _currentTextures[1],
            Slot2 = _currentTextures[2],
            Slot3 = _currentTextures[3],
            Slot4 = _currentTextures[4],
            Slot5 = _currentTextures[5],
            Slot6 = _currentTextures[6],
            Slot7 = _currentTextures[7],
        };

        _batcher.Begin(mode, set, _currentShaderHandle);
    }

    /// <summary>
    /// Ends rendering
    /// </summary>
    public static void End()
    {
        _batcher.End();
    }

    /// <summary>
    /// Flushes the batcher
    /// </summary>
    public static void Flush()
    {
        _batcher.Flush(_matrices.Projection);
    }

    #endregion
    #region Post Processing

    /// <summary>Creates a render target</summary>
    public static RenderTarget CreateRenderTarget(
        int width,
        int height,
        TextureFormat[]? colorFormats = null,
        TextureFormat depthFormat = TextureFormat.Depth24Stencil8,
        bool depthAsTexture = false
    )
    {
        colorFormats ??= [TextureFormat.Rgba8];
        return _backend.CreateRenderTarget(
            width,
            height,
            colorFormats,
            depthFormat,
            depthAsTexture
        );
    }

    /// <summary>Sets the render target</summary>
    public static void SetRenderTarget(RenderTarget? target)
    {
        Flush();

        _backend.SetRenderTarget(target);
        _cachedFboId = target?.FboId ?? 0;

        if (target == null)
        {
            _backend.SetViewport(0, 0, _screenWidth, _screenHeight);
        }
    }

    #endregion
    #region Vertex

    /// <summary>Sets the texture coordinates</summary>
    public static void TexCoord2(float x, float y)
    {
        _currentTexCoord = new Vector2(x, y);
    }

    /// <summary>Sets the color</summary>
    public static void Color3(float r, float g, float b)
    {
        _currentColor = new Vector4(r, g, b, 1);
    }

    /// <summary>Sets the color and alpha</summary>
    public static void Color4(float r, float g, float b, float a)
    {
        _currentColor = new Vector4(r, g, b, a);
    }

    /// <summary>Sets the normal</summary>
    public static void Normal3(float x, float y, float z)
    {
        _currentNormal = new Vector3(x, y, z);
    }

    /// <summary>Sets the normal</summary>
    public static void Normal3(in Vector3 normal)
    {
        _currentNormal = normal;
    }

    /// <summary>Sets the vertex</summary>
    public static void Vertex2(float x, float y)
    {
        Vertex3(x, y, 0);
    }

    /// <summary>Sets the vertex</summary>
    public static void Vertex3(float x, float y, float z)
    {
        _batcher.AddVertex(
            new Vector3(x, y, z),
            _currentTexCoord,
            _currentColor,
            _currentNormal,
            _matrices.ModelView
        );
    }

    #endregion
    #region Resources Management

    /// <summary>
    /// Applies a material
    /// </summary>
    public static void ApplyMaterial(Material material)
    {
        if (_currentMaterial == material)
            return;

        Flush();

        _currentMaterial = material;
        _currentShaderHandle = material.Shader.Handle;

        material.Apply();
        GlobalUniforms.ApplyTo(material.Shader);
    }

    /// <summary>
    /// Uses the default shader
    /// </summary>
    public static void UseDefaultShader()
    {
        _currentShaderHandle = _defaultShader;
    }

    /// <summary>
    /// Creates a shader
    /// </summary>
    /// <param name="vsCode">Code for the vertex shader</param>
    /// <param name="fsCode">Code for the fragment shader</param>
    /// <returns></returns>
    public static ShaderHandle CreateShader(string vsCode, string fsCode)
    {
        return _backend.CreateShader(vsCode, fsCode);
    }

    /// <summary>Uses a shader</summary>
    public static void UseShader(ShaderHandle shader)
    {
        _currentShaderHandle = shader.Id == 0 ? _defaultShader : shader;
    }

    /// <summary>Creates a texture</summary>
    public static TextureHandle CreateTexture(
        int width,
        int height,
        TextureFormat format,
        TextureFilter minFilter,
        TextureFilter magFilter,
        TextureWrap wrapS,
        TextureWrap wrapT,
        ReadOnlySpan<byte> data = default
    )
    {
        return _backend.CreateTexture(
            width,
            height,
            format,
            minFilter,
            magFilter,
            wrapS,
            wrapT,
            data
        );
    }

    /// <summary>
    /// Uses the default texture
    /// </summary>
    /// <param name="slot">The slot</param>
    public static void UseDefaultTexture(int slot = 0)
    {
        if (slot >= 0 && slot < MaxTextureSlots)
            _currentTextures[slot] = _defaultTexture;
    }

    /// <summary>
    /// Uses a texture
    /// </summary>
    /// <param name="texture">The texture</param>
    /// <param name="slot">The slot</param>
    public static void UseTexture(TextureHandle texture, int slot = 0)
    {
        if (slot >= 0 && slot < MaxTextureSlots)
            _currentTextures[slot] = texture.Id == 0 ? _defaultTexture : texture;
    }

    /// <summary>Updates a texture</summary>
    public static void UpdateTexture(
        TextureHandle handle,
        int x,
        int y,
        int width,
        int height,
        ReadOnlySpan<byte> data,
        TextureFormat format
    )
    {
        Flush();
        _backend.UpdateTexture(handle, x, y, width, height, data, format);
    }

    /// <summary>
    /// Deletes a texture
    /// </summary>
    /// <param name="handle">The handle</param>
    public static void DeleteTexture(TextureHandle handle)
    {
        // nullify from all active slots to prevent use-after-free
        for (int i = 0; i < MaxTextureSlots; i++)
        {
            if (_currentTextures[i].Id == handle.Id)
                UseDefaultTexture(i);
        }
        _backend.DeleteTexture(handle);
    }

    /// <summary>Sets the texture compare mode</summary>
    public static void SetTextureCompareMode(TextureHandle texture, KTextureCompareMode mode)
    {
        _backend.SetTextureCompareMode(texture, mode);
    }

    /// <summary>Sets the texture border color</summary>
    public static void SetTextureBorderColor(TextureHandle texture, Vector4 color)
    {
        _backend.SetTextureBorderColor(texture, color);
    }

    #endregion
    #region States

    /// <summary>
    /// Resets all internal render states to defaults.
    /// </summary>
    public static void ResetStates()
    {
        for (int i = 0; i < MaxTextureSlots; i++)
            UseDefaultTexture(i);

        UseDefaultShader();
        _currentTexCoord = Vector2.Zero;
        _currentColor = Vector4.One;

        // set default render states
        EnableDepthTest();
        DisableBlending();
        DisableCulling();
        FrontFace(FrontFaceState.Ccw);
        DepthMask(true);
        ColorMask(true, true, true, true);
        DepthFunc(DepthFunctionState.Less);
        StencilMask(0xFFFFFFFF);
        PolygonMode(PolygonState.Fill);
        DisableDither();
        LineWidth(1.0f);
        PointSize(1.0f);
        DisableStencilTest();
    }

    /// <summary>Enables depth testing.</summary>
    public static void EnableDepthTest()
    {
        _backend.SetDepthTest(true);
    }

    /// <summary>Disables depth testing.</summary>
    public static void DisableDepthTest()
    {
        _backend.SetDepthTest(false);
    }

    /// <summary>Enables blending.</summary>
    public static void EnableBlending()
    {
        Flush();
        _backend.SetBlending(true);
    }

    /// <summary>Disables blending.</summary>
    public static void DisableBlending()
    {
        Flush();
        _backend.SetBlending(false);
    }

    /// <summary>Enables culling.</summary>
    public static void EnableCulling(CullFaceState mode = CullFaceState.Back)
    {
        _backend.SetCulling(true, mode);
    }

    /// <summary>Disables culling.</summary>
    public static void DisableCulling()
    {
        _backend.SetCulling(false);
    }

    /// <summary>Sets the front face</summary>
    public static void FrontFace(FrontFaceState mode)
    {
        _backend.SetFrontFace(mode);
    }

    /// <summary>Sets the polygon mode</summary>
    public static void PolygonMode(PolygonState mode)
    {
        _backend.SetPolygonMode(mode);
    }

    /// <summary>Sets the polygon offset</summary>
    public static void PolygonOffset(float factor, float units)
    {
        _backend.SetPolygonOffset(factor, units);
    }

    /// <summary>Enables dithering</summary>
    public static void EnableDither()
    {
        _backend.SetDither(true);
    }

    /// <summary>Disables dithering</summary>
    public static void DisableDither()
    {
        _backend.SetDither(false);
    }

    /// <summary>Sets the line width</summary>
    public static void LineWidth(float width)
    {
        _backend.SetLineWidth(width);
    }

    /// <summary>Sets the point size</summary>
    public static void PointSize(float size)
    {
        _backend.SetPointSize(size);
    }

    /// <summary>Sets the depth mask</summary>
    public static void DepthMask(bool writeEnabled)
    {
        _backend.SetDepthMask(writeEnabled);
    }

    /// <summary>Sets the color mask</summary>
    public static void ColorMask(bool r, bool g, bool b, bool a)
    {
        _backend.SetColorMask(r, g, b, a);
    }

    /// <summary>Sets the blend function</summary>
    public static void BlendFunc(BlendingFactorState src, BlendingFactorState dst)
    {
        _backend.SetBlendFunc(src, dst);
    }

    /// <summary>Sets the blend equation</summary>
    public static void BlendEquation(BlendEquationState mode)
    {
        _backend.SetBlendEquation(mode);
    }

    /// <summary>Sets the depth function</summary>
    public static void DepthFunc(DepthFunctionState func)
    {
        _backend.SetDepthFunc(func);
    }

    /// <summary>Sets the stencil function</summary>
    public static void StencilFunc(StencilFunctionState func, int reference, uint mask)
    {
        _backend.SetStencilFunc(func, reference, mask);
    }

    /// <summary>Sets the stencil op</summary>
    public static void StencilOp(StencilOpState sfail, StencilOpState dpfail, StencilOpState dppass)
    {
        _backend.SetStencilOp(sfail, dpfail, dppass);
    }

    /// <summary>Sets the stencil mask</summary>
    public static void StencilMask(uint mask)
    {
        _backend.SetStencilMask(mask);
    }

    /// <summary>Sets the logic operation</summary>
    public static void LogicOp(LogicOpState op)
    {
        _backend.SetLogicOp(op);
    }

    /// <summary>Enables stencil testing</summary>
    public static void EnableStencilTest()
    {
        _backend.SetStencilTest(true);
    }

    /// <summary>Disables stencil testing</summary>
    public static void DisableStencilTest()
    {
        _backend.SetStencilTest(false);
    }

    /// <summary>Sets the clear color</summary>
    public static void ClearDepth(float depth)
    {
        _backend.SetClearDepth(depth);
    }

    /// <summary>Sets the clear stencil</summary>
    public static void ClearStencil(int stencil)
    {
        _backend.SetClearStencil(stencil);
    }

    #endregion
    #region Uniforms

    /// <summary>
    /// Sets a int uniform
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    public static void SetUniform(string name, int value)
    {
        Flush();
        _backend.SetUniformInt(_currentShaderHandle, name, value);
    }

    /// <summary>
    /// Sets a float uniform
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    public static void SetUniform(string name, float value)
    {
        Flush();
        _backend.SetUniformFloat(_currentShaderHandle, name, value);
    }

    /// <summary>
    /// Sets a vector2 uniform
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    public static void SetUniform(string name, in Vector2 value)
    {
        Flush();
        _backend.SetUniformVec2(_currentShaderHandle, name, value);
    }

    /// <summary>
    /// Sets a vector3 uniform
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    public static void SetUniform(string name, in Vector3 value)
    {
        Flush();
        _backend.SetUniformVec3(_currentShaderHandle, name, value);
    }

    /// <summary>
    /// Sets a vector4 uniform
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    public static void SetUniform(string name, in Vector4 value)
    {
        Flush();
        _backend.SetUniformVec4(_currentShaderHandle, name, value);
    }

    /// <summary>
    /// Sets a matrix 4x4 uniform
    /// </summary>
    /// <param name="name">The name</param>
    /// <param name="value">The value</param>
    public static void SetUniform(string name, in Matrix4x4 value)
    {
        Flush();
        _backend.SetUniformMatrix4(_currentShaderHandle, name, value);
    }

    /// <summary>
    /// Returns the graphics backend
    /// </summary>
    /// <returns>The graphics backend</returns>
    public static IGraphicsBackend GetBackend()
    {
        return _backend;
    }

    #endregion
}
