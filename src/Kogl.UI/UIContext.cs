using System.Numerics;
using Kogl.Common;
using Kogl.Common.InputManagement;

namespace Kogl.UI;

public partial class UIContext
{
    private const int CommandListSize = 256 * 1024;
    private const int ContainerPoolSize = 48;
    private const int TreeNodePoolSize = 48;

    // Core state
    public UIStyle Style { get; set; } = new UIStyle();
    private uint _hover;
    private uint _focus;
    private uint _lastId;
    private UIRect _lastRect;
    private int _lastZIndex;
    private bool _updatedFocus;
    private int _frame;
    private UIContainer? _hoverRoot;
    private UIContainer? _nextHoverRoot;
    private UIContainer? _scrollTarget;

    // String edits
    private readonly char[] _numberEditBuf = new char[127];

    // private readonly int _numberEditLen;
    // private readonly uint _numberEdit;

    // Text command pool buffer to prevent formatting allocations
    internal char[] TextBuffer = new char[128 * 1024];
    internal int TextBufferIdx;

    // Stacks & Lists
    internal UICommand[] CommandList = new UICommand[CommandListSize];
    internal int CommandListCount;
    private List<UIContainer> _rootList = new(32);
    private List<UIContainer> _containerStack = new(32);
    private List<UIRect> _clipStack = new(32);
    private List<uint> _idStack = new(32);
    private List<UILayout> _layoutStack = new(16);

    // Pools
    private UIPoolItem[] _containerPool = new UIPoolItem[ContainerPoolSize];
    private UIContainer[] _containers = new UIContainer[ContainerPoolSize];
    private UIPoolItem[] _treenodePool = new UIPoolItem[TreeNodePoolSize];

    // Input state
    private Vector2 _mousePos;
    private Vector2 _lastMousePos;
    private Vector2 _mouseDelta;
    private Vector2 _scrollDelta;
    private bool _mouseDown;
    private bool _mousePressed;
    private bool _keyDownShift;
    private bool _keyDownBackspace;
    private bool _keyDownReturn;
    private bool _keyPressedBackspace;
    private bool _keyPressedReturn;
    private string _inputText = string.Empty;

    private readonly UIRect _unclippedRect = new(0, 0, 0x1000000, 0x1000000);

    public UIContext()
    {
        for (int i = 0; i < ContainerPoolSize; i++)
            _containers[i] = new UIContainer();

        InputManager.KeyChar += OnKeyChar;
    }

    private void OnKeyChar(char c)
    {
        // ASCII printable or extended printable range
        if (c >= 32 && c != 127)
            _inputText += c;
    }

    public void UpdateInput()
    {
        _mousePos = InputManager.MousePosition;
        _mouseDelta = _mousePos - _lastMousePos;

        bool isDown = InputManager.IsMouseButtonDown(MouseButton.Left);
        bool isPressed = InputManager.IsMouseButtonPressed(MouseButton.Left);

        _mouseDown = isDown;
        _mousePressed = isPressed;

        _scrollDelta = InputManager.MouseScrollDelta;

        _keyDownShift =
            InputManager.IsKeyDown(Key.ShiftLeft) || InputManager.IsKeyDown(Key.ShiftRight);
        _keyDownBackspace = InputManager.IsKeyDown(Key.Backspace);
        _keyDownReturn = InputManager.IsKeyDown(Key.Enter);

        _keyPressedBackspace = InputManager.IsKeyPressed(Key.Backspace);
        _keyPressedReturn = InputManager.IsKeyPressed(Key.Enter);
    }

    public void Begin()
    {
        if (Style.Font == null)
            LogCat.Error("UI", "UIContext.Style.Font is null before Begin()!");

        UpdateInput();

        CommandListCount = 0;
        TextBufferIdx = 0;
        _rootList.Clear();
        _scrollTarget = null;
        _hoverRoot = _nextHoverRoot;
        _nextHoverRoot = null;
        _frame++;
    }

    public void End()
    {
        if (
            _containerStack.Count != 0
            || _clipStack.Count != 0
            || _idStack.Count != 0
            || _layoutStack.Count != 0
        )
            LogCat.Warn("UI", "Stack leak detected in UI loop!");

        // handle scroll input
        if (_scrollTarget != null)
        {
            _scrollTarget.Scroll.X -= _scrollDelta.X * 20;
            _scrollTarget.Scroll.Y -= _scrollDelta.Y * 20;
        }

        if (!_updatedFocus)
            _focus = 0;
        _updatedFocus = false;

        // bring hover root to front if clicked
        if (
            _mousePressed
            && _nextHoverRoot != null
            && _nextHoverRoot.ZIndex < _lastZIndex
            && _nextHoverRoot.ZIndex >= 0
        )
            BringToFront(_nextHoverRoot);

        // reset input state
        _inputText = string.Empty;
        _lastMousePos = _mousePos;

        // sort root containers by ZIndex
        _rootList.Sort((a, b) => a.ZIndex.CompareTo(b.ZIndex));

        // set root container jump commands
        for (int i = 0; i < _rootList.Count; i++)
        {
            UIContainer cnt = _rootList[i];
            if (i == 0)
            {
                CommandList[0].JumpDst = cnt.HeadIdx + 1;
            }
            else
            {
                UIContainer prev = _rootList[i - 1];
                CommandList[prev.TailIdx].JumpDst = cnt.HeadIdx + 1;
            }

            if (i == _rootList.Count - 1)
            {
                CommandList[cnt.TailIdx].JumpDst = CommandListCount;
            }
        }
    }

    public void SetFocus(uint id)
    {
        _focus = id;
        _updatedFocus = true;
    }

    public uint GetId(string data)
    {
        uint res = _idStack.Count > 0 ? _idStack[^1] : 2166136261;
        foreach (char c in data)
            res = (res ^ c) * 16777619;
        _lastId = res;
        return res;
    }

    public void PushId(string data)
    {
        _idStack.Add(GetId(data));
    }

    public void PopId()
    {
        _idStack.RemoveAt(_idStack.Count - 1);
    }

    private static UIRect IntersectRects(UIRect r1, UIRect r2)
    {
        int x1 = Math.Max(r1.X, r2.X);
        int y1 = Math.Max(r1.Y, r2.Y);
        int x2 = Math.Min(r1.X + r1.W, r2.X + r2.W);
        int y2 = Math.Min(r1.Y + r1.H, r2.Y + r2.H);
        if (x2 < x1)
            x2 = x1;
        if (y2 < y1)
            y2 = y1;
        return new UIRect(x1, y1, x2 - x1, y2 - y1);
    }

    private static bool RectOverlapsVec2(UIRect r, Vector2 p)
    {
        return p.X >= r.X && p.X < r.X + r.W && p.Y >= r.Y && p.Y < r.Y + r.H;
    }

    public void PushClipRect(UIRect rect)
    {
        _clipStack.Add(IntersectRects(rect, GetClipRect()));
    }

    public void PopClipRect()
    {
        _clipStack.RemoveAt(_clipStack.Count - 1);
    }

    public UIRect GetClipRect()
    {
        return _clipStack.Count > 0 ? _clipStack[^1] : _unclippedRect;
    }

    internal UIClipType CheckClip(UIRect r)
    {
        UIRect cr = GetClipRect();
        if (r.X > cr.X + cr.W || r.X + r.W < cr.X || r.Y > cr.Y + cr.H || r.Y + r.H < cr.Y)
            return UIClipType.All;
        if (r.X >= cr.X && r.X + r.W <= cr.X + cr.W && r.Y >= cr.Y && r.Y + r.H <= cr.Y + cr.H)
            return UIClipType.None;
        return UIClipType.Part;
    }

    internal UIContainer GetCurrentContainer()
    {
        return _containerStack[^1];
    }

    internal UIContainer? GetContainer(string name)
    {
        return GetContainerInternal(GetId(name), UIOpt.None);
    }

    internal void BringToFront(UIContainer cnt)
    {
        cnt.ZIndex = ++_lastZIndex;
    }

    private UIContainer? GetContainerInternal(uint id, UIOpt opt)
    {
        int idx = PoolGet(_containerPool, id);
        if (idx >= 0)
        {
            if (_containers[idx].Open || (opt & UIOpt.Closed) == 0)
                PoolUpdate(_containerPool, idx);
            return _containers[idx];
        }
        if ((opt & UIOpt.Closed) != 0)
            return null;

        idx = PoolInit(_containerPool, id);
        UIContainer cnt = _containers[idx];
        cnt.HeadIdx = 0;
        cnt.TailIdx = 0;
        cnt.Rect = new UIRect();
        cnt.Body = new UIRect();
        cnt.ContentSize = Vector2.Zero;
        cnt.Scroll = Vector2.Zero;
        cnt.Open = true;
        BringToFront(cnt);
        return cnt;
    }

    private int PoolInit(UIPoolItem[] items, uint id)
    {
        int n = -1;
        int f = _frame;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].LastUpdate < f)
            {
                f = items[i].LastUpdate;
                n = i;
            }
        }
        items[n].Id = id;
        PoolUpdate(items, n);
        return n;
    }

    private static int PoolGet(UIPoolItem[] items, uint id)
    {
        for (int i = 0; i < items.Length; i++)
            if (items[i].Id == id)
                return i;
        return -1;
    }

    private void PoolUpdate(UIPoolItem[] items, int idx)
    {
        items[idx].LastUpdate = _frame;
    }
}
