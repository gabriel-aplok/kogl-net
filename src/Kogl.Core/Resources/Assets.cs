using System.Collections.Concurrent;
using Kogl.Common;

namespace Kogl.Core.Resources;

public static class Assets
{
    private static readonly ConcurrentDictionary<string, AssetEntry> _registry = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly ConcurrentDictionary<string, FileSystemWatcher> _watchers = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly Lock _lock = new();
    public static event Action<string, object>? OnAssetHotReloaded;

    /// <summary>Loads an asset</summary>
    public static T Load<T>(string path)
        where T : class
    {
        AssetEntry entry = GetOrCreateEntry(path, typeof(T));

        lock (_lock)
        {
            entry.ReferenceCount++;
            if (entry.IsLoaded && entry.AssetInstance != null)
            {
                return (T)entry.AssetInstance;
            }

            LoadInternal(entry);
            return (T)entry.AssetInstance!;
        }
    }

    /// <summary>Loads an asset asynchronously</summary>
    public static Task<T> LoadAsync<T>(string virtualPath)
        where T : class
    {
        AssetEntry entry = GetOrCreateEntry(virtualPath, typeof(T));

        lock (_lock)
        {
            entry.ReferenceCount++;
            if (entry.IsLoaded && entry.AssetInstance != null)
            {
                return Task.FromResult((T)entry.AssetInstance);
            }
        }

        return Task.Run(() =>
        {
            lock (_lock)
            {
                if (!entry.IsLoaded)
                {
                    LoadInternal(entry);
                }
                return (T)entry.AssetInstance!;
            }
        });
    }

    /// <summary>Lowers reference counts. If it reaches 0, structural cleanup invokes Dispose() on the graphics subsystem</summary>
    public static void Unload(string virtualPath)
    {
        lock (_lock)
        {
            if (!_registry.TryGetValue(virtualPath, out AssetEntry? entry))
                return;

            entry.ReferenceCount--;
            if (entry.ReferenceCount <= 0)
            {
                UnloadInternal(entry);
            }
        }
    }

    /// <summary>Clears the entire cache and disposes all resources</summary>
    public static void UnloadAll()
    {
        lock (_lock)
        {
            foreach (AssetEntry entry in _registry.Values)
            {
                Unload(entry.VirtualPath);
            }
        }
    }

    /// <summary>Reloads an asset</summary>
    public static void Reload(string virtualPath)
    {
        lock (_lock)
        {
            if (!_registry.TryGetValue(virtualPath, out AssetEntry? entry) || !entry.IsLoaded)
                return;

            LogCat.Info("ASSETS", $"Hot-Reloading asset: {virtualPath}");

            // retain old instance handle to clear safely after replacement
            object? oldInstance = entry.AssetInstance;

            try
            {
                // re-execute factory parsing matching asset structural types
                entry.AssetInstance = CreateAssetInstance(entry.AssetType, entry.PhysicalPath);

                // notify runtime listeners (TODO: Sprite sheets, active materials)
                OnAssetHotReloaded?.Invoke(entry.VirtualPath, entry.AssetInstance);

                if (oldInstance is IDisposable disposable)
                {
                    // dispose graphic resources across game context layers
                    disposable.Dispose();
                }

                // propagate updates through cascading dependency chains
                foreach (string dependentPath in entry.Dependents)
                {
                    Reload(dependentPath);
                }
            }
            catch (Exception ex)
            {
                LogCat.Error(
                    "ASSETS",
                    $"Failed hot-reload sequence on {virtualPath}: {ex.Message}"
                );
                entry.AssetInstance = oldInstance; // fallback gracefully
            }
        }
    }

    /// <summary>Clears the entire cache and disposes all resources</summary>
    public static void ReloadAll()
    {
        lock (_lock)
        {
            foreach (AssetEntry entry in _registry.Values)
            {
                Reload(entry.VirtualPath);
            }
        }
    }

    private static AssetEntry GetOrCreateEntry(string virtualPath, Type type)
    {
        return _registry.GetOrAdd(
            virtualPath,
            path =>
            {
                string physical = AssetPath.Resolve(path);
                SetupWatcher(path, physical);

                return new AssetEntry
                {
                    VirtualPath = path,
                    PhysicalPath = physical,
                    AssetType = type,
                };
            }
        );
    }

    private static void LoadInternal(AssetEntry entry)
    {
        try
        {
            // TODO: dependency discovery (like matching text manifests or materials to texture items)
            if (entry.AssetType == typeof(Material))
            {
                // simulation of registering cross-linking rules:
                // TrackDependency(entry.VirtualPath, "res://shaders/default.glsl");
            }

            entry.AssetInstance = CreateAssetInstance(entry.AssetType, entry.PhysicalPath);
            entry.IsLoaded = true;
            LogCat.Trace(
                "ASSETS",
                $"Loaded resource: {entry.VirtualPath} (Refs: {entry.ReferenceCount})"
            );
        }
        catch (Exception ex)
        {
            LogCat.Error(
                "ASSETS",
                $"Parsing failure inside runtime registry loading {entry.VirtualPath}: {ex.Message}"
            );
            throw;
        }
    }

    private static object CreateAssetInstance(Type type, string physicalPath)
    {
        if (type == typeof(Texture))
        {
            return ResourceManager.Load<Texture>(physicalPath);
        }
        if (type == typeof(Shader))
        {
            return ResourceManager.Load<Shader>(physicalPath);
        }
        if (type == typeof(Model))
        {
            return ResourceManager.Load<Model>(physicalPath);
        }

        throw new NotSupportedException(
            $"Asset system type loading rule not mapped for: {type.Name}"
        );
    }

    private static void UnloadInternal(AssetEntry entry)
    {
        if (entry.AssetInstance is IDisposable disposable)
        {
            disposable.Dispose();
        }

        entry.AssetInstance = null;
        entry.IsLoaded = false;
        LogCat.Trace("ASSETS", $"Evicted object from physical layout cache: {entry.VirtualPath}");

        // cascading unloads across orphaned dependencies
        foreach (string dependencyPath in entry.Dependencies)
        {
            if (_registry.TryGetValue(dependencyPath, out AssetEntry? depEntry))
            {
                depEntry.Dependents.Remove(entry.VirtualPath);
                Unload(dependencyPath);
            }
        }
    }

    private static void TrackDependency(string assetPath, string dependencyVirtualPath)
    {
        if (!_registry.TryGetValue(assetPath, out AssetEntry? entry))
            return;

        AssetEntry depEntry = GetOrCreateEntry(dependencyVirtualPath, typeof(Shader)); // structural lookup fallback

        if (!entry.Dependencies.Contains(dependencyVirtualPath))
        {
            entry.Dependencies.Add(dependencyVirtualPath);
        }
        if (!depEntry.Dependents.Contains(assetPath))
        {
            depEntry.Dependents.Add(assetPath);
        }
    }

    private static void SetupWatcher(string virtualPath, string physicalPath)
    {
        if (_watchers.ContainsKey(virtualPath))
            return;

        string? directory = Path.GetDirectoryName(physicalPath);
        string? filename = Path.GetFileName(physicalPath);

        if (directory == null || !Directory.Exists(directory) || filename == null)
            return;

        FileSystemWatcher watcher = new(directory, filename)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        // debounce structural double triggers common to filesystem adjustments
        DateTime lastWrite = DateTime.MinValue;
        watcher.Changed += (s, e) =>
        {
            if (DateTime.Now - lastWrite < TimeSpan.FromMilliseconds(200))
                return;
            lastWrite = DateTime.Now;

            // queue back to the execution system loop safely
            Reload(virtualPath);
        };

        _watchers.TryAdd(virtualPath, watcher);
    }
}
