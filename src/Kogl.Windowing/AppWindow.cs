using ImGuiNET;
using Kogl.Common;
using Kogl.Common.InputManagement;
using Kogl.Core;
using Kogl.OpenGL;
using Kogl.Windowing.ImGuiImpl;
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

    private double _fpsTimer = 0.0;
    private int _fpsCounter = 0;

    public event Action? OnLoad;
    public event Action<double>? OnRender;
    public event Action<int, int>? OnResizeEvent;
    public event Action? OnUnload;

    #region  Window Properties

    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>Gets or sets the display title of the application window</summary>
    public string Title
    {
        get => _window.Title;
        set => _window.Title = value;
    }

    /// <summary>Gets or sets whether vertical synchronization is enabled</summary>
    public bool VSync
    {
        get => _window.VSync;
        set => _window.VSync = value;
    }

    /// <summary>Gets or sets the screen position of the window frame</summary>
    public Vector2D<int> Position
    {
        get => _window.Position;
        set => _window.Position = value;
    }

    /// <summary>Gets or sets the size vectors of the window layout area</summary>
    public Vector2D<int> Size
    {
        get => _window.Size;
        set => _window.Size = value;
    }

    /// <summary>Checks or updates the window's state to full monitor presentation mode</summary>
    public bool Fullscreen
    {
        get => _window.WindowState == WindowState.Fullscreen;
        set => _window.WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
    }

    /// <summary>Checks or updates if the window frame should strip its default operating system borders</summary>
    public bool Borderless
    {
        get => _window.WindowBorder == WindowBorder.Hidden;
        set => _window.WindowBorder = value ? WindowBorder.Hidden : WindowBorder.Resizable;
    }

    /// <summary>Indicates whether the window context has active input focus</summary>
    public bool IsVisible => _window.IsVisible;

    /// <summary>Indicates whether the host window system loop has been scheduled to close</summary>
    public bool ShouldClose => _window.IsClosing;

    /// <summary>Returns the measured Frames Per Second calculated on an exact 1-second rolling interval</summary>
    public int Fps { get; private set; }

    /// <summary>Returns the precise delta execution time of the previous frame update step in seconds</summary>
    public double FrameTime { get; private set; }

    /// <summary>Exposes total operational duration since the engine runtime layer was loaded</summary>
    public double Time => _window.Time;

    #endregion

    public AppWindow(int width, int height, string title)
    {
        Width = width;
        Height = height;

        GlfwWindowing.Use();
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

        _window = Window.Create(options);

        _window.Load += () =>
        {
            _window.Center();

            _gl = _window.CreateOpenGL();
            _input = _window.CreateInput();
            _controller = new ImGuiController(_gl, _window, _input);

            GLBackend backend = new(_gl);
            KoRender.Initialize(backend);

            InputBackend inputBackend = new(_input);
            InputManager.Initialize(inputBackend);

            // FirstLog(width, height, title, options, backend, inputBackend);

            OnLoad?.Invoke();
        };

        _window.Render += dt =>
        {
            FrameTime = dt;
            _fpsTimer += dt;
            _fpsCounter++;

            if (_fpsTimer >= 1.0)
            {
                Fps = _fpsCounter;
                _fpsCounter = 0;
                _fpsTimer -= 1.0;
            }

            _controller?.Update((float)dt);
            OnRender?.Invoke(dt);

            // ImGuiConsole.DrawConsoleWindow();

            // ImGui.Begin("fps");
            // ImGui.Text($"fps: {Fps}");
            // ImGui.End();

            // if (ImGui.BeginMainMenuBar())
            // {
            //     if (ImGui.BeginMenu("File"))
            //     {
            //         ImGui.MenuItem("New");
            //         ImGui.MenuItem("Open");
            //         ImGui.MenuItem("Save");
            //         ImGui.EndMenu();
            //     }

            //     if (ImGui.BeginMenu("Edit"))
            //     {
            //         ImGui.MenuItem("Undo");
            //         ImGui.MenuItem("Redo");
            //         ImGui.EndMenu();
            //     }

            //     if (ImGui.BeginMenu("Help"))
            //     {
            //         ImGui.MenuItem("About");
            //         ImGui.EndMenu();
            //     }

            //     ImGui.EndMainMenuBar();
            // }

            _controller?.Render();

            InputManager.Update();
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

    /// <summary>Enters the main blocking framework execution frame loop context</summary>
    public void Run()
    {
        _window.Run();
    }

    /// <summary>Forces an immediate shutdown instruction to break out of the native window presentation loop</summary>
    public void Close()
    {
        _window.Close();
    }

    /// <summary>Minimizes the window container display frame down into the system taskbar structure</summary>
    public void Minimize()
    {
        _window.WindowState = WindowState.Minimized;
    }

    /// <summary>Maximizes the window container layer bounds to fill the current desktop layout grid space</summary>
    public void Maximize()
    {
        _window.WindowState = WindowState.Maximized;
    }

    /// <summary>Restores the structural screen state of the window configuration back to an unscaled normal presentation window</summary>
    public void Restore()
    {
        _window.WindowState = WindowState.Normal;
    }

    private void OnResize(Vector2D<int> size)
    {
        Width = size.X;
        Height = size.Y;

        KoRender.SetViewport(0, 0, size.X, size.Y);

        OnResizeEvent?.Invoke(size.X, size.Y);
    }

    // not being used at the moment, but may be useful in the future, idk
    // private void FirstLog(
    //     int width,
    //     int height,
    //     string title,
    //     WindowOptions options,
    //     GLBackend renderBackend,
    //     InputBackend inputBackend
    // )
    // {
    //     if (_gl == null || _input == null)
    //         return;

    //     LogCat.Info("BACKEND", $"Rendering backend: {renderBackend.GetType().Name}");
    //     LogCat.Info("BACKEND", $"Input backend: {inputBackend.GetType().Name}");

    //     LogCat.Info(
    //         "OPENGL",
    //         $"Vendor: {_gl.GetStringS(StringName.Vendor)}"
    //             + $" | Renderer: {_gl.GetStringS(StringName.Renderer)}"
    //             + $" | Version: {_gl.GetStringS(StringName.Version)}"
    //             + $" | GLSL Version: {_gl.GetStringS(StringName.ShadingLanguageVersion)}"
    //     );

    //     LogCat.Info(
    //         "SYSTEM",
    //         $"OS: {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})"
    //             + $" | CPU Cores: {Environment.ProcessorCount} threads"
    //             + $" | Runtime: .NET {Environment.Version}"
    //     );

    //     IMonitor monitor = Silk.NET.Windowing.Monitor.GetMainMonitor(_window);
    //     LogCat.Info(
    //         "WINDOW",
    //         $"Display: {monitor.Name} ({monitor.Bounds.Size.X}x{monitor.Bounds.Size.Y} @ {monitor.VideoMode.RefreshRate}Hz)"
    //             + $" | Viewport Created: {width}x{height}"
    //     );

    //     LogCat.Info(
    //         "INPUT",
    //         $"Keyboards: {_input.Keyboards.Count} | Mice: {_input.Mice.Count} | Gamepads: {_input.Mice.Count}"
    //     );

    //     // _gl.GetInteger(GetPName.MaxTextureImageUnits, out int maxTextureSlots);
    //     // _gl.GetInteger(GetPName.MaxTextureSize, out int maxTextureSize);
    //     // _gl.GetInteger(GetPName.MaxUniformBlockSize, out int maxUniformBlockSize);
    //     // Log.Info(
    //     //     "OPENGL",
    //     //     $"Max Texture Slots: {maxTextureSlots} | Max Texture Dimension: {maxTextureSize}px | Uniform Buffer Max: {maxUniformBlockSize / 1024}KB"
    //     // );

    //     // just testing
    //     // bool hasDirectStateAccess = _gl.IsExtensionPresent("GL_ARB_direct_state_access");
    //     // Log.Info(
    //     //     "OPENGL",
    //     //     $"Features -> ARB_direct_state_access (DSA): {(hasDirectStateAccess ? "SUPPORTED" : "NOT SUPPORTED")}"
    //     // );
    // }
}
