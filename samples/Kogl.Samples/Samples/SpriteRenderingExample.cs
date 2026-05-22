using System.Numerics;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.FreeType;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class SpriteRenderingExample
{
    private static readonly AppWindow _app = new(
        800,
        600,
        "Kolpa - Multi-Texture Material Example"
    );
    private static Font _uiFont = null!;

    private static SpriteAtlas _atlas = null!;
    private static Texture _playerTex = null!;
    private static float _time;

    public static void Start()
    {
        _app.OnLoad += static () =>
        {
            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            _atlas = new SpriteAtlas();
            _playerTex = ResourceManager.Load<Texture>("assets/stalker.png");

            _atlas.AddPixelRegion("player_idle", _playerTex.Handle, 0, 0, 32, 32, 128, 96);
            _atlas.AddPixelRegion("enemy_ship", _playerTex.Handle, 32, 0, 32, 32, 128, 96);
        };

        _app.OnRender += RenderLoop;

        _app.OnUnload += () =>
        {
            _uiFont?.Dispose();
            _atlas?.Dispose();
        };

        _app.Run();
    }

    private static void RenderLoop(double dt)
    {
        _time += (float)dt;

        KoRender.Clear(1.0f, 1.0f, 1.0f, 1.0f);

        KoRender.EnableBlending();
        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, _app.Width, _app.Height, 0, -1, 1);
        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        SpriteRenderer.Draw(_atlas.Get("player_idle"), new Vector2(250, 250));

        SpriteRenderer.DrawTexture(
            texture: _playerTex.Handle,
            position: new Vector2(250, 250),
            size: new Vector2(64, 64),
            pivot: new Vector2(32, 32)
        );

        float rotation = _time;
        float scaleMod = 1.0f + (MathF.Sin(_time * 2f) * 0.2f);

        SpriteRenderer.Draw(
            sprite: _atlas.Get("enemy_ship"),
            position: new Vector2(600, 600),
            size: new Vector2(64 * scaleMod, 64 * scaleMod),
            pivot: new Vector2(32, 32),
            rotationRadians: rotation,
            tint: new Vector4(1, 0.8f, 0.8f, 1)
        );

        KoGLText.DrawText(_uiFont, "aaaaa", new Vector2(10, 10), new Vector4(0.2f, 0.8f, 0.2f, 1));

        KoRender.Flush();
    }
}
