// NOTE: this ray-cast car sample is a copied and slightly modified version
// of the jitter2 car example, which does the same with the JigLib vehicle example.

using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Kogl.Samples.Samples.Car;

/// <summary>
/// Handles raycast-based suspension spring forces, steering mechanics,
/// and slip friction handling for a customized rigid body vehicle.
/// </summary>
public class Wheel
{
    private readonly World _world;
    private readonly RigidBody _car;
    private readonly DynamicTree.RayCastFilterPre _rayCastFilter;

    private float _displacement;
    private float _upSpeed;
    private float _lastDisplacement;
    private bool _onFloor;
    private float _driveTorque;
    private float _angVel;
    private float _angVelForGrip; // Used to estimate friction boundaries
    private float _torque;

    // config
    public float SteerAngle { get; set; }
    public float WheelRotation { get; private set; }
    public float Damping { get; set; }
    public float Spring { get; set; }
    public float Inertia { get; set; }
    public float Radius { get; set; }
    public float SideFriction { get; set; }
    public float ForwardFriction { get; set; }
    public float WheelTravel { get; set; }
    public bool Locked { get; set; }
    public float MaximumAngularVelocity { get; set; }
    public int NumberOfRays { get; set; }
    public JVector Position { get; set; }

    public float AngularVelocity => _angVel;
    public readonly JVector Up = JVector.UnitY;

    public Wheel(World world, RigidBody car, JVector position, float radius)
    {
        _world = world;
        _car = car;
        Position = position;
        _rayCastFilter = RayCastCallback;

        // set default values
        SideFriction = 3.0f;
        ForwardFriction = 5.0f;
        Radius = radius;
        Inertia = 1.0f;
        WheelTravel = 0.2f;
        MaximumAngularVelocity = 200f;
        NumberOfRays = 1;
    }

    /// <summary>Gets the center of the wheel</summary>
    public JVector GetWheelCenter()
    {
        return Position + (JVector.Transform(Up, _car.Orientation) * _displacement);
    }

    /// <summary>Adds torque to the wheel</summary>
    public void AddTorque(float torqueAmount)
    {
        _driveTorque += torqueAmount;
    }

    /// <summary>Updates the internal velocity stack</summary>
    public void PostStep(float timeStep)
    {
        if (timeStep <= 0.0f)
            return;

        float origAngVel = _angVel;
        _upSpeed = (_displacement - _lastDisplacement) / timeStep;

        if (Locked)
        {
            _angVel = 0f;
            _torque = 0f;
            return;
        }

        _angVel += _torque * timeStep / Inertia;
        _torque = 0f;

        if (!_onFloor)
        {
            _driveTorque *= 0.1f;
        }

        // prevent friction calculation from erroneously flipping directions
        if (
            (origAngVel > _angVelForGrip && _angVel < _angVelForGrip)
            || (origAngVel < _angVelForGrip && _angVel > _angVelForGrip)
        )
        {
            _angVel = _angVelForGrip;
        }

        _angVel += _driveTorque * timeStep / Inertia;
        _driveTorque = 0f;

        _angVel = Math.Clamp(_angVel, -MaximumAngularVelocity, MaximumAngularVelocity);
        WheelRotation += timeStep * _angVel;
    }

    public void PreStep(float timeStep)
    {
        JVector force = JVector.Zero;
        _lastDisplacement = _displacement;
        _displacement = 0.0f;

        // cache shared structural transforms
        JMatrix carOrient = JMatrix.CreateFromQuaternion(_car.Orientation);
        JVector worldPos = _car.Position + JVector.Transform(Position, carOrient);
        JVector worldAxis = JVector.Transform(Up, carOrient);

        JVector forward = JVector.Transform(-JVector.UnitZ, carOrient);
        JVector wheelFwd = JVector.Transform(
            forward,
            JMatrix.CreateRotationMatrix(worldAxis, SteerAngle)
        );

        JVector wheelLeft = JVector.Cross(worldAxis, wheelFwd);
        JVector.NormalizeInPlace(ref wheelLeft);
        JVector wheelUp = JVector.Cross(wheelFwd, wheelLeft);

        float rayLen = (2.0f * Radius) + WheelTravel;
        JVector wheelRayEnd = worldPos - (Radius * worldAxis);
        JVector wheelRayOrigin = wheelRayEnd + (rayLen * worldAxis);
        JVector wheelRayDelta = wheelRayEnd - wheelRayOrigin;

        float deltaFwd = 2.0f * Radius / (NumberOfRays + 1);
        _onFloor = false;

        JVector groundNormal = JVector.Zero;
        JVector groundPos = JVector.Zero;
        float deepestFrac = float.MaxValue;
        RigidBody? worldBody = null;

        // multi-ray casting assessment block
        for (int i = 0; i < NumberOfRays; i++)
        {
            float distFwd = (deltaFwd * (i + 1)) - Radius;
            float zOffset = Radius * (1.0f - MathF.Cos(MathF.PI * 0.5f * (distFwd / Radius)));

            JVector newOrigin = wheelRayOrigin + (distFwd * wheelFwd) + (zOffset * wheelUp);

            bool hit = _world.DynamicTree.RayCast(
                newOrigin,
                wheelRayDelta,
                _rayCastFilter,
                null,
                out IDynamicTreeProxy? shape,
                out JVector normal,
                out float frac
            );

            if (hit && frac <= 1.0f && frac < deepestFrac)
            {
                deepestFrac = frac;
                groundPos = newOrigin + (frac * wheelRayDelta);
                worldBody = (shape as RigidBodyShape)!.RigidBody;
                groundNormal = normal;
                _onFloor = true;
            }
        }

        if (!_onFloor || worldBody == null)
            return;

        if (groundNormal.LengthSquared() > 0.0f)
        {
            JVector.NormalizeInPlace(ref groundNormal);
        }

        _displacement = Math.Clamp(rayLen * (1.0f - deepestFrac), 0.0f, WheelTravel);

        // suspension calculation stack
        float displacementForceMag = _displacement * Spring * JVector.Dot(groundNormal, worldAxis);
        float dampingForceMag = _upSpeed * Damping;
        float totalForceMag = Math.Max(0.0f, displacementForceMag + dampingForceMag);

        force += totalForceMag * worldAxis;

        // coordinate frame calculation for ground tracking
        JVector groundUp = groundNormal;
        JVector groundLeft = JVector.Cross(groundNormal, wheelFwd);
        if (groundLeft.LengthSquared() > 0.0f)
        {
            JVector.NormalizeInPlace(ref groundLeft);
        }
        JVector groundFwd = JVector.Cross(groundLeft, groundUp);

        // compute relative ground contact velocities
        JVector wheelPointVel =
            _car.Velocity
            + JVector.Cross(_car.AngularVelocity, JVector.Transform(Position, carOrient));
        wheelPointVel += _angVel * JVector.Cross(wheelLeft, groundPos - worldPos);

        JVector worldVel =
            worldBody.Velocity
            + JVector.Cross(worldBody.AngularVelocity, groundPos - worldBody.Position);
        wheelPointVel -= worldVel;

        // sideways slipping force evaluation
        float sideVel = JVector.Dot(wheelPointVel, groundLeft);
        float sideFrictionFactor = CalculateFrictionFactor(sideVel, SideFriction);
        force += sideFrictionFactor * totalForceMag * groundLeft;

        // forward/backward driving force evaluation
        float fwdVel = JVector.Dot(wheelPointVel, groundFwd);
        float fwdFrictionFactor = CalculateFrictionFactor(fwdVel, ForwardFriction);
        float fwdForce = fwdFrictionFactor * totalForceMag;
        force += fwdForce * groundFwd;

        // track velocity metrics back to drivetrain variables
        JVector wheelCentreVel =
            _car.Velocity
            + JVector.Cross(_car.AngularVelocity, JVector.Transform(Position, carOrient));
        _angVelForGrip = JVector.Dot(wheelCentreVel, groundFwd) / Radius;
        _torque += -fwdForce * Radius;

        // commit driving outputs back to car rigid body frame
        _car.AddForce(force, groundPos);

        // commit reciprocal load back to standard dynamic obstacles
        if (worldBody.MotionType == MotionType.Dynamic)
        {
            const float maxOtherBodyAcc = 500.0f;
            float maxOtherBodyForce = maxOtherBodyAcc * worldBody.Mass;

            JVector reactiveForce = force * -1f;
            if (reactiveForce.LengthSquared() > (maxOtherBodyForce * maxOtherBodyForce))
            {
                reactiveForce *= maxOtherBodyForce / reactiveForce.Length();
            }

            worldBody.SetActivationState(true);
            worldBody.AddForce(reactiveForce, groundPos);
        }
    }

    /// <summary>Reduce slip-ratio and velocity friction evaluation structures</summary>
    private static float CalculateFrictionFactor(float currentVelocity, float baseFriction)
    {
        const float noslipVel = 0.2f;
        const float slipVel = 0.4f;
        const float slipFactor = 0.7f;
        const float smallVel = 3.0f;

        float friction = baseFriction;
        float absVel = Math.Abs(currentVelocity);

        if (absVel > slipVel)
        {
            friction *= slipFactor;
        }
        else if (absVel > noslipVel)
        {
            friction *= 1.0f - ((1.0f - slipFactor) * (absVel - noslipVel) / (slipVel - noslipVel));
        }

        if (currentVelocity < 0.0f)
            friction *= -1.0f;
        if (absVel < smallVel)
            friction *= absVel / smallVel;

        return -friction;
    }

    /// <summary>Callback for ray-cast collision detection</summary>
    private bool RayCastCallback(IDynamicTreeProxy shape)
    {
        return shape is RigidBodyShape rbs && rbs.RigidBody != _car;
    }
}
