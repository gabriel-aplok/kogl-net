// NOTE: this ray-cast car sample is a copied and slightly modified version
// of the jitter2 car example, which does the same with the JigLib vehicle example.

using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Kogl.Samples.Samples.Car;

/// <summary>
/// Implements a raycast vehicle controller with suspension springs and steering setups.
/// </summary>
public class RayCastCar
{
    private readonly World _world;

    private float _destSteering;
    private float _destAccelerate;
    private float _steering;
    private float _accelerate;

    private const float _dampingFrac = 0.8f;
    private const float _springFrac = 0.45f;

    public RigidBody Body { get; }
    public Wheel[] Wheels { get; } = new Wheel[4];

    public float SteerAngle { get; set; }
    public float DriveTorque { get; set; }
    public float AccelerationRate { get; set; }
    public float SteerRate { get; set; }

    public RayCastCar(World world)
    {
        _world = world;

        // set default values
        AccelerationRate = 10f;
        SteerAngle = (float)JAngle.FromDegree(40.0f);
        DriveTorque = 340.0f;
        SteerRate = 5.0f;

        Body = world.CreateRigidBody();

        // create vehicle box chassis segments
        TransformedShape lowerChassis = new(new BoxShape(3.1f, 1.4f, 8f), new JVector(0, 0.4f, 0));
        TransformedShape upperCabin = new(
            new BoxShape(2.4f, 0.8f, 5f),
            new JVector(0.0f, 1.5f, 1.1f)
        );

        Body.AddShape(lowerChassis);
        Body.AddShape(upperCabin);

        // mass and solid box tensor matrix configs
        float mass = 100.0f;
        JVector sides = new(3.1f, 1.0f, 8.0f);

        float ix = 1.0f / 12.0f * mass * ((sides.Y * sides.Y) + (sides.Z * sides.Z));
        float iy = 1.0f / 12.0f * mass * ((sides.X * sides.X) + (sides.Z * sides.Z));
        float iz = 1.0f / 12.0f * mass * ((sides.X * sides.X) + (sides.Y * sides.Y));

        JMatrix inertia = new(ix, 0, 0, 0, iy, 0, 0, 0, iz);

        Body.Position = new JVector(0, 0.5f, -4f);
        Body.SetMassInertia(inertia, mass);
        Body.Damping = (0.0001f, 0.0001f);

        // setup wheels layout (0: FL, 1: FR, 2: BL, 3: BR)
        Wheels[0] = new Wheel(world, Body, new JVector(-1.3f, -0.1f, -2.5f), 0.60f);
        Wheels[1] = new Wheel(world, Body, new JVector(+1.3f, -0.1f, -2.5f), 0.60f);
        Wheels[2] = new Wheel(world, Body, new JVector(-1.3f, -0.1f, +2.4f), 0.60f);
        Wheels[3] = new Wheel(world, Body, new JVector(+1.3f, -0.1f, +2.4f), 0.60f);

        AdjustWheelValues();
    }

    /// <summary>Recalculates wheel values</summary>
    public void AdjustWheelValues()
    {
        float quarterMass = Body.Mass * 0.25f;
        float wheelMass = Body.Mass * 0.03f;
        float gravityLen = _world.Gravity.Length();

        for (int i = 0; i < Wheels.Length; i++)
        {
            Wheel w = Wheels[i];
            if (w == null)
                continue;

            w.Inertia = 0.5f * (w.Radius * w.Radius) * wheelMass;
            w.Spring = quarterMass * gravityLen / (w.WheelTravel * _springFrac);
            w.Damping = 2.0f * MathF.Sqrt(w.Spring * Body.Mass) * 0.25f * _dampingFrac;
        }
    }

    /// <summary>Sets the vehicle input commands</summary>
    public void SetInput(float accelerateInput, float steerInput)
    {
        _destAccelerate = Math.Clamp(accelerateInput, -1.0f, 1.0f);
        _destSteering = Math.Clamp(steerInput, -1.0f, 1.0f);

        // force-wake car body if active commands are requested to prevent physics freeze
        if (_destAccelerate != 0f || _destSteering != 0f)
        {
            Body.SetActivationState(true);
        }
    }

    /// <summary>Updates the vehicle state</summary>
    public void Step(float timestep)
    {
        if (timestep <= 0.0f)
            return;

        // process wheels ray casting pass
        for (int i = 0; i < Wheels.Length; i++)
        {
            Wheels[i].PreStep(timestep);
        }

        // linear steering and acceleration input relaxation loops
        float deltaAccelerate = timestep * AccelerationRate;
        float deltaSteering = timestep * SteerRate;

        float dAccelerate = Math.Clamp(
            _destAccelerate - _accelerate,
            -deltaAccelerate,
            deltaAccelerate
        );
        float dSteering = Math.Clamp(_destSteering - _steering, -deltaSteering, deltaSteering);

        _accelerate += dAccelerate;
        _steering += dSteering;

        // distribute drivetrain torque and apply slow deceleration resistance
        float maxTorque = DriveTorque * 0.5f;

        for (int i = 0; i < Wheels.Length; i++)
        {
            Wheel w = Wheels[i];
            w.AddTorque(maxTorque * _accelerate);

            if (_destAccelerate == 0.0f && w.AngularVelocity < 0.8f)
            {
                w.AddTorque(-w.AngularVelocity);
            }
        }

        // transform front wheel directional profiles
        float alpha = SteerAngle * _steering;
        Wheels[0].SteerAngle = alpha; // front-Left
        Wheels[1].SteerAngle = alpha; // front-Right

        // advance structural angular rotation ticks
        for (int i = 0; i < Wheels.Length; i++)
        {
            Wheels[i].PostStep(timestep);
        }
    }
}
