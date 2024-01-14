using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class Relay : TypedDevice<Model.Devices.Relay, ElectricLib.Devices.Relay, ElectricLib.Devices.RelaySim>
    {
        public Relay(Model.Devices.Relay m) : base(m)
        {
            NumPoles = new NumPoles(this, "Number of poles", () => functional.NumPoles, SetNumPoles); // Setting the value later, it builds related state
            Function = new RelayFunction(this, "Function",
                () => functional.Function,
                (v) =>
                {
                    functional.Function = v;
                    RaisePropertyChanged(nameof(UsesSignal));
                    RaisePropertyChanged(nameof(TimeBased));
                });
            Interval = new Duration(this, "Interval",
                () => functional.Interval,
                (v) => functional.Interval = v);
            Voltage = new NullableVoltage(this, "Voltage",
                () => NullableVoltage.Convert(functional.Voltage),
                (v) => functional.Voltage = NullableVoltage.Convert(v));
            VoltageTolerance = new Percent(this, "Voltage tolerance %",
                () => functional.VoltageTolerance,
                (v) => functional.VoltageTolerance = v);
            Frequency = new NullableFrequency(this, "Frequency",
                () => NullableFrequency.Convert(functional.Frequency),
                (v) => functional.Frequency = NullableFrequency.Convert(v));
            FrequencyTolerance = new Frequency(this, "Frequency +/-",
                () => functional.FrequencyTolerance.Value,
                (v) => functional.FrequencyTolerance = new ElectricLib.Hz(v));
            PolarityMatters = new DesignOnlyBoolean(this, "Polarity matters",
                () => functional.PolarityMatters,
                (v) => functional.PolarityMatters = v);
            AllowIncompatiblePotentials = new DesignOnlyBoolean(this, "Allow incompatible potentials",
                () => functional.AllowIncompatiblePotentials,
                (v) => functional.AllowIncompatiblePotentials = v);

            poles = [];
            Poles = new ReadOnlyObservableCollection<RelayPole>(poles);

            // The coil is down, and the poles are arranged from there.
            // So we need to recompute all the pins offsets when adding or removing poles.

            // We need to force the creation of the associated state, which will not happen if the value happens to be the one the functional was created with.
            SetNumPoles(m.NumPoles);
            Function.Value = m.Function;
            Interval.Value = m.Interval;
            Voltage.Value = m.Voltage;
            VoltageTolerance.Value = m.VoltageTolerance;
            Frequency.Value = m.Frequency;
            FrequencyTolerance.Value = m.FrequencyTolerance;
            PolarityMatters.Value = m.PolarityMatters;
            AllowIncompatiblePotentials.Value = m.AllowIncompatiblePotentials;
        }

        protected override void FillModel(Model.Devices.Relay m)
        {
            m.Function = Function.Value;
            m.Interval = Interval.Value;
            m.Voltage = Voltage.Value;
            m.VoltageTolerance = VoltageTolerance.Value;
            m.Frequency = Frequency.Value;
            m.FrequencyTolerance = FrequencyTolerance.Value;
            m.PolarityMatters = PolarityMatters.Value;
            m.AllowIncompatiblePotentials = AllowIncompatiblePotentials.Value;
            m.NumPoles = NumPoles.Value;
        }

        /// <summary>
        /// The operating mode of the relay.
        /// </summary>
        public RelayFunction Function { get; }

        /// <summary>
        /// Does the current function use the signal pin.
        /// </summary>
        public bool UsesSignal => ElectricLib.Devices.Relay.FunctionUsesSignal(Function.Value);

        /// <summary>
        /// The time interval of the relay.
        /// </summary>
        public Duration Interval { get; }

        /// <summary>
        /// Does the current function depend on time.
        /// </summary>
        public bool TimeBased => ElectricLib.Devices.Relay.FunctionUsesTime(Function.Value);


        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public NullableVoltage Voltage { get; }


        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public Percent VoltageTolerance { get; }


        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public NullableFrequency Frequency { get; }


        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public Frequency FrequencyTolerance { get; }

        /// <summary>
        /// If true and this is a DC device (<see cref="ViewModel.Frequency"/> == 0), the first pin is positive and the second is negative.
        /// </summary>
        public DesignOnlyBoolean PolarityMatters { get; }

        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public DesignOnlyBoolean AllowIncompatiblePotentials { get; }

        /// <summary>
        /// Number of poles.
        /// </summary>
        public NumPoles NumPoles { get; }

        public ReadOnlyObservableCollection<RelayPole> Poles { get; }
        readonly ObservableCollection<RelayPole> poles;


        public int CanvasWidth => 50;
        public int PoleHeight => 40;
        public int CoilHeight => 40;
        public int CanvasHeight => NumPoles.Value * PoleHeight + CoilHeight + PoleHeight / 2;
        public int PinHOffset => 30;

        void SetPinOffsets()
        {
            // Pin offsets are relative to the origin, which we want to be in the center of the first pole.
            var p = 0;
            Pins[p++].Offset = new Point(-PinHOffset, CoilHeight / 2 + PoleHeight / 2);
            Pins[p++].Offset = new Point(PinHOffset, CoilHeight / 2 + PoleHeight / 2);
            Pins[p++].Offset = new Point(0, CoilHeight + PoleHeight);
            for (int i = 0; i < poles.Count; i++)
            {
                Pins[p++].Offset = new Point(-PinHOffset, -PoleHeight * i); // COM
                Pins[p++].Offset = new Point(PinHOffset, -PoleHeight * i - PoleHeight / 4); // NC
                Pins[p++].Offset = new Point(PinHOffset, -PoleHeight * i + PoleHeight / 4); // NO
            }

            OriginOffset = new Point(25, poles.Count * PoleHeight - PoleHeight / 2 + 5);
            RaisePropertyChanged(nameof(OriginOffset));
        }


        void SetNumPoles(int value)
        {
            if (Schematic != null && Schematic.UndoManager.State == Undo.UndoManagerState.Doing)
            {
                // In Undo/Redo mode, these have already been recorded.
                for (int i = value; i < functional.NumPoles; i++)
                {
                    Schematic.RemoveConnections(Pins[functional.CommonPinIndex(i)], true);
                    Schematic.RemoveConnections(Pins[functional.NCPinIndex(i)], true);
                    Schematic.RemoveConnections(Pins[functional.NOPinIndex(i)], true);
                }
            }

            functional.NumPoles = value;
            RaisePropertyChanged(nameof(CanvasHeight));

            if (poles.Count < value)
            {
                for (int i = poles.Count; i < value; i++)
                    poles.Add(new RelayPole(i));
            }
            else
            {
                // Works because the functional removes poles from the end.
                while (poles.Count > value)
                    poles.RemoveAt(poles.Count - 1);
            }

            SetPinOffsets();
        }

        /// <summary>
        /// Is the relay energized.
        /// </summary>
        public bool IsEnergized
        {
            get => isEnergized;
            set
            {
                if (isEnergized == value)
                    return;
                isEnergized = value;
                RaisePropertyChanged();
            }
        }
        bool isEnergized;

        /// <summary>
        /// Is the timer running.
        /// </summary>
        public bool TimerRunning
        {
            get => timerRunning;
            set
            {
                if (timerRunning == value)
                    return;
                timerRunning = value;
                RaisePropertyChanged();
            }
        }
        private bool timerRunning;


        /// <summary>
        /// Time left, as a fraction of the interval.
        /// </summary>
        public double TimeLeftFraction
        {
            get => timeLeftFraction;
            set
            {
                if (timeLeftFraction == value)
                    return;
                timeLeftFraction = value;
                RaisePropertyChanged();
            }
        }
        private double timeLeftFraction;


        /// <summary>
        /// Is the signal applied.
        /// </summary>
        public bool Signaled
        {
            get => signaled;
            set
            {
                if (signaled == value)
                    return;
                signaled = value;
                RaisePropertyChanged();
            }
        }
        private bool signaled;



        protected override void OnSimulationChanged()
        {
            base.OnSimulationChanged();

            if (InSimulation)
            {
                Schematic.Simulation.Updated += OnSimulationUpdated;
                OnSimulationUpdated();
            }
            else
            {
                IsEnergized = false;
                TimerRunning = false;
                TimeLeftFraction = 0;
                Signaled = false;
            }
        }

        void OnSimulationUpdated()
        {
            IsEnergized = simulation.IsOn;
            TimerRunning = simulation.Powered && simulation.TimerEnd > simulation.Simulation.Now;
            TimeLeftFraction = TimerRunning ? (simulation.TimerEnd - simulation.Simulation.Now).TotalSeconds / Interval.Value.TotalSeconds : 0;
            Signaled = simulation.Signaled;
            if (!Voltage.HasValue)
                Voltage.Detected = simulation.DetectedVoltage?.Value;
            if (!Frequency.HasValue)
                Frequency.Detected = simulation.DetectedFrequency?.Value;
        }
    }

    /// <summary>
    /// An abstract device pole. Used so that the view can enumerate them in order to create visuals for each.
    /// </summary>
    public class RelayPole(int index)
    {
        public int Index { get; } = index;
    }

    public sealed class RelayFunction(Device device, string property, Func<ElectricLib.Devices.RelayFunction> get, Action<ElectricLib.Devices.RelayFunction> set) : TypedDeviceProperty<ElectricLib.Devices.RelayFunction>(device, property, true, get, set)
    {
    }
}
