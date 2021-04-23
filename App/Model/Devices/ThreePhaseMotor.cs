using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class ThreePhaseMotor : TypedDevice<ElectricLib.Devices.ThreePhaseMotor>
    {
        /// <summary>
        /// Motor configuration.
        /// </summary>
        public ElectricLib.Devices.ThreePhaseMotorConfig Configuration { get; set; }

        /// <summary>
        /// Nominal supply voltage in star configuration. Null means automatic detection.
        /// </summary>
        public float? StarVoltage { get; set; }

        /// <summary>
        /// Nominal supply voltage in delta configuration. Null means automatic detection.
        /// </summary>
        public float? DeltaVoltage { get; set; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Minimum supply frequency. Null means any is acceptable.
        /// </summary>
        public float? MinFrequency { get; set; }

        /// <summary>
        /// Maximum supply frequency. Null means any is acceptable.
        /// </summary>
        public float? MaxFrequency { get; set; }
    }
}
