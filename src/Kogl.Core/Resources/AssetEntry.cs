namespace Kogl.Core.Resources;

/// <summary>A loaded asset in the registry</summary>
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
