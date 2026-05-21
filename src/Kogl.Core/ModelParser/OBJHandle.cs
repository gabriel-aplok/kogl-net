using System.Numerics;

namespace Kogl.Core.ModelParser;

// I just got it from my software renderer, so I will port it to the framework internals soon.
// ref: https://github.com/gabriel-aplok/csharp-software-renderer-3d

internal class Vertex : IVertex
{
    public Vector3 Position { get; set; }
    public Vector3 Normal { get; set; }
    public Vector2 UV { get; set; }
}

internal record Material(int Index, string Name) : IMaterial;

internal class Cluster(int materialIndex, int startIndex) : ICluster
{
    public int MaterialIndex { get; } = materialIndex;
    public int StartIndex { get; } = startIndex;
    public int IndexCount { get; set; } = 0;
}

internal class OBJHandle : IOBJHandle
{
    public IVertex[] Vertices { get; set; } = [];
    public int[] Indices { get; set; } = [];
    public ICluster[] Clusters { get; set; } = [];
    public IMaterial[] Materials { get; set; } = [];
}
