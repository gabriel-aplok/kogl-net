using System.Numerics;

using Kogl.Input;

using Silk.NET.Input;

namespace Kogl.Windowing;

internal class InputBackend : IInputBackend
{
    private readonly IInputContext _input;
    private readonly IMouse? _mouse;
    private readonly IKeyboard? _keyboard;

    public InputBackend(IInputContext input)
    {
        _input = input;
#pragma warning disable CA1826
        _mouse = _input.Mice.FirstOrDefault();
        _keyboard = _input.Keyboards.FirstOrDefault();
#pragma warning restore CA1826

        if (_keyboard != null)
        {
            // due to our 1:1 mapping design, cast directly for 0-overhead translation
            _keyboard.KeyDown += static (kb, key, arg3) =>
                InputManager.Internal_SetKeyState((Input.Key)key, true);
            _keyboard.KeyUp += static (kb, key, arg3) =>
                InputManager.Internal_SetKeyState((Input.Key)key, false);
            _keyboard.KeyChar += static (kb, ch) => InputManager.Internal_OnKeyChar(ch);
        }

        if (_mouse != null)
        {
            _mouse.MouseMove += static (m, pos) =>
                InputManager.Internal_SetMousePosition(new Vector2(pos.X, pos.Y));
            _mouse.MouseDown += static (m, btn) =>
                InputManager.Internal_SetMouseButton((Input.MouseButton)btn, true);
            _mouse.MouseUp += static (m, btn) =>
                InputManager.Internal_SetMouseButton((Input.MouseButton)btn, false);
            _mouse.Scroll += static (m, wheel) =>
                InputManager.Internal_SetMouseScroll(new Vector2(wheel.X, wheel.Y));
        }
    }

    public void SetCursorMode(Input.CursorMode mode)
    {
        if (_mouse == null)
            return;
        _mouse.Cursor.CursorMode = mode switch
        {
            Input.CursorMode.Normal => Silk.NET.Input.CursorMode.Normal,
            Input.CursorMode.Hidden => Silk.NET.Input.CursorMode.Hidden,
            Input.CursorMode.Locked => Silk.NET.Input.CursorMode.Raw,
            _ => Silk.NET.Input.CursorMode.Normal,
        };
    }

    public void SetCursorPosition(Vector2 position)
    {
        _mouse?.Position = (Vector2)new Silk.NET.Maths.Vector2D<float>(position.X, position.Y);
    }
}
