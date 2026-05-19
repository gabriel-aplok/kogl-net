using System.Numerics;

namespace Kogl.Input;

/// <summary>The input backend</summary>
public interface IInputBackend
{
    /// <summary>Sets the cursor mode</summary>
    public void SetCursorMode(CursorMode mode);

    /// <summary>Sets the cursor position</summary>
    public void SetCursorPosition(Vector2 position);
}
