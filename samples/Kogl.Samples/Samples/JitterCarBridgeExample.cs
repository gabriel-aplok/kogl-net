using System.Numerics;
using System.Runtime.InteropServices;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.Samples.Samples.Car;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal static class JitterCarBridgeExample
{
    private static readonly AppWindow _app = new(1200, 800, "Kolpa - Raycast Car Example");
    private static readonly Camera _camera = new();

    private static Shader _prototypeShader = null!;
    private static Material _carChassisMat = null!;
    private static Material _wheelMat = null!;
    private static Material _bridgeMat = null!;
    private static Material _floorMat = null!;

    private static Texture _protoWhiteTex = null!;
    private static Texture _protoGreenTex = null!;

    private static World _physicsWorld = null!;
    private static RigidBody _floorBody = null!;
    private static ConstraintCar _carInstance = null!;

    private static readonly List<HingeJoint> _bridgeHinges = new(32);
    private static readonly List<RigidBody> _bridgeSegments = new(32);

    private static float _yaw = -90f;
    private static float _pitch = 0f;

    public static void Start()
    {
        _app.OnLoad += () =>
        {
            _camera.Position = new Vector3(-20.0f, 15.0f, -10.0f);
            _camera.Projection = CameraProjection.Perspective;
            _camera.Fov = 50f;
            _camera.LookAt(new Vector3(2.0f, 6.0f, -20.0f));

            InputMap.Bind("Up", Key.Up);
            InputMap.Bind("Down", Key.Down);
            InputMap.Bind("Left", Key.Left);
            InputMap.Bind("Right", Key.Right);

            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);

            const string vs = """
                #version 330 core
                layout(location = 0) in vec3 aPos;
                layout(location = 1) in vec2 aTex;
                layout(location = 2) in vec4 aCol;

                out vec2 fTex;
                out vec4 fCol;

                uniform mat4 uMVP;

                void main() {
                    gl_Position = uMVP * vec4(aPos, 1.0);
                    fTex = aTex;
                    fCol = aCol;
                }
                """;

            const string fs = """
                #version 330 core
                in vec2 fTex;
                in vec4 fCol;

                out vec4 FragColor;

                uniform sampler2D uTex;
                uniform vec2 uUVScale;
                uniform vec2 uUVOffset;

                void main() {
                    vec2 transformedUV = (fTex * uUVScale) + uUVOffset;
                    FragColor = texture(uTex, transformedUV) * fCol  ;
                }
                """;

            _prototypeShader = Shader.Create(vs, fs);
            _prototypeShader.AddProperty("uTex", ShaderPropertyType.Texture2D);
            _prototypeShader.AddProperty("uUVScale", ShaderPropertyType.Vec2);
            _prototypeShader.AddProperty("uUVOffset", ShaderPropertyType.Vec2);

            _protoWhiteTex = Assets.Load<Texture>("res://textures/prototype/light/texture_08.png");
            _protoGreenTex = Assets.Load<Texture>("res://textures/prototype/green/texture_10.png");

            Material baseMat = new(_prototypeShader) { DepthTest = true, Blending = false };
            baseMat.SetTexture("uTex", _protoWhiteTex);
            baseMat.SetVector2("uUVScale", Vector2.One);
            baseMat.SetVector2("uUVOffset", Vector2.Zero);

            // floor
            _floorMat = baseMat.CreateInstance();
            _floorMat.SetVector2("uUVScale", new Vector2(25.0f, 25.0f));
            _floorMat.SetVector2("uUVOffset", Vector2.Zero);

            _bridgeMat = baseMat.CreateInstance();
            _bridgeMat.SetVector2("uUVScale", new Vector2(1.0f, 1.0f));

            _carChassisMat = baseMat.CreateInstance();
            _carChassisMat.SetTexture("uTex", _protoGreenTex);
            _carChassisMat.SetVector2("uUVScale", new Vector2(1.0f, 1.0f));

            _wheelMat = baseMat.CreateInstance();
            _wheelMat.SetTexture("uTex", _protoGreenTex);
            _wheelMat.SetVector2("uUVScale", Vector2.One);

            _physicsWorld = new World { SubstepCount = 4, SolverIterations = (2, 2) };

            _floorBody = _physicsWorld.CreateRigidBody();
            _floorBody.AddShape(new BoxShape(50f));
            _floorBody.Position = new JVector(0f, -25f, 0f);
            _floorBody.MotionType = MotionType.Static;

            _bridgeHinges.Clear();
            _bridgeSegments.Clear();

            RigidBody previousSegment = null!;
            JVector startPos = new(-10, 8, -20);
            const int numElements = 30;

            for (int i = 0; i < numElements; i++)
            {
                RigidBody segmentBody = _physicsWorld.CreateRigidBody();
                segmentBody.AddShape(new BoxShape(0.7f, 0.1f, 4f));
                segmentBody.Position = startPos + new JVector(i * 0.8f, 0, 0);

                _bridgeSegments.Add(segmentBody);

                if (i == 0)
                {
                    _ = new HingeJoint(
                        _physicsWorld,
                        _physicsWorld.NullBody,
                        segmentBody,
                        startPos + new JVector((i * 0.8f) - 0.7f, 0, 0),
                        JVector.UnitZ
                    );
                }
                else
                {
                    HingeJoint hinge = new(
                        _physicsWorld,
                        previousSegment,
                        segmentBody,
                        startPos + new JVector((i * 0.8f) - 0.1f, 0, 0),
                        JVector.UnitZ
                    );

                    hinge.BallSocket.Softness = 0.1f;
                    _bridgeHinges.Add(hinge);
                }

                if (i == numElements - 1)
                {
                    _ = new HingeJoint(
                        _physicsWorld,
                        segmentBody,
                        _physicsWorld.NullBody,
                        startPos + new JVector((i * 0.8f) + 0.7f, 0, 0),
                        JVector.UnitZ
                    );
                }

                previousSegment = segmentBody;
            }

            _carInstance = new ConstraintCar();
            JVector carPos = new(0, 9, 0);
            JQuaternion rot = JQuaternion.CreateRotationY(MathF.PI / 2.0f);

            _carInstance.BuildCar(
                _physicsWorld,
                carPos,
                body =>
                {
                    body.Position = JVector.Transform(body.Position, rot) + carPos;
                    body.Orientation = rot;
                }
            );
        };

        _app.OnRender += RenderLoop;
        _app.OnResizeEvent += (width, height) =>
        {
            _camera.UpdateViewport(width, height);
        };
        _app.Run();
        _app.Maximize();
    }

    private static void RenderLoop(double dt)
    {
        float frameTime = (float)dt;
        UpdateInput(frameTime);

        Span<HingeJoint> hingesSpan = CollectionsMarshal.AsSpan(_bridgeHinges);
        for (int i = hingesSpan.Length; i-- > 0; )
        {
            HingeJoint hinge = hingesSpan[i];
            if (hinge.BallSocket.Impulse.LengthSquared() > 0.25f)
            {
                hinge.Remove();
                _bridgeHinges.RemoveAt(i);
            }
        }

        _carInstance.UpdateControls();
        _physicsWorld.Step(frameTime, true);

        KoRender.Clear(0.12f, 0.14f, 0.18f, 1.0f);
        KoRender.EnableDepthTest();

        KoRender.BeginCamera(_camera);
        DrawWorld();
        KoRender.EndCamera();
    }

    private static void UpdateInput(float dt)
    {
        if (InputManager.IsMouseButtonDown(MouseButton.Right))
        {
            InputManager.CursorMode = CursorMode.Locked;

            Vector2 delta = InputManager.MouseDelta;
            _yaw -= delta.X * 0.15f;
            _pitch -= delta.Y * 0.15f;
            _pitch = Math.Clamp(_pitch, -89f, 89f);
            _camera.Rotation = new Vector3(_pitch, _yaw, 0);

            Vector2 inputDir = InputMap.GetVector(
                "MoveLeft",
                "MoveRight",
                "MoveBackward",
                "MoveForward"
            );

            Vector3 horizontalFront = Vector3.Normalize(
                new Vector3(_camera.Front.X, 0, _camera.Front.Z)
            );

            _camera.Position += horizontalFront * inputDir.Y * 8.0f * dt;
            _camera.Position += _camera.Right * inputDir.X * 8.0f * dt;

            if (InputManager.IsKeyDown(Key.Space))
            {
                _camera.Position += Vector3.UnitY * 8.0f * dt;
            }
            if (InputManager.IsKeyDown(Key.ShiftLeft))
            {
                _camera.Position -= Vector3.UnitY * 8.0f * dt;
            }
        }
        else
        {
            InputManager.CursorMode = CursorMode.Normal;
        }
    }

    private static void DrawWorld()
    {
        // floor
        KoRender.PushMatrix();
        KoRender.Translate(_floorBody.Position.X, _floorBody.Position.Y, _floorBody.Position.Z);
        KoRender.Scale(25f, 25f, 25f);
        DrawCubePrimitive(_floorMat);
        KoRender.PopMatrix();

        // bridge deck segments
        ReadOnlySpan<RigidBody> bridgeSpan = CollectionsMarshal.AsSpan(_bridgeSegments);
        for (int i = 0; i < bridgeSpan.Length; i++)
        {
            RigidBody bridgeSeg = bridgeSpan[i];
            KoRender.PushMatrix();
            KoRender.Translate(bridgeSeg.Position.X, bridgeSeg.Position.Y, bridgeSeg.Position.Z);
            ApplyBodyOrientation(bridgeSeg.Orientation);
            KoRender.Scale(0.35f, 0.05f, 2.0f);
            DrawCubePrimitive(_bridgeMat);
            KoRender.PopMatrix();
        }

        // cars
        foreach (RigidBody body in _physicsWorld.RigidBodies)
        {
            if (
                body == _floorBody
                || body == _physicsWorld.NullBody
                || _bridgeSegments.Contains(body)
            )
                continue;

            KoRender.PushMatrix();
            KoRender.Translate(body.Position.X, body.Position.Y, body.Position.Z);
            _camera.LookAt(body.Position);
            ApplyBodyOrientation(body.Orientation);

            for (int s = 0; s < body.Shapes.Count; s++)
            {
                Shape shape = body.Shapes[s];

                if (shape is BoxShape box)
                {
                    KoRender.Scale(box.Size.X * 0.5f, box.Size.Y * 0.5f, box.Size.Z * 0.5f);
                    DrawCubePrimitive(_carChassisMat);
                }
                else if (shape is TransformedShape transformed)
                {
                    KoRender.PushMatrix();
                    KoRender.Translate(
                        transformed.Translation.X,
                        transformed.Translation.Y,
                        transformed.Translation.Z
                    );

                    if (transformed.OriginalShape is BoxShape subBox)
                    {
                        KoRender.Scale(
                            subBox.Size.X * 0.5f,
                            subBox.Size.Y * 0.5f,
                            subBox.Size.Z * 0.5f
                        );
                        DrawCubePrimitive(_carChassisMat);
                    }

                    KoRender.PopMatrix();
                }
                else if (shape is CylinderShape cylinder)
                {
                    KoRender.Scale(cylinder.Radius, cylinder.Height * 0.5f, cylinder.Radius);
                    DrawCubePrimitive(_wheelMat);
                }
                else
                {
                    KoRender.Scale(0.5f, 0.5f, 0.5f);
                    DrawCubePrimitive(_carChassisMat);
                }
            }

            KoRender.PopMatrix();
        }
    }

    private static void ApplyBodyOrientation(in JQuaternion jq)
    {
        Quaternion sysQuaternion = new(jq.X, jq.Y, jq.Z, jq.W);
        KoRender.Rotate(sysQuaternion);
    }

    private static void DrawCubePrimitive(Material mat)
    {
        KoRender.ApplyMaterial(mat);
        KoRender.EnableCulling(CullFaceState.Back);
        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 1, 1, 1);

        // Front (+Z)
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, 1);

        // Back (-Z)
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, 1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);

        // Top (+Y)
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, -1);

        // Bottom (-Y)
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, -1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, -1, 1);

        // Right (+X)
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, -1, 1);

        // Left (-X)
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, 1, -1);

        KoRender.End();
    }
}
