namespace Kogl.UI;

[Flags]
public enum UIResult
{
    None = 0,
    Active = 1 << 0,
    Submit = 1 << 1,
    Change = 1 << 2,
}

[Flags]
public enum UIOpt
{
    None = 0,
    AlignCenter = 1 << 0,
    AlignRight = 1 << 1,
    NoInteract = 1 << 2,
    NoFrame = 1 << 3,
    NoResize = 1 << 4,
    NoScroll = 1 << 5,
    NoClose = 1 << 6,
    NoTitle = 1 << 7,
    HoldFocus = 1 << 8,
    AutoSize = 1 << 9,
    Popup = 1 << 10,
    Closed = 1 << 11,
    Expanded = 1 << 12,
}

public enum UIColorId
{
    Text,
    Border,
    WindowBg,
    TitleBg,
    TitleText,
    PanelBg,
    Button,
    ButtonHover,
    ButtonFocus,
    Base,
    BaseHover,
    BaseFocus,
    ScrollBase,
    ScrollThumb,
    Max,
}

public enum UIIcon
{
    None = 0,
    Close = 1,
    Check,
    Collapsed,
    Expanded,
    Max,
}

internal enum UIClipType
{
    None = 0,
    Part = 1,
    All = 2,
}

internal enum UICommandType
{
    Jump = 1,
    Clip,
    Rect,
    Text,
    Icon,
    Max,
}
