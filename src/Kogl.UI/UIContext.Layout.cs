using System.Numerics;

namespace Kogl.UI;

public partial class UIContext
{
    private void PushLayout(UIRect body, Vector2 scroll)
    {
        UILayout layout = new();
        layout.InitWidths();
        layout.Body = new UIRect(body.X - (int)scroll.X, body.Y - (int)scroll.Y, body.W, body.H);
        layout.Max = new Vector2(-0x1000000, -0x1000000);
        _layoutStack.Add(layout);
        LayoutRow(1, [0], 0);
    }

    private UILayout GetLayout()
    {
        return _layoutStack[^1];
    }

    private void UpdateLayout(UILayout l)
    {
        _layoutStack[^1] = l;
    }

    private void PopContainer()
    {
        UIContainer cnt = GetCurrentContainer();
        UILayout layout = GetLayout();
        cnt.ContentSize.X = layout.Max.X - layout.Body.X;
        cnt.ContentSize.Y = layout.Max.Y - layout.Body.Y;
        _containerStack.RemoveAt(_containerStack.Count - 1);
        _layoutStack.RemoveAt(_layoutStack.Count - 1);
        PopId();
    }

    public void LayoutBeginColumn()
    {
        PushLayout(LayoutNext(), Vector2.Zero);
    }

    public void LayoutEndColumn()
    {
        UILayout b = GetLayout();
        _layoutStack.RemoveAt(_layoutStack.Count - 1);
        UILayout a = GetLayout();

        a.Position.X = Math.Max(a.Position.X, b.Position.X + b.Body.X - a.Body.X);
        a.NextRow = Math.Max(a.NextRow, b.NextRow + b.Body.Y - a.Body.Y);
        a.Max.X = Math.Max(a.Max.X, b.Max.X);
        a.Max.Y = Math.Max(a.Max.Y, b.Max.Y);

        UpdateLayout(a);
    }

    public void LayoutRow(int items, int[]? widths, int height)
    {
        UILayout layout = GetLayout();
        if (widths != null)
        {
            for (int i = 0; i < items && i < 16; i++)
                layout.Widths[i] = widths[i];
        }
        layout.Items = items;
        layout.Position = new Vector2(layout.Indent, layout.NextRow);
        layout.Size.Y = height;
        layout.ItemIndex = 0;
        UpdateLayout(layout);
    }

    public void LayoutWidth(int width)
    {
        UILayout l = GetLayout();
        l.Size.X = width;
        UpdateLayout(l);
    }

    public void LayoutHeight(int height)
    {
        UILayout l = GetLayout();
        l.Size.Y = height;
        UpdateLayout(l);
    }

    public void LayoutSetNext(UIRect r, bool relative)
    {
        UILayout layout = GetLayout();
        layout.Next = r;
        layout.NextType = relative ? 1 : 2;
        UpdateLayout(layout);
    }

    public UIRect LayoutNext()
    {
        UILayout layout = GetLayout();
        UIRect res;

        if (layout.NextType != 0)
        {
            int type = layout.NextType;
            layout.NextType = 0;
            res = layout.Next;
            if (type == 2) // Absolute
            {
                _lastRect = res;
                UpdateLayout(layout);
                return res;
            }
        }
        else
        {
            if (layout.ItemIndex == layout.Items)
            {
                LayoutRow(layout.Items, null, (int)layout.Size.Y);
                layout = GetLayout();
            }

            res.X = (int)layout.Position.X;
            res.Y = (int)layout.Position.Y;
            res.W = layout.Items > 0 ? layout.Widths[layout.ItemIndex] : (int)layout.Size.X;
            res.H = (int)layout.Size.Y;

            if (res.W == 0)
                res.W = (int)Style.Size.X + Style.Padding * 2;
            if (res.H == 0)
                res.H = (int)Style.Size.Y + Style.Padding * 2;
            if (res.W < 0)
                res.W += layout.Body.W - res.X + 1;
            if (res.H < 0)
                res.H += layout.Body.H - res.Y + 1;

            layout.ItemIndex++;
        }

        layout.Position.X += res.W + Style.Spacing;
        layout.NextRow = Math.Max(layout.NextRow, res.Y + res.H + Style.Spacing);

        res.X += layout.Body.X;
        res.Y += layout.Body.Y;

        layout.Max.X = Math.Max(layout.Max.X, res.X + res.W);
        layout.Max.Y = Math.Max(layout.Max.Y, res.Y + res.H);

        UpdateLayout(layout);
        _lastRect = res;
        return res;
    }
}
