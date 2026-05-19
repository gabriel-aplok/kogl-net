using System.Numerics;
using Kogl.Abstractions;
using Kogl.Abstractions.Types;
using Silk.NET.OpenGL;

namespace Kogl.OpenGL;

/// <summary>
/// The OpenGL backend, focused on Desktop OpenGL.
/// </summary>
/// <param name="glContext">The gl context</param>
public sealed unsafe class OpenGLBackend(GL glContext) : IGraphicsBackend
{
    private readonly GL _gl = glContext;
    private uint _vao,
        _vbo,
        _ebo;

    // state caching, to avoid redundant state changes
    private CullFaceState _currentCullMode = CullFaceState.Back;
    private FrontFaceState _currentFrontFace = FrontFaceState.Ccw;
    private PolygonState _currentPolygonMode = PolygonState.Fill;
    private float _polygonOffsetFactor = 0;
    private float _polygonOffsetUnits = 0;
    private bool _ditherEnabled = true;
    private float _currentLineWidth = 1.0f;
    private float _currentPointSize = 1.0f;
    private bool _depthMask = true;
    private StencilFunctionState _currentStencilFunc = StencilFunctionState.Always;
    private int _currentStencilRef = 0;
    private uint _currentStencilMask = 0xFFFFFFFF;
    private StencilOpState _currentStencilSFail = StencilOpState.Keep;
    private StencilOpState _currentStencilDPFail = StencilOpState.Keep;
    private StencilOpState _currentStencilDPPass = StencilOpState.Keep;
    private LogicOpState _currentLogicOp = LogicOpState.Copy;
    private bool _stencilTest = false;

    private uint _cachedVao;
    private uint _cachedVbo;
    private uint _cachedEbo;
    private uint _cachedShader;
    private uint _cachedFbo;

    private readonly uint[] _cachedTextures = new uint[8];
    private int _activeTextureSlot = -1;

    private readonly Dictionary<(uint, string), int> _uniformLocations = [];

    #region Initialization

    /// <summary>Initializes the backend</summary>
    public void Initialize()
    {
        _vao = _gl.GenVertexArray();
        BindVaoInternal(_vao);

        _vbo = _gl.GenBuffer();
        BindVboInternal(_vbo);

        // pre-allocate structural dynamic buffer
        _gl.BufferData(
            BufferTargetARB.ArrayBuffer,
            (nuint)(8192 * sizeof(VertexData)),
            null,
            BufferUsageARB.DynamicDraw
        );

        _ebo = _gl.GenBuffer();
        BindEboInternal(_ebo);
        _gl.BufferData(
            BufferTargetARB.ElementArrayBuffer,
            (nuint)(8192 * 6 * sizeof(ushort)),
            null,
            BufferUsageARB.DynamicDraw
        );

        // attribute setup: location 0 = position
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(VertexData),
            (void*)0
        );

        // attribute setup: location 1 = texCoord
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(
            1,
            2,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(VertexData),
            (void*)12
        );

        // attribute setup: location 2 = color
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(
            2,
            4,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(VertexData),
            (void*)20
        );

        BindVaoInternal(0);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    /// <summary>Sets the viewport</summary>
    public void SetViewport(int x, int y, int w, int h)
    {
        _gl.Viewport(x, y, (uint)w, (uint)h);
    }

    /// <summary>Clears the screen</summary>
    public void Clear(float r, float g, float b, float a)
    {
        _gl.ClearColor(r, g, b, a);
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    #endregion
    #region States

    /// <summary>Enables or disables depth testing</summary>
    public void SetDepthTest(bool enabled)
    {
        if (enabled)
            _gl.Enable(EnableCap.DepthTest);
        else
            _gl.Disable(EnableCap.DepthTest);
    }

    /// <summary>Enables or disables blending</summary>
    public void SetBlending(bool enabled)
    {
        if (enabled)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }
        else
        {
            _gl.Disable(EnableCap.Blend);
        }
    }

    /// <summary>Enables or disables culling</summary>
    public void SetCulling(bool enabled, CullFaceState mode = CullFaceState.Back)
    {
        if (enabled)
        {
            _gl.Enable(EnableCap.CullFace);
            if (_currentCullMode != mode)
            {
                _gl.CullFace(
                    mode switch
                    {
                        CullFaceState.Front => TriangleFace.Front,
                        CullFaceState.Back => TriangleFace.Back,
                        _ => TriangleFace.FrontAndBack,
                    }
                );
                _currentCullMode = mode;
            }
        }
        else
        {
            _gl.Disable(EnableCap.CullFace);
        }
    }

    /// <summary>Sets the front face</summary>
    public void SetFrontFace(FrontFaceState mode)
    {
        if (_currentFrontFace == mode)
            return;
        _currentFrontFace = mode;
        _gl.FrontFace(mode == FrontFaceState.Cw ? FrontFaceDirection.CW : FrontFaceDirection.Ccw);
    }

    /// <summary>Sets the polygon mode</summary>
    public void SetPolygonMode(PolygonState mode)
    {
        if (_currentPolygonMode == mode)
            return;
        _currentPolygonMode = mode;

        _gl.PolygonMode(
            TriangleFace.FrontAndBack,
            mode switch
            {
                PolygonState.Fill => PolygonMode.Fill,
                PolygonState.Line => PolygonMode.Line,
                PolygonState.Point => PolygonMode.Point,
                _ => PolygonMode.Fill,
            }
        );
    }

    /// <summary>Sets the polygon offset</summary>
    public void SetPolygonOffset(float factor, float units)
    {
        if (
            MathF.Abs(_polygonOffsetFactor - factor) < 0.001f
            && MathF.Abs(_polygonOffsetUnits - units) < 0.001f
        )
            return;

        _polygonOffsetFactor = factor;
        _polygonOffsetUnits = units;

        _gl.PolygonOffset(factor, units);
    }

    /// <summary>Sets the dither</summary>
    public void SetDither(bool enabled)
    {
        if (_ditherEnabled == enabled)
            return;
        _ditherEnabled = enabled;

        if (enabled)
            _gl.Enable(EnableCap.Dither);
        else
            _gl.Disable(EnableCap.Dither);
    }

    /// <summary>Sets the line width</summary>
    public void SetLineWidth(float width)
    {
        if (MathF.Abs(_currentLineWidth - width) > 0.001f)
        {
            _gl.LineWidth(width);
            _currentLineWidth = width;
        }
    }

    /// <summary>Sets the point size</summary>
    public void SetPointSize(float size)
    {
        if (MathF.Abs(_currentPointSize - size) > 0.001f)
        {
            _gl.PointSize(size);
            _currentPointSize = size;
        }
    }

    /// <summary>Sets the depth mask</summary>
    public void SetDepthMask(bool writeEnabled)
    {
        if (_depthMask != writeEnabled)
        {
            _gl.DepthMask(writeEnabled);
            _depthMask = writeEnabled;
        }
    }

    /// <summary>Sets the color mask</summary>
    public void SetColorMask(bool r, bool g, bool b, bool a)
    {
        _gl.ColorMask(r, g, b, a);
    }

    /// <summary>Sets the blend function</summary>
    public void SetBlendFunc(BlendingFactorState src, BlendingFactorState dst)
    {
        _gl.BlendFunc(
            src switch
            {
                BlendingFactorState.SrcAlpha => BlendingFactor.SrcAlpha,
                BlendingFactorState.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
                _ => BlendingFactor.One,
            },
            dst switch
            {
                BlendingFactorState.SrcAlpha => BlendingFactor.SrcAlpha,
                BlendingFactorState.OneMinusSrcAlpha => BlendingFactor.OneMinusSrcAlpha,
                _ => BlendingFactor.One,
            }
        );
    }

    /// <summary>Sets the blend equation</summary>
    public void SetBlendEquation(BlendEquationState mode)
    {
        _gl.BlendEquation(
            mode switch
            {
                BlendEquationState.Subtract => BlendEquationModeEXT.FuncSubtract,
                BlendEquationState.ReverseSubtract => BlendEquationModeEXT.FuncReverseSubtract,
                _ => BlendEquationModeEXT.FuncAdd,
            }
        );
    }

    /// <summary>Sets the depth function</summary>
    public void SetDepthFunc(DepthFunctionState func)
    {
        _gl.DepthFunc(
            func switch
            {
                DepthFunctionState.Never => DepthFunction.Never,
                DepthFunctionState.Less => DepthFunction.Less,
                DepthFunctionState.Equal => DepthFunction.Equal,
                DepthFunctionState.Lequal => DepthFunction.Lequal,
                DepthFunctionState.Greater => DepthFunction.Greater,
                DepthFunctionState.NotEqual => DepthFunction.Notequal,
                DepthFunctionState.Gequal => DepthFunction.Gequal,
                _ => DepthFunction.Less,
            }
        );
    }

    /// <summary>Sets the stencil function</summary>
    public void SetStencilFunc(StencilFunctionState func, int reference, uint mask)
    {
        if (
            _currentStencilFunc == func
            && _currentStencilRef == reference
            && _currentStencilMask == mask
        )
            return;

        _currentStencilFunc = func;
        _currentStencilRef = reference;
        _currentStencilMask = mask;

        _gl.StencilFunc(
            func switch
            {
                StencilFunctionState.Never => StencilFunction.Never,
                StencilFunctionState.Less => StencilFunction.Less,
                StencilFunctionState.Equal => StencilFunction.Equal,
                StencilFunctionState.Lequal => StencilFunction.Lequal,
                StencilFunctionState.Greater => StencilFunction.Greater,
                StencilFunctionState.NotEqual => StencilFunction.Notequal,
                StencilFunctionState.Gequal => StencilFunction.Gequal,
                _ => StencilFunction.Always,
            },
            reference,
            mask
        );
    }

    /// <summary>Sets the stencil op</summary>
    public void SetStencilOp(StencilOpState sfail, StencilOpState dpfail, StencilOpState dppass)
    {
        if (
            _currentStencilSFail == sfail
            && _currentStencilDPFail == dpfail
            && _currentStencilDPPass == dppass
        )
            return;

        _currentStencilSFail = sfail;
        _currentStencilDPFail = dpfail;
        _currentStencilDPPass = dppass;

        _gl.StencilOp(ToGlStencilOp(sfail), ToGlStencilOp(dpfail), ToGlStencilOp(dppass));
    }

    /// <summary>Converts a stencil op state to a gl enum</summary>
    private static StencilOp ToGlStencilOp(StencilOpState op)
    {
        return op switch
        {
            StencilOpState.Zero => StencilOp.Zero,
            StencilOpState.Replace => StencilOp.Replace,
            StencilOpState.Incr => StencilOp.Incr,
            StencilOpState.IncrWrap => StencilOp.IncrWrap,
            StencilOpState.Decr => StencilOp.Decr,
            StencilOpState.DecrWrap => StencilOp.DecrWrap,
            StencilOpState.Invert => StencilOp.Invert,
            _ => StencilOp.Keep,
        };
    }

    /// <summary>Sets the stencil mask</summary>
    public void SetStencilMask(uint mask)
    {
        if (_currentStencilMask == mask)
            return;
        _currentStencilMask = mask;

        _gl.StencilMask(mask);
    }

    /// <summary>Sets the logic operation</summary>
    public void SetLogicOp(LogicOpState op)
    {
        if (_currentLogicOp == op)
            return;
        _currentLogicOp = op;

        _gl.LogicOp(
            op switch
            {
                LogicOpState.Clear => LogicOp.Clear,
                LogicOpState.And => LogicOp.And,
                LogicOpState.Copy => LogicOp.Copy,
                LogicOpState.Xor => LogicOp.Xor,
                LogicOpState.Or => LogicOp.Or,
                LogicOpState.Nor => LogicOp.Nor,
                LogicOpState.Equiv => LogicOp.Equiv,
                LogicOpState.Invert => LogicOp.Invert,
                LogicOpState.OrReverse => LogicOp.OrReverse,
                _ => LogicOp.Copy,
            }
        );
    }

    /// <summary>Enables or disables stencil testing</summary>
    public void SetStencilTest(bool enabled)
    {
        if (_stencilTest != enabled)
        {
            if (enabled)
                _gl.Enable(EnableCap.StencilTest);
            else
                _gl.Disable(EnableCap.StencilTest);
            _stencilTest = enabled;
        }
    }

    /// <summary>Sets the clear depth</summary>
    public void SetClearDepth(float depth)
    {
        _gl.ClearDepth(depth);
    }

    /// <summary>Sets the clear stencil</summary>
    public void SetClearStencil(int stencil)
    {
        _gl.ClearStencil(stencil);
    }

    #endregion
    #region Scissors

    /// <summary>Sets the scissor</summary>
    public void SetScissor(int x, int y, int width, int height)
    {
        _gl.Scissor(x, y, (uint)width, (uint)height);
    }

    /// <summary>Enables or disables scissor</summary>
    public void SetScissorEnabled(bool enabled)
    {
        if (enabled)
            _gl.Enable(EnableCap.ScissorTest);
        else
            _gl.Disable(EnableCap.ScissorTest);
    }

    #endregion

    public void UpdateVertexBuffer(ReadOnlySpan<VertexData> vertices)
    {
        BindVboInternal(_vbo);
        fixed (VertexData* ptr = vertices)
        {
            _gl.BufferSubData(
                BufferTargetARB.ArrayBuffer,
                0,
                (nuint)(vertices.Length * sizeof(VertexData)),
                ptr
            );
        }
    }

    public void UpdateIndexBuffer(ReadOnlySpan<ushort> indices)
    {
        BindEboInternal(_ebo);
        fixed (ushort* ptr = indices)
        {
            _gl.BufferSubData(
                BufferTargetARB.ElementArrayBuffer,
                0,
                (nuint)(indices.Length * sizeof(ushort)),
                ptr
            );
        }
    }

    public void DrawBatch(in RenderBatch batch)
    {
        BindVaoInternal(_vao);
        BindShaderInternal(batch.Shader.Id);

        BindTextureInternal(batch.Textures.Slot0.Id, 0);
        BindTextureInternal(batch.Textures.Slot1.Id, 1);
        BindTextureInternal(batch.Textures.Slot2.Id, 2);
        BindTextureInternal(batch.Textures.Slot3.Id, 3);
        BindTextureInternal(batch.Textures.Slot4.Id, 4);
        BindTextureInternal(batch.Textures.Slot5.Id, 5);
        BindTextureInternal(batch.Textures.Slot6.Id, 6);
        BindTextureInternal(batch.Textures.Slot7.Id, 7);

        PrimitiveType glMode = batch.Mode switch
        {
            PrimitiveMode.Lines => PrimitiveType.Lines,
            PrimitiveMode.LineStrip => PrimitiveType.LineStrip,
            PrimitiveMode.Triangles => PrimitiveType.Triangles,
            PrimitiveMode.TriangleStrip => PrimitiveType.TriangleStrip,
            PrimitiveMode.TriangleFan => PrimitiveType.TriangleFan,
            PrimitiveMode.Quads => PrimitiveType.Triangles, // handled beautifully via CPU triangulation indices
            _ => PrimitiveType.Triangles,
        };

        void* offset = (void*)(batch.IndexOffset * sizeof(ushort));
        _gl.DrawElements(glMode, (uint)batch.IndexCount, DrawElementsType.UnsignedShort, offset);
    }

    public TextureHandle CreateTexture(
        int width,
        int height,
        TextureFormat format,
        TextureFilter minFilter,
        TextureFilter magFilter,
        TextureWrap wrapS,
        TextureWrap wrapT,
        ReadOnlySpan<byte> pixelData
    )
    {
        uint id = _gl.GenTexture();
        BindTextureInternal(id, 0);

        (InternalFormat InternalFormat, PixelFormat PixelFormat, PixelType PixelType) mapping =
            GetFormatMapping(format);

        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)GetGlMinFilter(minFilter)
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)GetGlMagFilter(magFilter)
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)GetGlWrap(wrapS)
        );
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)GetGlWrap(wrapT)
        );

        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        fixed (byte* ptr = pixelData)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                mapping.InternalFormat,
                (uint)width,
                (uint)height,
                0,
                mapping.PixelFormat,
                mapping.PixelType,
                pixelData.IsEmpty ? null : ptr
            );
        }

        if (
            minFilter
            is TextureFilter.NearestMipmapNearest
                or TextureFilter.LinearMipmapNearest
                or TextureFilter.NearestMipmapLinear
                or TextureFilter.LinearMipmapLinear
        )
        {
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        _gl.BindTexture(TextureTarget.Texture2D, 0);
        return new TextureHandle(id);
    }

    public void UpdateTexture(
        TextureHandle texture,
        int xOffset,
        int yOffset,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        TextureFormat format
    )
    {
        BindTextureInternal(texture.Id, 0);
        (InternalFormat InternalFormat, PixelFormat PixelFormat, PixelType PixelType) mapping =
            GetFormatMapping(format);

        fixed (byte* ptr = pixelData)
        {
            _gl.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                xOffset,
                yOffset,
                (uint)width,
                (uint)height,
                mapping.PixelFormat,
                mapping.PixelType,
                ptr
            );
        }
    }

    public void SetTextureCompareMode(TextureHandle texture, KTextureCompareMode mode)
    {
        BindTextureInternal(texture.Id, 0);
        if (mode == KTextureCompareMode.CompareRefToTexture)
        {
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureCompareMode,
                (int)GLEnum.CompareRefToTexture
            );
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureCompareFunc,
                (int)DepthFunction.Lequal
            );
        }
        else
        {
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureCompareMode,
                (int)GLEnum.None
            );
        }
    }

    public void SetTextureBorderColor(TextureHandle texture, Vector4 color)
    {
        BindTextureInternal(texture.Id, 0);
        float[] c = [color.X, color.Y, color.Z, color.W];
        fixed (float* ptr = c)
        {
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, ptr);
        }
    }

    public RenderTarget CreateRenderTarget(
        int width,
        int height,
        ReadOnlySpan<TextureFormat> colorFormats,
        TextureFormat depthFormat,
        bool depthAsTexture
    )
    {
        uint fbo = _gl.GenFramebuffer();
        BindFramebufferInternal(fbo);

        TextureHandle[] colorTextures = new TextureHandle[colorFormats.Length];
        Span<DrawBufferMode> drawBuffers = stackalloc DrawBufferMode[colorFormats.Length];

        for (int i = 0; i < colorFormats.Length; i++)
        {
            uint tex = _gl.GenTexture();
            BindTextureInternal(tex, 0);

            (InternalFormat InternalFormat, PixelFormat PixelFormat, PixelType PixelType) mapping =
                GetFormatMapping(colorFormats[i]);

            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                mapping.InternalFormat,
                (uint)width,
                (uint)height,
                0,
                mapping.PixelFormat,
                mapping.PixelType,
                null
            );
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear
            );
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear
            );
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge
            );
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge
            );

            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0 + i,
                TextureTarget.Texture2D,
                tex,
                0
            );

            colorTextures[i] = new TextureHandle(tex);
            drawBuffers[i] = DrawBufferMode.ColorAttachment0 + i;
        }

        if (colorFormats.Length > 0)
        {
            fixed (DrawBufferMode* drawBuffersPtr = drawBuffers)
            {
                _gl.DrawBuffers((uint)colorFormats.Length, drawBuffersPtr);
            }
        }
        else
        {
            // nullify color writing for depth-only rendering configurations
            _gl.DrawBuffer(DrawBufferMode.None);
            _gl.ReadBuffer(ReadBufferMode.None);
        }

        uint rbo = 0;
        TextureHandle depthTexture = default;

        if (depthFormat != TextureFormat.None)
        {
            (InternalFormat InternalFormat, PixelFormat PixelFormat, PixelType PixelType) mapping =
                GetFormatMapping(depthFormat);
            FramebufferAttachment attachment =
                mapping.PixelFormat == PixelFormat.DepthStencil
                    ? FramebufferAttachment.DepthStencilAttachment
                    : FramebufferAttachment.DepthAttachment;

            if (depthAsTexture)
            {
                uint depthTex = _gl.GenTexture();
                BindTextureInternal(depthTex, 0);

                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    mapping.InternalFormat,
                    (uint)width,
                    (uint)height,
                    0,
                    mapping.PixelFormat,
                    mapping.PixelType,
                    null
                );
                _gl.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureMinFilter,
                    (int)TextureMinFilter.Nearest
                );
                _gl.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureMagFilter,
                    (int)TextureMagFilter.Nearest
                );
                _gl.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureWrapS,
                    (int)TextureWrapMode.ClampToBorder
                );
                _gl.TexParameter(
                    TextureTarget.Texture2D,
                    TextureParameterName.TextureWrapT,
                    (int)TextureWrapMode.ClampToBorder
                );

                float[] borderColor = [1.0f, 1.0f, 1.0f, 1.0f];
                fixed (float* borderPtr = borderColor)
                {
                    _gl.TexParameter(
                        TextureTarget.Texture2D,
                        TextureParameterName.TextureBorderColor,
                        borderPtr
                    );
                }

                _gl.FramebufferTexture2D(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    TextureTarget.Texture2D,
                    depthTex,
                    0
                );
                depthTexture = new TextureHandle(depthTex);
            }
            else
            {
                rbo = _gl.GenRenderbuffer();
                _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
                _gl.RenderbufferStorage(
                    RenderbufferTarget.Renderbuffer,
                    mapping.InternalFormat,
                    (uint)width,
                    (uint)height
                );
                _gl.FramebufferRenderbuffer(
                    FramebufferTarget.Framebuffer,
                    attachment,
                    RenderbufferTarget.Renderbuffer,
                    rbo
                );
            }
        }

        BindFramebufferInternal(0);
        return new RenderTarget(fbo, rbo, colorTextures, depthTexture, width, height);
    }

    public void SetRenderTarget(RenderTarget? target)
    {
        if (target.HasValue)
        {
            BindFramebufferInternal(target.Value.FboId);
            _gl.Viewport(0, 0, (uint)target.Value.Width, (uint)target.Value.Height);
        }
        else
        {
            BindFramebufferInternal(0);
            // TODO: the frontend API will need to restore the screen viewport size
        }
    }

    public void DeleteRenderTarget(RenderTarget target)
    {
        if (_cachedFbo == target.FboId)
            _cachedFbo = 0;

        _gl.DeleteFramebuffer(target.FboId);

        if (target.RboId != 0)
            _gl.DeleteRenderbuffer(target.RboId);

        if (target.ColorTextures != null)
        {
            foreach (TextureHandle tex in target.ColorTextures)
            {
                DeleteTexture(tex);
            }
        }

        if (target.DepthTexture.Id != 0)
        {
            DeleteTexture(target.DepthTexture);
        }
    }

    public ShaderHandle CreateShader(string vertexSrc, string fragmentSrc)
    {
        uint vs = CompileSingleShader(ShaderType.VertexShader, vertexSrc);
        uint fs = CompileSingleShader(ShaderType.FragmentShader, fragmentSrc);

        uint prog = _gl.CreateProgram();
        _gl.AttachShader(prog, vs);
        _gl.AttachShader(prog, fs);
        _gl.LinkProgram(prog);

        _gl.GetProgram(prog, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetProgramInfoLog(prog);
            throw new Exception($"Shader Link Failed: {log}");
        }

        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);

        return new ShaderHandle(prog);
    }

    private uint CompileSingleShader(ShaderType type, string src)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, src);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
        {
            string log = _gl.GetShaderInfoLog(shader);
            throw new Exception($"{type} Compilation Failed: {log}");
        }
        return shader;
    }

    private int GetUniformLocation(uint program, string name)
    {
        if (_uniformLocations.TryGetValue((program, name), out int loc))
            return loc;

        loc = _gl.GetUniformLocation(program, name);
        _uniformLocations[(program, name)] = loc;
        return loc;
    }

    public void SetUniformInt(ShaderHandle shader, string name, int value)
    {
        BindShaderInternal(shader.Id);
        int loc = GetUniformLocation(shader.Id, name);
        if (loc != -1)
            _gl.Uniform1(loc, value);
    }

    public void SetUniformFloat(ShaderHandle shader, string name, float value)
    {
        BindShaderInternal(shader.Id);
        int loc = GetUniformLocation(shader.Id, name);
        if (loc != -1)
            _gl.Uniform1(loc, value);
    }

    public void SetUniformVec2(ShaderHandle shader, string name, in Vector2 value)
    {
        BindShaderInternal(shader.Id);
        int loc = GetUniformLocation(shader.Id, name);
        if (loc != -1)
            _gl.Uniform2(loc, value.X, value.Y);
    }

    public void SetUniformVec3(ShaderHandle shader, string name, in Vector3 value)
    {
        BindShaderInternal(shader.Id);
        int loc = GetUniformLocation(shader.Id, name);
        if (loc != -1)
            _gl.Uniform3(loc, value.X, value.Y, value.Z);
    }

    public void SetUniformVec4(ShaderHandle shader, string name, in Vector4 value)
    {
        BindShaderInternal(shader.Id);
        int loc = GetUniformLocation(shader.Id, name);
        if (loc != -1)
            _gl.Uniform4(loc, value.X, value.Y, value.Z, value.W);
    }

    public void SetUniformMatrix4(ShaderHandle shader, string name, in Matrix4x4 matrix)
    {
        BindShaderInternal(shader.Id);
        int loc = GetUniformLocation(shader.Id, name);
        if (loc != -1)
        {
            fixed (float* ptr = &matrix.M11)
            {
                _gl.UniformMatrix4(loc, 1, false, ptr);
            }
        }
    }

    public void BindTexture(TextureHandle texture, int slot)
    {
        BindTextureInternal(texture.Id, slot);
    }

    public void BindShader(ShaderHandle shader)
    {
        BindShaderInternal(shader.Id);
    }

    public void DeleteTexture(TextureHandle texture)
    {
        for (int i = 0; i < _cachedTextures.Length; i++)
        {
            if (_cachedTextures[i] == texture.Id)
            {
                _cachedTextures[i] = 0;
            }
        }

        _gl.DeleteTexture(texture.Id);
    }

    #region Cache (Internal)

    private void BindVaoInternal(uint id)
    {
        if (_cachedVao != id)
        {
            _gl.BindVertexArray(id);
            _cachedVao = id;
        }
    }

    private void BindVboInternal(uint id)
    {
        if (_cachedVbo != id)
        {
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, id);
            _cachedVbo = id;
        }
    }

    private void BindEboInternal(uint id)
    {
        if (_cachedEbo != id)
        {
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, id);
            _cachedEbo = id;
        }
    }

    private void BindTextureInternal(uint id, int slot)
    {
        // switch the active hardware unit if needed
        if (_activeTextureSlot != slot)
        {
            _gl.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + slot));
            _activeTextureSlot = slot;
        }

        // bind the specific texture id to the current active unit if changed
        if (_cachedTextures[slot] != id)
        {
            _gl.BindTexture(TextureTarget.Texture2D, id);
            _cachedTextures[slot] = id;
        }
    }

    private void BindShaderInternal(uint id)
    {
        if (_cachedShader != id)
        {
            _gl.UseProgram(id);
            _cachedShader = id;
        }
    }

    private void BindFramebufferInternal(uint id)
    {
        if (_cachedFbo != id)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, id);
            _cachedFbo = id;
        }
    }

    #endregion
    #region Internals

    private static (
        InternalFormat InternalFormat,
        PixelFormat PixelFormat,
        PixelType PixelType
    ) GetFormatMapping(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8 => (InternalFormat.R8, PixelFormat.Red, PixelType.UnsignedByte),
            TextureFormat.Rg8 => (InternalFormat.RG8, PixelFormat.RG, PixelType.UnsignedByte),
            TextureFormat.Rgb8 => (InternalFormat.Rgb8, PixelFormat.Rgb, PixelType.UnsignedByte),
            TextureFormat.Rgba8 => (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
            TextureFormat.Rgba16F => (
                InternalFormat.Rgba16f,
                PixelFormat.Rgba,
                PixelType.HalfFloat
            ),
            TextureFormat.Rgba32F => (InternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.Float),
            TextureFormat.Depth16 => (
                InternalFormat.DepthComponent16,
                PixelFormat.DepthComponent,
                PixelType.UnsignedShort
            ),
            TextureFormat.Depth24 => (
                InternalFormat.DepthComponent24,
                PixelFormat.DepthComponent,
                PixelType.UnsignedInt
            ),
            TextureFormat.Depth32F => (
                InternalFormat.DepthComponent32f,
                PixelFormat.DepthComponent,
                PixelType.Float
            ),
            TextureFormat.Depth24Stencil8 => (
                InternalFormat.Depth24Stencil8,
                PixelFormat.DepthStencil,
                PixelType.UnsignedInt248
            ),
            _ => (InternalFormat.Rgba8, PixelFormat.Rgba, PixelType.UnsignedByte),
        };
    }

    private static TextureMinFilter GetGlMinFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest => TextureMinFilter.Nearest,
            TextureFilter.Linear => TextureMinFilter.Linear,
            TextureFilter.NearestMipmapNearest => TextureMinFilter.NearestMipmapNearest,
            TextureFilter.LinearMipmapNearest => TextureMinFilter.LinearMipmapNearest,
            TextureFilter.NearestMipmapLinear => TextureMinFilter.NearestMipmapLinear,
            TextureFilter.LinearMipmapLinear => TextureMinFilter.LinearMipmapLinear,
            _ => TextureMinFilter.Linear,
        };
    }

    private static TextureMagFilter GetGlMagFilter(TextureFilter filter)
    {
        return filter switch
        {
            TextureFilter.Nearest => TextureMagFilter.Nearest,
            _ => TextureMagFilter.Linear,
        };
    }

    private static TextureWrapMode GetGlWrap(TextureWrap wrap)
    {
        return wrap switch
        {
            TextureWrap.Repeat => TextureWrapMode.Repeat,
            TextureWrap.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            TextureWrap.ClampToEdge => TextureWrapMode.ClampToEdge,
            TextureWrap.ClampToBorder => TextureWrapMode.ClampToBorder,
            _ => TextureWrapMode.Repeat,
        };
    }

    #endregion

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }
}
