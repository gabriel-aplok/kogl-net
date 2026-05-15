using Kogl.Core;
using Kogl.OpenGL;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Kogl.Windowing;

public class AppWindow
{
    private readonly IWindow _window;
    public event Action<double>? OnRender;
    public event Action? OnLoad;
    public event Action? OnUnload;

    public AppWindow(int width, int height, string title)
    {
        WindowOptions options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
        options.Title = title;
        options.API = new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Default,
            new APIVersion(3, 3)
        );

        _window = Window.Create(options);
        _window.Load += () =>
        {
            GL gl = _window.CreateOpenGL();
            OpenGLBackend backend = new(gl);
            RenderApi.Initialize(backend);
            OnLoad?.Invoke();
        };
        _window.Render += (dt) => OnRender?.Invoke(dt);
        _window.FramebufferResize += (s) => RenderApi.SetViewport(0, 0, s.X, s.Y);
        _window.Closing += () =>
        {
            OnUnload?.Invoke();
        };
    }

    public void Run()
    {
        _window.Run();
    }
}
