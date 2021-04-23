using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A three phase motor.
    /// </summary>
    public class ThreePhaseMotor : Device<ThreePhaseMotorSim>
    {
        public override string DefaultNamePrefix => "M";

        public ThreePhaseMotor() : this(null) { }
        public ThreePhaseMotor(string name) : base(name)
        {
            pins.Add(U1 = new Pin(this, "U1"));
            pins.Add(V1 = new Pin(this, "V1"));
            pins.Add(W1 = new Pin(this, "W1"));
            pins.Add(U2 = new Pin(this, "U2"));
            pins.Add(V2 = new Pin(this, "V2"));
            pins.Add(W2 = new Pin(this, "W2"));
        }

        public Pin U1 { get; }
        public Pin V1 { get; }
        public Pin W1 { get; }
        public Pin U2 { get; }
        public Pin V2 { get; }
        public Pin W2 { get; }

        /// <summary>
        /// Motor configuration.
        /// </summary>
        public ThreePhaseMotorConfig Configuration { get; set; }

        /// <summary>
        /// Nominal supply voltage in star configuration. Null means automatic detection.
        /// </summary>
        public Volt? StarVoltage { get; set; }

        /// <summary>
        /// Nominal supply voltage in delta configuration. Null means automatic detection.
        /// </summary>
        public Volt? DeltaVoltage { get; set; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Minimum supply frequency. Null means any is acceptable.
        /// </summary>
        public Hz? MinFrequency { get; set; }

        /// <summary>
        /// Maximum supply frequency. Null means any is acceptable.
        /// </summary>
        public Hz? MaxFrequency { get; set; }


        internal override ThreePhaseMotorSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new ThreePhaseMotorSim(this, sim, pins);

        /// <summary>
        /// Does the specified configuration use the UVW1 pins.
        /// </summary>
        public static bool ConfigUsesUVW1(ThreePhaseMotorConfig _) => true;

        /// <summary>
        /// Does the specified configuration use the UVW2 pins.
        /// </summary>
        public static bool ConfigUsesUVW2(ThreePhaseMotorConfig cfg) => cfg == ThreePhaseMotorConfig.StarDelta || cfg == ThreePhaseMotorConfig.Dahlander;
    }

    /// <summary>
    /// Motor configurations.
    /// </summary>
    public enum ThreePhaseMotorConfig
    {
        /// <summary>
        /// A motor with fixed star configuration. Three terminals, U1, V1, W1.
        /// </summary>
        Star,

        /// <summary>
        /// A motor with fixed delta configuration. Three terminals, U1, V1, W1.
        /// </summary>
        Delta,

        /// <summary>
        /// A motor that can be connected star or delta. Six terminals.
        /// </summary>
        StarDelta,

        /// <summary>
        /// A Dahlander two-speed motor. Six terminals.
        /// </summary>
        Dahlander
    }

    public class ThreePhaseMotorSim : DeviceSimulation
    {
        public ThreePhaseMotorSim(ThreePhaseMotor device, Simulation simulation, ArraySegment<PinSim> pins) : base(device, simulation, pins)
        {
            // Because using a single 6-pin consumer would result in series connections with a Dahlander motor and switch (going twice through itself).
            consumer1 = simulation.AddConsumer(device, 3);
            for (int i = 0; i < 3; i++)
                simulation.Connect(Pins[i], consumer1.Pins[i]);
            consumer2 = simulation.AddConsumer(device, 3);
            for (int i = 0; i < 3; i++)
                simulation.Connect(Pins[i + 3], consumer2.Pins[i]);

            validateCurrentType = device.Configuration switch
            {
                ThreePhaseMotorConfig.Star => ValidateStar,
                ThreePhaseMotorConfig.Delta => ValidateDelta,
                ThreePhaseMotorConfig.StarDelta => ValidateStarDelta,
                ThreePhaseMotorConfig.Dahlander => ValidateDahlander,
                _ => throw new NotSupportedException()
            };
        }

        readonly Simulation.Consumer consumer1, consumer2;
        readonly Action<ThreePhaseMotor> validateCurrentType;

        /// <summary>
        /// Direction the motor is currently turning, or none.
        /// </summary>
        public TurnDirection TurnDirection { get; protected set; }

        /// <summary>
        /// If this is a Dahlander motor, it will be set when turning in high-speed mode.
        /// </summary>
        public bool HiSpeed { get; protected set; }


        /// <summary>
        /// Detected supply voltage, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. 220V at some point and 380V at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Volt? DetectedStarVoltage => detectedStarVoltage;
        Volt? detectedStarVoltage;

        /// <summary>
        /// Detected supply voltage, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. 220V at some point and 380V at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Volt? DetectedDeltaVoltage => detectedDeltaVoltage;
        Volt? detectedDeltaVoltage;


        public PinSim U1 => Pins[0];
        public PinSim V1 => Pins[1];
        public PinSim W1 => Pins[2];
        public PinSim U2 => Pins[3];
        public PinSim V2 => Pins[4];
        public PinSim W2 => Pins[5];

        internal override void Update()
        {
            // Making these checks here and not in validate because we have to account for transient conditions too.
            validateCurrentType((ThreePhaseMotor)Device);
        }

        internal override void Validate() { }

        void ValidateSimpleConfig(ThreePhaseMotor motor, Volt? assignedVoltage, ref Volt? detectedVoltage, int firstPin = 0)
        {
            // Get potentials and compute voltages between the U1, V1, and W1 pins.
            var potentials = new Potential?[3];
            for (int i = 0; i < 3; i++)
                potentials[i] = Simulation.GetPotential(Pins[firstPin + i]);

            // Check all pins not connected or at the same potential.
            var allNull = potentials.All(p => !p.HasValue || p == potentials[0]);
            if (allNull)
            {
                TurnDirection = TurnDirection.None;
                return;
            }

            var voltages = new Volt[3];
            for (int i = 0; i < 3; i++)
            {
                if (!Validators.VoltageDifference(potentials[i], potentials[(i + 1) % 3], out Volt? v))
                    throw new SimException(ErrorCode.IncompatiblePotentialsOnDevice, Device);
                if (!v.HasValue)
                    throw new SimException(ErrorCode.InvalidConnections, Device);
                voltages[i] = v.Value;
            }
            if (voltages[0] != voltages[1] || voltages[1] != voltages[2])
                throw new SimException(ErrorCode.InvalidConnections, Device);


            // Check voltage. Zero is always acceptable, but don't set it as the automatically detected value.
            var volts = voltages[0];
            if (!volts.IsZero)
            {
                if (assignedVoltage.HasValue)
                {
                    // If a nominal voltage has been defined, check that the supply matches.
                    if (!Validators.IsVoltageInRange(volts, assignedVoltage.Value, motor.VoltageTolerance, false))
                        throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
                }
                else
                {
                    // Check that the supply matches the automatically detected one, or store the current value.
                    if (detectedVoltage.HasValue && !Validators.IsVoltageInRange(volts, detectedVoltage.Value, motor.VoltageTolerance, false))
                        throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
                    detectedVoltage = volts;
                }

                // Check phases and determine turning direction.
                var phase = new Phase[]
                {
                    potentials[0].Value.Phase,
                    potentials[1].Value.Phase,
                    potentials[2].Value.Phase,
                };
                if (phase[0] == phase[1] && phase[0] == phase[2])
                {
                    // All connected to the same phase.
                    TurnDirection = TurnDirection.None;
                }
                else if (phase[0] == phase[1] || phase[1] == phase[2] || phase[2] == phase[0])
                {
                    // Duplicate phase.
                    throw new SimException(ErrorCode.InvalidConnections, Device);
                }
                else
                {
                    TurnDirection = phase[1] - phase[0] > 0 ? TurnDirection.CW : TurnDirection.CCW;
                }
            }
            else
            {
                TurnDirection = TurnDirection.None;
            }

            // Check frequency.
            var hz = potentials[0].Value.Frequency;
            if ((motor.MinFrequency.HasValue && hz < motor.MinFrequency.Value) ||
                (motor.MaxFrequency.HasValue && hz > motor.MaxFrequency.Value))
                throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
        }

        void ValidateStar(ThreePhaseMotor motor)
        {
            ValidateSimpleConfig(motor, motor.StarVoltage, ref detectedStarVoltage);
        }

        void ValidateDelta(ThreePhaseMotor motor)
        {
            ValidateSimpleConfig(motor, motor.DeltaVoltage, ref detectedDeltaVoltage);
        }

        void ValidateStarDelta(ThreePhaseMotor motor)
        {
            if (Simulation.AreConnected(U2, V2))
            {
                // Star, UVW2 connected.
                if (!Simulation.AreConnected(U2, W2))
                    throw new SimException(ErrorCode.InvalidConnections, Device);

                ValidateSimpleConfig(motor, motor.StarVoltage, ref detectedStarVoltage);

                // The center must be either not connected or connected to the null of the same source.
                var centerP = Simulation.GetPotential(U2);
                if (centerP.HasValue && (!centerP.Value.Voltage.IsZero || !Validators.VoltageDifference(centerP, Simulation.GetPotential(U1), out _)))
                    throw new SimException(ErrorCode.InvalidConnections, Device);
            }
            else if (Simulation.AreConnected(U1, V1))
            {
                // Star, UVW1 connected.
                if (!Simulation.AreConnected(U1, W1))
                    throw new SimException(ErrorCode.InvalidConnections, Device);

                ValidateSimpleConfig(motor, motor.StarVoltage, ref detectedStarVoltage, 3);

                // Flip rotation direction.
                if (TurnDirection == TurnDirection.CW)
                    TurnDirection = TurnDirection.CCW;
                else if (TurnDirection == TurnDirection.CCW)
                    TurnDirection = TurnDirection.CW;

                // The center must be either not connected or connected to the null of the same source.
                var centerP = Simulation.GetPotential(U2);
                if (centerP.HasValue && (!centerP.Value.Voltage.IsZero || !Validators.VoltageDifference(centerP, Simulation.GetPotential(U1), out _)))
                    throw new SimException(ErrorCode.InvalidConnections, Device);
            }
            else
            {
                // Delta or not connected.

                // Check all pins not connected or at the same potential.
                var allSame = true;
                var p = Simulation.GetPotential(Pins[0]);
                for (int i = 1; i < 6 && allSame; i++)
                {
                    var pi = Simulation.GetPotential(Pins[i]);
                    if (pi != p)
                        allSame = false;
                }
                if (allSame)
                {
                    TurnDirection = TurnDirection.None;
                    return;
                }

                // The coils must be connected in the same order: U1V2 V1W2 W1U2 is ok, U1V2 V1W1 W2U2 is not.
                if (Simulation.AreConnected(U1, V1) || Simulation.AreConnected(V1, W1) || Simulation.AreConnected(W1, U1) ||
                    Simulation.AreConnected(U2, V2) || Simulation.AreConnected(V2, W2) || Simulation.AreConnected(W2, U2))
                    throw new SimException(ErrorCode.InvalidConnections, Device);

                if (!Simulation.AreConnected(U1, V2) || !Simulation.AreConnected(V1, W2) || !Simulation.AreConnected(W1, U2))
                    throw new SimException(ErrorCode.InvalidConnections, Device);

                ValidateSimpleConfig(motor, motor.DeltaVoltage, ref detectedDeltaVoltage, 0);
            }
        }

        void ValidateDahlander(ThreePhaseMotor motor)
        {
            // Check all pins not connected or at the same potential.
            var allSame = true;
            var p = Simulation.GetPotential(Pins[0]);
            for (int i = 1; i < 6 && allSame; i++)
            {
                var pi = Simulation.GetPotential(Pins[i]);
                if (pi != p)
                    allSame = false;
            }
            if (allSame)
            {
                TurnDirection = TurnDirection.None;
                return;
            }

            // High speed, double star configuration.
            if (Simulation.AreConnected(U1, V1) && Simulation.AreConnected(V1, W1))
            {
                // The center must be either not connected or connected to the null of the same source.
                var centerP = Simulation.GetPotential(U1);
                if (centerP.HasValue && (!centerP.Value.Voltage.IsZero || !Validators.VoltageDifference(centerP, Simulation.GetPotential(U2), out _)))
                    throw new SimException(ErrorCode.InvalidConnections, Device);

                ValidateSimpleConfig(motor, motor.StarVoltage, ref detectedStarVoltage, 3);

                HiSpeed = true;
            }
            else // Low speed, delta configuration.
            {
                // U2, V2, W2 must not be connected.
                if (Simulation.GetPotential(U2).HasValue || Simulation.GetPotential(V2).HasValue || Simulation.GetPotential(W2).HasValue)
                    throw new SimException(ErrorCode.InvalidConnections, Device);

                ValidateSimpleConfig(motor, motor.DeltaVoltage, ref detectedDeltaVoltage, 0);

                HiSpeed = false;
            }
        }
    }
}
