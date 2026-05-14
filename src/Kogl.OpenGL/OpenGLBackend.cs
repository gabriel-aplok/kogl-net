using Kogl.Abstractions;
using Silk.NET.OpenGL;

namespace Kogl.OpenGL;

public sealed unsafe class OpenGLBackend(GL glContext) : IGraphicsBackend
{
    private readonly GL _gl = glContext;
    private uint _vao,
        _vbo,
        _ebo;

    public void Initialize()
    {
        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        // pre-allocate buffer
        _gl.BufferData(
            BufferTargetARB.ArrayBuffer,
            (nuint)(8192 * sizeof(VertexData)),
            null,
            BufferUsageARB.DynamicDraw
        );

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData(
            BufferTargetARB.ElementArrayBuffer,
            (nuint)(8192 * 6 * sizeof(ushort)),
            null,
            BufferUsageARB.DynamicDraw
        );

        // position
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(VertexData),
            (void*)0
        );
        // texCoord
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(
            1,
            2,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(VertexData),
            (void*)12
        );
        // color
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(
            2,
            4,
            VertexAttribPointerType.Float,
            false,
            (uint)sizeof(VertexData),
            (void*)20
        );

        _gl.BindVertexArray(0);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    public void UpdateVertexBuffer(ReadOnlySpan<VertexData> vertices)
    {
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
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
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
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
        _gl.BindVertexArray(_vao);
        _gl.BindTexture(TextureTarget.Texture2D, batch.Texture.Id);
        _gl.UseProgram(batch.Shader.Id);

        PrimitiveType glMode = batch.Mode switch
        {
            PrimitiveMode.Lines => PrimitiveType.Lines,
            PrimitiveMode.LineStrip => PrimitiveType.LineStrip,
            PrimitiveMode.Triangles => PrimitiveType.Triangles,
            PrimitiveMode.TriangleStrip => PrimitiveType.TriangleStrip,
            PrimitiveMode.TriangleFan => PrimitiveType.TriangleFan,
            PrimitiveMode.Quads => PrimitiveType.Triangles, // mapped via indices
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

    public ShaderHandle CreateShader(string vertexSrc, string fragmentSrc)
    {
        uint vs = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vs, vertexSrc);
        _gl.CompileShader(vs);

        uint fs = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fs, fragmentSrc);
        _gl.CompileShader(fs);

        uint prog = _gl.CreateProgram();
        _gl.AttachShader(prog, vs);
        _gl.AttachShader(prog, fs);
        _gl.LinkProgram(prog);

        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);

        return new ShaderHandle(prog);
    }

    public void SetUniformMatrix4(
        ShaderHandle shader,
        string name,
        in System.Numerics.Matrix4x4 matrix
    )
    {
        _gl.UseProgram(shader.Id);
        int loc = _gl.GetUniformLocation(shader.Id, name);
        if (loc != -1)
        {
            fixed (System.Numerics.Matrix4x4* ptr = &matrix)
            {
                _gl.UniformMatrix4(loc, 1, false, (float*)ptr);
            }
        }
    }

    public void Clear(float r, float g, float b, float a)
    {
        _gl.ClearColor(r, g, b, a);
        _gl.Clear((uint)ClearBufferMask.ColorBufferBit);
    }

    public void SetViewport(int x, int y, int w, int h)
    {
        _gl.Viewport(x, y, (uint)w, (uint)h);
    }

    public void BindTexture(TextureHandle texture)
    {
        _gl.BindTexture(TextureTarget.Texture2D, texture.Id);
    }

    public void BindShader(ShaderHandle shader)
    {
        _gl.UseProgram(shader.Id);
    }

    public void DeleteTexture(TextureHandle texture)
    {
        _gl.DeleteTexture(texture.Id);
    }

    public void Dispose()
    {
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }
}
