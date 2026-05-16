using Kogl.Abstractions;
using StbImageSharp;

namespace Kogl.Core.Resources;

public static class ResourceManager
{
    private static readonly Dictionary<string, Resource> _cache = [];

    /// <summary>
    /// Loads a resource of type T. If it's already loaded, returns the cached version.
    /// </summary>
    public static T Load<T>(string path)
        where T : Resource
    {
        if (_cache.TryGetValue(path, out Resource? existing))
        {
            return (T)existing;
        }

        T resource = typeof(T) switch
        {
            var t when t == typeof(Texture) => (T)(object)LoadTexture(path),
            _ => throw new NotSupportedException(
                $"Resource type {typeof(T).Name} is not supported yet."
            ),
        };

        resource.Path = path;
        resource.Name = Path.GetFileNameWithoutExtension(path);

        _cache[path] = resource;
        return resource;
    }

    private static Texture LoadTexture(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

        StbImage.stbi_set_flip_vertically_on_load(1);

        using FileStream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        TextureHandle handle = KoGL.GetBackend()
            .CreateTexture(image.Data, image.Width, image.Height, 4);
        return new Texture(handle, image.Width, image.Height);
    }

    /// <summary>
    /// Unloads a specific resource and disposes its GPU data.
    /// </summary>
    public static void Unload(string path)
    {
        if (_cache.Remove(path, out Resource? resource))
        {
            resource.Dispose();
        }
    }

    /// <summary>
    /// Clears the entire cache and disposes all resources.
    /// </summary>
    public static void UnloadAll()
    {
        foreach (Resource resource in _cache.Values)
        {
            resource.Dispose();
        }
        _cache.Clear();
    }
}
