using System.Numerics;
using Kogl.Abstractions;

namespace Kogl.Core;

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
    private TextureHandle _currentTexture;
    private ShaderHandle _currentShader;

    private bool _isBuildingBatch;

    public void Begin(PrimitiveMode mode, TextureHandle texture, ShaderHandle shader)
    {
        if (_isBuildingBatch)
        {
            if (
                _currentMode == mode
                && _currentTexture.Id == texture.Id
                && _currentShader.Id == shader.Id
            )
            {
                return;
            }
            End();
        }

        _currentMode = mode;
        _currentTexture = texture;
        _currentShader = shader;
        _isBuildingBatch = true;
        StartNewBatch();
    }

    public void AddVertex(Vector3 position, Vector2 uv, Vector4 color, in Matrix4x4 modelViewMatrix)
    {
        // safe preventative flush boundary to prevent array overflows on tight loop thresholds
        if (_vertexCount >= _maxVertices - 4 || _indexCount >= _maxIndices - 6)
        {
            PrimitiveMode savedMode = _currentMode;
            TextureHandle savedTexture = _currentTexture;
            ShaderHandle savedShader = _currentShader;

            End();
            Flush(RenderApi.GetProjectionMatrix());

            // transparently restart accumulation pipeline state for remaining primitives
            Begin(savedMode, savedTexture, savedShader);
        }

        // apply CPU-side SIMD transform matrix projection
        Vector3 transformedPosition = Vector3.Transform(position, modelViewMatrix);

        _vertices[_vertexCount++] = new VertexData(transformedPosition, uv, color);
    }

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
            // drop empty context generations safely to keep clean command queues
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

    public void Flush(in Matrix4x4 projectionMatrix)
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
                _backend.SetUniformMatrix4(batch.Shader, "uMVP", projectionMatrix);
                lastBoundShader = batch.Shader;
            }

            _backend.DrawBatch(in batch);
        }

        _vertexCount = 0;
        _indexCount = 0;
        _batchCount = 0;
    }

    private void StartNewBatch()
    {
        if (_batchCount >= _maxBatches)
        {
            Flush(RenderApi.GetProjectionMatrix());
        }

        _batches[_batchCount] = new RenderBatch
        {
            Mode = _currentMode,
            Texture = _currentTexture,
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
