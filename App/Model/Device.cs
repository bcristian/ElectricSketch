using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ElectricSketch.Model
{
    // We could save the type and pins without needing models for each device type.
    // But we still need them in order to save the device-specific parameters (e.g. voltage, number of poles).

    // Using integer positions is important, or we need to watch for a host of issues arising from rounding and representation errors.
    // E.g. 105 % 10 is 100, but 155 % 10 is 160 using double rounding.

    [Serializable]
    public abstract class Device
    {
        public string Name { get; set; }
        public Point Position { get; set; }

        public List<Pin> Pins { get; set; }
    }

    [Serializable]
    public abstract class TypedDevice<T> : Device where T : ElectricLib.IDevice
    {
    }
}
