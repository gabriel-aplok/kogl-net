## Rich Text

Implementing a simple markup parser (like <color=red>) by swapping RenderApi.Color4 mid-string

## World-Space UI

Since it's integrated into the MatrixStack, maybe I can use RenderApi.Rotate or RenderApi.Scale to put text directly on 3D surfaces.

## Texture Atlasing

If you have multiple small sprites, putting them on one big texture will allow them to all be drawn in a single draw call, even if they are "different" images.

## Input Handling

Mouse manipulation, keyboard manipulation, as in Unity or Godot, all possible interactions, mouse position tracking (MouseMove and MouseScroll, etc.), mouse button actions (MouseDown, MouseUp, double-click, button support, etc.), cursor management and mouse state (cursor visibility, cursor lock/lock, cursor icon, raw mouse input, etc.), key state changes (KeyDown, KeyUp, KeyChar, IsKeyPressed, etc.), physical key support (standard keys, modifier keys, function keys, numeric keypad keys, navigation keys).

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
