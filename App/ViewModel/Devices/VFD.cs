using ElectricLib.Devices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class VFD : TypedDevice<Model.Devices.VFD, ElectricLib.Devices.VFD, VFDSim>
    {
        public VFD(Model.Devices.VFD m) : base(m)
        {
            OriginOffset = new Point(45, 60);

            // in RST
            Pins[(int)VFDPin.R].Offset = new Point(-50, -40);
            Pins[(int)VFDPin.S].Offset = new Point(-50, -20);
            Pins[(int)VFDPin.T].Offset = new Point(-50, 0);

            // out UVW
            Pins[(int)VFDPin.U].Offset = new Point(50, -40);
            Pins[(int)VFDPin.V].Offset = new Point(50, -20);
            Pins[(int)VFDPin.W].Offset = new Point(50, 0);

            // DI com, fwd, rev, stop
            Pins[(int)VFDPin.DICom].Offset = new Point(-50, 40);
            Pins[(int)VFDPin.DIFwd].Offset = new Point(-50, 60);
            Pins[(int)VFDPin.DIRev].Offset = new Point(-50, 80);
            Pins[(int)VFDPin.DIStop].Offset = new Point(-50, 100);

            // Run relay
            Pins[(int)VFDPin.RunCom].Offset = new Point(50, 40);
            Pins[(int)VFDPin.RunNC].Offset = new Point(50, 60);
            Pins[(int)VFDPin.RunNO].Offset = new Point(50, 80);

            // Fault relay
            Pins[(int)VFDPin.FaultCom].Offset = new Point(50, 120);
            Pins[(int)VFDPin.FaultNC].Offset = new Point(50, 140);
            Pins[(int)VFDPin.FaultNO].Offset = new Point(50, 160);

            PowerSupply = new VFDSupply(this, "Supply",
                () => functional.PowerSupply,
                (v) => functional.PowerSupply = v);
            InVoltage = new NullableVoltage(this, "Input voltage",
                () => NullableVoltage.Convert(functional.InVoltage),
                (v) => functional.InVoltage = NullableVoltage.Convert(v));
            VoltageTolerance = new Percent(this, "Voltage tolerance %",
                () => functional.VoltageTolerance,
                (v) => functional.VoltageTolerance = v);
            InFrequency = new NullableFrequency(this, "Input frequency",
                () => NullableFrequency.Convert(functional.InFrequency),
                (v) => functional.InFrequency = NullableFrequency.Convert(v));
            FrequencyTolerance = new Frequency(this, "Frequency +/-",
                () => functional.FrequencyTolerance.Value,
                (v) => functional.FrequencyTolerance = new ElectricLib.Hz(v));
            OutVoltage = new Voltage(this, "Output voltage",
                () => functional.OutVoltage.Value,
                (v) => functional.OutVoltage = new ElectricLib.Volt(v));
            OutFrequency = new Frequency(this, "Output frequency",
                () => functional.OutFrequency.Value,
                (v) => functional.OutFrequency = new ElectricLib.Hz(v));

            PowerSupply.Value = m.PowerSupply;
            InVoltage.Value = m.InVoltage;
            VoltageTolerance.Value = m.VoltageTolerance;
            InFrequency.Value = m.InFrequency;
            FrequencyTolerance.Value = m.FrequencyTolerance;
            OutVoltage.Value = m.OutVoltage;
            OutFrequency.Value = m.OutFrequency;
        }

        protected override void FillModel(Model.Devices.VFD m)
        {
            m.PowerSupply = PowerSupply.Value;
            m.InVoltage = InVoltage.Value;
            m.VoltageTolerance = VoltageTolerance.Value;
            m.InFrequency = InFrequency.Value;
            m.FrequencyTolerance = FrequencyTolerance.Value;
            m.OutVoltage = OutVoltage.Value;
            m.OutFrequency = OutFrequency.Value;
        }


        /// <summary>
        /// Power supply mode.
        /// </summary>
        public VFDSupply PowerSupply { get; }

        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public NullableVoltage InVoltage { get; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public Percent VoltageTolerance { get; }

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public NullableFrequency InFrequency { get; }

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public Frequency FrequencyTolerance { get; }


        /// <summary>
        /// Output voltage.
        /// </summary>
        public Voltage OutVoltage { get; }

        /// <summary>
        /// Output frequency.
        /// </summary>
        public Frequency OutFrequency { get; }


        public bool IsPowered
        {
            get => isPowered;
            protected set
            {
                if (isPowered == value)
                    return;
                isPowered = value;
                RaisePropertyChanged();
            }
        }
        bool isPowered;

        /// <summary>
        /// Output power direction (CW = fwd, CCW = rev).
        /// </summary>
        public TurnDirection Output
        {
            get => output;
            protected set
            {
                if (output == value)
                    return;
                var wasRunning = Running;
                output = value;
                RaisePropertyChanged();
                if (wasRunning != Running)
                    RaisePropertyChanged(nameof(Running));
            }
        }
        TurnDirection output;

        /// <summary>
        /// True when providing power to the output.
        /// </summary>
        public bool Running => Output != TurnDirection.None;

        /// <summary>
        /// True if the device is faulted.
        /// </summary>
        public bool Faulted
        {
            get => faulted;
            set
            {
                if (faulted == value)
                    return;
                faulted = value;
                RaisePropertyChanged();
            }
        }
        private bool faulted;

        /// <summary>
        /// Simulate a fault.
        /// </summary>
        public void Fault()
        {
            simulation.Fault();
        }


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
                IsPowered = false;
                Output = TurnDirection.None;
                Faulted = false;
                InVoltage.Detected = null;
                InFrequency.Detected = null;
            }
        }

        void OnSimulationUpdated()
        {
            IsPowered = simulation.IsPowered;
            Output = simulation.Output;
            Faulted = simulation.Faulted;
            if (!InVoltage.HasValue)
                InVoltage.Detected = simulation.DetectedVoltage?.Value;
            if (!InFrequency.HasValue)
                InFrequency.Detected = simulation.DetectedFrequency?.Value;
        }
    }

    public sealed class VFDSupply(Device device, string property, Func<ElectricLib.Devices.VFDSupply> get, Action<ElectricLib.Devices.VFDSupply> set) : TypedDeviceProperty<ElectricLib.Devices.VFDSupply>(device, property, true, get, set)
    {
    }
}
