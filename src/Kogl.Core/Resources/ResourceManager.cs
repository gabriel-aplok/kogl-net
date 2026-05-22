using System.IO.Hashing;
using System.Text;
using Kogl.Common.Types;
using StbImageSharp;

namespace Kogl.Core.Resources;

/// <summary>Manages the loading and unloading of resources</summary>
public static class ResourceManager
{
    private static readonly Dictionary<string, Resource> _cache = [];
    private static readonly UTF8Encoding _safeUtf8 = new(false, false);

    #region API

    /// <summary>Loads a resource of type T. If it's already loaded, returns the cached version</summary>
    public static T Load<T>(string path)
        where T : Resource
    {
        if (_cache.TryGetValue(path, out Resource? existing))
            return (T)existing;

        T resource = typeof(T) switch
        {
            var t when t == typeof(Texture) => (T)(object)LoadTexture(path),
            var t when t == typeof(Shader) => (T)(object)LoadShaderFromFile(path),
            var t when t == typeof(Model) => (T)(object)ModelParser.OBJFileLoader.Load(path),
            _ => throw new NotSupportedException(
                $"Resource type {typeof(T).Name} is not supported."
            ),
        };

        resource.UniqueId = GetId(path);
        resource.Name = Path.GetFileNameWithoutExtension(path);
        resource.Path = path;

        _cache[path] = resource;
        return resource;
    }

    /// <summary>Unloads a specific resource and disposes its GPU data</summary>
    public static void Unload(string path)
    {
        if (_cache.Remove(path, out Resource? resource))
            resource.Dispose();
    }

    /// <summary>Clears the entire cache and disposes all resources</summary>
    public static void UnloadAll()
    {
        foreach (Resource resource in _cache.Values)
            resource.Dispose();

        _cache.Clear();
    }

    /// <summary>Generates 64-bit asset id from a unique path.</summary>
    public static ulong GetId(ReadOnlySpan<char> assetPath)
    {
        if (assetPath.IsEmpty)
            return 0;

        // sanitize & normalize paths into a fixed stack buffer (up to 512 characters)
        int pathLength = assetPath.Length;

        // use stack memory
        Span<char> normalizedChars =
            pathLength <= 512 ? (stackalloc char[512])[..pathLength] : new char[pathLength];

        for (int i = 0; i < pathLength; i++)
        {
            char c = assetPath[i];

            // normalize slashes
            if (c == '\\')
                c = '/';

            // enforce lowercase for case-insensitivity
            normalizedChars[i] = char.ToLowerInvariant(c);
        }

        // transcode normalized chars to utf-8 bytes
        int maxByteCount = _safeUtf8.GetMaxByteCount(pathLength);
        Span<byte> utf8Bytes =
            maxByteCount <= 1024 ? (stackalloc byte[1024])[..maxByteCount] : new byte[maxByteCount];

        int actualByteCount = _safeUtf8.GetBytes(normalizedChars, utf8Bytes);
        Span<byte> finalPayload = utf8Bytes[..actualByteCount];

        // compute the 64-bit hash
        return XxHash3.HashToUInt64(finalPayload);
    }

    #endregion
    #region Shaders

    /// <summary>
    /// Compiles a shader instance explicitly from raw string sources. This instance will not be added to the disk cache automatically.
    /// </summary>
    public static Shader LoadShader(string name, string vertexSource, string fragmentSource)
    {
        ShaderHandle handle = KoRender.CreateShader(vertexSource, fragmentSource);
        Shader shader = new(handle) { Name = name, Path = string.Empty };
        return shader;
    }

    private static Shader LoadShaderFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shader source file not found: {path}");

        string extension = Path.GetExtension(path).ToLowerInvariant();
        string vertexSource;
        string fragmentSource;

        if (extension == ".glsl" || extension == ".shader")
        {
            // unified shader
            string fullSource = File.ReadAllText(path);
            (vertexSource, fragmentSource) = ParseUnifiedShaderSource(fullSource);
        }
        else if (extension == ".vert" || extension == ".vs")
        {
            // vertex shader
            vertexSource = File.ReadAllText(path);
            string fragPath = Path.ChangeExtension(path, ".frag");
            if (!File.Exists(fragPath))
                fragPath = Path.ChangeExtension(path, ".fs");

            if (!File.Exists(fragPath))
                throw new FileNotFoundException(
                    $"Matching fragment shader counterpart not found for vertex target: {path}"
                );

            fragmentSource = File.ReadAllText(fragPath);
        }
        else if (extension == ".frag" || extension == ".fs")
        {
            // fragment shader
            fragmentSource = File.ReadAllText(path);
            string vertPath = Path.ChangeExtension(path, ".vert");
            if (!File.Exists(vertPath))
                vertPath = Path.ChangeExtension(path, ".vs");

            if (!File.Exists(vertPath))
                throw new FileNotFoundException(
                    $"Matching vertex shader counterpart not found for fragment target: {path}"
                );

            vertexSource = File.ReadAllText(vertPath);
        }
        else
        {
            throw new NotSupportedException(
                $"Unrecognized shader file format extension: {extension}"
            );
        }

        ShaderHandle handle = KoRender.GetBackend().CreateShader(vertexSource, fragmentSource);
        return new Shader(handle);
    }

    private static (string vertex, string fragment) ParseUnifiedShaderSource(string fullSource)
    {
        string[] lines = fullSource.Split(['\r', '\n'], StringSplitOptions.None);

        StringBuilder vertBuilder = new();
        StringBuilder fragBuilder = new();
        StringBuilder? currentBuilder = null;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("#type", StringComparison.OrdinalIgnoreCase))
            {
                string stage = trimmed["#type".Length..].Trim().ToLowerInvariant();
                if (stage == "vertex" || stage == "vert" || stage == "vs")
                {
                    currentBuilder = vertBuilder;
                }
                else if (stage == "fragment" || stage == "frag" || stage == "fs")
                {
                    currentBuilder = fragBuilder;
                }
                else
                {
                    currentBuilder = null;
                }
                continue;
            }

            currentBuilder?.AppendLine(line);
        }

        string vertexResult = vertBuilder.ToString();
        string fragmentResult = fragBuilder.ToString();

        if (string.IsNullOrWhiteSpace(vertexResult))
            throw new InvalidDataException(
                "Parsed unified shader does not contain a valid '#type vertex' block."
            );
        if (string.IsNullOrWhiteSpace(fragmentResult))
            throw new InvalidDataException(
                "Parsed unified shader does not contain a valid '#type fragment' block."
            );

        return (vertexResult, fragmentResult);
    }

    #endregion
    #region Textures

    private static Texture LoadTexture(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

        StbImage.stbi_set_flip_vertically_on_load(1);

        using FileStream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        TextureFormat format =
            image.Comp == ColorComponents.RedGreenBlueAlpha
                ? TextureFormat.Rgba8
                : TextureFormat.Rgb8;

        TextureHandle handle = KoRender
            .GetBackend()
            .CreateTexture(
                image.Width,
                image.Height,
                format,
                TextureFilter.NearestMipmapNearest,
                TextureFilter.Nearest,
                TextureWrap.Repeat,
                TextureWrap.Repeat,
                image.Data
            );

        return new Texture(handle, image.Width, image.Height);
    }

    #endregion
}
