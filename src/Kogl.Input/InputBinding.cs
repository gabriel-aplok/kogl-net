namespace Kogl.Input;

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
            BindingType.Key => Input.IsKeyDown((Key)Code),
            BindingType.MouseButton => Input.IsMouseButtonDown((MouseButton)Code),
            _ => false,
        };
    }

    public bool IsPressed()
    {
        return Type switch
        {
            BindingType.Key => Input.IsKeyPressed((Key)Code),
            BindingType.MouseButton => Input.IsMouseButtonPressed((MouseButton)Code),
            _ => false,
        };
    }

    public bool IsReleased()
    {
        return Type switch
        {
            BindingType.Key => Input.IsKeyReleased((Key)Code),
            BindingType.MouseButton => Input.IsMouseButtonReleased((MouseButton)Code),
            _ => false,
        };
    }
}
