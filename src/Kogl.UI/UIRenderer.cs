using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.FreeType;

namespace Kogl.UI;

/// <summary>Backend-agnostic integration bridging the UI subsystem strictly to KoRender and the Batcher</summary>
public static class UIRenderer
{
    public static void Render(UIContext ctx)
    {
        if (ctx.CommandListCount == 0)
            return;

        KoRender.Flush();

        KoRender.PushMatrix();
        KoRender.LoadIdentity();

        KoRender.Ortho(0, KoRender.ViewportWidth, KoRender.ViewportHeight, 0, -1.0f, 1.0f);

        KoRender.DisableDepthTest();
        KoRender.EnableBlending();
        KoRender.BlendFunc(BlendingFactorState.SrcAlpha, BlendingFactorState.OneMinusSrcAlpha);
        KoRender.DisableCulling();

        bool isScissorActive = false;

        int cmdIdx = 0;
        while (cmdIdx < ctx.CommandListCount)
        {
            ref UICommand cmd = ref ctx.CommandList[cmdIdx];

            if (cmd.Type == UICommandType.Jump)
            {
                cmdIdx = cmd.JumpDst;
                continue;
            }

            switch (cmd.Type)
            {
                case UICommandType.Clip:
                    if (isScissorActive)
                        KoRender.EndScissor();

                    KoRender.BeginScissor(cmd.Rect.X, cmd.Rect.Y, cmd.Rect.W, cmd.Rect.H);
                    isScissorActive = true;
                    break;
                case UICommandType.Rect:
                    KoRender.UseDefaultTexture();
                    KoRender.UseDefaultShader();
                    KoRender.Color4(cmd.Color.X, cmd.Color.Y, cmd.Color.Z, cmd.Color.W);

                    KoRender.Begin(PrimitiveMode.Quads);
                    KoRender.Vertex2(cmd.Rect.X, cmd.Rect.Y);
                    KoRender.Vertex2(cmd.Rect.X + cmd.Rect.W, cmd.Rect.Y);
                    KoRender.Vertex2(cmd.Rect.X + cmd.Rect.W, cmd.Rect.Y + cmd.Rect.H);
                    KoRender.Vertex2(cmd.Rect.X, cmd.Rect.Y + cmd.Rect.H);
                    KoRender.End();
                    break;
                case UICommandType.Text:
                    KoRender.Flush();

                    ReadOnlySpan<char> textSpan =
                        cmd.TextStr != null
                            ? cmd.TextStr.AsSpan()
                            : new ReadOnlySpan<char>(ctx.TextBuffer, cmd.TextStart, cmd.TextLen);

                    KoGLText.DrawText(
                        cmd.Font,
                        textSpan,
                        new Vector2(cmd.Rect.X, cmd.Rect.Y),
                        cmd.Color
                    );
                    break;

                case UICommandType.Icon:
                    KoRender.UseDefaultTexture();
                    KoRender.UseDefaultShader();
                    DrawProceduralIcon(cmd.Icon, cmd.Rect, cmd.Color);
                    break;
            }
            cmdIdx++;
        }

        KoRender.Flush();

        if (isScissorActive)
            KoRender.EndScissor();

        KoRender.PopMatrix();
        KoRender.EnableDepthTest();
        KoRender.EnableCulling();
    }

    private static void DrawProceduralIcon(UIIcon icon, UIRect rect, Vector4 color)
    {
        KoRender.Color4(color.X, color.Y, color.Z, color.W);

        switch (icon)
        {
            case UIIcon.Close:
                KoRender.LineWidth(2.0f);
                KoRender.Begin(PrimitiveMode.Lines);
                KoRender.Vertex2(rect.X + 4, rect.Y + 4);
                KoRender.Vertex2(rect.X + rect.W - 4, rect.Y + rect.H - 4);
                KoRender.Vertex2(rect.X + rect.W - 4, rect.Y + 4);
                KoRender.Vertex2(rect.X + 4, rect.Y + rect.H - 4);
                KoRender.End();
                KoRender.LineWidth(1.0f);
                break;

            case UIIcon.Check:
                KoRender.LineWidth(2.0f);
                KoRender.Begin(PrimitiveMode.Lines);
                KoRender.Vertex2(rect.X + 4, rect.Y + (rect.H / 2));
                KoRender.Vertex2(rect.X + (rect.W / 2), rect.Y + rect.H - 4);
                KoRender.Vertex2(rect.X + (rect.W / 2), rect.Y + rect.H - 4);
                KoRender.Vertex2(rect.X + rect.W - 4, rect.Y + 4);
                KoRender.End();
                KoRender.LineWidth(1.0f);
                break;

            case UIIcon.Collapsed:
                KoRender.Begin(PrimitiveMode.Triangles);
                KoRender.Vertex2(rect.X + 4, rect.Y + 4);
                KoRender.Vertex2(rect.X + rect.W - 4, rect.Y + (rect.H / 2));
                KoRender.Vertex2(rect.X + 4, rect.Y + rect.H - 4);
                KoRender.End();
                break;

            case UIIcon.Expanded:
                KoRender.Begin(PrimitiveMode.Triangles);
                KoRender.Vertex2(rect.X + 4, rect.Y + 4);
                KoRender.Vertex2(rect.X + rect.W - 4, rect.Y + 4);
                KoRender.Vertex2(rect.X + (rect.W / 2), rect.Y + rect.H - 4);
                KoRender.End();
                break;
        }
    }
}
