using Kogl.Common;
using Kogl.Core;
using Kogl.Core.Resources;
using Kogl.FreeType;
using Kogl.UI;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class UiExample
{
    private static readonly UIContext _uiContext = new();

    private static float _clearColorR = 0.1f;
    private static float _clearColorG = 0.1f;
    private static float _clearColorB = 0.15f;

    private static bool _showExtraPanel = true;
    private static bool _checkboxState = false;

    public static void Start()
    {
        AppWindow app = new(1024, 768, "Kolpa - UI Example");

        app.OnLoad += static () =>
        {
            Font defaultFont = Font.LoadSdf("assets/fonts/regular.ttf", 24);
            _uiContext.Style.Font = defaultFont;

            LogCat.Info("APP", "UI custom sample initialized.");
        };

        app.OnRender += RenderLoop;

        app.OnUnload += static () =>
        {
            AssetManager.UnloadAll();
            ResourceManager.UnloadAll();
        };

        app.Run();
    }

    private static void RenderLoop(double dt)
    {
        KoRender.Clear(_clearColorR, _clearColorG, _clearColorB, 1.0f);
        KoRender.EnableDepthTest();

        _uiContext.Begin();

        BuildWindowInterface();

        _uiContext.End();

        UIRenderer.Render(_uiContext);
    }

    private static void BuildWindowInterface()
    {
        UIRect windowRect = new(50, 50, 350, 450);

        // --- DEMO WINDOW 1 ---
        if (
            (
                _uiContext.BeginWindow("Engine Control Panel", windowRect, UIOpt.None)
                & UIResult.Active
            ) != 0
        )
        {
            _uiContext.LayoutRow(1, [-1], 24);
            _uiContext.Label("Welcome to the custom UI layer.");

            if ((_uiContext.Header("Background Settings") & UIResult.Active) != 0)
            {
                _uiContext.LayoutRow(1, [-1], 20);

                _uiContext.Label($"Red Channel ({_clearColorR:F2})");
                _uiContext.Slider(ref _clearColorR, 0.0f, 1.0f, 0.01f);

                _uiContext.Label($"Green Channel ({_clearColorG:F2})");
                _uiContext.Slider(ref _clearColorG, 0.0f, 1.0f, 0.01f);

                _uiContext.Label($"Blue Channel ({_clearColorB:F2})");
                _uiContext.Slider(ref _clearColorB, 0.0f, 1.0f, 0.01f);
            }

            if ((_uiContext.Header("Component States") & UIResult.Active) != 0)
            {
                _uiContext.LayoutRow(2, [140, -1], 24);

                _uiContext.Checkbox("Enable Extra Panel", ref _showExtraPanel);
                _uiContext.Checkbox("Boolean Flag Test", ref _checkboxState);
            }

            if ((_uiContext.Header("Action Triggers") & UIResult.Active) != 0)
            {
                _uiContext.LayoutRow(1, [-1], 30);

                if ((_uiContext.Button("Reset Clear Colors") & UIResult.Submit) != 0)
                {
                    _clearColorR = 0.1f;
                    _clearColorG = 0.1f;
                    _clearColorB = 0.15f;
                    LogCat.Info("UI", "Environment colors restored back to engine default values.");
                }
            }

            _uiContext.EndWindow();
        }

        if (_showExtraPanel)
        {
            UIRect alternateRect = new(430, 50, 300, 200);

            if (
                (
                    _uiContext.BeginWindow("Diagnostic Overlay", alternateRect, UIOpt.NoResize)
                    & UIResult.Active
                ) != 0
            )
            {
                _uiContext.LayoutRow(1, [-1], 20);

                _uiContext.Label($"Flag state status: {_checkboxState}");
                _uiContext.Text(
                    "This secondary panel block will automatically collapse when unchecking the state visibility box inside the primary controller interface."
                );

                _uiContext.EndWindow();
            }
        }
    }
}
