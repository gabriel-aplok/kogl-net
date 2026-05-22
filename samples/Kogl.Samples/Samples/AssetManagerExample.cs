// FILE: src/Kogl.Samples/Samples/AssetManagerExample.cs
using System.Numerics;
using Kogl.Common;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class AssetManagerExample
{
    private static Texture _logoTexture = null!;
    private static Task<Texture> _backgroundLoadingTask = null!;
    private static bool _asyncLoadComplete;
    private static float _rotation;

    public static void Start()
    {
        AppWindow app = new(800, 600, "KoGL Engine - Asset Infrastructure Diagnostic");

        app.OnLoad += () =>
        {
            _logoTexture = Assets.Load<Texture>("res://textures/logo.png");
            _backgroundLoadingTask = Assets.LoadAsync<Texture>("res://textures/container.jpg");
        };

        app.OnRender += (dt) =>
        {
            _rotation += (float)dt * 0.5f;

            KoRender.Clear(0.12f, 0.12f, 0.14f, 1.0f);

            // setup uniform matrix layout blocks
            KoRender.MatrixMode(MatrixState.Projection);
            KoRender.LoadIdentity();
            KoRender.Ortho(0, 800, 600, 0, -1, 1);
            KoRender.MatrixMode(MatrixState.ModelView);
            KoRender.LoadIdentity();

            // handle validation checking across thread synchronization primitives
            if (!_asyncLoadComplete && _backgroundLoadingTask.IsCompleted)
            {
                _asyncLoadComplete = true;
                LogCat.Info(
                    "SAMPLE",
                    "Async operation resolved seamlessly without pipeline stalling."
                );
            }

            // draw active asset safely. If you modify assets/textures/logo.png in your file explorer,
            // the system automatically reloads the file without restarting the engine!
            if (_logoTexture != null)
            {
                SpriteRenderer.DrawTexture(
                    _logoTexture.Handle, // uses the underlying explicit system handles mapping directly
                    new Vector2(400, 300),
                    new Vector2(128, 128),
                    pivot: new Vector2(64, 64),
                    rotationRadians: _rotation
                );
            }

            KoRender.Flush();
        };

        app.OnUnload += () =>
        {
            // unload cleanly decrements reference counts and releases the assets when they are no longer needed
            Assets.Unload("res://textures/logo.png");
            Assets.Unload("res://textures/container.jpg");
        };

        app.Run();
    }
}
