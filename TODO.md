## High-Performance Text Rendering

An extension on top of Kogl.Abstractions for signed distance field (SDF) or bitmap fonts. the system utilizes a high-performance batching engine, drawing an entire paragraph of text will automatically group into a single dynamic draw call as long as they share the same font atlas texture.

## Texture Atlasing

If you have multiple small sprites, putting them on one big texture will allow them to all be drawn in a single draw call, even if they are "different" images.

## Scissor Testing

Adding a RenderApi.BeginScissor(x, y, w, h) for UI work.

## Wire up StbImageSharp for Textures

Yea...

```csharp
public static TextureHandle LoadTexture(string path, IGraphicsBackend backend)
{
  ...
}
```

## Done

- Custom Shader: Done
- Automated State Caching: Done
- Post-Processing: Done
