// NOTE: this ray-cast car sample is a copied and slightly modified version
// of the jitter2 car example, which does the same with the JigLib vehicle example.

using System.Runtime.InteropServices;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Kogl.Common.InputManagement;

namespace Kogl.Samples.Samples.Car;

/// <summary>
/// A joint and constraint-based 4-wheel physical vehicle simulation.
/// Uses individual rigid bodies for the suspension dampers and wheel rims.
/// </summary>
public class ConstraintCar
{
    private RigidBody _car = null!;
    private readonly RigidBody?[] _damper = new RigidBody[4];
    private readonly RigidBody?[] _wheels = new RigidBody[4];
    private readonly HingeJoint?[] _sockets = new HingeJoint[4];
    private readonly PrismaticJoint?[] _damperJoints = new PrismaticJoint[4];
    private readonly AngularMotor?[] _steerMotor = new AngularMotor[2];

    private const int _frontLeft = 0;
    private const int _frontRight = 1;
    private const int _backLeft = 2;
    private const int _backRight = 3;

    private const float _maxAngle = 40;
    private float _steer;

    /// <summary>Constructs the full vehicle chassis assembly inside the physical world.</summary>
    public void BuildCar(World world, JVector position, Action<RigidBody>? action = null)
    {
        List<RigidBody> bodies = [_car];

        _car = world.CreateRigidBody();
        _car.MotionType = MotionType.Dynamic;

        bodies[0] = _car;

        TransformedShape tfs1 = new(new BoxShape(1.5f, 0.60f, 3), new JVector(0, -0.3f, 0.0f));
        TransformedShape tfs2 = new(new BoxShape(1.0f, 0.45f, 1.5f), new JVector(0, 0.20f, 0.3f));

        _car.AddShape(tfs1);
        _car.AddShape(tfs2);
        _car.Position = position;
        _car.SetMassInertia(new JMatrix(0.4f, 0, 0, 0, 0.4f, 0, 0, 0, 1.0f), 1.0f);

        for (int i = 0; i < 4; i++)
        {
            _damper[i] = world.CreateRigidBody();
            _damper[i]!.AddShape(new BoxShape(0.2f));
            _damper[i]!.SetMassInertia(0.1f);

            _wheels[i] = world.CreateRigidBody();

            CylinderShape shape = new(0.1f, 0.3f);
            TransformedShape tf = new(
                shape,
                JVector.Zero,
                JMatrix.CreateRotationZ(MathF.PI / 2.0f)
            );

            _wheels[i]!.AddShape(tf);

            bodies.Add(_wheels[i]!);
            bodies.Add(_damper[i]!);
        }

        _car.DeactivationTime = TimeSpan.MaxValue;

        _damper[_frontLeft]!.Position = position + new JVector(-0.75f, -0.6f, -1.1f);
        _damper[_frontRight]!.Position = position + new JVector(+0.75f, -0.6f, -1.1f);
        _damper[_backLeft]!.Position = position + new JVector(-0.75f, -0.6f, 1.1f);
        _damper[_backRight]!.Position = position + new JVector(+0.75f, -0.6f, 1.1f);

        for (int i = 0; i < 4; i++)
        {
            _wheels[i]!.Position = _damper[i]!.Position;
        }

        for (int i = 0; i < 4; i++)
        {
            _damperJoints[i] = new PrismaticJoint(
                world,
                _car,
                _damper[i]!,
                _damper[i]!.Position,
                JVector.UnitY,
                LinearLimit.Fixed,
                false
            );

            _damperJoints[i]!.Slider.LimitBias = 2;
            _damperJoints[i]!.Slider.LimitSoftness = 0.6f;
            _damperJoints[i]!.Slider.Bias = 0.2f;

            _damperJoints[i]!.HingeAngle?.LimitBias = 0.6f;
            _damperJoints[i]!.HingeAngle?.LimitSoftness = 0.01f;
        }

        _damperJoints[_frontLeft]!
            .HingeAngle
            ?.Limit = AngularLimit.FromDegree(-_maxAngle, _maxAngle);
        _damperJoints[_frontRight]!
            .HingeAngle
            ?.Limit = AngularLimit.FromDegree(-_maxAngle, _maxAngle);
        _damperJoints[_backLeft]!.HingeAngle?.Limit = AngularLimit.Fixed;
        _damperJoints[_backRight]!.HingeAngle?.Limit = AngularLimit.Fixed;

        for (int i = 0; i < 4; i++)
        {
            _sockets[i] = new HingeJoint(
                world,
                _damper[i]!,
                _wheels[i]!,
                _wheels[i]!.Position,
                JVector.UnitX,
                true
            );
        }

        if (world.BroadPhaseFilter is not Common.IgnoreCollisionBetweenFilter filter)
        {
            filter = new Common.IgnoreCollisionBetweenFilter();
            world.BroadPhaseFilter = filter;
        }

        for (int i = 0; i < 4; i++)
        {
            filter.IgnoreCollisionBetween(_wheels[i]!.Shapes[0], _damper[i]!.Shapes[0]);

            // multi-shape verification pass over composite body
            for (int s = 0; s < _car.Shapes.Count; s++)
            {
                filter.IgnoreCollisionBetween(_car.Shapes[s], _damper[i]!.Shapes[0]);
                filter.IgnoreCollisionBetween(_car.Shapes[s], _wheels[i]!.Shapes[0]);
            }
        }

        _steerMotor[_frontLeft] = world.CreateConstraint<AngularMotor>(_car, _damper[_frontLeft]!);
        _steerMotor[_frontLeft]!.Initialize(JVector.UnitY);
        _steerMotor[_frontRight] = world.CreateConstraint<AngularMotor>(
            _car,
            _damper[_frontRight]!
        );
        _steerMotor[_frontRight]!.Initialize(JVector.UnitY);

        if (action != null)
        {
            ReadOnlySpan<RigidBody> bodiesSpan = CollectionsMarshal.AsSpan(bodies);
            for (int i = 0; i < bodiesSpan.Length; i++)
            {
                action(bodiesSpan[i]);
            }
        }
    }

    /// <summary>Updates the controls</summary>
    public void UpdateControls()
    {
        float accelerate;

        if (InputMap.IsActionDown("Up"))
            accelerate = 1.0f;
        else if (InputMap.IsActionDown("Down"))
            accelerate = -1.0f;
        else
        {
            accelerate = 0.0f;
        }

        if (InputMap.IsActionDown("Left"))
            _steer += 0.1f;
        else if (InputMap.IsActionDown("Right"))
            _steer -= 0.1f;
        else
            _steer *= 0.9f;

        if (accelerate != 0.0f || _steer != 0.0f)
        {
            _car.SetActivationState(true);
            for (int i = 0; i < _wheels.Length; i++)
            {
                _wheels[i]?.SetActivationState(true);
            }
        }

        _steer = Math.Clamp(_steer, -1.0f, 1.0f);

        float targetAngle = _steer * (_maxAngle / 180.0f) * MathF.PI;

#pragma warning disable CS8629
        float currentAngleL =
            _damperJoints[_frontLeft] != null
                ? (float)(JAngle)(_damperJoints[_frontLeft]!.HingeAngle?.Angle)
                : targetAngle;
        float currentAngleR =
            _damperJoints[_frontRight] != null
                ? (float)(JAngle)(_damperJoints[_frontRight]!.HingeAngle?.Angle)
                : targetAngle;

#pragma warning restore CS8629

        // guards against NaN injection errors originating inside the Jitter hinge math matrices
        if (float.IsNaN(currentAngleL))
            currentAngleL = targetAngle;
        if (float.IsNaN(currentAngleR))
            currentAngleR = targetAngle;

        float diffL = targetAngle - currentAngleL;
        float diffR = targetAngle - currentAngleR;

        if (_steerMotor[_frontLeft] != null)
        {
            _steerMotor[_frontLeft]!.MaximumForce = Math.Max(0.0f, 10.0f * Math.Abs(diffL));
            _steerMotor[_frontLeft]!.TargetVelocity = 10.0f * diffL;
        }

        if (_steerMotor[_frontRight] != null)
        {
            _steerMotor[_frontRight]!.MaximumForce = Math.Max(0.0f, 10.0f * Math.Abs(diffR));
            _steerMotor[_frontRight]!.TargetVelocity = 10.0f * diffR;
        }

        for (int i = 0; i < 4; i++)
        {
            if (_wheels[i] != null)
                _wheels[i]!.Friction = 0.8f;

            if (_sockets[i] != null)
            {
                _sockets[i]!.Motor?.MaximumForce = 1.0f * MathF.Abs(accelerate);
                _sockets[i]!.Motor?.TargetVelocity = -80.0f * accelerate;
            }
        }
    }
}
