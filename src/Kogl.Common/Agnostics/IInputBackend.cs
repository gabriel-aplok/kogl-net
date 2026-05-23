using System.Numerics;
using Kogl.Common.InputManagement;

namespace Kogl.Common.Agnostics;

/// <summary>The input backend</summary>
public interface IInputBackend
{
    public void SetCursorMode(CursorMode mode);
    public void SetCursorPosition(Vector2 position);
}
