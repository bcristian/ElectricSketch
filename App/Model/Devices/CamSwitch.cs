using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class CamSwitch : TypedDevice<ElectricLib.Devices.CamSwitch>
    {
        /// <summary>
        /// Number of switch positions.
        /// </summary>
        public int NumPositions { get; set; } = 2;

        /// <summary>
        /// Number of contacts.
        /// </summary>
        public int NumContacts { get; set; } = 1;

        /// <summary>
        /// The contact pattern.
        /// pattern[contact, position] = is the contact closed in that selector position
        /// </summary>
        public bool[,] Pattern { get; set; }

        /// <summary>
        /// Selector position, 0 to <see cref="NumPositions"/> - 1.
        /// </summary>
        public int SelectorPosition { get; set; }

        /// <summary>
        /// <see cref="ElectricLib.ErrorCode.DangerousSwitch"/>
        /// </summary>
        public bool AllowIncompatiblePotentials { get; set; } = true;
    }
}
