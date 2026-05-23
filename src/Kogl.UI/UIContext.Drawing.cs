using System.Numerics;
using Kogl.FreeType;

namespace Kogl.UI;

public partial class UIContext
{
    private int PushCommand(UICommandType type)
    {
        if (CommandListCount >= CommandListSize)
            return -1;
        int idx = CommandListCount++;
        CommandList[idx].Type = type;
        return idx;
    }

    private int PushJump(int dst)
    {
        int idx = PushCommand(UICommandType.Jump);
        if (idx >= 0)
            CommandList[idx].JumpDst = dst;
        return idx;
    }

    internal void SetClip(UIRect rect)
    {
        int idx = PushCommand(UICommandType.Clip);
        if (idx >= 0)
            CommandList[idx].Rect = rect;
    }

    internal void DrawRect(UIRect rect, Vector4 color)
    {
        rect = IntersectRects(rect, GetClipRect());
        if (rect.W > 0 && rect.H > 0)
        {
            int idx = PushCommand(UICommandType.Rect);
            if (idx >= 0)
            {
                CommandList[idx].Rect = rect;
                CommandList[idx].Color = color;
            }
        }
    }

    internal void DrawBox(UIRect rect, Vector4 color)
    {
        DrawRect(new UIRect(rect.X + 1, rect.Y, rect.W - 2, 1), color);
        DrawRect(new UIRect(rect.X + 1, rect.Y + rect.H - 1, rect.W - 2, 1), color);
        DrawRect(new UIRect(rect.X, rect.Y, 1, rect.H), color);
        DrawRect(new UIRect(rect.X + rect.W - 1, rect.Y, 1, rect.H), color);
    }

    internal void DrawText(string str, Vector2 pos, Vector4 color)
    {
        Vector2 size = KoGLText.Measure(Style.Font, str.AsSpan());
        UIRect rect = new((int)pos.X, (int)pos.Y, (int)size.X, Style.Font.LineHeight);

        UIClipType clipped = CheckClip(rect);
        if (clipped == UIClipType.All)
            return;
        if (clipped == UIClipType.Part)
            SetClip(GetClipRect());

        int idx = PushCommand(UICommandType.Text);
        if (idx >= 0)
        {
            CommandList[idx].TextStr = str;
            CommandList[idx].Rect = new UIRect((int)pos.X, (int)pos.Y, 0, 0);
            CommandList[idx].Color = color;
            CommandList[idx].Font = Style.Font;
        }

        if (clipped != UIClipType.None)
            SetClip(_unclippedRect);
    }

    internal void DrawTextBuffer(int start, int len, Vector2 pos, Vector4 color)
    {
        Vector2 size = KoGLText.Measure(Style.Font, new ReadOnlySpan<char>(TextBuffer, start, len));
        UIRect rect = new((int)pos.X, (int)pos.Y, (int)size.X, Style.Font.LineHeight);

        UIClipType clipped = CheckClip(rect);
        if (clipped == UIClipType.All)
            return;
        if (clipped == UIClipType.Part)
            SetClip(GetClipRect());

        int idx = PushCommand(UICommandType.Text);
        if (idx >= 0)
        {
            CommandList[idx].TextStr = null;
            CommandList[idx].TextStart = start;
            CommandList[idx].TextLen = len;
            CommandList[idx].Rect = new UIRect((int)pos.X, (int)pos.Y, 0, 0);
            CommandList[idx].Color = color;
            CommandList[idx].Font = Style.Font;
        }

        if (clipped != UIClipType.None)
            SetClip(_unclippedRect);
    }

    internal void DrawIcon(UIIcon id, UIRect rect, Vector4 color)
    {
        UIClipType clipped = CheckClip(rect);
        if (clipped == UIClipType.All)
            return;
        if (clipped == UIClipType.Part)
            SetClip(GetClipRect());

        int idx = PushCommand(UICommandType.Icon);
        if (idx >= 0)
        {
            CommandList[idx].Icon = id;
            CommandList[idx].Rect = rect;
            CommandList[idx].Color = color;
        }

        if (clipped != UIClipType.None)
            SetClip(_unclippedRect);
    }

    private void DrawFrame(UIRect rect, UIColorId colorid)
    {
        DrawRect(rect, Style.Colors[(int)colorid]);
        if (
            colorid == UIColorId.ScrollBase
            || colorid == UIColorId.ScrollThumb
            || colorid == UIColorId.TitleBg
        )
            return;

        if (Style.Colors[(int)UIColorId.Border].W > 0)
        {
            DrawBox(
                new UIRect(rect.X - 1, rect.Y - 1, rect.W + 2, rect.H + 2),
                Style.Colors[(int)UIColorId.Border]
            );
        }
    }
}
