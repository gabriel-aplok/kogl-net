using System.Numerics;

namespace Kogl.Common.InputManagement;

public static class InputMap
{
    private static readonly Dictionary<string, List<InputBinding>> _actions = [];

    public static void Bind(string action, Key key)
    {
        if (!_actions.ContainsKey(action))
            _actions[action] = [];
        _actions[action].Add(new InputBinding(BindingType.Key, (int)key));
    }

    public static void Bind(string action, MouseButton button)
    {
        if (!_actions.ContainsKey(action))
            _actions[action] = [];
        _actions[action].Add(new InputBinding(BindingType.MouseButton, (int)button));
    }

    public static bool IsActionDown(string action)
    {
        if (!_actions.TryGetValue(action, out List<InputBinding>? bindings))
            return false;
        foreach (InputBinding b in bindings)
            if (b.IsDown())
                return true;
        return false;
    }

    public static bool IsActionPressed(string action)
    {
        if (!_actions.TryGetValue(action, out List<InputBinding>? bindings))
            return false;
        foreach (InputBinding b in bindings)
            if (b.IsPressed())
                return true;
        return false;
    }

    public static bool IsActionReleased(string action)
    {
        if (!_actions.TryGetValue(action, out List<InputBinding>? bindings))
            return false;
        foreach (InputBinding b in bindings)
            if (b.IsReleased())
                return true;
        return false;
    }

    /// <summary>Returns an axis value from -1.0f to 1.0f based on bound actions</summary>
    public static float GetAxis(string negativeAction, string positiveAction)
    {
        float val = 0;
        if (IsActionDown(positiveAction))
            val += 1.0f;
        if (IsActionDown(negativeAction))
            val -= 1.0f;
        return val;
    }

    /// <summary>Returns a normalized 2D direction vector</summary>
    public static Vector2 GetVector(
        string leftAction,
        string rightAction,
        string downAction,
        string upAction
    )
    {
        float x = GetAxis(leftAction, rightAction);
        float y = GetAxis(downAction, upAction);
        Vector2 vec = new(x, y);

        if (vec.LengthSquared() > 1.0f)
            vec = Vector2.Normalize(vec);

        return vec;
    }
}
