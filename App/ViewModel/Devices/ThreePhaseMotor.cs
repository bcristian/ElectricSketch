using ElectricLib.Devices;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ElectricSketch.ViewModel.Devices
{
    public class ThreePhaseMotor : TypedDevice<Model.Devices.ThreePhaseMotor, ElectricLib.Devices.ThreePhaseMotor, ThreePhaseMotorSim>
    {
        public ThreePhaseMotor(Model.Devices.ThreePhaseMotor m) : base(m)
        {
            Pins[0].Offset = new Point(-20, -30);
            Pins[1].Offset = new Point(0, -30);
            Pins[2].Offset = new Point(20, -30);
            Pins[3].Offset = new Point(-20, 30);
            Pins[4].Offset = new Point(0, 30);
            Pins[5].Offset = new Point(20, 30);
            OriginOffset = new Point(25, 25);

            Configuration = new ThreePhaseMotorConfig(this, "Configuration",
                () => functional.Configuration,
                (v) =>
                {
                    functional.Configuration = v;
                    RaisePropertyChanged(nameof(UsesUVW1));
                    RaisePropertyChanged(nameof(UsesUVW2));
                });
            StarVoltage = new NullableVoltage(this, "Star voltage",
                () => NullableVoltage.Convert(functional.StarVoltage),
                (v) => functional.StarVoltage = NullableVoltage.Convert(v));
            DeltaVoltage = new NullableVoltage(this, "Delta voltage",
                () => NullableVoltage.Convert(functional.DeltaVoltage),
                (v) => functional.DeltaVoltage = NullableVoltage.Convert(v));
            VoltageTolerance = new Percent(this, "Voltage tolerance %",
                () => functional.VoltageTolerance,
                (v) => functional.VoltageTolerance = v);
            MinFrequency = new NullableFrequency(this, "Min frequency",
                () => NullableFrequency.Convert(functional.MinFrequency),
                (v) => functional.MinFrequency = NullableFrequency.Convert(v));
            MaxFrequency = new NullableFrequency(this, "Max frequency",
                () => NullableFrequency.Convert(functional.MaxFrequency),
                (v) => functional.MaxFrequency = NullableFrequency.Convert(v));

            Configuration.Value = m.Configuration;
            StarVoltage.Value = m.StarVoltage;
            DeltaVoltage.Value = m.DeltaVoltage;
            VoltageTolerance.Value = m.VoltageTolerance;
            MinFrequency.Value = m.MinFrequency;
            MaxFrequency.Value = m.MaxFrequency;
        }

        protected override void FillModel(Model.Devices.ThreePhaseMotor m)
        {
            m.Configuration = Configuration.Value;
            m.StarVoltage = StarVoltage.Value;
            m.DeltaVoltage = DeltaVoltage.Value;
            m.VoltageTolerance = VoltageTolerance.Value;
            m.MinFrequency = MinFrequency.Value;
            m.MaxFrequency = MaxFrequency.Value;
        }

        /// <summary>
        /// Motor configuration.
        /// </summary>
        public ThreePhaseMotorConfig Configuration { get; }

        /// <summary>
        /// Does the current configuration use the UVW1 pins.
        /// </summary>
        public bool UsesUVW1 => ElectricLib.Devices.ThreePhaseMotor.ConfigUsesUVW1(Configuration.Value);

        /// <summary>
        /// Does the current configuration use the UVW2 pins.
        /// </summary>
        public bool UsesUVW2 => ElectricLib.Devices.ThreePhaseMotor.ConfigUsesUVW2(Configuration.Value);

        /// <summary>
        /// Nominal supply voltage in star configuration. Null means automatic detection.
        /// </summary>
        public NullableVoltage StarVoltage { get; }

        /// <summary>
        /// Nominal supply voltage in delta configuration. Null means automatic detection.
        /// </summary>
        public NullableVoltage DeltaVoltage { get; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public Percent VoltageTolerance { get; }

        /// <summary>
        /// Minimum supply frequency. Null means any is acceptable.
        /// </summary>
        public NullableFrequency MinFrequency { get; }

        /// <summary>
        /// Maximum supply frequency. Null means any is acceptable.
        /// </summary>
        public NullableFrequency MaxFrequency { get; }


        /// <summary>
        /// Direction the motor is currently turning, or none.
        /// </summary>
        public TurnDirection TurnDirection
        {
            get => turnDirection;
            protected set
            {
                if (turnDirection == value)
                    return;
                turnDirection = value;
                RaisePropertyChanged();
                IsTurning = turnDirection != TurnDirection.None;
            }
        }
        TurnDirection turnDirection;

        public bool IsTurning
        {
            get => isTurning;
            protected set
            {
                if (isTurning == value)
                    return;
                isTurning = value;
                RaisePropertyChanged();
            }
        }
        bool isTurning;

        /// <summary>
        /// If this is a Dahlander motor, it will be set when turning in high-speed mode.
        /// </summary>
        public bool HiSpeed
        {
            get => hiSpeed;
            protected set
            {
                if (hiSpeed == value)
                    return;
                hiSpeed = value;
                RaisePropertyChanged();
            }
        }
        bool hiSpeed;

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
                TurnDirection = TurnDirection.None;
                HiSpeed = false;
            }
        }

        void OnSimulationUpdated()
        {
            TurnDirection = simulation.TurnDirection;
            HiSpeed = simulation.HiSpeed;
            if (!StarVoltage.HasValue)
                StarVoltage.Detected = simulation.DetectedStarVoltage?.Value;
            if (!DeltaVoltage.HasValue)
                DeltaVoltage.Detected = simulation.DetectedDeltaVoltage?.Value;
        }
    }

    public sealed class ThreePhaseMotorConfig : TypedDeviceProperty<ElectricLib.Devices.ThreePhaseMotorConfig>
    {
        public ThreePhaseMotorConfig(Device device, string property, Func<ElectricLib.Devices.ThreePhaseMotorConfig> get, Action<ElectricLib.Devices.ThreePhaseMotorConfig> set) : base(device, property, true, get, set) { }
    }
}
