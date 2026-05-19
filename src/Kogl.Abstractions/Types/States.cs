namespace Kogl.Abstractions.Types;

public enum CullFaceState
{
    Front,
    Back,
    FrontAndBack,
}

public enum FrontFaceState
{
    Ccw,
    Cw,
}

public enum PolygonState
{
    Fill,
    Line,
    Point,
}

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

public enum BlendEquationState
{
    Add,
    Subtract,
    ReverseSubtract,
}

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

public enum StencilFunctionState
{
    Never = 0x200,
    Less = 0x201,
    Equal = 0x202,
    Lequal = 0x203,
    Greater = 0x204,
    NotEqual = 0x205,
    Gequal = 0x206,
    Always = 0x207,
}

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
