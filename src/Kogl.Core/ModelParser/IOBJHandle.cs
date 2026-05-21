using System.Numerics;

namespace Kogl.Core.ModelParser;

// I just got it from my software renderer, so I will port it to the framework internals soon.
// ref: https://github.com/gabriel-aplok/csharp-software-renderer-3d

public interface IVertex
{
    public Vector3 Position { get; }
    public Vector3 Normal { get; }
    public Vector2 UV { get; }
}

public interface IMaterial
{
    public string Name { get; }
}

public interface ICluster
{
    public int MaterialIndex { get; }
    public int StartIndex { get; }
    public int IndexCount { get; }
}

public interface IOBJHandle
{
    public IVertex[] Vertices { get; }
    public int[] Indices { get; }
    public ICluster[] Clusters { get; }
    public IMaterial[] Materials { get; }
}
