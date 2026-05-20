using Kogl.Common;
using Kogl.Common.Types;
using StbImageSharp;

namespace Kogl.Core.Resources;

/// <summary>Manages the loading and unloading of resources</summary>
public static class ResourceManager
{
    private static readonly Dictionary<string, Resource> _cache = [];

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
            _ => throw new NotSupportedException(
                $"Resource type {typeof(T).Name} is not supported."
            ),
        };

        resource.Path = path;
        resource.Name = Path.GetFileNameWithoutExtension(path);

        _cache[path] = resource;
        return resource;
    }

    /// <summary>
    /// Compiles a shader instance explicitly from raw string sources. This instance will not be added to the disk cache automatically.
    /// </summary>
    public static Shader LoadShader(string name, string vertexSource, string fragmentSource)
    {
        ShaderHandle handle = KoRender.GetBackend().CreateShader(vertexSource, fragmentSource);
        Shader shader = new(handle);
        shader.Name = name;
        shader.Path = string.Empty;
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

            Log.Info("SHADER", $"Loaded unified shader: {path}");
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

            Log.Info("SHADER", $"Loaded vertex shader: {path}");
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

            Log.Info("SHADER", $"Loaded fragment shader: {path}");
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

        System.Text.StringBuilder vertBuilder = new();
        System.Text.StringBuilder fragBuilder = new();
        System.Text.StringBuilder? currentBuilder = null;

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
}
