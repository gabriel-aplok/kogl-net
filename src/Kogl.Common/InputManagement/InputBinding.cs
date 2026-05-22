namespace Kogl.Common.InputManagement;

public enum BindingType
{
    Key,
    MouseButton,
}

public readonly struct InputBinding(BindingType type, int code)
{
    public BindingType Type { get; } = type;
    public int Code { get; } = code;

    public bool IsDown()
    {
        return Type switch
        {
            BindingType.Key => InputManager.IsKeyDown((Key)Code),
            BindingType.MouseButton => InputManager.IsMouseButtonDown((MouseButton)Code),
            _ => false,
        };
    }

    public bool IsPressed()
    {
        return Type switch
        {
            BindingType.Key => InputManager.IsKeyPressed((Key)Code),
            BindingType.MouseButton => InputManager.IsMouseButtonPressed((MouseButton)Code),
            _ => false,
        };
    }

    public bool IsReleased()
    {
        return Type switch
        {
            BindingType.Key => InputManager.IsKeyReleased((Key)Code),
            BindingType.MouseButton => InputManager.IsMouseButtonReleased((MouseButton)Code),
            _ => false,
        };
    }
}
