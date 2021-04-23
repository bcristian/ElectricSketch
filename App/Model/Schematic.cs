using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ElectricSketch.Model
{
    // In this app the model is only used for serialization, the ViewModel does all the work, since there cannot be multiple VM's for the same sketch.

    /// <summary>
    /// The electrical circuit, the root document.
    /// </summary>
    [Serializable]
    public class Schematic
    {
        /// <summary>
        /// Devices in the circuit.
        /// </summary>
        public List<Device> Devices { get; } = new List<Device>();

        /// <summary>
        /// List of pin pairs.
        /// </summary>
        public List<PinInfo[]> Connections { get; } = new List<PinInfo[]>();

        public static Schematic Example()
        {
            var sch = new Schematic();

            sch.Devices.Add(new Devices.Junction() { Name = "J1", Position = new Point(100, 100) });
            sch.Devices.Add(new Devices.Junction() { Name = "J2", Position = new Point(150, 200) });
            sch.Devices.Add(new Devices.Lamp() { Name = "L1", Position = new Point(250, 150) });

            sch.Connections.Add(new PinInfo[] { new PinInfo(0, 0), new PinInfo(2, 0) });
            sch.Connections.Add(new PinInfo[] { new PinInfo(1, 0), new PinInfo(2, 1) });
            sch.Connections.Add(new PinInfo[] { new PinInfo(0, 0), new PinInfo(1, 0) });

            return sch;
        }
    }

    [Serializable]
    public struct PinInfo
    {
        public PinInfo(int dev, int pin)
        {
            DeviceIndex = dev;
            PinIndex = pin;
        }

        [JsonProperty("dev")]
        public int DeviceIndex { get; set; }

        [JsonProperty("pin")]
        public int PinIndex { get; set; }
    }
}
