using System.Numerics;
using Kogl.FreeType;

namespace Kogl.UI;

public partial class UIContext
{
    private bool InHoverRoot()
    {
        for (int i = _containerStack.Count - 1; i >= 0; i--)
        {
            if (_containerStack[i] == _hoverRoot)
                return true;
            if (_containerStack[i].HeadIdx != 0)
                break; // root boundary
        }
        return false;
    }

    private void DrawControlFrame(uint id, UIRect rect, UIColorId colorid, UIOpt opt)
    {
        if ((opt & UIOpt.NoFrame) != 0)
            return;
        int colorOffset = _focus == id ? 2 : (_hover == id ? 1 : 0);
        DrawFrame(rect, colorid + colorOffset);
    }

    private void DrawControlText(string str, UIRect rect, UIColorId colorid, UIOpt opt)
    {
        Vector2 pos = new();
        Font font = Style.Font;
        int tw = (int)KoGLText.Measure(font, str.AsSpan()).X;

        PushClipRect(rect);
        pos.Y = rect.Y + (rect.H - font.LineHeight) / 2;
        if ((opt & UIOpt.AlignCenter) != 0)
            pos.X = rect.X + (rect.W - tw) / 2;
        else if ((opt & UIOpt.AlignRight) != 0)
            pos.X = rect.X + rect.W - tw - Style.Padding;
        else
            pos.X = rect.X + Style.Padding;

        DrawText(str, pos, Style.Colors[(int)colorid]);
        PopClipRect();
    }

    private void DrawControlTextBuffer(
        int start,
        int len,
        UIRect rect,
        UIColorId colorid,
        UIOpt opt
    )
    {
        Vector2 pos = new();
        Font font = Style.Font;
        int tw = (int)KoGLText.Measure(font, new ReadOnlySpan<char>(TextBuffer, start, len)).X;

        PushClipRect(rect);
        pos.Y = rect.Y + (rect.H - font.LineHeight) / 2;
        if ((opt & UIOpt.AlignCenter) != 0)
            pos.X = rect.X + (rect.W - tw) / 2;
        else if ((opt & UIOpt.AlignRight) != 0)
            pos.X = rect.X + rect.W - tw - Style.Padding;
        else
            pos.X = rect.X + Style.Padding;

        DrawTextBuffer(start, len, pos, Style.Colors[(int)colorid]);
        PopClipRect();
    }

    private bool MouseOver(UIRect rect)
    {
        return RectOverlapsVec2(rect, _mousePos)
            && RectOverlapsVec2(GetClipRect(), _mousePos)
            && InHoverRoot();
    }

    private void UpdateControl(uint id, UIRect rect, UIOpt opt)
    {
        bool mouseover = MouseOver(rect);

        if (_focus == id)
            _updatedFocus = true;
        if ((opt & UIOpt.NoInteract) != 0)
            return;
        if (mouseover && !_mouseDown)
            _hover = id;

        if (_focus == id)
        {
            if (_mousePressed && !mouseover)
                SetFocus(0);
            if (!_mouseDown && (opt & UIOpt.HoldFocus) == 0)
                SetFocus(0);
        }

        if (_hover == id)
        {
            if (_mousePressed)
                SetFocus(id);
            else if (!mouseover)
                _hover = 0;
        }
    }

    public void Text(string text)
    {
        int width = -1;
        Font font = Style.Font;
        Vector4 color = Style.Colors[(int)UIColorId.Text];

        LayoutBeginColumn();
        LayoutRow(1, [width], font.LineHeight);

        string[] lines = text.Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries); // Basic wrap simulation
        UIRect r = LayoutNext();
        int w = 0;
        string currentLine = "";

        foreach (string word in lines)
        {
            int wordW = (int)KoGLText.Measure(font, word.AsSpan()).X;
            if (w + wordW > r.W && currentLine.Length > 0)
            {
                DrawText(currentLine, new Vector2(r.X, r.Y), color);
                r = LayoutNext();
                currentLine = word + " ";
                w = wordW + (int)KoGLText.Measure(font, " ".AsSpan()).X;
            }
            else
            {
                currentLine += word + " ";
                w += wordW + (int)KoGLText.Measure(font, " ".AsSpan()).X;
            }
        }

        if (currentLine.Length > 0)
            DrawText(currentLine, new Vector2(r.X, r.Y), color);

        LayoutEndColumn();
    }

    public void Label(string text)
    {
        DrawControlText(text, LayoutNext(), UIColorId.Text, 0);
    }

    public UIResult Button(string label, UIIcon icon = UIIcon.None, UIOpt opt = UIOpt.AlignCenter)
    {
        UIResult res = UIResult.None;
        uint id = label != null ? GetId(label) : GetId(icon.ToString());
        UIRect r = LayoutNext();
        UpdateControl(id, r, opt);

        if (_mousePressed && _focus == id)
            res |= UIResult.Submit;

        DrawControlFrame(id, r, UIColorId.Button, opt);
        if (label != null)
            DrawControlText(label, r, UIColorId.Text, opt);
        if (icon != UIIcon.None)
            DrawIcon(icon, r, Style.Colors[(int)UIColorId.Text]);

        return res;
    }

    public UIResult Checkbox(string label, ref bool state)
    {
        UIResult res = UIResult.None;
        uint id = GetId(label + "chk");
        UIRect r = LayoutNext();
        UIRect box = new(r.X, r.Y, r.H, r.H);
        UpdateControl(id, r, 0);

        if (_mousePressed && _focus == id)
        {
            res |= UIResult.Change;
            state = !state;
        }

        DrawControlFrame(id, box, UIColorId.Base, 0);
        if (state)
            DrawIcon(UIIcon.Check, box, Style.Colors[(int)UIColorId.Text]);

        r = new UIRect(r.X + box.W, r.Y, r.W - box.W, r.H);
        DrawControlText(label, r, UIColorId.Text, 0);

        return res;
    }

    public UIResult Slider(
        ref float value,
        float low,
        float high,
        float step = 0f,
        string fmt = "0.00",
        UIOpt opt = UIOpt.AlignCenter
    )
    {
        UIResult res = UIResult.None;
        float last = value,
            v = last;
        uint id = GetId(fmt + high + low); // unique id base
        UIRect baseRect = LayoutNext();

        UpdateControl(id, baseRect, opt);

        if (_focus == id && (_mouseDown || _mousePressed))
        {
            v = low + (_mousePos.X - baseRect.X) * (high - low) / baseRect.W;
            if (step > 0)
                v = (float)Math.Round((v + step / 2) / step) * step;
        }

        value = Math.Clamp(v, low, high);
        if (last != value)
            res |= UIResult.Change;

        DrawControlFrame(id, baseRect, UIColorId.Base, opt);

        int w = Style.ThumbSize;
        int x = (int)((value - low) * (baseRect.W - w) / (high - low));
        UIRect thumb = new(baseRect.X + x, baseRect.Y, w, baseRect.H);
        DrawControlFrame(id, thumb, UIColorId.Button, opt);

        // Zero-alloc formatting
        Span<char> spanBuf = stackalloc char[32];
        value.TryFormat(spanBuf, out int charsWritten, fmt);

        // Push into TextBuffer
        int start = TextBufferIdx;
        spanBuf
            .Slice(0, charsWritten)
            .CopyTo(new Span<char>(TextBuffer, TextBufferIdx, charsWritten));
        TextBufferIdx += charsWritten;

        DrawControlTextBuffer(start, charsWritten, baseRect, UIColorId.Text, opt);

        return res;
    }

    private UIResult HeaderBase(string label, bool isTreeNode, UIOpt opt)
    {
        uint id = GetId(label);
        int idx = PoolGet(_treenodePool, id);
        LayoutRow(1, [-1], 0);

        bool active = idx >= 0;
        bool expanded = (opt & UIOpt.Expanded) != 0 ? !active : active;
        UIRect r = LayoutNext();
        UpdateControl(id, r, 0);

        if (_mousePressed && _focus == id)
            active = !active;

        if (idx >= 0)
        {
            if (active)
                PoolUpdate(_treenodePool, idx);
            else
                _treenodePool[idx] = default;
        }
        else if (active)
        {
            PoolInit(_treenodePool, id);
        }

        if (isTreeNode)
        {
            if (_hover == id)
                DrawFrame(r, UIColorId.ButtonHover);
        }
        else
        {
            DrawControlFrame(id, r, UIColorId.Button, 0);
        }

        DrawIcon(
            expanded ? UIIcon.Expanded : UIIcon.Collapsed,
            new UIRect(r.X, r.Y, r.H, r.H),
            Style.Colors[(int)UIColorId.Text]
        );
        r.X += r.H - Style.Padding;
        r.W -= r.H - Style.Padding;
        DrawControlText(label, r, UIColorId.Text, 0);

        return expanded ? UIResult.Active : UIResult.None;
    }

    public UIResult Header(string label, UIOpt opt = UIOpt.None)
    {
        return HeaderBase(label, false, opt);
    }

    public UIResult BeginTreeNode(string label, UIOpt opt = UIOpt.None)
    {
        UIResult res = HeaderBase(label, true, opt);
        if ((res & UIResult.Active) != 0)
        {
            UILayout l = GetLayout();
            l.Indent += Style.Indent;
            UpdateLayout(l);
            PushId(label);
        }
        return res;
    }

    public void EndTreeNode()
    {
        UILayout l = GetLayout();
        l.Indent -= Style.Indent;
        UpdateLayout(l);
        PopId();
    }

    private void Scrollbar(UIContainer cnt, ref UIRect body, Vector2 cs, bool isVertical)
    {
        int maxscroll = isVertical ? (int)cs.Y - body.H : (int)cs.X - body.W;

        if (maxscroll > 0 && (isVertical ? body.H : body.W) > 0)
        {
            uint id = GetId("!scrollbar" + (isVertical ? "y" : "x"));
            UIRect baseRect = body;

            if (isVertical)
            {
                baseRect.X = body.X + body.W;
                baseRect.W = Style.ScrollbarSize;
            }
            else
            {
                baseRect.Y = body.Y + body.H;
                baseRect.H = Style.ScrollbarSize;
            }

            UpdateControl(id, baseRect, 0);
            if (_focus == id && _mouseDown)
            {
                if (isVertical)
                    cnt.Scroll.Y += _mouseDelta.Y * cs.Y / baseRect.H;
                else
                    cnt.Scroll.X += _mouseDelta.X * cs.X / baseRect.W;
            }

            if (isVertical)
                cnt.Scroll.Y = Math.Clamp(cnt.Scroll.Y, 0, maxscroll);
            else
                cnt.Scroll.X = Math.Clamp(cnt.Scroll.X, 0, maxscroll);

            DrawFrame(baseRect, UIColorId.ScrollBase);
            UIRect thumb = baseRect;

            if (isVertical)
            {
                thumb.H = Math.Max(Style.ThumbSize, baseRect.H * body.H / (int)cs.Y);
                thumb.Y += (int)(cnt.Scroll.Y * (baseRect.H - thumb.H) / maxscroll);
            }
            else
            {
                thumb.W = Math.Max(Style.ThumbSize, baseRect.W * body.W / (int)cs.X);
                thumb.X += (int)(cnt.Scroll.X * (baseRect.W - thumb.W) / maxscroll);
            }

            DrawFrame(thumb, UIColorId.ScrollThumb);

            if (MouseOver(body))
                _scrollTarget = cnt;
        }
        else
        {
            if (isVertical)
                cnt.Scroll.Y = 0;
            else
                cnt.Scroll.X = 0;
        }
    }

    private void PushContainerBody(UIContainer cnt, UIRect body, UIOpt opt)
    {
        if ((opt & UIOpt.NoScroll) == 0)
        {
            int sz = Style.ScrollbarSize;
            Vector2 cs = new(
                cnt.ContentSize.X + Style.Padding * 2,
                cnt.ContentSize.Y + Style.Padding * 2
            );
            PushClipRect(body);
            if (cs.Y > cnt.Body.H)
                body.W -= sz;
            if (cs.X > cnt.Body.W)
                body.H -= sz;
            Scrollbar(cnt, ref body, cs, true);
            Scrollbar(cnt, ref body, cs, false);
            PopClipRect();
        }

        PushLayout(
            new UIRect(
                body.X + Style.Padding,
                body.Y + Style.Padding,
                body.W - Style.Padding * 2,
                body.H - Style.Padding * 2
            ),
            cnt.Scroll
        );
        cnt.Body = body;
    }

    public UIResult BeginWindow(string title, UIRect rect, UIOpt opt = UIOpt.None)
    {
        uint id = GetId(title);
        UIContainer? cnt = GetContainerInternal(id, opt);
        if (cnt == null || !cnt.Open)
            return UIResult.None;

        PushId(title);

        if (cnt.Rect.W == 0)
            cnt.Rect = rect;

        _containerStack.Add(cnt);
        _rootList.Add(cnt);
        cnt.HeadIdx = PushJump(-1);

        if (
            RectOverlapsVec2(cnt.Rect, _mousePos)
            && (_nextHoverRoot == null || cnt.ZIndex > _nextHoverRoot.ZIndex)
        )
            _nextHoverRoot = cnt;

        PushClipRect(_unclippedRect);

        rect = cnt.Rect;
        UIRect body = cnt.Rect;

        if ((opt & UIOpt.NoFrame) == 0)
            DrawFrame(rect, UIColorId.WindowBg);

        if ((opt & UIOpt.NoTitle) == 0)
        {
            UIRect tr = rect;
            tr.H = Style.TitleHeight;
            DrawFrame(tr, UIColorId.TitleBg);

            uint titleId = GetId("!title");
            UpdateControl(titleId, tr, opt);
            DrawControlText(title, tr, UIColorId.TitleText, opt);

            if (titleId == _focus && _mouseDown)
            {
                cnt.Rect.X += (int)_mouseDelta.X;
                cnt.Rect.Y += (int)_mouseDelta.Y;
            }
            body.Y += tr.H;
            body.H -= tr.H;

            if ((opt & UIOpt.NoClose) == 0)
            {
                uint closeId = GetId("!close");
                UIRect cr = new(tr.X + tr.W - tr.H, tr.Y, tr.H, tr.H);
                tr.W -= cr.W;
                DrawIcon(UIIcon.Close, cr, Style.Colors[(int)UIColorId.TitleText]);
                UpdateControl(closeId, cr, opt);
                if (_mousePressed && closeId == _focus)
                    cnt.Open = false;
            }
        }

        PushContainerBody(cnt, body, opt);

        if ((opt & UIOpt.NoResize) == 0)
        {
            int sz = Style.TitleHeight;
            uint resizeId = GetId("!resize");
            UIRect rr = new(rect.X + rect.W - sz, rect.Y + rect.H - sz, sz, sz);
            UpdateControl(resizeId, rr, opt);
            if (resizeId == _focus && _mouseDown)
            {
                cnt.Rect.W = Math.Max(96, cnt.Rect.W + (int)_mouseDelta.X);
                cnt.Rect.H = Math.Max(64, cnt.Rect.H + (int)_mouseDelta.Y);
            }
        }

        if ((opt & UIOpt.AutoSize) != 0)
        {
            UIRect r = GetLayout().Body;
            cnt.Rect.W = (int)cnt.ContentSize.X + (cnt.Rect.W - r.W);
            cnt.Rect.H = (int)cnt.ContentSize.Y + (cnt.Rect.H - r.H);
        }

        if ((opt & UIOpt.Popup) != 0 && _mousePressed && _hoverRoot != cnt)
            cnt.Open = false;

        PushClipRect(cnt.Body);
        return UIResult.Active;
    }

    public void EndWindow()
    {
        PopClipRect();

        UIContainer cnt = GetCurrentContainer();
        cnt.TailIdx = PushJump(-1);
        CommandList[cnt.HeadIdx].JumpDst = CommandListCount;

        PopClipRect();
        PopContainer();
    }
}
