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
    /// Updates the shader uniforms for this light.
    /// Transforms position and target to View Space using the active camera's view matrix.
    /// </summary>
    public readonly void UpdateValues(Shader shader, int index, Matrix4x4 viewMatrix)
    {
        string prefix = $"lights[{index}].";

        // Transform position to view space
        Vector3 viewPos = Vector3.Transform(Position, viewMatrix);
        // Transform target to view space (for directional lights)
        Vector3 viewTarget = Vector3.Transform(Target, viewMatrix);

        KoRender.SetUniform(prefix + "enabled", Enabled ? 1 : 0);
        KoRender.SetUniform(prefix + "type", (int)Type);
        KoRender.SetUniform(prefix + "position", viewPos);
        KoRender.SetUniform(prefix + "target", viewTarget);
        KoRender.SetUniform(prefix + "color", Color);
    }
}
