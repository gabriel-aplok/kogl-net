using System.Numerics;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Maths;
using Kogl.Core.Rendering;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

public class SampleGizmo
{
    private static AppWindow? _window;
    private static Camera? _camera;
    private static Transform _myCubeTransform;

    public static void Start()
    {
        _window = new AppWindow(1280, 720, "Kogl Gizmo Test");
        _window.OnLoad += OnLoad;
        _window.OnRender += OnRender;
        _window.Run();
    }

    private static void OnLoad()
    {
        _camera = new Camera
        {
            Position = new Vector3(5, 5, 5),
            ViewportWidth = 1280,
            ViewportHeight = 720,
        };
        _camera.LookAt(Vector3.Zero);

        _myCubeTransform = Transform.Identity;
        _myCubeTransform.Translation = new Vector3(0, 1, 0);
    }

    private static void OnRender(double dt)
    {
        if (_window == null || _camera == null)
            return;

        KoRender.Clear(0.2f, 0.2f, 0.2f, 1.0f);
        KoRender.BeginCamera(_camera);

        // draw reference grid
        KoRender.Begin(PrimitiveMode.Lines);
        KoRender.Color3(0.4f, 0.4f, 0.4f);
        for (int i = -10; i <= 10; i++)
        {
            KoRender.Vertex3(i, 0, -10);
            KoRender.Vertex3(i, 0, 10);
            KoRender.Vertex3(-10, 0, i);
            KoRender.Vertex3(10, 0, i);
        }
        KoRender.End();

        // ----------------------------------------------------
        // evaluate gizmo interactively!
        // The ID pattern safely protects collision checks against
        // overlaps if drawing multiple gizmos in sequence
        // ----------------------------------------------------
        bool interacting = KoGizmo.DrawGizmo3D(100, GizmoFlags.Translate, ref _myCubeTransform);

        // draw the cube
        KoRender.PushMatrix();
        KoRender.Multiply(_myCubeTransform.ToMatrix());

        if (interacting)
            KoRender.Color3(1, 1, 0);
        else
            KoRender.Color3(1, 1, 1);

        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.End();

        KoRender.PopMatrix();
        KoRender.EndCamera();
    }
}
