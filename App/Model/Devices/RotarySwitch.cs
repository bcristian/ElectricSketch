using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class RotarySwitch : TypedDevice<ElectricLib.Devices.RotarySwitch>
    {
        /// <summary>
        /// Number of switch positions.
        /// </summary>
        public int NumPositions { get; set; } = 2;

        /// <summary>
        /// Number of poles.
        /// </summary>
        public int NumPoles { get; set; } = 1;

        /// <summary>
        /// If true, the switch can be set to any position at any time, not just the previous and next ones.
        /// </summary>
        public bool AllowArbitraryPositionChange { get; set; }

        /// <summary>
        /// Connected position, 0 to <see cref="NumPositions"/> - 1.
        /// </summary>
        public int CurrentPosition { get; set; }

        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public bool AllowIncompatiblePotentials { get; set; } = true;

        /// <summary>
        /// If true, the switch will not stay in the first position.
        /// </summary>
        public bool MomentaryFirstPosition { get; set; }

        /// <summary>
        /// If true, the switch will not stay in the last position.
        /// </summary>
        public bool MomentaryLastPosition { get; set; }
    }
}
