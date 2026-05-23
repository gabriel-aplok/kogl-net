using System.Numerics;
using Kogl.FreeType;

namespace Kogl.UI;

public struct UIRect(int x, int y, int w, int h)
{
    public int X = x,
        Y = y,
        W = w,
        H = h;
}

internal struct UICommand
{
    public UICommandType Type;
    public int JumpDst;
    public UIRect Rect;
    public Vector4 Color;
    public UIIcon Icon;
    public Font Font;

    // 0-allocation text references
    public string? TextStr;
    public int TextStart;
    public int TextLen;
}

internal struct UIPoolItem
{
    public uint Id;
    public int LastUpdate;
}

internal class UIContainer
{
    public int HeadIdx;
    public int TailIdx;
    public UIRect Rect;
    public UIRect Body;
    public Vector2 ContentSize;
    public Vector2 Scroll;
    public int ZIndex;
    public bool Open;
}

internal struct UILayout
{
    public UIRect Body;
    public UIRect Next;
    public Vector2 Position;
    public Vector2 Size;
    public Vector2 Max;
    public int[] Widths; // limited to 16
    public int Items;
    public int ItemIndex;
    public int NextRow;
    public int NextType;
    public int Indent;

    public void InitWidths()
    {
        if (Widths == null)
            Widths = new int[16];
    }
}
