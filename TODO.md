## Reorganize the entire codebase

For now I'm just designing a simple game framework and messing around with it, but soon I want to scale it up further, so as development progresses I'll reorganize the code and improve everything.

> I know, I have to start fixing this quickly so it doesn't snowball.

## Rich Text

Implementing a simple markup parser (like <color=red>) by swapping RenderApi.Color4 mid-string

## World-Space UI

Since it's integrated into the MatrixStack, maybe I can use RenderApi.Rotate or RenderApi.Scale to put text directly on 3D surfaces.

## Texture Atlasing

If you have multiple small sprites, putting them on one big texture will allow them to all be drawn in a single draw call, even if they are "different" images.

## Done

- Automatic Batching: Done
- Custom Shader: Done
- Automated State Caching: Done
- Post-Processing (Framebuffer): Done
- Scissor Testing: Done
- Resource Management: Done
- Texture Loading: Done
- Orthographic, Perspective and Frustum Projection: Done
- Text Rendering: Done
- Signed Distance Fields: Done
- Input Handling: Done
- Material System: Done
