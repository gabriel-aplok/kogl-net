namespace Kogl.Core.Resources;

public static class AssetPath
{
    private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>Resolves a virtual engine path into an absolute local filesystem path</summary>
    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
        {
            string localPart = path["res://".Length..].Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(_rootPath, "assets", localPart));
        }

        return Path.GetFullPath(path);
    }
}
