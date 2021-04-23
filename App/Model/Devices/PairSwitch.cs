using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class PairSwitch : TypedDevice<ElectricLib.Devices.PairSwitch>
    {
        /// <summary>
        /// Number of poles.
        /// </summary>
        public int NumPoles { get; set; } = 1;

        /// <summary>
        /// True = button, false = switch.
        /// </summary>
        public bool Momentary { get; set; }

        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public bool AllowIncompatiblePotentials { get; set; }
    }
}
