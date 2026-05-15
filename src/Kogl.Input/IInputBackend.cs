using System.Numerics;

namespace Kogl.Input;

public interface IInputBackend
{
    public void SetCursorMode(CursorMode mode);
    public void SetCursorPosition(Vector2 position);
}
