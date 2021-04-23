using System;
using System.Collections.Generic;
using System.Text;

namespace ElectricSketch.Model
{
    /// <summary>
    /// A connection point on a device.
    /// </summary>
    [Serializable]
    public class Pin
    {
        public string Name { get; set; }
    }
}
