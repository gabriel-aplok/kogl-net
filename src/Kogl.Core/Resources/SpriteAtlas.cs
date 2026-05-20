using System.Numerics;
using Kogl.Abstractions.Types;

namespace Kogl.Core.Resources;

/// <summary>Atlas container that maps localized sprite IDs to precise regions inside underlying texture sheets.</summary>
public class SpriteAtlas : IDisposable
{
    private readonly Dictionary<string, SpriteRegion> _regions = new(StringComparer.Ordinal);
    private readonly List<TextureHandle> _pages = [];

    /// <summary>Retrieves a sprite config region registered within the atlas pages.</summary>
    public SpriteRegion Get(string regionName)
    {
        if (_regions.TryGetValue(regionName, out SpriteRegion region))
        {
            return region;
        }

        Log.Warn(
            "SPRITE",
            $"Sprite region '{regionName}' not found in atlas. Returning fallback empty region."
        );
        return default;
    }

    /// <summary>Explicitly registers a pre-calculated sub-texture region within this atlas.</summary>
    public void RegisterRegion(string name, in SpriteRegion region)
    {
        _regions[name] = region;
        if (!_pages.Contains(region.Texture))
        {
            _pages.Add(region.Texture);
        }
    }

    /// <summary>Utility helper to create a region using pixel metrics relative to a specific texture sheet.</summary>
    public void AddPixelRegion(
        string name,
        TextureHandle texture,
        int x,
        int y,
        int width,
        int height,
        int texWidth,
        int taxHeight
    )
    {
        Vector2 uvMin = new((float)x / texWidth, (float)y / taxHeight);
        Vector2 uvMax = new((float)(x + width) / texWidth, (float)(y + height) / taxHeight);

        SpriteRegion region = new(
            texture,
            new Vector2(x, y),
            new Vector2(width, height),
            uvMin,
            uvMax
        );

        RegisterRegion(name, region);
    }

    public void Dispose()
    {
        _regions.Clear();
        _pages.Clear();
        GC.SuppressFinalize(this);
    }
}
