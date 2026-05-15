using Kogl.Core;
using Kogl.OpenGL;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;

namespace Kogl.Windowing;

public class AppWindow
{
    private readonly IWindow _window;
    private ImGuiController? _controller;
    private GL? _gl;
    private IInputContext? _input;

    public event Action<double>? OnRender;
    public event Action? OnLoad;
    public event Action? OnUnload;

    public AppWindow(int width, int height, string title)
    {
        GlfwWindowing.RegisterPlatform();
        GlfwInput.RegisterPlatform();

        WindowOptions options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
        options.Title = title;
        options.API = new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Default,
            new APIVersion(4, 6)
        );

        _window = Window.Create(options);

        _window.Load += () =>
        {
            _gl = _window.CreateOpenGL();
            _input = _window.CreateInput();
            _controller = new ImGuiController(_gl, _window, _input);

            OpenGLBackend backend = new(_gl);
            RenderApi.Initialize(backend);

            InputBackend inputBackend = new(_input);
            Input.InputManager.Initialize(inputBackend);

            OnLoad?.Invoke();
        };

        _window.Render += dt =>
        {
            _controller?.Update((float)dt);
            OnRender?.Invoke(dt);

            // ImGuiNET.ImGui.ShowDemoWindow();

            _controller?.Render();

            Input.InputManager.Update();
        };

        _window.FramebufferResize += s => RenderApi.SetViewport(0, 0, s.X, s.Y);

        _window.Closing += () =>
        {
            _controller?.Dispose();
            _input?.Dispose();
            _gl?.Dispose();
            OnUnload?.Invoke();
        };
    }

    public void Run()
    {
        _window.Run();
    }
}
