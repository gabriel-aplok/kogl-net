# KoGL roadmap

immediate-mode-inspired rendering framework built on modern GPU pipelines with backend abstraction and engine-grade architecture.

---

# Current features

## Rendering core

- [x] Automatic dynamic batching
- [x] Indexed rendering
- [x] Draw call minimization
- [x] Automated GPU state caching
- [x] Dynamic VBO/VAO management
- [x] Internal render command pipeline
- [x] Modern OpenGL backend
- [x] Backend abstraction architecture
- [x] Custom shader support
- [x] Shader uniform management
- [x] Resource lifetime management
- [x] Texture loading
- [x] Texture filtering & wrapping
- [x] Multi-texture support
- [x] Framebuffer/Post-Processing support
- [x] Scissor testing
- [x] Blend state management
- [x] Viewport management

---

## Projection & Camera

- [x] Orthographic projection
- [x] Perspective projection
- [x] Frustum projection
- [x] Matrix stack emulation
- [x] Transform pipeline
- [x] Camera transform support

---

## Text & Fonts

- [x] TTF/OTF font loading
- [x] Unicode text rendering
- [x] Signed distance field fonts (SDF)
- [x] Dynamic font atlas generation
- [x] Glyph caching
- [x] Kerning support
- [x] Batched text rendering
- [x] Multi-Line text rendering

---

## Input system

- [x] Keyboard handling
- [x] Mouse handling
- [x] Mouse delta & scroll
- [x] Raw mouse input
- [x] Cursor locking & visibility
- [x] Action mapping system
- [x] Axis mapping
- [x] Vector input mapping
- [x] Input event system

---

## Materials & Shaders

- [x] Material system
- [ ] Shader reflection
- [ ] Shader hot reloading
- [ ] Shader include system
- [x] Material parameters
- [x] Global shader uniform buffers
- [ ] Node-based shader graph

---

# In progress / planned

## Rich text rendering

Implement lightweight markup parsing for inline styling.

Examples:

```html id="ufn2e9"
<color ="red">Hello</color>
<wave>Animated Text</wave>
<shake>Warning!</shake>
```

Planned Features:

- [ ] Inline color changes
- [ ] Bold/italic tags
- [ ] Size changes
- [ ] Gradient text
- [ ] Animated text effects
- [ ] Shadow/outline tags
- [ ] Embedded sprites/icons
- [ ] Nested tags
- [ ] BBCode-style support

Implementation idea:

- Parse tags during text layout
- Swap renderer state mid-string
- Reuse batching system
- Avoid breaking draw batching unnecessarily

---

## World-Space UI & 3D Text

Integrate text rendering directly into the transform pipeline.

Examples:

- 3D labels (Needed for my game)
- Floating health bars
- In-world UI panels
- Billboard text
- Debug overlays
- Editor gizmos

Possible usage:

```csharp id="n1m5gd"
RenderApi.PushMatrix();

RenderApi.Translate(position);
RenderApi.Rotate(rotation);
RenderApi.Scale(scale);

KoGLText.DrawText(font, "Hello", Vector2.Zero, Color.White);

RenderApi.PopMatrix();
```

Planned Features:

- [ ] Billboard rendering
- [ ] Depth-aware text
- [ ] 3D UI transforms
- [ ] Anchor systems
- [ ] Pixel-perfect scaling
- [ ] World-space canvases
- [ ] Distance-based scaling

---

## Texture atlasing system

Large atlas support for minimizing texture swaps and maximizing batching efficiency.

Goals:

- Reduce draw calls
- Improve batching efficiency
- Minimize texture binds
- Improve sprite rendering performance

Planned features:

- [ ] Automatic atlas packing
- [ ] Runtime atlas generation
- [ ] Atlas resizing
- [ ] Multi-page atlases
- [ ] Sprite region extraction
- [ ] Atlas serialization
- [ ] UV remapping utilities
- [ ] Atlas debugging tools

Future possibilities:

- Font atlas sharing
- UI atlas support
- Sprite animation atlases
- Hot-reload atlas rebuilding

---

# Planned Major Features

## Renderer features

- [x] Multi-render target support (MRT)
- [ ] Instanced rendering
- [ ] Compute shader support
- [ ] Render graph system
- [ ] Deferred rendering pipeline
- [ ] Forward+ rendering
- [ ] GPU occlusion queries
- [ ] Indirect rendering
- [ ] GPU buffer streaming
- [ ] Bindless texture asrchitecture
- [ ] Render pass abstraction
- [ ] Pipeline state objects

---

## Advanced post processing

- [ ] Bloom
- [ ] SSAO
- [ ] Motion blur
- [ ] Tone mapping
- [ ] FXAA/TAA
- [ ] LUT color grading
- [ ] Chromatic aberration
- [ ] Depth of field
- [ ] Volumetric lighting
- [ ] HDR pipeline

---

## Sprite & 2D Systems

- [ ] Sprite renderer
- [ ] Sprite animation system
- [ ] Tilemap renderer
- [ ] Nine-patch rendering
- [ ] Particle system
- [ ] GPU particles
- [ ] 2D lighting
- [ ] Sprite masking

---

## UI System

- [ ] Immediate-mode UI layer
- [ ] Retained UI system
- [ ] Layout engine
- [ ] Flex/Grid layouts
- [ ] UI animations
- [ ] UI event propagation
- [ ] Docking system (or not?)
- [ ] Theme/Skin system

---

## Asset Pipeline

- [ ] Asset manager
- [ ] Resource hot reloading
- [ ] Async asset streaming
- [ ] Virtual file system
- [ ] Asset serialization
- [ ] Import pipeline
- [ ] Texture compression pipeline

---

## Platform & Backend Expansion

- [ ] Vulkan backend
  - **If someone do that, I would love it. I hate Vulkan.**
- [ ] DirectX backend
- [ ] Metal backend
- [ ] WebGPU backend
- [ ] Software renderer backend
- [ ] Headless rendering backend

---

## Audio

- [ ] Audio playback
- [ ] Streaming audio
- [ ] Spatial audio
- [ ] Audio bus system
- [ ] DSP effects
- [ ] Audio mixer

---

## ECS & Scene Systems

- [ ] Scene graph
- [ ] ECS integration
- [ ] Transform hierarchy
- [ ] Serialization system
- [ ] Prefab system
- [ ] Runtime reflection helpers

---

## Tooling & Debugging

- [ ] GPU debug Overlay
- [ ] RenderDoc integration
- [ ] Live renderer statistics
- [ ] Frame capture tools
- [ ] Batch visualizer
- [ ] Atlas visualizer
- [ ] Shader debugging tools
- [ ] Memory tracking

---

## Performance & Optimization

- [ ] SIMD math optimizations
- [ ] Job system
- [ ] Multi-threaded renderer
- [ ] Async GPU upload queue
- [ ] Parallel command recording
- [ ] Memory arenas
- [ ] Custom allocators
- [ ] Frame allocator system

---

## Networking & Multiplayer Helpers

- [ ] Snapshot interpolation helpers
- [ ] Client prediction helpers
- [ ] Debug net visualization
- [ ] Multiplayer debug overlays

---

## Future Experimental Ideas

- [ ] GPU-Driven rendering
- [ ] Clustered rendering
- [ ] Mesh shaders
- [ ] Virtual texturing
- [ ] Sparse texturing
- [ ] Nanite-Style experiments
- [ ] GPU culling
- [ ] Procedural geometry pipelines
- [ ] Runtime mesh generation
- [ ] Vector graphics renderer
- [ ] SVG rendering
- [ ] Markdown renderer
- [ ] Embedded scripting integration
  - It would be cool to implement my ([**MiniScript**](https://github.com/gabriel-aplok/miniscript)) language as well, in addition to others and C# itself.
- [ ] Editor framework
- [ ] Visual node graph framework
