using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricLib.Devices
{
    /// <summary>
    /// A relay, or contactor. For simplicity, we model them as having any number of synchronized SPDT switches.
    /// This trivially covers the NO/NC cases, by simply not using the other contact.
    /// </summary>
    public class Relay : Device<RelaySim>, ISinglePhaseConsumer
    {
        public override string DefaultNamePrefix => "RL";

        public Relay() : this(1) { }
        public Relay(int numPoles, string name = null) : base(name)
        {
            pins.Add(new Pin(this, "A1"));
            pins.Add(new Pin(this, "A2"));
            pins.Add(new Pin(this, "B1"));

            NumPoles = numPoles;
        }

        public const int A1Index = 0;
        public const int A2Index = 1;
        public const int B1Index = 2;

        /// <summary>
        /// The coil terminals.
        /// </summary>
        public Pin A1 => Pins[A1Index];
        public Pin A2 => Pins[A2Index];

        /// <summary>
        /// The signal (control) terminal. Used if <see cref="ControlMode"/> is <see cref="RelayControlMode.Signal"/>.
        /// </summary>
        public Pin B1 => Pins[B1Index];

        /// <summary>
        /// The operating mode of the relay.
        /// </summary>
        public RelayFunction Function { get; set; }

        /// <summary>
        /// The time interval of the relay.
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Nominal coil supply voltage. Null means automatic detection.
        /// </summary>
        public Volt? Voltage { get; set; }

        /// <summary>
        /// Acceptable coil supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Nominal coil supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public Hz? Frequency { get; set; }

        /// <summary>
        /// Acceptable coil supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public Hz FrequencyTolerance { get; set; } = new Hz(15);

        /// <summary>
        /// If true and this is a DC device (<see cref="Frequency"/> == 0), the potential of pin <see cref="A1"/> must be greater than that of pin <see cref="A2"/>.
        /// </summary>
        public bool PolarityMatters { get; set; } = true;

        /// <summary>
        /// Central pin for the specified pole.
        /// </summary>
        public Pin Common(int pole) => pins[CommonPinIndex(pole)];

        /// <summary>
        /// Index of the common pin for the specified pole.
        /// </summary>
        public int CommonPinIndex(int pole)
        {
            if (pole < 0 || pole >= NumPoles)
                throw new IndexOutOfRangeException();
            return 3 + pole * 3;
        }

        /// <summary>
        /// NC pin for the specified pole.
        /// </summary>
        public Pin NC(int pole) => pins[NCPinIndex(pole)];

        /// <summary>
        /// Index of the NC pin for the specified pole.
        /// </summary>
        public int NCPinIndex(int pole) => CommonPinIndex(pole) + 1;

        /// <summary>
        /// NO pin for the specified pole.
        /// </summary>
        public Pin NO(int pole) => pins[NOPinIndex(pole)];

        /// <summary>
        /// Index of the NO pin for the specified pole.
        /// </summary>
        public int NOPinIndex(int pole) => CommonPinIndex(pole) + 2;

        /// <summary>
        /// Number of poles.
        /// </summary>
        public int NumPoles
        {
            get => numPoles;
            set
            {
                if (numPoles == value)
                    return;
                if (value < 1)
                    throw new ArgumentException("At least 1 pole must exist for this to be a relay");
                var prevNumPoles = numPoles;
                numPoles = value;

                if (prevNumPoles < numPoles)
                {
                    for (int p = prevNumPoles; p < NumPoles; p++)
                    {
                        pins.Add(new Pin(this, ((p + 1) * 10 + 5).ToString())); // 15, 25, ...
                        pins.Add(new Pin(this, ((p + 1) * 10 + 6).ToString())); // 16, 26, ...
                        pins.Add(new Pin(this, ((p + 1) * 10 + 8).ToString())); // 18, 28, ...
                    }
                }
                else
                {
                    while (pins.Count > NumPoles * 3 + 3)
                        pins.RemoveAt(pins.Count - 1);
                }
            }
        }
        int numPoles;

        /// <summary>
        /// <see cref="ErrorCode.DangerousSwitch"/>
        /// </summary>
        /// <remarks>
        /// There are many ways to use relays with apparent conflicting potentials.
        /// </remarks>
        public bool AllowIncompatiblePotentials { get; set; } = true;


        internal override RelaySim CreateSimulation(Simulation sim, ArraySegment<PinSim> pins) => new RelaySim(this, sim, pins);


        public static bool FunctionUsesSignal(RelayFunction function)
        {
            return function == RelayFunction.Command || function == RelayFunction.OnDelayBySignal || function == RelayFunction.OffDelay
                || function == RelayFunction.SingleShotLeadingEdge || function == RelayFunction.SingleShotTrailingEdge;
        }

        public static bool FunctionUsesTime(RelayFunction function)
        {
            return function != RelayFunction.Simple && function != RelayFunction.Command;
        }
    }

    /// <summary>
    /// The functioning mode of the relay. Signal means bringing B1 at the same potential as A1, while power is applied to A1 and A2.
    /// Removing power immediately puts the relay in the default state.
    /// </summary>
    public enum RelayFunction
    {
        /// <summary>
        /// The relay switches state immediately, according to the coil voltage. The signal pin is not used.
        /// </summary>
        Simple,

        /// <summary>
        /// The relay switches state immediately, based on the signal.
        /// </summary>
        Command,

        /// <summary>
        /// When power is applied, the time delay begins. After the delay, the relay is energized. It remains so until power is removed.
        /// The command signal is not used.
        /// </summary>
        OnDelay,

        /// <summary>
        /// When signaled, the time delay begins. After the delay, the relay is energized. It remains so until the signal is removed.
        /// If the command is applied for less than the time delay, a new interval starts when re-applied.
        /// </summary>
        OnDelayBySignal,

        /// <summary>
        /// When the signal is applied, the relay is energized. Removing the signal starts the time delay. After the delay, the relay returns
        /// to the default state. The interval is reset when the signal is applied.
        /// </summary>
        OffDelay,

        /// <summary>
        /// When the signal is applied, the relay is energized and the timer starts. After the timer expires, the relay returns to the default state.
        /// Changes to the signal state during the interval have no effect. The signal must transition from 0 to 1 for the interval to start, keeping
        /// the signal applied will not keep the relay energized.
        /// </summary>
        SingleShotLeadingEdge,

        /// <summary>
        /// When the signal is removed, the relay is energized and the timer starts. After the timer expires, the relay returns to the default state.
        /// Changes to the signal state during the interval have no effect. The signal must transition from 1 to 0 for the interval to start, keeping
        /// the signal removed will not keep the relay energized.
        /// </summary>
        SingleShotTrailingEdge,

        /// <summary>
        /// On/off alternation, each period equal to the time interval. Starts in the off (default) state. Signal not used.
        /// </summary>
        FlasherPauseFirst,

        /// <summary>
        /// On/off alternation, each period equal to the time interval. Starts in the on (energized) state. Signal not used.
        /// </summary>
        FlasherPulseFirst,
    }



    public class RelaySim : DeviceSimulation, IRotarySwitch
    {
        public RelaySim(Relay dev, Simulation sim, ArraySegment<PinSim> pins) : base(dev, sim, pins)
        {
            numPoles = dev.NumPoles;
            coilSim = new SinglePhaseConsumerSim(dev, dev, sim, pins.Slice(0, 2));
            switchSim = new RotarySwitchSim(this, dev, sim, pins.Slice(3));
        }

        readonly int numPoles;
        readonly SinglePhaseConsumerSim coilSim;
        readonly RotarySwitchSim switchSim;


        /// <summary>
        /// True if the coil is energized.
        /// </summary>
        public bool IsOn { get; protected set; }

        /// <summary>
        /// Detected coil supply voltage, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. 220V at some point and 380V at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Volt? DetectedVoltage => coilSim.DetectedVoltage;

        /// <summary>
        /// Detected coil supply frequency, if the device is set to automatically detect it.
        /// We store it so that we can detect if the device is being powered by e.g. AC at some point and DC at another.
        /// Such devices do exist, but that is not generally the case.
        /// </summary>
        public Hz? DetectedFrequency => coilSim.DetectedFrequency;

        int IRotarySwitch.NumPositions => 2;
        int IRotarySwitch.NumPoles => numPoles;
        int IRotarySwitch.Position => 0;
        bool IRotarySwitch.AllowIncompatiblePotentials => ((Relay)Device).AllowIncompatiblePotentials;
        bool IRotarySwitch.AllowArbitraryPositionChange => false;

        internal override void Update()
        {
            var wasPowered = coilSim.IsOn;
            coilSim.Update();

            var relay = (Relay)Device;

            if (relay.Function == RelayFunction.Simple)
            {
                IsOn = coilSim.IsOn;
            }
            else if (coilSim.IsOn)
            {
                // Even if the relay is in a mode that does not use the signal, it is still illegal to connect it to an incompatible potential.
                var wasSignaled = wasPowered && Signaled;
                Signaled = Simulation.AreConnected(Pins[Relay.A1Index], Pins[Relay.B1Index]);
                if (!Signaled && !Validators.VoltageDifference(Simulation.GetPotential(Pins[Relay.A1Index]), Simulation.GetPotential(Pins[Relay.B1Index]), out Volt? _))
                    throw new SimException(ErrorCode.IncompatiblePotentialsOnDevice, Device);

                switch (relay.Function)
                {
                    case RelayFunction.Command:
                        IsOn = Signaled;
                        break;

                    case RelayFunction.OnDelay:
                        if (!wasPowered)
                        {
                            System.Diagnostics.Debug.Assert(IsOn == false);
                            TimerEnd = Simulation.Now + relay.Interval;
                        }
                        else if (Simulation.Now >= TimerEnd)
                            IsOn = true;
                        else
                            System.Diagnostics.Debug.Assert(IsOn == false);
                        break;

                    case RelayFunction.OnDelayBySignal:
                        if (Signaled)
                            if (!wasSignaled)
                            {
                                TimerEnd = Simulation.Now + relay.Interval;
                                System.Diagnostics.Debug.Assert(IsOn == false);
                            }
                            else if (Simulation.Now >= TimerEnd)
                                IsOn = true;
                            else
                                System.Diagnostics.Debug.Assert(IsOn == false);
                        else
                            IsOn = false;
                        break;

                    case RelayFunction.OffDelay:
                        if (!Signaled && wasSignaled)
                            TimerEnd = Simulation.Now + relay.Interval;
                        IsOn = Signaled || Simulation.Now < TimerEnd;
                        break;

                    case RelayFunction.SingleShotLeadingEdge:
                        if (Signaled && !wasSignaled && !IsOn)
                            TimerEnd = Simulation.Now + relay.Interval;
                        IsOn = Simulation.Now < TimerEnd;
                        break;

                    case RelayFunction.SingleShotTrailingEdge:
                        if (!Signaled && wasSignaled && !IsOn)
                            TimerEnd = Simulation.Now + relay.Interval;
                        IsOn = Simulation.Now < TimerEnd;
                        break;

                    case RelayFunction.FlasherPauseFirst:
                        if (!wasPowered)
                        {
                            TimerEnd = Simulation.Now + relay.Interval;
                            IsOn = false;
                        }
                        else if (Simulation.Now >= TimerEnd)
                        {
                            TimerEnd += relay.Interval; // so that we don't accumulate update intervals
                            IsOn = !IsOn;
                        }
                        break;

                    case RelayFunction.FlasherPulseFirst:
                        if (!wasPowered)
                        {
                            TimerEnd = Simulation.Now + relay.Interval;
                            IsOn = true;
                        }
                        else if (Simulation.Now >= TimerEnd)
                        {
                            TimerEnd += relay.Interval; // so that we don't accumulate update intervals
                            IsOn = !IsOn;
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
            else // no power
            {
                IsOn = false;
                Signaled = false;
            }

            switchSim.Position = IsOn ? 1 : 0;
            switchSim.Update();
        }

        public bool Powered => coilSim.IsOn;
        public bool Signaled { get; private set; }
        public DateTime TimerEnd { get; private set; }

        internal override void Validate()
        {
            coilSim.Validate();
            switchSim.Validate();
        }
    }
}
