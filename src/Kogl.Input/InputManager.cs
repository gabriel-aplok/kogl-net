using System.Numerics;

namespace Kogl.Input;

public static class InputManager
{
    private static IInputBackend _backend = null!;

    private const int _maxKeys = 512;
    private const int _maxButtons = 16;

    // arrays guarantee 0-allocation tracking btw
    private static readonly bool[] _keysDown = new bool[_maxKeys];
    private static readonly bool[] _keysPressed = new bool[_maxKeys];
    private static readonly bool[] _keysReleased = new bool[_maxKeys];

    private static readonly bool[] _buttonsDown = new bool[_maxButtons];
    private static readonly bool[] _buttonsPressed = new bool[_maxButtons];
    private static readonly bool[] _buttonsReleased = new bool[_maxButtons];

    private static long _lastClickTime;
    private static MouseButton _lastClickButton;

    public static Vector2 MousePosition { get; private set; }
    public static Vector2 MouseDelta { get; private set; }
    public static Vector2 MouseScrollDelta { get; private set; }

    private static CursorMode _cursorMode = CursorMode.Normal;
    public static CursorMode CursorMode
    {
        get => _cursorMode;
        set
        {
            _cursorMode = value;
            _backend?.SetCursorMode(value);
        }
    }

    // standard events
    public static event Action<Key>? KeyDown;
    public static event Action<Key>? KeyUp;
    public static event Action<char>? KeyChar;
    public static event Action<MouseButton>? MouseDown;
    public static event Action<MouseButton>? MouseUp;
    public static event Action<MouseButton>? MouseDoubleClicked;
    public static event Action<Vector2>? MouseMoved;
    public static event Action<Vector2>? MouseScrolled;

    public static void Initialize(IInputBackend backend)
    {
        _backend = backend;
    }

    /// <summary>Flushes frame-specific state tracking. Must be called at the VERY END of the frame loop</summary>
    public static void Update()
    {
        Array.Clear(_keysPressed, 0, _maxKeys);
        Array.Clear(_keysReleased, 0, _maxKeys);
        Array.Clear(_buttonsPressed, 0, _maxButtons);
        Array.Clear(_buttonsReleased, 0, _maxButtons);

        MouseDelta = Vector2.Zero;
        MouseScrollDelta = Vector2.Zero;
    }

    // ==================
    // API
    // ==================

    #region API

    public static bool IsKeyDown(Key key)
    {
        return key >= 0 && (int)key < _maxKeys && _keysDown[(int)key];
    }

    public static bool IsKeyPressed(Key key)
    {
        return key >= 0 && (int)key < _maxKeys && _keysPressed[(int)key];
    }

    public static bool IsKeyReleased(Key key)
    {
        return key >= 0 && (int)key < _maxKeys && _keysReleased[(int)key];
    }

    public static bool IsMouseButtonDown(MouseButton button)
    {
        return button >= 0 && (int)button < _maxButtons && _buttonsDown[(int)button];
    }

    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return button >= 0 && (int)button < _maxButtons && _buttonsPressed[(int)button];
    }

    public static bool IsMouseButtonReleased(MouseButton button)
    {
        return button >= 0 && (int)button < _maxButtons && _buttonsReleased[(int)button];
    }

    public static bool IsDragging(MouseButton button)
    {
        return IsMouseButtonDown(button) && MouseDelta.LengthSquared() > 0.001f;
    }

    public static void SetCursorPosition(Vector2 pos)
    {
        _backend.SetCursorPosition(pos);
    }

    #endregion

    // ==================
    // Backend Pipeline
    // ==================

    #region Backend Pipeline

    public static void Internal_SetKeyState(Key key, bool isDown)
    {
        if (key < 0 || (int)key >= _maxKeys)
            return;

        if (isDown && !_keysDown[(int)key])
        {
            _keysPressed[(int)key] = true;
            KeyDown?.Invoke(key);
        }
        else if (!isDown && _keysDown[(int)key])
        {
            _keysReleased[(int)key] = true;
            KeyUp?.Invoke(key);
        }

        _keysDown[(int)key] = isDown;
    }

    public static void Internal_SetMouseButton(MouseButton button, bool isDown)
    {
        if (button < 0 || (int)button >= _maxButtons)
            return;

        if (isDown && !_buttonsDown[(int)button])
        {
            _buttonsPressed[(int)button] = true;
            MouseDown?.Invoke(button);

            long time = Environment.TickCount64;
            if (button == _lastClickButton && time - _lastClickTime < 300) // 300ms double click threshold
            {
                MouseDoubleClicked?.Invoke(button);
                _lastClickTime = 0; // prevent triple click trigger
            }
            else
            {
                _lastClickTime = time;
                _lastClickButton = button;
            }
        }
        else if (!isDown && _buttonsDown[(int)button])
        {
            _buttonsReleased[(int)button] = true;
            MouseUp?.Invoke(button);
        }

        _buttonsDown[(int)button] = isDown;
    }

    public static void Internal_SetMousePosition(Vector2 pos)
    {
        MouseDelta += pos - MousePosition;
        MousePosition = pos;
        MouseMoved?.Invoke(pos);
    }

    public static void Internal_SetMouseScroll(Vector2 delta)
    {
        MouseScrollDelta += delta;
        MouseScrolled?.Invoke(delta);
    }

    public static void Internal_OnKeyChar(char c)
    {
        KeyChar?.Invoke(c);
    }

    #endregion
}
