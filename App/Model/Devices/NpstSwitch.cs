using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class NpstSwitch : TypedDevice<ElectricLib.Devices.NpstSwitch>
    {
        /// <summary>
        /// Number of poles.
        /// </summary>
        public int NumPoles { get; set; } = 1;

        /// <summary>
        /// Default state, i.e. as drawn in the schematic.
        /// </summary>
        public bool Closed { get; set; }

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
