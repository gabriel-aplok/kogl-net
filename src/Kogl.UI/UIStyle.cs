using System.Numerics;
using Kogl.FreeType;

namespace Kogl.UI;

public class UIStyle
{
    public Font Font { get; set; } = null!;
    public Vector2 Size { get; set; } = new Vector2(68, 10);
    public int Padding { get; set; } = 6;
    public int Spacing { get; set; } = 4;
    public int Indent { get; set; } = 20;
    public int TitleHeight { get; set; } = 28;
    public int ScrollbarSize { get; set; } = 10;
    public int ThumbSize { get; set; } = 6;

    public Vector4[] Colors { get; } = new Vector4[(int)UIColorId.Max];

    public UIStyle()
    {
        Colors[(int)UIColorId.Text] = new Vector4(0.95f, 0.95f, 0.96f, 1.0f); // #F3F4F6
        Colors[(int)UIColorId.Border] = new Vector4(0.15f, 0.15f, 0.17f, 1.0f); // #27272A
        Colors[(int)UIColorId.WindowBg] = new Vector4(0.06f, 0.06f, 0.07f, 1.0f); // #0F0F11
        Colors[(int)UIColorId.TitleBg] = new Vector4(0.10f, 0.10f, 0.11f, 1.0f); // #1A1A1E
        Colors[(int)UIColorId.TitleText] = new Vector4(0.98f, 0.98f, 0.99f, 1.0f); // #FAFAFB

        Colors[(int)UIColorId.PanelBg] = new Vector4(0.09f, 0.09f, 0.10f, 0.95f); // Semi-transparent dark

        Colors[(int)UIColorId.Button] = new Vector4(0.16f, 0.16f, 0.18f, 1.0f); // #29292F
        Colors[(int)UIColorId.ButtonHover] = new Vector4(0.22f, 0.22f, 0.25f, 1.0f); // #38383F
        Colors[(int)UIColorId.ButtonFocus] = new Vector4(0.28f, 0.28f, 0.32f, 1.0f); // #48484F

        Colors[(int)UIColorId.Base] = new Vector4(0.12f, 0.12f, 0.14f, 1.0f); // #1F1F23
        Colors[(int)UIColorId.BaseHover] = new Vector4(0.18f, 0.18f, 0.20f, 1.0f);
        Colors[(int)UIColorId.BaseFocus] = new Vector4(0.24f, 0.24f, 0.27f, 1.0f);

        Colors[(int)UIColorId.ScrollBase] = new Vector4(0.14f, 0.14f, 0.16f, 1.0f);
        Colors[(int)UIColorId.ScrollThumb] = new Vector4(0.35f, 0.35f, 0.38f, 1.0f); // Softer thumb
    }
}
