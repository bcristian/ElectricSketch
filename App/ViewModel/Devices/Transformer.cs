using ElectricLib.Devices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class Transformer : TypedDevice<Model.Devices.Transformer, ElectricLib.Devices.Transformer, TransformerSim>
    {
        public Transformer(Model.Devices.Transformer m) : base(m)
        {
            Pins[0].Offset = new Point(-30, -20);
            Pins[1].Offset = new Point(-30, 20);
            Pins[2].Offset = new Point(30, -20);
            Pins[3].Offset = new Point(30, 20);

            OriginOffset = new Point(25, 25);

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
            PolarityMatters = new DesignOnlyBoolean(this, "Polarity matters",
                () => functional.PolarityMatters,
                (v) => functional.PolarityMatters = v);
            OutVoltage = new Voltage(this, "Output voltage",
                () => functional.OutVoltage.Value,
                (v) => functional.OutVoltage = new ElectricLib.Volt(v));
            OutFrequency = new Frequency(this, "Output frequency",
                () => functional.OutFrequency.Value,
                (v) => functional.OutFrequency = new ElectricLib.Hz(v));

            InVoltage.Value = m.InVoltage;
            VoltageTolerance.Value = m.VoltageTolerance;
            InFrequency.Value = m.InFrequency;
            FrequencyTolerance.Value = m.FrequencyTolerance;
            PolarityMatters.Value = m.PolarityMatters;
            OutVoltage.Value = m.OutVoltage;
            OutFrequency.Value = m.OutFrequency;
        }

        protected override void FillModel(Model.Devices.Transformer m)
        {
            m.InVoltage = InVoltage.Value;
            m.VoltageTolerance = VoltageTolerance.Value;
            m.InFrequency = InFrequency.Value;
            m.FrequencyTolerance = FrequencyTolerance.Value;
            m.PolarityMatters = PolarityMatters.Value;
            m.OutVoltage = OutVoltage.Value;
            m.OutFrequency = OutFrequency.Value;
        }


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
        /// If true and this is a DC device (<see cref="Frequency"/> == 0), the first pin is positive and the second is negative.
        /// </summary>
        public DesignOnlyBoolean PolarityMatters { get; }


        /// <summary>
        /// Output voltage.
        /// </summary>
        public Voltage OutVoltage { get; }

        /// <summary>
        /// Output frequency.
        /// </summary>
        public Frequency OutFrequency { get; }


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
                InVoltage.Detected = null;
                InFrequency.Detected = null;
            }
        }

        void OnSimulationUpdated()
        {
            IsEnergized = simulation.IsOn;
            if (!InVoltage.HasValue)
                InVoltage.Detected = simulation.DetectedVoltage?.Value;
            if (!InFrequency.HasValue)
                InFrequency.Detected = simulation.DetectedFrequency?.Value;
        }
    }
}
