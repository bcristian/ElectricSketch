using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// The basis for single-phase consumer devices, e.g. lamps, relay coils.
    /// </summary>
    public abstract class SinglePhaseConsumer<TSim> : Device<TSim>, ISinglePhaseConsumer where TSim : SinglePhaseConsumerSim
    {
        public SinglePhaseConsumer(Func<IDevice, Simulation, ArraySegment<PinSim>, TSim> simCreator, string name = null) : base(name)
        {
            pins.Add(A1 = new Pin(this));
            pins.Add(A2 = new Pin(this));
            SetPinNames();
            this.simCreator = simCreator;
        }

        public Pin A1 { get; }
        public Pin A2 { get; }

        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public Volt? Voltage { get; set; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public Hz? Frequency
        {
            get => frequency;
            set
            {
                if (frequency == value)
                    return;
                frequency = value;
                SetPinNames();
            }
        }
        Hz? frequency;

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public Hz FrequencyTolerance { get; set; } = new Hz(15);

        /// <summary>
        /// If true and this is a DC device (<see cref="Frequency"/> == 0), the potential of pin <see cref="A1"/> must be greater than that of pin <see cref="A2"/>.
        /// </summary>
        public bool PolarityMatters
        {
            get => polarityMatters;
            set
            {
                if (polarityMatters == value)
                    return;
                polarityMatters = value;
                SetPinNames();
            }
        }
        bool polarityMatters = false;


        void SetPinNames()
        {
            if (polarityMatters && frequency.HasValue && frequency.Value.Value == 0)
            {
                Pins[0].Name = "+";
                Pins[1].Name = "-";
            }
            else
            {
                Pins[0].Name = "A1";
                Pins[1].Name = "A2";
            }
        }

        readonly Func<IDevice, Simulation, ArraySegment<PinSim>, TSim> simCreator;
        internal override TSim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => simCreator(this, sim, pins);
    }

    public interface ISinglePhaseConsumer
    {
        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        Volt? Voltage { get; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        float VoltageTolerance { get; }

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        Hz? Frequency { get; }

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        Hz FrequencyTolerance { get; }

        /// <summary>
        /// If true and this is a DC device (<see cref="Frequency"/> == 0), the potential of pin <see cref="A"/> must be greater than that of pin <see cref="B"/>.
        /// </summary>
        bool PolarityMatters { get; }
    }

    public class SinglePhaseConsumerSim : DeviceSimulation
    {
        internal SinglePhaseConsumerSim(ISinglePhaseConsumer parameters, IDevice device, Simulation simulation, IList<PinSim> pins) : base(device, simulation, pins)
        {
            this.parameters = parameters;
            consumer = simulation.AddConsumer(device, 2);
            simulation.Connect(A, consumer.Pins[0]);
            simulation.Connect(B, consumer.Pins[1]);
        }

        internal readonly ISinglePhaseConsumer parameters;
        internal readonly Simulation.Consumer consumer;

        /// <summary>
        /// True if the device has adequate supply power.
        /// </summary>
        public bool IsOn { get; internal set; }

        /// <summary>
        /// Detected supply voltage, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. 220V at some point and 380V at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Volt? DetectedVoltage { get; internal set; }

        /// <summary>
        /// Detected supply frequency, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. AC at some point and DC at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Hz? DetectedFrequency { get; internal set; }


        internal PinSim A => Pins[0];
        internal PinSim B => Pins[1];

        internal override void Update()
        {
            // Making these checks here and not in validate because we have to account for transient conditions too.
            // E.g. something feeds 380V AC into this one, which in turn disconnects that source and connects and self-maintains at 24V from somewhere else.

            // Check that the potentials are compatible.
            var pA = Simulation.GetPotential(A);
            var pB = Simulation.GetPotential(B);
            if (!Validators.VoltageDifference(pA, pB, out Volt? volts))
                throw new SimException(ErrorCode.IncompatiblePotentialsOnDevice, Device);

            if (volts == null)
            {
                IsOn = false;
                return;
            }

            // Check voltage. Zero is always acceptable, but don't set it as the automatically detected value.
            if (!volts.Value.IsZero)
            {
                if (parameters.Voltage.HasValue)
                {
                    // If a nominal voltage has been defined, check that the supply matches.
                    if (!Validators.IsVoltageInRange(volts.Value, parameters.Voltage.Value, parameters.VoltageTolerance, parameters.PolarityMatters))
                        throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
                }
                else
                {
                    // Check that the supply matches the automatically detected one, or store the current value.
                    if (DetectedVoltage.HasValue && !Validators.IsVoltageInRange(volts.Value, DetectedVoltage.Value, parameters.VoltageTolerance, parameters.PolarityMatters))
                        throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
                    DetectedVoltage = volts;
                }
            }

            // Check frequency. Frequency always matters, even if the voltage is zero (but not null).
            if (parameters.Frequency.HasValue)
            {
                // If a nominal frequency has been defined, check that the supply matches.
                if (!Validators.IsFrequencyInRange(pA.Value.Frequency, parameters.Frequency.Value, parameters.FrequencyTolerance))
                    throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
            }
            else
            {
                // Check that the supply matches the automatically detected one, or store the current value.
                if (!DetectedFrequency.HasValue)
                    DetectedFrequency = pA.Value.Frequency;
                else if (!Validators.IsFrequencyInRange(pA.Value.Frequency, DetectedFrequency.Value, parameters.FrequencyTolerance))
                    throw new SimException(ErrorCode.InvalidSupplyVoltage, Device);
            }

            IsOn = !volts.Value.IsZero;
        }

        internal override void Validate() { }
    }
}
