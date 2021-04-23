using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class SimpleMotor : TypedDevice<ElectricLib.Devices.SimpleMotor>
    {
        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public float? Voltage { get; set; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public float? Frequency { get; set; }

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public float FrequencyTolerance { get; set; } = 15;

        /// <summary>
        /// If true and this is a DC device (<see cref="Frequency"/> == 0), the first pin is the positive and the second is the negative side.
        /// </summary>
        public bool PolarityMatters { get; set; } = false;
    }
}
