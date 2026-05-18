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
        ReadOnlySpan<byte> pixelData,
        int width,
        int height,
        int channels
    )
    {
        uint id = _gl.GenTexture();
        BindTextureInternal(id, 0);

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

        InternalFormat format = channels == 4 ? InternalFormat.Rgba : InternalFormat.Rgb;
        PixelFormat pxFormat = channels == 4 ? PixelFormat.Rgba : PixelFormat.Rgb;

        fixed (byte* ptr = pixelData)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                format,
                (uint)width,
                (uint)height,
                0,
                pxFormat,
                PixelType.UnsignedByte,
                ptr
            );
        }

        return new TextureHandle(id);
    }

    public void UpdateTexture(
        TextureHandle texture,
        int xOffset,
        int yOffset,
        int width,
        int height,
        ReadOnlySpan<byte> pixelData,
        int channels
    )
    {
        BindTextureInternal(texture.Id, 0);

        PixelFormat pxFormat = channels == 4 ? PixelFormat.Rgba : PixelFormat.Rgb;
        fixed (byte* ptr = pixelData)
        {
            _gl.TexSubImage2D(
                TextureTarget.Texture2D,
                0,
                xOffset,
                yOffset,
                (uint)width,
                (uint)height,
                pxFormat,
                PixelType.UnsignedByte,
                ptr
            );
        }
    }

    public RenderTarget CreateRenderTarget(int width, int height, int colorAttachments = 1)
    {
        if (colorAttachments < 1)
            throw new ArgumentOutOfRangeException(
                nameof(colorAttachments),
                "Must have at least 1 color attachment."
            );

        // generate fbo
        uint fbo = _gl.GenFramebuffer();
        BindFramebufferInternal(fbo);

        TextureHandle[] textures = new TextureHandle[colorAttachments];
        Span<DrawBufferMode> drawBuffers = stackalloc DrawBufferMode[colorAttachments];

        for (int i = 0; i < colorAttachments; i++)
        {
            // generate and bind the texture
            uint tex = _gl.GenTexture();

            BindTextureInternal(tex, 0);

            // allocate empty texture memory
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba,
                (uint)width,
                (uint)height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                (void*)0
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

            // attach texture to fbo dynamically offset by current iteration
            _gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                (FramebufferAttachment)((int)FramebufferAttachment.ColorAttachment0 + i),
                TextureTarget.Texture2D,
                tex,
                0
            );

            textures[i] = new TextureHandle(tex);
            drawBuffers[i] = (DrawBufferMode)((int)DrawBufferMode.ColorAttachment0 + i);
        }

        // register multi-draw attachments to the gl pipeline
        fixed (DrawBufferMode* drawBuffersPtr = drawBuffers)
        {
            _gl.DrawBuffers((uint)colorAttachments, drawBuffersPtr);
        }

        // generate renderbuffer for depth/stencil (required if draw 3d inside the fbo)
        uint rbo = _gl.GenRenderbuffer();

        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
        _gl.RenderbufferStorage(
            RenderbufferTarget.Renderbuffer,
            InternalFormat.Depth24Stencil8,
            (uint)width,
            (uint)height
        );

        _gl.FramebufferRenderbuffer(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthStencilAttachment,
            RenderbufferTarget.Renderbuffer,
            rbo
        );

        BindFramebufferInternal(0);

        return new RenderTarget(fbo, rbo, textures, width, height);
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
        _gl.DeleteRenderbuffer(target.RboId);

        // delete all dynamically generated textures attached to this FBO
        if (target.Textures != null)
        {
            foreach (TextureHandle tex in target.Textures)
            {
                DeleteTexture(tex);
            }
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

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }
}
