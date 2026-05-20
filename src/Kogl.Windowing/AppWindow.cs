using Kogl.Common;
using Kogl.Core;
using Kogl.OpenGL;
using Silk.NET.Input;
using Silk.NET.Input.Glfw;
using Silk.NET.Maths;
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

    public event Action? OnLoad;
    public event Action<double>? OnRender;
    public event Action<int, int>? OnResizeEvent;
    public event Action? OnUnload;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public AppWindow(int width, int height, string title)
    {
        Width = width;
        Height = height;

        GlfwWindowing.RegisterPlatform();
        GlfwInput.RegisterPlatform();

        WindowOptions options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.API = new GraphicsAPI(
            ContextAPI.OpenGL,
            ContextProfile.Core,
            ContextFlags.Default,
            new APIVersion(4, 6)
        );
        options.VSync = false;
        // options.WindowState = WindowState.Maximized;

        _window = Window.Create(options);

        _window.Load += () =>
        {
            _window.Center();

            _gl = _window.CreateOpenGL();
            _input = _window.CreateInput();
            _controller = new ImGuiController(_gl, _window, _input);

            OpenGLBackend backend = new(_gl);
            KoRender.Initialize(backend);

            InputBackend inputBackend = new(_input);
            Input.InputManager.Initialize(inputBackend);

            FirstLog(width, height, title, options, backend, inputBackend);

            OnLoad?.Invoke();
        };

        _window.Render += dt =>
        {
            _controller?.Update((float)dt);
            OnRender?.Invoke(dt);

            _controller?.Render();

            Input.InputManager.Update();
        };

        _window.FramebufferResize += OnResize;

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

    private void OnResize(Vector2D<int> size)
    {
        Width = size.X;
        Height = size.Y;

        KoRender.SetViewport(0, 0, size.X, size.Y);

        OnResizeEvent?.Invoke(size.X, size.Y);
    }

    private void FirstLog(
        int width,
        int height,
        string title,
        WindowOptions options,
        OpenGLBackend renderBackend,
        InputBackend inputBackend
    )
    {
        if (_gl == null || _input == null)
            return;

        // OpenGL
        Log.Info($"KoGL backend: {renderBackend.GetType().Name}");
        Log.Info(
            "OPENGL",
            $"Requested API: {options.API.API} {options.API.Version.MajorVersion}.{options.API.Version.MinorVersion}"
        );
        Log.Info(
            "OPENGL",
            $"Requested Profile: {options.API.Profile} | Flags: {options.API.Flags}"
        );
        Log.Info("OPENGL", $"Vendor: {_gl.GetStringS(StringName.Vendor)}");
        Log.Info("OPENGL", $"Renderer: {_gl.GetStringS(StringName.Renderer)}");
        Log.Info("OPENGL", $"Version: {_gl.GetStringS(StringName.Version)}");
        Log.Info("OPENGL", $"GLSL Version: {_gl.GetStringS(StringName.ShadingLanguageVersion)}");

        // System information
        Log.Info(
            "SYSTEM",
            $"OS: {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})"
        );
        Log.Info(
            "SYSTEM",
            $"CPU Cores: {Environment.ProcessorCount} threads | Runtime: .NET {Environment.Version}"
        );

        // Window information
        IMonitor monitor = Silk.NET.Windowing.Monitor.GetMainMonitor(_window);
        Log.Info(
            "WINDOW",
            $"Display: {monitor.Name} ({monitor.Bounds.Size.X}x{monitor.Bounds.Size.Y} @ {monitor.VideoMode.RefreshRate}Hz)"
        );
        Log.Info("WINDOW", $"Viewport Created: {width}x{height} | Title: \"{title}\"");

        // OpenGL information
        _gl.GetInteger(GetPName.MaxTextureImageUnits, out int maxTextureSlots);
        _gl.GetInteger(GetPName.MaxTextureSize, out int maxTextureSize);
        _gl.GetInteger(GetPName.MaxUniformBlockSize, out int maxUniformBlockSize);
        Log.Info(
            "OPENGL",
            $"Max Texture Slots: {maxTextureSlots} | Max Texture Dimension: {maxTextureSize}px | Uniform Buffer Max: {maxUniformBlockSize / 1024}KB"
        );

        bool hasDirectStateAccess = _gl.IsExtensionPresent("GL_ARB_direct_state_access");
        Log.Info(
            "OPENGL",
            $"Features -> ARB_direct_state_access (DSA): {(hasDirectStateAccess ? "SUPPORTED" : "NOT SUPPORTED")}"
        );

        // Input
        Log.Info($"KoGL backend: {inputBackend.GetType().Name}");
        Log.Info(
            "INPUT",
            $"Keyboards: {_input.Keyboards.Count} | Mice: {_input.Mice.Count} | Gamepads: {_input.Mice.Count}"
        );
    }
}
