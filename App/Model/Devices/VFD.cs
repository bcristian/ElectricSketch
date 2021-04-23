using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class VFD : TypedDevice<ElectricLib.Devices.VFD>
    {
        /// <summary>
        /// Power supply mode.
        /// </summary>
        public ElectricLib.Devices.VFDSupply PowerSupply { get; set; }

        /// <summary>
        /// Nominal supply voltage. Null means automatic detection.
        /// </summary>
        public float? InVoltage { get; set; }

        /// <summary>
        /// Acceptable supply voltage deviation, in percents. E.g. +/- 20% of nominal.
        /// </summary>
        public float VoltageTolerance { get; set; } = 20;

        /// <summary>
        /// Nominal supply frequency. Null means automatic detection. Zero means DC. To define a device that can accept both AC and DC
        /// set this to zero and the frequency tolerance to the maximum allowable AC frequency.
        /// </summary>
        public float? InFrequency { get; set; }

        /// <summary>
        /// Acceptable supply frequency deviation, in Hz. E.g. +/- 15Hz.
        /// </summary>
        public float FrequencyTolerance { get; set; } = 15;

        /// <summary>
        /// Output voltage.
        /// </summary>
        public float OutVoltage { get; set; } = 230;

        /// <summary>
        /// Output frequency.
        /// </summary>
        public float OutFrequency { get; set; } = 400;
    }
}
