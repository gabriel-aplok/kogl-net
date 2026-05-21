namespace Kogl.Common.Types;

/// <summary>Face culling mode.</summary>
public enum CullFaceState
{
    Front,
    Back,
    FrontAndBack,
}

/// <summary>Polygon front face winding order</summary>
public enum FrontFaceState
{
    Ccw, // Counter-clockwise
    Cw, // Clockwise
}

/// <summary>Polygon rasterization mode</summary>
public enum PolygonState
{
    Fill,
    Line,
    Point,
}

/// <summary>Blending factor</summary>
public enum BlendingFactorState
{
    Zero,
    One,
    SrcAlpha,
    OneMinusSrcAlpha,
    DstAlpha,
    OneMinusDstAlpha,
    SrcColor,
    OneMinusSrcColor,
    DstColor,
    OneMinusDstColor,
}

/// <summary>Blend equation</summary>
public enum BlendEquationState
{
    Add,
    Subtract,
    ReverseSubtract,
}

/// <summary>Depth comparison function</summary>
public enum DepthFunctionState
{
    Never,
    Less,
    Equal,
    Lequal,
    Greater,
    NotEqual,
    Gequal,
    Always,
}

/// <summary>Stencil comparison function</summary>
public enum StencilFunctionState
{
    Never,
    Less,
    Equal,
    Lequal,
    Greater,
    NotEqual,
    Gequal,
    Always,
}

/// <summary>Stencil operation</summary>
public enum StencilOpState
{
    Keep,
    Zero,
    Replace,
    Incr,
    IncrWrap,
    Decr,
    DecrWrap,
    Invert,
}

/// <summary>Logical operation for color blending</summary>
public enum LogicOpState
{
    Clear,
    And,
    AndReverse,
    Copy,
    AndInverted,
    Noop,
    Xor,
    Or,
    Nor,
    Equiv,
    Invert,
    OrReverse,
    CopyInverted,
    OrInverted,
    Nand,
    Set,
}
