using System.Numerics;
// Physics API imports
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Kogl.Common.InputManagement;
using Kogl.Common.Types;
using Kogl.Core;
using Kogl.Core.Rendering;
using Kogl.Core.Resources;
using Kogl.FreeType;
using Kogl.Windowing;

namespace Kogl.Samples.Samples;

internal class JitterPhysicsDropExample
{
    private static readonly AppWindow _app = new(
        1200,
        800,
        "Kolpa - Jitter 2 BoxDrop Physics Simulation"
    );
    private static readonly Camera _camera = new();
    private static Font _uiFont = null!;

    private static Shader _pbrShader = null!;
    private static Material _boxMaterial = null!;
    private static Material _floorMaterial = null!;

    private static Texture _brickAlbedoTex = null!;
    private static Texture _brickNormalTex = null!;
    private static Texture _containerAlbedoTex = null!;

    // Jitter Physics World definitions
    private static World _physicsWorld = null!;
    private static RigidBody _floorBody = null!;
    private const int _numberOfBoxes = 15;

    private static float _yaw = -90f;
    private static float _pitch = 0f;

    public static void Start()
    {
        _app.OnLoad += () =>
        {
            _camera.Position = new Vector3(15.0f, 10.0f, -5.0f);
            _camera.Projection = CameraProjection.Perspective;
            _camera.Fov = 45f;
            _camera.LookAt(new Vector3(0.0f, 4.0f, 0.0f));

            _uiFont = Font.Load("assets/fonts/arial.ttf", 20);

            InputMap.Bind("MoveLeft", Key.A);
            InputMap.Bind("MoveRight", Key.D);
            InputMap.Bind("MoveForward", Key.W);
            InputMap.Bind("MoveBackward", Key.S);

            string vs =
                @"#version 330 core
            layout(location = 0) in vec3 aPos;
            layout(location = 1) in vec2 aTex;
            layout(location = 2) in vec4 aCol;
            layout(location = 3) in vec3 aNormal;
            layout(location = 4) in vec4 aTangent;

            out vec2 fTex;
            out vec4 fCol;
            out vec3 fNormal;
            out vec3 fTangent;
            out vec3 fBitangent;

            uniform mat4 uMVP;

            void main() {
                gl_Position = uMVP * vec4(aPos, 1.0);
                fTex = aTex;
                fCol = aCol;
                mat3 normalMatrix = mat3(uMVP);
                fNormal    = normalize(normalMatrix * aNormal);
                fTangent   = normalize(normalMatrix * aTangent.xyz);
                fBitangent = cross(fNormal, fTangent) * aTangent.w;
            }";

            string fs =
                @"#version 330 core
            in vec2 fTex;
            in vec4 fCol;
            in vec3 fNormal;
            in vec3 fTangent;
            in vec3 fBitangent;

            out vec4 FragColor;

            uniform sampler2D uAlbedoTex;
            uniform sampler2D uNormalTex;
            uniform vec4 uTint;

            void main() {
                vec4 albedo = texture(uAlbedoTex, fTex);
                vec3 normalMap = texture(uNormalTex, fTex).rgb * 2.0 - 1.0;
                mat3 TBN = mat3(fTangent, fBitangent, fNormal);
                vec3 finalNormal = normalize(TBN * normalMap);

                vec3 sunDirection = normalize(vec3(0.4, 1.0, 0.3));
                vec3 sunColor = vec3(1.1, 1.05, 0.95);
                vec3 ambientColor = vec3(0.25, 0.25, 0.28);

                float diffuseFactor = max(dot(finalNormal, sunDirection), 0.0);
                vec3 lighting = ambientColor + (diffuseFactor * sunColor);

                FragColor = albedo * vec4(lighting, 1.0) * fCol * uTint;
            }";

            _pbrShader = Shader.Create(vs, fs);
            _pbrShader.AddProperty("uAlbedoTex", ShaderPropertyType.Texture2D);
            _pbrShader.AddProperty("uNormalTex", ShaderPropertyType.Texture2D);
            _pbrShader.AddProperty("uTint", ShaderPropertyType.Vec4);

            _brickAlbedoTex = AssetManager.Load<Texture>("res://brickwall.jpg");
            _brickNormalTex = AssetManager.Load<Texture>("res://brickwall_normal.jpg");
            _containerAlbedoTex = AssetManager.Load<Texture>("res://container.jpg");

            Material baseMat = new(_pbrShader);
            baseMat.SetTexture("uAlbedoTex", _brickAlbedoTex);
            baseMat.SetTexture("uNormalTex", _brickNormalTex);
            baseMat.SetVector4("uTint", Vector4.One);
            baseMat.DepthTest = true;
            baseMat.Blending = false;

            // floor config
            _floorMaterial = baseMat.CreateInstance();
            _floorMaterial.SetVector4("uTint", new Vector4(0.6f, 0.6f, 0.6f, 1.0f));

            // rigid dynamic entities config
            _boxMaterial = baseMat.CreateInstance();
            _boxMaterial.SetTexture("uAlbedoTex", _containerAlbedoTex);
            _boxMaterial.SetVector4("uTint", new Vector4(0.9f, 0.4f, 0.4f, 1.0f));

            // physics config
            _physicsWorld = new World { SubstepCount = 4, SolveMode = SolveMode.Deterministic };

            // instantiating static physical ground layer floor bounds
            // matching sizing extensions (A 20-unit box sitting at Y = -10 stretches up to Y = 0)
            _floorBody = _physicsWorld.CreateRigidBody();
            _floorBody.AddShape(new BoxShape(20f));
            _floorBody.Position = new JVector(0f, -10f, 0f);
            _floorBody.MotionType = MotionType.Static;

            // distributing interactive stacked dynamic bodies aloft
            for (int i = 0; i < _numberOfBoxes; i++)
            {
                RigidBody body = _physicsWorld.CreateRigidBody();

                // matches standard DrawMaterialCube structural size footprint bounds (extents)
                body.AddShape(new BoxShape(2.0f));

                // slightly offset position stack sequence along height vector line
                body.Position = new JVector(0.0f, (i * 2.5f) + 2.0f, Random(-1f, 1f));
            }
        };

        _app.OnRender += RenderLoop;
        _app.OnResizeEvent += (width, height) =>
        {
            _camera.AspectRatio = height == 0 ? 1f : (float)width / height;
        };
        _app.OnUnload += () =>
        {
            _uiFont?.Dispose();
            AssetManager.UnloadAll();
            ResourceManager.UnloadAll();
        };
        _app.Run();
    }

    private static float Random(float v1, float v2)
    {
        Random random = new();
        return (random.NextSingle() * (v2 - v1)) + v1;
    }

    private static void RenderLoop(double dt)
    {
        UpdateInput((float)dt);

        // TODO: separate the update and fixed update loops
        _physicsWorld.Step((float)dt, true);

        KoRender.Clear(0.15f, 0.22f, 0.32f, 1.0f);
        KoRender.EnableDepthTest();

        KoRender.BeginCamera(_camera);
        DrawWorld();
        KoRender.EndCamera();

        KoRender.DisableDepthTest();
        DrawUI();
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

            _camera.Position += horizontalFront * inputDir.Y * 9.0f * dt;
            _camera.Position += _camera.Right * inputDir.X * 9.0f * dt;

            if (InputManager.IsKeyDown(Key.Space))
                _camera.Position += Vector3.UnitY * 9.0f * dt;
            if (InputManager.IsKeyDown(Key.ShiftLeft))
                _camera.Position -= Vector3.UnitY * 9.0f * dt;
        }
        else
        {
            InputManager.CursorMode = CursorMode.Normal;
        }
    }

    private static void DrawWorld()
    {
        KoRender.PushMatrix();
        KoRender.Translate(_floorBody.Position.X, _floorBody.Position.Y, _floorBody.Position.Z);

        KoRender.Scale(10f, 10f, 10f);
        DrawMaterialCube(_floorMaterial);
        KoRender.PopMatrix();

        foreach (RigidBody body in _physicsWorld.RigidBodies)
        {
            if (body == _floorBody || body == _physicsWorld.NullBody)
                continue;

            KoRender.PushMatrix();

            KoRender.Translate(body.Position.X, body.Position.Y, body.Position.Z);

            JQuaternion jq = body.Orientation;
            Quaternion q = new(jq.X, jq.Y, jq.Z, jq.W);
            KoRender.Rotate(q);

            DrawMaterialCube(_boxMaterial);
            KoRender.PopMatrix();
        }
    }

    private static void DrawMaterialCube(Material mat)
    {
        KoRender.ApplyMaterial(mat);
        KoRender.EnableCulling(CullFaceState.Back);
        KoRender.Begin(PrimitiveMode.Quads);
        KoRender.Color4(1, 1, 1, 1);

        // Front (+Z)
        KoRender.Normal3(0, 0, 1);
        KoRender.Tangent4(1, 0, 0, 1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, -1, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, -1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, 1);

        // Back (-Z)
        KoRender.Normal3(0, 0, -1);
        KoRender.Tangent4(-1, 0, 0, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, 1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);

        // Top (+Y)
        KoRender.Normal3(0, 1, 0);
        KoRender.Tangent4(1, 0, 0, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(-1, 1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(-1, 1, 1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, -1);

        // Bottom (-Y)
        KoRender.Normal3(0, -1, 0);
        KoRender.Tangent4(1, 0, 0, -1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(-1, -1, -1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, -1, 1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(-1, -1, 1);

        // Right (+X)
        KoRender.Normal3(1, 0, 0);
        KoRender.Tangent4(0, 0, 1, 1);
        KoRender.TexCoord2(0, 1);
        KoRender.Vertex3(1, -1, -1);
        KoRender.TexCoord2(1, 1);
        KoRender.Vertex3(1, 1, -1);
        KoRender.TexCoord2(1, 0);
        KoRender.Vertex3(1, 1, 1);
        KoRender.TexCoord2(0, 0);
        KoRender.Vertex3(1, -1, 1);

        // Left (-X)
        KoRender.Normal3(-1, 0, 0);
        KoRender.Tangent4(0, 0, -1, 1);
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

    private static void DrawUI()
    {
        KoRender.EnableBlending();

        KoRender.MatrixMode(MatrixState.Projection);
        KoRender.LoadIdentity();
        KoRender.Ortho(0, _app.Width, _app.Height, 0, -1, 1);
        KoRender.MatrixMode(MatrixState.ModelView);
        KoRender.LoadIdentity();

        string labelInfo =
            $"Physics Stack simulation Active. Loaded Bodies: {_physicsWorld.RigidBodies.Count}";
        KoGLText.DrawText(
            _uiFont,
            labelInfo,
            new Vector2(10, 10),
            new Vector4(0.3f, 0.9f, 0.4f, 1f)
        );
    }
}
