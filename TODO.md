## High-Performance Text Rendering

An extension on top of Kogl.Abstractions for signed distance field (SDF) or bitmap fonts. the system utilizes a high-performance batching engine, drawing an entire paragraph of text will automatically group into a single dynamic draw call as long as they share the same font atlas texture.

## Custom Shader & Material Support

I willil add a basic Material system where users can pass custom shaders to Kogl.Begin().

```csharp
var customShader = myBackend.CreateShader(vsCode, fsCode);
// custom properties...
RenderApi.Begin(PrimitiveMode.Quads, texture, customShader);
```

## Automated State Caching

State cache inside OpenGLBackend (or any future backends like Vulkan). Before executing a native binding command like glBindTexture or glUseProgram, check if the requested ID matches what is currently bound on the hardware. This completely eliminates redundant driver overhead during deep draw-call trees!

## Wire up StbImageSharp for Textures

Yea...

```csharp
public static TextureHandle LoadTexture(string path, IGraphicsBackend backend)
{
  ...
}
```
