namespace Kogl.Core.Resources;

internal sealed class AssetEntry
{
    public required string VirtualPath { get; init; }
    public required string PhysicalPath { get; init; }
    public required Type AssetType { get; init; }
    public object? AssetInstance { get; set; }
    public int ReferenceCount { get; set; }
    public List<string> Dependencies { get; } = [];
    public List<string> Dependents { get; } = [];
    public bool IsLoaded { get; set; }
}
