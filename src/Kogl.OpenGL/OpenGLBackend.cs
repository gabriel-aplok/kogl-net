using System.Numerics;
using Kogl.Abstractions;
using Silk.NET.OpenGL;

namespace Kogl.OpenGL;

/// <summary>
/// The OpenGL backend
/// </summary>
/// <param name="glContext">The gl context</param>
// TODO: add docs, reorganize and cleanup
public sealed unsafe class OpenGLBackend(GL glContext) : IGraphicsBackend
{
    private readonly GL _gl = glContext;
    private uint _vao,
        _vbo,
        _ebo;

    private uint _cachedVao;
    private uint _cachedVbo;
    private uint _cachedEbo;
    private uint _cachedTexture;
    private uint _cachedShader;
    private uint _cachedFbo;

    private readonly Dictionary<(uint, string), int> _uniformLocations = [];

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
        BindTextureInternal(batch.Texture.Id);
        BindShaderInternal(batch.Shader.Id);

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
        _gl.BindTexture(TextureTarget.Texture2D, id);

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
        BindTextureInternal(texture.Id);

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

    public RenderTarget CreateRenderTarget(int width, int height)
    {
        // generate fbo
        uint fbo = _gl.GenFramebuffer();
        BindFramebufferInternal(fbo);

        // generate and bind the Texture
        uint tex = _gl.GenTexture();
        BindTextureInternal(tex);

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

        // attach texture to fbo
        _gl.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D,
            tex,
            0
        );

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

        // if (
        //     _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer)
        //     != FramebufferErrorCode.FramebufferComplete
        // )
        // {
        //     throw new Exception("Framebuffer is not complete!");
        // }

        BindFramebufferInternal(0); // ynbind

        return new RenderTarget(fbo, rbo, new TextureHandle(tex), width, height);
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
        DeleteTexture(target.Texture);
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

    public void Clear(float r, float g, float b, float a)
    {
        _gl.ClearColor(r, g, b, a);
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void SetViewport(int x, int y, int w, int h)
    {
        _gl.Viewport(x, y, (uint)w, (uint)h);
    }

    public void BindTexture(TextureHandle texture)
    {
        BindTextureInternal(texture.Id);
    }

    public void BindShader(ShaderHandle shader)
    {
        BindShaderInternal(shader.Id);
    }

    public void DeleteTexture(TextureHandle texture)
    {
        if (_cachedTexture == texture.Id)
        {
            _cachedTexture = 0;
        }
        _gl.DeleteTexture(texture.Id);
    }

    public void SetDepthTest(bool enabled)
    {
        if (enabled)
            _gl.Enable(EnableCap.DepthTest);
        else
            _gl.Disable(EnableCap.DepthTest);
    }

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

    public void SetScissor(int x, int y, int width, int height)
    {
        _gl.Scissor(x, y, (uint)width, (uint)height);
    }

    public void SetScissorEnabled(bool enabled)
    {
        if (enabled)
            _gl.Enable(EnableCap.ScissorTest);
        else
            _gl.Disable(EnableCap.ScissorTest);
    }

    // ==========================================
    // Cache
    // ==========================================

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

    private void BindTextureInternal(uint id)
    {
        if (_cachedTexture != id)
        {
            _gl.BindTexture(TextureTarget.Texture2D, id);
            _cachedTexture = id;
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

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }
}
