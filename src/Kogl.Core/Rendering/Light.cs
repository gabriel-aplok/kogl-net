using System.Numerics;
using Kogl.Core.Resources;

namespace Kogl.Core.Rendering;

public enum LightType
{
    Directional = 0,
    Point = 1,
}

public struct Light
{
    public LightType Type;
    public bool Enabled;
    public Vector3 Position;
    public Vector3 Target;
    public Vector4 Color;

    public static Light Create(LightType type, Vector3 position, Vector3 target, Vector4 color)
    {
        return new Light
        {
            Type = type,
            Enabled = true,
            Position = position,
            Target = target,
            Color = color,
        };
    }

    /// <summary>
    /// Updates the shader uniforms for this light in world space.
    /// </summary>
    public readonly void UpdateValues(Shader shader, int index)
    {
        string prefix = $"lights[{index}].";

        KoRender.SetUniform(prefix + "enabled", Enabled ? 1 : 0);
        KoRender.SetUniform(prefix + "type", (int)Type);
        KoRender.SetUniform(prefix + "position", Position);
        KoRender.SetUniform(prefix + "target", Target);
        KoRender.SetUniform(prefix + "color", Color);
    }
}
