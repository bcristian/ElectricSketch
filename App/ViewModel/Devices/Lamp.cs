using ElectricLib.Devices;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Media;

namespace ElectricSketch.ViewModel.Devices
{
    public class Lamp : TypedDevice<Model.Devices.Lamp, ElectricLib.Devices.Lamp, SinglePhaseConsumerSim>
    {
        public Lamp(Model.Devices.Lamp m) : base(m)
        {
            Pins[0].Offset = new Point(-30, 0);
            Pins[1].Offset = new Point(30, 0);
            OriginOffset = new Point(25, 25);

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

            Voltage.Value = m.Voltage;
            VoltageTolerance.Value = m.VoltageTolerance;
            Frequency.Value = m.Frequency;
            FrequencyTolerance.Value = m.FrequencyTolerance;
            PolarityMatters.Value = m.PolarityMatters;
        }

        protected override void FillModel(Model.Devices.Lamp m)
        {
            m.Voltage = Voltage.Value;
            m.VoltageTolerance = VoltageTolerance.Value;
            m.Frequency = Frequency.Value;
            m.FrequencyTolerance = FrequencyTolerance.Value;
            m.PolarityMatters = PolarityMatters.Value;
        }


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
                Voltage.Detected = null;
            }
        }

        void OnSimulationUpdated()
        {
            IsEnergized = simulation.IsOn;
            if (!Voltage.HasValue)
                Voltage.Detected = simulation.DetectedVoltage?.Value;
            if (!Frequency.HasValue)
                Frequency.Detected = simulation.DetectedFrequency?.Value;
        }
    }
}
