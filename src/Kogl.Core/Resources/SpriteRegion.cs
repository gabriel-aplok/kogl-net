using System.Numerics;
using Kogl.Common.Types;

namespace Kogl.Core.Resources;

/// <summary>Sub-region slice of a texture</summary>
public readonly record struct SpriteRegion(
    TextureHandle Texture,
    Vector2 Position,
    Vector2 Size,
    Vector2 UVMin,
    Vector2 UVMax
);
