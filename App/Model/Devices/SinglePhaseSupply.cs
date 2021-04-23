using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model.Devices
{
    // Not using units for the parameters to simplify the serialized data.

    [Serializable]
    public class SinglePhaseSupply : TypedDevice<ElectricLib.Devices.SinglePhaseSupply>
    {
        public float Voltage { get; set; }
        public float Frequency { get; set; }
    }
}
