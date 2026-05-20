namespace Kogl.Core.Resources;

public static class AssetPath
{
    private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>Resolves a virtual engine path into an absolute local filesystem path.</summary>
    public static string Resolve(string virtualPath)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
            return string.Empty;

        if (virtualPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            string localPart = virtualPath["res://".Length..]
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(_rootPath, "assets", localPart));
        }

        return Path.GetFullPath(virtualPath);
    }
}
