using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    [Serializable]
    public class ThreePhaseSupply : TypedDevice<ElectricLib.Devices.ThreePhaseSupply>
    {
        public float Voltage { get; set; }
        public float Frequency { get; set; }
    }
}
