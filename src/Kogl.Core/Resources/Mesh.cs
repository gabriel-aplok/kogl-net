using Kogl.Common.Types;

namespace Kogl.Core.Resources;

/// <summary>Static geo data buffered directly into GPU memory.</summary>
public class Mesh(MeshHandle handle, int indexCount) : Resource
{
    public MeshHandle Handle { get; } = handle;
    public int IndexCount { get; } = indexCount;

    protected override void DisposeManaged()
    {
        // KoRender.GetBackend().DeleteMesh(Handle);
    }
}
