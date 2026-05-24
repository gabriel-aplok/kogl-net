using System.Numerics;
using Kogl.Common.Agnostics;
using Kogl.Common.Types;

namespace Kogl.Core.Rendering;

internal class Batcher(IGraphicsBackend backend)
{
    private const int _maxVertices = 8192;
    private const int _maxIndices = _maxVertices * 6;
    private const int _maxBatches = 2048;

    private readonly IGraphicsBackend _backend = backend;
    private readonly VertexData[] _vertices = new VertexData[_maxVertices];
    private readonly ushort[] _indices = new ushort[_maxIndices];
    private readonly RenderBatch[] _batches = new RenderBatch[_maxBatches];

    private int _vertexCount;
    private int _indexCount;
    private int _batchCount;

    private PrimitiveMode _currentMode;
    private TextureSet _currentTextures;
    private ShaderHandle _currentShader;

    private bool _isBuildingBatch;

    /// <summary>Begins rendering</summary>
    public void Begin(PrimitiveMode mode, in TextureSet textures, ShaderHandle shader)
    {
        if (_isBuildingBatch)
        {
            if (
                _currentMode == mode
                && _currentTextures == textures
                && _currentShader.Id == shader.Id
            )
            {
                return;
            }
            End();
        }

        _currentMode = mode;
        _currentTextures = textures;
        _currentShader = shader;
        _isBuildingBatch = true;
        StartNewBatch();
    }

    /// <summary>Adds a vertex</summary>
    public void AddVertex(
        Vector3 position,
        Vector2 uv,
        Vector4 color,
        Vector3 normal,
        Vector4 tangent,
        in Matrix4x4 modelViewMatrix
    )
    {
        if (_vertexCount >= _maxVertices - 4 || _indexCount >= _maxIndices - 6)
        {
            PrimitiveMode savedMode = _currentMode;
            TextureSet savedTextures = _currentTextures;
            ShaderHandle savedShader = _currentShader;

            End();
            Flush(KoRender.GetViewProjectionMatrix());

            Begin(savedMode, in savedTextures, savedShader);
        }

        Vector3 transformedPosition = Vector3.Transform(position, modelViewMatrix);
        Vector3 transformedNormal = Vector3.TransformNormal(normal, modelViewMatrix);
        Vector4 transformedTangent = new(
            Vector3.TransformNormal(tangent.AsVector3(), modelViewMatrix),
            tangent.W
        );

        _vertices[_vertexCount++] = new VertexData(
            transformedPosition,
            uv,
            color,
            transformedNormal,
            transformedTangent
        );
    }

    /// <summary>Ends the current batch</summary>
    public void End()
    {
        if (!_isBuildingBatch)
            return;

        if (_batchCount == 0)
            return;

        ref RenderBatch current = ref _batches[_batchCount - 1];
        int batchVertexCount = _vertexCount - current.VertexOffset;

        if (batchVertexCount <= 0)
        {
            _batchCount--;
            _isBuildingBatch = false;
            return;
        }

        if (_currentMode == PrimitiveMode.Quads)
        {
            int quadCount = batchVertexCount / 4;
            for (int i = 0; i < quadCount; i++)
            {
                int vo = current.VertexOffset + (i * 4);

                _indices[_indexCount++] = (ushort)(vo + 0);
                _indices[_indexCount++] = (ushort)(vo + 1);
                _indices[_indexCount++] = (ushort)(vo + 2);
                _indices[_indexCount++] = (ushort)(vo + 0);
                _indices[_indexCount++] = (ushort)(vo + 2);
                _indices[_indexCount++] = (ushort)(vo + 3);
            }
        }
        else
        {
            for (int i = current.VertexOffset; i < _vertexCount; i++)
            {
                _indices[_indexCount++] = (ushort)i;
            }
        }

        current.VertexCount = batchVertexCount;
        current.IndexCount = _indexCount - current.IndexOffset;
        _isBuildingBatch = false;
    }

    /// <summary>Flushes the current batch</summary>
    public void Flush(in Matrix4x4 viewProjectionMatrix)
    {
        if (_isBuildingBatch)
            End();

        if (_batchCount == 0 || _vertexCount == 0)
            return;

        _backend.UpdateVertexBuffer(new ReadOnlySpan<VertexData>(_vertices, 0, _vertexCount));
        _backend.UpdateIndexBuffer(new ReadOnlySpan<ushort>(_indices, 0, _indexCount));

        ShaderHandle? lastBoundShader = null;

        for (int i = 0; i < _batchCount; i++)
        {
            ref readonly RenderBatch batch = ref _batches[i];

            // check against localized context execution boundaries before uniform updates
            if (lastBoundShader == null || lastBoundShader.Value.Id != batch.Shader.Id)
            {
                _backend.SetUniformMatrix4x4(batch.Shader, "uMVP", viewProjectionMatrix);
                lastBoundShader = batch.Shader;
            }

            _backend.DrawBatch(in batch);
        }

        _vertexCount = 0;
        _indexCount = 0;
        _batchCount = 0;
    }

    /// <summary>Starts a new batch</summary>
    private void StartNewBatch()
    {
        if (_batchCount >= _maxBatches)
        {
            Flush(KoRender.GetViewProjectionMatrix());
        }

        _batches[_batchCount] = new RenderBatch
        {
            Mode = _currentMode,
            Textures = _currentTextures,
            Shader = _currentShader,
            VertexOffset = _vertexCount,
            IndexOffset = _indexCount,
            VertexCount = 0,
            IndexCount = 0,
            LineWidth = 1.0f,
        };
        _batchCount++;
    }
}
