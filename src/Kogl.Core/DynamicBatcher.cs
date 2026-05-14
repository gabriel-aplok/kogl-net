using System.Numerics;
using Kogl.Abstractions;

namespace Kogl.Core;

internal class DynamicBatcher(IGraphicsBackend backend)
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

    public void Begin(PrimitiveMode mode, TextureHandle tex, ShaderHandle shader)
    {
        if (_isBuildingBatch)
            Flush();

        _currentMode = mode;
        _currentTexture = tex;
        _currentShader = shader;
        _isBuildingBatch = true;

        StartNewBatch();
    }

    public void AddVertex(Vector3 position, Vector2 uv, Vector4 color, in Matrix4x4 transform)
    {
        if (_vertexCount >= _maxVertices - 4)
            Flush(); // prevent overflow

        // apply CPU transformation (SIMD accelerated)
        Vector3 transformed = Vector3.Transform(position, transform);

        _vertices[_vertexCount++] = new VertexData(transformed, uv, color);
    }

    public void End()
    {
        if (!_isBuildingBatch)
            return;

        ref RenderBatch current = ref _batches[_batchCount - 1];

        // triangulate quads
        if (_currentMode == PrimitiveMode.Quads)
        {
            int quadCount = _vertexCount / 4;
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
            current.IndexCount = _indexCount - current.IndexOffset;
        }
        else
        {
            // linear indexing for other types
            for (int i = current.VertexOffset; i < _vertexCount; i++)
            {
                _indices[_indexCount++] = (ushort)i;
            }
            current.IndexCount = _indexCount - current.IndexOffset;
        }

        current.VertexCount = _vertexCount - current.VertexOffset;
        _isBuildingBatch = false;
    }

    public void Flush()
    {
        if (_batchCount == 0 || _vertexCount == 0)
            return;

        _backend.UpdateVertexBuffer(new ReadOnlySpan<VertexData>(_vertices, 0, _vertexCount));
        _backend.UpdateIndexBuffer(new ReadOnlySpan<ushort>(_indices, 0, _indexCount));

        for (int i = 0; i < _batchCount; i++)
        {
            _backend.DrawBatch(in _batches[i]);
        }

        _vertexCount = 0;
        _indexCount = 0;
        _batchCount = 0;
    }

    private void StartNewBatch()
    {
        if (_batchCount >= _maxBatches)
            Flush();

        _batches[_batchCount] = new RenderBatch
        {
            Mode = _currentMode,
            Texture = _currentTexture,
            Shader = _currentShader,
            VertexOffset = _vertexCount,
            IndexOffset = _indexCount,
        };
        _batchCount++;
    }
}
