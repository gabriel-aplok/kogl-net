namespace Kogl.Common.Types;

/// <summary>Graphics primitive drawing modes</summary>
public enum PrimitiveMode
{
    /// <summary>Individual line segments</summary>
    Lines,

    /// <summary>Connected line segments forming a polyline</summary>
    LineStrip,

    /// <summary>Individual triangles</summary>
    Triangles,

    /// <summary>Connected triangles sharing edges (strip)</summary>
    TriangleStrip,

    /// <summary>Triangles sharing a common central vertex (fan)</summary>
    TriangleFan,

    /// <summary>Individual quadrilaterals (quads)</summary>
    Quads,
}
