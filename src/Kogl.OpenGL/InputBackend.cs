using System.Numerics;
using Kogl.Common.Agnostics;
using Kogl.Common.InputManagement;
using Silk.NET.Input;

namespace Kogl.OpenGL;

public class InputBackend : IInputBackend
{
    private readonly IInputContext _input;
    private readonly IMouse? _mouse;
    private readonly IKeyboard? _keyboard;

    public InputBackend(IInputContext input)
    {
        _input = input;
        _mouse = _input.Mice.Count > 0 ? _input.Mice[0] : null;
        _keyboard = _input.Keyboards.Count > 0 ? _input.Keyboards[0] : null;

        // _mouse = _input.Mice.FirstOrDefault();
        // _keyboard = _input.Keyboards.FirstOrDefault();

        if (_keyboard != null)
        {
            _keyboard.KeyDown += static (kb, key, _) =>
                InputManager.Internal_SetKeyState((Common.InputManagement.Key)key, true);
            _keyboard.KeyUp += static (kb, key, _) =>
                InputManager.Internal_SetKeyState((Common.InputManagement.Key)key, false);
            _keyboard.KeyChar += static (kb, ch) => InputManager.Internal_OnKeyChar(ch);
        }

        if (_mouse != null)
        {
            _mouse.MouseMove += static (m, pos) =>
                InputManager.Internal_SetMousePosition(new Vector2(pos.X, pos.Y));
            _mouse.MouseDown += static (m, btn) =>
                InputManager.Internal_SetMouseButton((Common.InputManagement.MouseButton)btn, true);
            _mouse.MouseUp += static (m, btn) =>
                InputManager.Internal_SetMouseButton(
                    (Common.InputManagement.MouseButton)btn,
                    false
                );
            _mouse.Scroll += static (m, wheel) =>
                InputManager.Internal_SetMouseScroll(new Vector2(wheel.X, wheel.Y));
        }
    }

    public void SetCursorMode(Common.InputManagement.CursorMode mode)
    {
        if (_mouse == null)
            return;

        _mouse.Cursor.CursorMode = mode switch
        {
            Common.InputManagement.CursorMode.Normal => Silk.NET.Input.CursorMode.Normal,
            Common.InputManagement.CursorMode.Hidden => Silk.NET.Input.CursorMode.Hidden,
            Common.InputManagement.CursorMode.Locked => Silk.NET.Input.CursorMode.Raw,
            _ => Silk.NET.Input.CursorMode.Normal,
        };
    }

    public void SetCursorPosition(Vector2 position)
    {
        _mouse?.Position = new Vector2(position.X, position.Y);
    }
}
